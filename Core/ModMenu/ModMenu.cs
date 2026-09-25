using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using RDR2;
using RDR2.Native;
using Screen = RDR2.UI.Screen;

namespace AIPlayground
{
    // F9 opens the menu. The main page lists on/off mods (Enter toggles) and ends with "Actions >". The Actions page
    // lists category folders (Needs, Crime...) and any actions without a category directly (like Body Type), so it's
    // never more than one folder deep. Enter uses a button or opens a folder, Left/Right change a scrolling setting,
    // Backspace goes back a level, F9 closes. A few seconds after loading it announces what loaded, as a smoke test.
    public class ModMenu : Script
    {
        private const Keys MenuKey = Keys.F9;

        // Wait for every mod DLL to finish loading and registering before announcing.
        private const int AnnounceDelayMs = 3000;

        // Rows on the Actions page in this order first (folders and inline actions by name); others follow alphabetically.
        private static readonly string[] ActionsOrder = { "Needs", "Body Type", "Crime" };

        // Layout, in fractions of the screen.
        private const float Left = 0.05f;
        private const float Top = 0.15f;
        private const float Width = 0.3f;
        private const float RowHeight = 0.035f;
        private const float SmallRowHeight = 0.026f;
        private const float TextInset = 0.008f;
        private const float TextScale = 0.4f;
        private const float SmallTextScale = 0.3f;

        private const string ActionsRowName = "Actions";
        private const string ActionsRowDescription = "One-press buttons and settings.";

        private enum Page { Mods, Actions, Folder }

        // A row on the Actions page: either a category folder or an action/setting shown directly.
        private sealed class ActionsRow
        {
            public string Folder;
            public ModInfo Item;

            public string Name => Folder ?? Item.Name;
        }

        private bool open;
        private Page page = Page.Mods;
        private readonly Dictionary<Page, int> selected = new Dictionary<Page, int> { [Page.Mods] = 0, [Page.Actions] = 0, [Page.Folder] = 0 };
        private string openFolder;

        private List<ModInfo> toggles = new List<ModInfo>();
        private List<ActionsRow> actionsRows = new List<ActionsRow>();
        private List<ModInfo> folderItems = new List<ModInfo>();

        private readonly int announceAt;
        private bool announced;

        public ModMenu()
        {
            announceAt = Environment.TickCount + AnnounceDelayMs;
            Tick += OnTick;
            KeyDown += OnKeyDown;
            Aborted += (sender, e) => ScreenText.FreeAll();
            Log.Write("Mod menu loaded, F9 to open");
        }

        // The mods page has one extra row at the bottom that opens Actions, when there are any.
        private int RowCount =>
            page == Page.Mods ? toggles.Count + (actionsRows.Count > 0 ? 1 : 0) :
            page == Page.Actions ? actionsRows.Count : folderItems.Count;

        private int Selected
        {
            get => selected[page];
            set => selected[page] = value;
        }

        private bool ActionsRowSelected => page == Page.Mods && actionsRows.Count > 0 && Selected == toggles.Count;

        private ActionsRow SelectedActionsRow => page == Page.Actions && Selected < actionsRows.Count ? actionsRows[Selected] : null;

        // The button or setting under the cursor, on the Actions page or in a folder.
        private ModInfo SelectedItem =>
            page == Page.Folder ? (Selected < folderItems.Count ? folderItems[Selected] : null) :
            SelectedActionsRow?.Item;

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == MenuKey)
            {
                open = !open;
                page = Page.Mods;
                return;
            }

            if (!open)
            {
                return;
            }

            switch (e.KeyCode)
            {
                case Keys.Up:
                    Move(-1);
                    break;
                case Keys.Down:
                    Move(1);
                    break;
                case Keys.Left:
                    StepChoice(-1);
                    break;
                case Keys.Right:
                    StepChoice(1);
                    break;
                case Keys.Enter:
                    Select();
                    break;
                case Keys.Back:
                    Back();
                    break;
            }
        }

        private void Move(int step)
        {
            int rows = RowCount;
            if (rows > 0)
            {
                Selected = (Selected + step + rows) % rows;
            }
        }

        private void StepChoice(int step)
        {
            ModInfo item = SelectedItem;
            if (item != null && item.IsChoice)
            {
                item.Step(step);
            }
        }

        private void Select()
        {
            if (page == Page.Mods)
            {
                if (ActionsRowSelected)
                {
                    page = Page.Actions;
                }
                else if (Selected < toggles.Count)
                {
                    toggles[Selected].Activate();
                }
                return;
            }

            ActionsRow row = SelectedActionsRow;
            if (row != null && row.Folder != null)
            {
                openFolder = row.Folder;
                page = Page.Folder;
                selected[Page.Folder] = 0;
                return;
            }

            ModInfo item = SelectedItem;
            if (item == null)
            {
                return;
            }
            if (item.IsChoice)
            {
                item.Step(1);
            }
            else
            {
                item.Activate();
            }
        }

        private void Back()
        {
            if (page == Page.Folder)
            {
                page = Page.Actions;
            }
            else if (page == Page.Actions)
            {
                page = Page.Mods;
            }
            else
            {
                open = false;
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!announced && Environment.TickCount > announceAt)
            {
                announced = true;
                List<ModInfo> all = ModRegistry.GetAll();
                int mods = all.Count(m => m.IsToggle);
                int on = all.Count(m => m.IsEnabled);
                Screen.DisplaySubtitle($"RDR2 Mod Menu: {mods} mods loaded ({on} on), {all.Count - mods} actions. F9 for menu.");
                Log.Write($"Loaded: {string.Join(", ", all.Select(m => $"{m.Name} ({(m.IsToggle ? (m.IsEnabled ? "on" : "off") : m.Category ?? "Actions")})"))}");
            }

            if (!open)
            {
                return;
            }

            Refresh();

            // The menu is modal: keep arrow keys, Enter and Backspace from also acting in the game.
            Game.DisableAllControlsThisFrame();
            switch (page)
            {
                case Page.Mods:
                    DrawModsPage();
                    break;
                case Page.Actions:
                    DrawActionsPage();
                    break;
                case Page.Folder:
                    DrawFolderPage();
                    break;
            }
        }

        private void Refresh()
        {
            List<ModInfo> all = ModRegistry.GetAll();
            toggles = all.Where(m => m.IsToggle).ToList();

            List<ModInfo> actions = all.Where(m => !m.IsToggle).ToList();
            actionsRows = actions.Where(m => m.Category != null).Select(m => m.Category).Distinct()
                .Select(folder => new ActionsRow { Folder = folder })
                .Concat(actions.Where(m => m.Category == null).Select(m => new ActionsRow { Item = m }))
                .OrderBy(r => Array.IndexOf(ActionsOrder, r.Name) is int i && i >= 0 ? i : ActionsOrder.Length)
                .ThenBy(r => r.Name)
                .ToList();
            folderItems = actions.Where(m => m.Category != null && m.Category == openFolder).ToList();

            if (page == Page.Folder && folderItems.Count == 0)
            {
                page = Page.Actions;
            }
            if (page == Page.Actions && actionsRows.Count == 0)
            {
                page = Page.Mods;
            }
            Selected = Math.Min(Selected, Math.Max(0, RowCount - 1));
        }

        private void DrawModsPage()
        {
            float y = DrawTitle("RDR2 Mod Menu");

            if (RowCount == 0)
            {
                DrawRow(y, 0, 0, 0, 190);
                DrawText("No mods loaded", Left + TextInset, y, TextScale, 180, 180, 180);
                y += RowHeight;
            }

            for (int i = 0; i < toggles.Count; i++)
            {
                ModInfo mod = toggles[i];
                DrawItemRow(y, i == Selected, mod.Name);
                if (mod.IsEnabled)
                {
                    DrawText("ON", Left + Width - 0.04f, y, TextScale, 90, 220, 90);
                }
                else
                {
                    DrawText("OFF", Left + Width - 0.04f, y, TextScale, 170, 170, 170);
                }
                y += RowHeight;
            }

            if (actionsRows.Count > 0)
            {
                DrawItemRow(y, ActionsRowSelected, ActionsRowName);
                DrawFolderMark(y);
                y += RowHeight;
            }

            string description = ActionsRowSelected ? ActionsRowDescription
                : Selected < toggles.Count ? toggles[Selected].Description : null;
            y = DrawDescription(y, description);
            DrawFooter(y, "Up/Down select    Enter toggle/open    F9 close");
        }

        private void DrawActionsPage()
        {
            float y = DrawTitle("RDR2 Mod Menu  >  Actions");

            for (int i = 0; i < actionsRows.Count; i++)
            {
                ActionsRow row = actionsRows[i];
                DrawItemRow(y, i == Selected, row.Name);
                if (row.Folder != null)
                {
                    DrawFolderMark(y);
                }
                else
                {
                    DrawItemValue(y, row.Item);
                }
                y += RowHeight;
            }

            ModInfo item = SelectedItem;
            y = DrawDescription(y, item?.Description);
            DrawFooter(y, FooterFor(item, "Enter open"));
        }

        private void DrawFolderPage()
        {
            float y = DrawTitle($"RDR2 Mod Menu  >  Actions  >  {openFolder}");

            for (int i = 0; i < folderItems.Count; i++)
            {
                DrawItemRow(y, i == Selected, folderItems[i].Name);
                DrawItemValue(y, folderItems[i]);
                y += RowHeight;
            }

            ModInfo item = SelectedItem;
            y = DrawDescription(y, item?.Description);
            DrawFooter(y, FooterFor(item, "Enter use"));
        }

        private static string FooterFor(ModInfo item, string enterHint)
        {
            if (item != null && item.IsChoice)
            {
                return "Left/Right change    Backspace back    F9 close";
            }
            return $"Up/Down select    {(item != null ? "Enter use" : enterHint)}    Backspace back    F9 close";
        }

        // "USE" for a button, "<  Medium  >" for a scrolling setting.
        private static void DrawItemValue(float y, ModInfo item)
        {
            if (item.IsChoice)
            {
                string[] choices = item.Choices;
                int current = item.CurrentChoice;
                string value = current >= 0 && current < choices.Length ? choices[current] : "";
                DrawText($"<  {value}  >", Left + Width - 0.1f, y, TextScale, 230, 190, 90);
            }
            else
            {
                DrawText("USE", Left + Width - 0.04f, y, TextScale, 230, 190, 90);
            }
        }

        private static void DrawFolderMark(float y)
        {
            DrawText(">", Left + Width - 0.025f, y, TextScale, 230, 190, 90);
        }

        private float DrawTitle(string title)
        {
            DrawRow(Top, 150, 20, 20, 230);
            DrawText(title, Left + TextInset, Top, TextScale, 255, 255, 255);
            return Top + RowHeight;
        }

        private static void DrawItemRow(float y, bool selected, string name)
        {
            if (selected)
            {
                DrawRow(y, 90, 90, 90, 220);
            }
            else
            {
                DrawRow(y, 0, 0, 0, 190);
            }
            DrawText(name, Left + TextInset, y, TextScale, 255, 255, 255);
        }

        private static float DrawDescription(float y, string description)
        {
            if (description == null)
            {
                return y;
            }
            foreach (string line in ScreenText.Wrap(description, Width - 2 * TextInset, SmallTextScale))
            {
                DrawRow(y, 0, 0, 0, 190, SmallRowHeight);
                DrawText(line, Left + TextInset, y, SmallTextScale, 220, 220, 220);
                y += SmallRowHeight;
            }
            return y;
        }

        private static void DrawFooter(float y, string text)
        {
            DrawRow(y, 0, 0, 0, 190, SmallRowHeight + 0.006f);
            DrawText(text, Left + TextInset, y + 0.004f, SmallTextScale, 180, 180, 180);
        }

        private static void DrawRow(float top, int r, int g, int b, int a, float height = RowHeight)
        {
            GRAPHICS.DRAW_RECT(Left + Width / 2f, top + height / 2f, Width, height, r, g, b, a, false, false);
        }

        private static void DrawText(string text, float x, float top, float scale, int r, int g, int b)
        {
            ScreenText.Draw(text, x, top + 0.004f, scale, r, g, b);
        }
    }
}
