using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using RDR2;
using RDR2.Native;
using RDR2.UI;
using Screen = RDR2.UI.Screen;

namespace AIPlayground
{
    // F9 opens a menu listing every loaded AI Playground mod. Up/Down selects, Enter toggles, F9 or Backspace closes.
    // A few seconds after loading it also announces how many mods loaded, as a smoke test.
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

        private bool open;
        private int selected;
        private List<ModInfo> mods = new List<ModInfo>();
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

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == MenuKey)
            {
                open = !open;
                return;
            }

            if (!open)
            {
                return;
            }

            switch (e.KeyCode)
            {
                case Keys.Up:
                    selected = mods.Count == 0 ? 0 : (selected - 1 + mods.Count) % mods.Count;
                    break;
                case Keys.Down:
                    selected = mods.Count == 0 ? 0 : (selected + 1) % mods.Count;
                    break;
                case Keys.Enter:
                    if (selected < mods.Count)
                    {
                        mods[selected].RequestToggle();
                    }
                    break;
                case Keys.Back:
                    open = false;
                    break;
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!announced && Environment.TickCount > announceAt)
            {
                announced = true;
                List<ModInfo> all = ModRegistry.GetAll();
                int on = all.Count(m => m.IsEnabled);
                Screen.DisplaySubtitle($"AI Playground: {all.Count} mods loaded, {on} on. F9 for menu.");
                Log.Write($"Loaded mods: {string.Join(", ", all.Select(m => $"{m.Name} ({(m.IsEnabled ? "on" : "off")})"))}");
            }

            if (!open)
            {
                return;
            }

            mods = ModRegistry.GetAll();
            if (selected >= mods.Count)
            {
                selected = Math.Max(0, mods.Count - 1);
            }

            // The menu is modal: keep arrow keys, Enter and Backspace from also acting in the game.
            Game.DisableAllControlsThisFrame();
            Draw();
        }

        private void Draw()
        {
            float y = Top;

            DrawRow(y, 150, 20, 20, 230);
            DrawText("AI Playground Mods", Left + TextInset, y, TextScale, 255, 255, 255);
            y += RowHeight;

            if (mods.Count == 0)
            {
                DrawRow(y, 0, 0, 0, 190);
                DrawText("No mods loaded", Left + TextInset, y, TextScale, 180, 180, 180);
                y += RowHeight;
            }

            for (int i = 0; i < mods.Count; i++)
            {
                ModInfo mod = mods[i];
                bool isSelected = i == selected;

                if (isSelected)
                {
                    DrawRow(y, 90, 90, 90, 220);
                }
                else
                {
                    DrawRow(y, 0, 0, 0, 190);
                }

                DrawText(mod.Name, Left + TextInset, y, TextScale, 255, 255, 255);
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

            if (selected < mods.Count)
            {
                foreach (string line in ScreenText.Wrap(mods[selected].Description, Width - 2 * TextInset, SmallTextScale))
                {
                    DrawRow(y, 0, 0, 0, 190, SmallRowHeight);
                    DrawText(line, Left + TextInset, y, SmallTextScale, 220, 220, 220);
                    y += SmallRowHeight;
                }
            }

            DrawRow(y, 0, 0, 0, 190, SmallRowHeight + 0.006f);
            DrawText("Up/Down select    Enter toggle    F9 close", Left + TextInset, y + 0.004f, SmallTextScale, 180, 180, 180);
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
