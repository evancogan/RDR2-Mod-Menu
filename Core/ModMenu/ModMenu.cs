using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using RDR2;
using RDR2.Native;
using Screen = RDR2.UI.Screen;

namespace RDR2ModMenu
{
    // F9 opens the menu: a list of sections (Player, Needs, Weapons, Horse, Crime...). Enter opens one. A section lists
    // everything about that topic, whatever kind it is: on/off mods (Enter toggles), buttons (Enter uses) and scrolling
    // settings (Left/Right change). Backspace goes back, F9 closes. A few seconds after loading it announces what
    // loaded, as a smoke test.
    public class ModMenu : Script
    {
        private const Keys MenuKey = Keys.F9;

        // Wait for every mod DLL to finish loading and registering before announcing.
        private const int AnnounceDelayMs = 3000;

        // Sections in this order first; any others follow alphabetically.
        private static readonly string[] SectionOrder = { "Player", "Needs", "Weapons", "Horse", "Horse Needs", "Crime" };

        // Anything registered without a section ends up here.
        private const string OtherSection = "Other";

        // Layout, in fractions of the screen.
        private const float Left = 0.05f;
        private const float Top = 0.15f;
        private const float Width = 0.3f;
        private const float RowHeight = 0.035f;
        private const float SmallRowHeight = 0.026f;
        private const float TextInset = 0.008f;
        private const float TextScale = 0.4f;
        private const float SmallTextScale = 0.3f;

        private enum Page { Sections, Section }

        private bool open;
        private Page page = Page.Sections;
        private readonly Dictionary<Page, int> selected = new Dictionary<Page, int> { [Page.Sections] = 0, [Page.Section] = 0 };
        private string openSection;

        private List<string> sections = new List<string>();
        private Dictionary<string, List<ModInfo>> sectionItems = new Dictionary<string, List<ModInfo>>();

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

        private List<ModInfo> OpenItems => openSection != null && sectionItems.TryGetValue(openSection, out List<ModInfo> items) ? items : new List<ModInfo>();

        private int RowCount => page == Page.Sections ? sections.Count : OpenItems.Count;

        private int Selected
        {
            get => selected[page];
            set => selected[page] = value;
        }

        private ModInfo SelectedItem => page == Page.Section && Selected < OpenItems.Count ? OpenItems[Selected] : null;

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == MenuKey)
            {
                open = !open;
                page = Page.Sections;
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
                    if (page == Page.Section)
                    {
                        page = Page.Sections;
                    }
                    else
                    {
                        open = false;
                    }
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
            if (page == Page.Sections)
            {
                if (Selected < sections.Count)
                {
                    openSection = sections[Selected];
                    page = Page.Section;
                    selected[Page.Section] = 0;
                }
                return;
            }

            ModInfo item = SelectedItem;
            if (item == null)
            {
                return;
            }
            if (item.IsChoice && !item.CanUse)
            {
                item.Step(1);
            }
            else
            {
                // Toggles a mod, uses a button, or uses a choice row that has an Enter action (like "refill now").
                item.Activate();
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
                Log.Write($"Loaded: {string.Join(", ", all.Select(m => $"{m.Name} ({SectionOf(m)}{(m.IsToggle ? (m.IsEnabled ? ", on" : ", off") : "")})"))}");
            }

            if (!open)
            {
                return;
            }

            Refresh();

            // The menu is modal: keep arrow keys, Enter and Backspace from also acting in the game.
            Game.DisableAllControlsThisFrame();
            if (page == Page.Sections)
            {
                DrawSectionsPage();
            }
            else
            {
                DrawSectionPage();
            }
        }

        private static string SectionOf(ModInfo item) => item.Category ?? OtherSection;

        // Within a section: on/off mods first, then scrolling settings, then buttons, each alphabetically.
        private static int KindOrder(ModInfo item) => item.IsToggle ? 0 : item.IsChoice ? 1 : 2;

        private void Refresh()
        {
            List<ModInfo> all = ModRegistry.GetAll();
            sectionItems = all.GroupBy(SectionOf)
                .ToDictionary(g => g.Key, g => g.OrderBy(KindOrder).ThenBy(m => m.Name).ToList());
            sections = sectionItems.Keys
                .OrderBy(s => Array.IndexOf(SectionOrder, s) is int i && i >= 0 ? i : SectionOrder.Length)
                .ThenBy(s => s)
                .ToList();

            if (page == Page.Section && OpenItems.Count == 0)
            {
                page = Page.Sections;
            }
            Selected = Math.Min(Selected, Math.Max(0, RowCount - 1));
        }

        private void DrawSectionsPage()
        {
            float y = DrawTitle("RDR2 Mod Menu");

            if (sections.Count == 0)
            {
                DrawRow(y, 0, 0, 0, 190);
                DrawText("No mods loaded", Left + TextInset, y, TextScale, 180, 180, 180);
                y += RowHeight;
            }

            for (int i = 0; i < sections.Count; i++)
            {
                string section = sections[i];
                DrawItemRow(y, i == Selected, section);

                // How many of the section's mods are on, so you can see at a glance without opening it.
                int on = sectionItems[section].Count(m => m.IsEnabled);
                if (on > 0)
                {
                    DrawText($"{on} ON", Left + Width - 0.075f, y, TextScale, 90, 220, 90);
                }
                DrawText(">", Left + Width - 0.025f, y, TextScale, 230, 190, 90);
                y += RowHeight;
            }

            string contents = Selected < sections.Count ? string.Join(", ", sectionItems[sections[Selected]].Select(m => m.Name)) : null;
            y = DrawDescription(y, contents);
            DrawFooter(y, "Up/Down select    Enter open    F9 close");
        }

        private void DrawSectionPage()
        {
            float y = DrawTitle($"RDR2 Mod Menu  >  {openSection}");
            List<ModInfo> items = OpenItems;

            for (int i = 0; i < items.Count; i++)
            {
                DrawItemRow(y, i == Selected, items[i].Name);
                DrawItemValue(y, items[i]);
                y += RowHeight;
            }

            ModInfo item = SelectedItem;
            y = DrawDescription(y, item?.Description);
            // Kept short enough to fit on one line of the panel.
            string footer =
                item == null ? "Backspace back    F9 close" :
                item.IsChoice && item.CanUse ? "Left/Right change   Enter use   Backspace back   F9 close" :
                item.IsChoice ? "Up/Down select    Left/Right change    Backspace back    F9 close" :
                item.IsToggle ? "Up/Down select    Enter toggle    Backspace back    F9 close" :
                "Up/Down select    Enter use    Backspace back    F9 close";
            DrawFooter(y, footer);
        }

        // ON/OFF for a mod, USE for a button, "<  Medium  >" for a scrolling setting.
        private static void DrawItemValue(float y, ModInfo item)
        {
            if (item.IsToggle)
            {
                if (item.IsEnabled)
                {
                    DrawText("ON", Left + Width - 0.04f, y, TextScale, 90, 220, 90);
                }
                else
                {
                    DrawText("OFF", Left + Width - 0.04f, y, TextScale, 170, 170, 170);
                }
            }
            else if (item.IsChoice)
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
            if (string.IsNullOrEmpty(description))
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
