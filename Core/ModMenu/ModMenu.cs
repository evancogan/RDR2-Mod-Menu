using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using RDR2;
using RDR2.Native;
using Screen = RDR2.UI.Screen;

namespace AIPlayground
{
    // F9 opens the menu. The main page lists on/off mods (Enter toggles) and ends with "Actions >", which opens a list
    // of categories (Needs, Body Type, Crime...). A category lists its buttons (Enter uses one) and scrolling choices
    // (Left/Right). Backspace goes back a level, F9 closes. A few seconds after loading it announces what loaded,
    // as a smoke test.
    public class ModMenu : Script
    {
        private const Keys MenuKey = Keys.F9;

        // Wait for every mod DLL to finish loading and registering before announcing.
        private const int AnnounceDelayMs = 3000;

        // Categories in this order first; any others follow alphabetically.
        private static readonly string[] CategoryOrder = { "Needs", "Body Type", "Crime" };

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
        private const string ActionsRowDescription = "One-press buttons and settings, by category.";

        private enum Page { Mods, Categories, Category }

        private bool open;
        private Page page = Page.Mods;
        private readonly Dictionary<Page, int> selected = new Dictionary<Page, int> { [Page.Mods] = 0, [Page.Categories] = 0, [Page.Category] = 0 };
        private string openCategory;

        private List<ModInfo> toggles = new List<ModInfo>();
        private List<string> categories = new List<string>();
        private List<ModInfo> categoryItems = new List<ModInfo>();

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
            page == Page.Mods ? toggles.Count + (categories.Count > 0 ? 1 : 0) :
            page == Page.Categories ? categories.Count : categoryItems.Count;

        private int Selected
        {
            get => selected[page];
            set => selected[page] = value;
        }

        private bool ActionsRowSelected => page == Page.Mods && categories.Count > 0 && Selected == toggles.Count;

        private ModInfo SelectedItem => page == Page.Category && Selected < categoryItems.Count ? categoryItems[Selected] : null;

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
                    SelectedItem?.Step(-1);
                    break;
                case Keys.Right:
                    SelectedItem?.Step(1);
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

        private void Select()
        {
            switch (page)
            {
                case Page.Mods:
                    if (ActionsRowSelected)
                    {
                        page = Page.Categories;
                    }
                    else if (Selected < toggles.Count)
                    {
                        toggles[Selected].Activate();
                    }
                    break;
                case Page.Categories:
                    if (Selected < categories.Count)
                    {
                        openCategory = categories[Selected];
                        page = Page.Category;
                        selected[Page.Category] = 0;
                    }
                    break;
                case Page.Category:
                    ModInfo item = SelectedItem;
                    if (item == null)
                    {
                        break;
                    }
                    if (item.IsChoice)
                    {
                        item.Step(1);
                    }
                    else
                    {
                        item.Activate();
                    }
                    break;
            }
        }

        private void Back()
        {
            if (page == Page.Category)
            {
                page = Page.Categories;
            }
            else if (page == Page.Categories)
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
                Log.Write($"Loaded: {string.Join(", ", all.Select(m => $"{m.Name} ({(m.IsToggle ? (m.IsEnabled ? "on" : "off") : m.Category)})"))}");
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
                case Page.Categories:
                    DrawCategoriesPage();
                    break;
                case Page.Category:
                    DrawCategoryPage();
                    break;
            }
        }

        private void Refresh()
        {
            List<ModInfo> all = ModRegistry.GetAll();
            toggles = all.Where(m => m.IsToggle).ToList();
            categories = all.Where(m => !m.IsToggle && m.Category != null).Select(m => m.Category).Distinct()
                .OrderBy(c => Array.IndexOf(CategoryOrder, c) is int i && i >= 0 ? i : CategoryOrder.Length)
                .ThenBy(c => c).ToList();
            categoryItems = all.Where(m => !m.IsToggle && m.Category == openCategory).ToList();

            if (page == Page.Category && categoryItems.Count == 0)
            {
                page = Page.Categories;
            }
            if (page == Page.Categories && categories.Count == 0)
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

            if (categories.Count > 0)
            {
                DrawItemRow(y, ActionsRowSelected, ActionsRowName);
                DrawText(">", Left + Width - 0.025f, y, TextScale, 230, 190, 90);
                y += RowHeight;
            }

            string description = ActionsRowSelected ? ActionsRowDescription
                : Selected < toggles.Count ? toggles[Selected].Description : null;
            y = DrawDescription(y, description);
            DrawFooter(y, "Up/Down select    Enter toggle/open    F9 close");
        }

        private void DrawCategoriesPage()
        {
            float y = DrawTitle("RDR2 Mod Menu  >  Actions");

            for (int i = 0; i < categories.Count; i++)
            {
                DrawItemRow(y, i == Selected, categories[i]);
                DrawText(">", Left + Width - 0.025f, y, TextScale, 230, 190, 90);
                y += RowHeight;
            }

            DrawFooter(y, "Up/Down select    Enter open    Backspace back    F9 close");
        }

        private void DrawCategoryPage()
        {
            float y = DrawTitle($"RDR2 Mod Menu  >  Actions  >  {openCategory}");

            for (int i = 0; i < categoryItems.Count; i++)
            {
                ModInfo item = categoryItems[i];
                DrawItemRow(y, i == Selected, item.Name);
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
                y += RowHeight;
            }

            ModInfo selectedItem = SelectedItem;
            y = DrawDescription(y, selectedItem?.Description);
            DrawFooter(y, selectedItem != null && selectedItem.IsChoice
                ? "Left/Right change    Backspace back    F9 close"
                : "Up/Down select    Enter use    Backspace back    F9 close");
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
