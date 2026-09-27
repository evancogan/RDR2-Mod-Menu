using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using RDR2;
using RDR2.Native;
using Screen = RDR2.UI.Screen;

namespace RDR2ModMenu
{
    // F9 opens the menu: Turn All Mods Off, then the sections (Player, Weapons, World, Game, Horse, Crime, Speech,
    // Debug). Enter opens a section. A section lists everything about that topic, whatever kind it is: on/off mods (Enter
    // toggles), buttons (Enter uses) and scrolling settings (Left/Right change). Backspace goes back. F9 closes it, and
    // opens it again where it was left. A few seconds after loading it announces what loaded, as a smoke test.
    public class ModMenu : Script
    {
        private const Keys MenuKey = Keys.F9;

        // Wait for every mod DLL to finish loading and registering before announcing.
        private const int AnnounceDelayMs = 3000;

        // Always shown, in this order, even when nothing is in them yet. Categories not listed follow alphabetically.
        private static readonly string[] Sections = { "Player", "Weapons", "World", "Game", "Horse", "Crime", "Speech", "Debug" };

        // Anything registered without a section ends up here.
        private const string OtherSection = "Other";

        private const string TurnAllOffRow = "Turn All Mods Off";

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

        // Where the cursor is on each page, kept while the menu is closed. On the sections page, row 0 is Turn All Mods
        // Off and the sections follow.
        private readonly Dictionary<Page, int> selected = new Dictionary<Page, int> { [Page.Sections] = 0, [Page.Section] = 0 };
        private string openSection;

        private List<string> sections = new List<string>();
        private Dictionary<string, List<ModInfo>> sectionItems = new Dictionary<string, List<ModInfo>>();

        // The registry version the lists were built from, and each setting row's widest choice (its length in characters).
        private int builtVersion = -1;
        private readonly Dictionary<string[], int> widestChoice = new Dictionary<string[], int>();

        private readonly int announceAt;
        private bool announced;

        public ModMenu()
        {
            Log.RollOver();
            announceAt = Environment.TickCount + AnnounceDelayMs;
            Tick += OnTick;
            KeyDown += OnKeyDown;
            Aborted += (sender, e) => ScreenText.FreeAll();
            Log.Write("Mod menu loaded, F9 to open");
        }

        private List<ModInfo> OpenItems => openSection != null && sectionItems.TryGetValue(openSection, out List<ModInfo> items) ? items : new List<ModInfo>();

        private int RowCount => page == Page.Sections ? sections.Count + 1 : OpenItems.Count;

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

        private void Select()
        {
            if (page == Page.Sections)
            {
                if (Selected == 0)
                {
                    TurnAllOff();
                }
                else if (Selected - 1 < sections.Count)
                {
                    openSection = sections[Selected - 1];
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

        private void TurnAllOff()
        {
            List<string> turnedOff = ModRegistry.GetAll().Where(item => item.TurnOff()).Select(item => item.Name).ToList();
            Screen.DisplaySubtitle(turnedOff.Count == 0 ? "Nothing was on" : $"Turned off {turnedOff.Count} mods");
            Log.Write(turnedOff.Count == 0 ? "Turn All Mods Off: nothing was on" : $"Turn All Mods Off: {string.Join(", ", turnedOff)}");
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

            if (builtVersion != ModRegistry.Version)
            {
                Rebuild();
            }

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

        // Rebuilds the section lists from the registry. Only needed when something has loaded or unloaded.
        private void Rebuild()
        {
            builtVersion = ModRegistry.Version;
            sectionItems = ModRegistry.GetAll().GroupBy(SectionOf)
                .ToDictionary(g => g.Key, g => g.OrderBy(KindOrder).ThenBy(m => m.Name).ToList());
            foreach (string section in Sections)
            {
                if (!sectionItems.ContainsKey(section))
                {
                    sectionItems[section] = new List<ModInfo>();
                }
            }
            sections = Sections.Concat(sectionItems.Keys.Except(Sections).OrderBy(s => s)).ToList();
            widestChoice.Clear();

            Selected = Math.Min(Selected, Math.Max(0, RowCount - 1));
        }

        private void DrawSectionsPage()
        {
            float y = DrawTitle("RDR2 Mod Menu");

            // Stands apart from the sections: a slim dark red row (brighter when selected) with small grey text.
            if (Selected == 0)
            {
                DrawRow(y, 190, 40, 40, 235, SmallRowHeight);
            }
            else
            {
                DrawRow(y, 90, 15, 15, 215, SmallRowHeight);
            }
            DrawText(TurnAllOffRow, Left + TextInset, y, SmallTextScale, 180, 180, 180);
            DrawText("USE", Left + Width - 0.03f, y, SmallTextScale, 180, 180, 180);
            y += SmallRowHeight;

            for (int i = 0; i < sections.Count; i++)
            {
                string section = sections[i];
                DrawItemRow(y, Selected == i + 1, section);

                // How many of the section's mods are on, so you can see at a glance without opening it.
                int on = sectionItems[section].Count(m => m.IsEnabled);
                if (on > 0)
                {
                    DrawText($"{on} ON", Left + Width - 0.075f, y, TextScale, 90, 220, 90);
                }
                DrawText(">", Left + Width - 0.025f, y, TextScale, 230, 190, 90);
                y += RowHeight;
            }

            string description;
            if (Selected == 0)
            {
                description = "Turns off every mod that's on, and sets rows like Refill Health back to Once.";
            }
            else
            {
                List<ModInfo> items = sectionItems[sections[Selected - 1]];
                description = items.Count == 0 ? "Nothing here yet." : string.Join(", ", items.Select(m => m.Name));
            }
            y = DrawDescription(y, description);
            DrawFooter(y, Selected == 0 ? "Up/Down select    Enter use    F9 close" : "Up/Down select    Enter open    F9 close");
        }

        private void DrawSectionPage()
        {
            float y = DrawTitle($"RDR2 Mod Menu  >  {openSection}");
            List<ModInfo> items = OpenItems;

            if (items.Count == 0)
            {
                DrawRow(y, 0, 0, 0, 190);
                DrawText("Nothing here yet", Left + TextInset, y, TextScale, 180, 180, 180);
                y += RowHeight;
            }
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
        private void DrawItemValue(float y, ModInfo item)
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
                // Placed so the row's longest choice ends at the panel's edge, keeping "<" still while scrolling.
                if (!widestChoice.TryGetValue(choices, out int widest))
                {
                    widest = choices.Length == 0 ? 0 : choices.Max(c => c.Length);
                    widestChoice[choices] = widest;
                }
                float width = ScreenText.EstimateWidth(widest + "<    >".Length, TextScale);
                DrawText($"<  {value}  >", Left + Width - TextInset - width, y, TextScale, 230, 190, 90);
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
