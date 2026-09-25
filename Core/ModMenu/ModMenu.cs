using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using RDR2;
using RDR2.Native;
using Screen = RDR2.UI.Screen;

namespace AIPlayground
{
    // F9 opens a menu listing every loaded mod. Up/Down selects, Enter toggles a mod, F9 closes.
    // One-shot actions live in an Actions submenu, opened from the last row: Enter uses one, Backspace goes back.
    // A few seconds after loading it also announces what loaded, as a smoke test.
    public class ModMenu : Script
    {
        private const Keys MenuKey = Keys.F9;

        // Wait for every mod DLL to finish loading and registering before announcing.
        private const int AnnounceDelayMs = 3000;

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
        private const string ActionsRowDescription = "One-press buttons, like refilling needs.";

        private enum Page { Mods, Actions }

        private bool open;
        private Page page = Page.Mods;
        private int selectedMod;
        private int selectedAction;
        private List<ModInfo> toggles = new List<ModInfo>();
        private List<ModInfo> actions = new List<ModInfo>();
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

        // The mods page has one extra row at the bottom that opens the Actions page, when there are any actions.
        private int ModsPageRows => toggles.Count + (actions.Count > 0 ? 1 : 0);

        private bool ActionsRowSelected => page == Page.Mods && actions.Count > 0 && selectedMod == toggles.Count;

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

            int rows = page == Page.Mods ? ModsPageRows : actions.Count;
            switch (e.KeyCode)
            {
                case Keys.Up:
                    Move(rows, -1);
                    break;
                case Keys.Down:
                    Move(rows, 1);
                    break;
                case Keys.Enter:
                    Select();
                    break;
                case Keys.Back:
                    if (page == Page.Actions)
                    {
                        page = Page.Mods;
                    }
                    else
                    {
                        open = false;
                    }
                    break;
            }
        }

        private void Move(int rows, int step)
        {
            if (rows == 0)
            {
                return;
            }
            if (page == Page.Mods)
            {
                selectedMod = (selectedMod + step + rows) % rows;
            }
            else
            {
                selectedAction = (selectedAction + step + rows) % rows;
            }
        }

        private void Select()
        {
            if (page == Page.Actions)
            {
                if (selectedAction < actions.Count)
                {
                    actions[selectedAction].Activate();
                }
            }
            else if (ActionsRowSelected)
            {
                page = Page.Actions;
            }
            else if (selectedMod < toggles.Count)
            {
                toggles[selectedMod].Activate();
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!announced && Environment.TickCount > announceAt)
            {
                announced = true;
                List<ModInfo> all = ModRegistry.GetAll();
                int on = all.Count(m => m.IsEnabled);
                Screen.DisplaySubtitle($"RDR2 Mod Menu: {all.Count(m => !m.IsAction)} mods loaded ({on} on), {all.Count(m => m.IsAction)} actions. F9 for menu.");
                Log.Write($"Loaded: {string.Join(", ", all.Select(m => $"{m.Name} ({(m.IsAction ? "action" : m.IsEnabled ? "on" : "off")})"))}");
            }

            if (!open)
            {
                return;
            }

            List<ModInfo> all2 = ModRegistry.GetAll();
            toggles = all2.Where(m => !m.IsAction).ToList();
            actions = all2.Where(m => m.IsAction).ToList();
            selectedMod = Math.Min(selectedMod, Math.Max(0, ModsPageRows - 1));
            selectedAction = Math.Min(selectedAction, Math.Max(0, actions.Count - 1));
            if (page == Page.Actions && actions.Count == 0)
            {
                page = Page.Mods;
            }

            // The menu is modal: keep arrow keys, Enter and Backspace from also acting in the game.
            Game.DisableAllControlsThisFrame();
            if (page == Page.Mods)
            {
                DrawModsPage();
            }
            else
            {
                DrawActionsPage();
            }
        }

        private void DrawModsPage()
        {
            float y = DrawTitle("RDR2 Mod Menu");

            if (ModsPageRows == 0)
            {
                DrawRow(y, 0, 0, 0, 190);
                DrawText("No mods loaded", Left + TextInset, y, TextScale, 180, 180, 180);
                y += RowHeight;
            }

            for (int i = 0; i < toggles.Count; i++)
            {
                ModInfo mod = toggles[i];
                DrawItemRow(y, i == selectedMod, mod.Name);
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

            if (actions.Count > 0)
            {
                DrawItemRow(y, ActionsRowSelected, ActionsRowName);
                DrawText(">", Left + Width - 0.025f, y, TextScale, 230, 190, 90);
                y += RowHeight;
            }

            string description = ActionsRowSelected ? ActionsRowDescription
                : selectedMod < toggles.Count ? toggles[selectedMod].Description : null;
            y = DrawDescription(y, description);
            DrawFooter(y, "Up/Down select    Enter toggle/open    F9 close");
        }

        private void DrawActionsPage()
        {
            float y = DrawTitle("RDR2 Mod Menu  >  Actions");

            for (int i = 0; i < actions.Count; i++)
            {
                DrawItemRow(y, i == selectedAction, actions[i].Name);
                DrawText("USE", Left + Width - 0.04f, y, TextScale, 230, 190, 90);
                y += RowHeight;
            }

            y = DrawDescription(y, selectedAction < actions.Count ? actions[selectedAction].Description : null);
            DrawFooter(y, "Up/Down select    Enter use    Backspace back    F9 close");
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
