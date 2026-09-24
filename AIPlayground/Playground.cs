using System;
using System.Windows.Forms;
using RDR2;
using RDR2.UI;
using Screen = RDR2.UI.Screen;

namespace AIPlayground
{
    public class Playground : Script
    {
        public Playground()
        {
            Tick += OnTick;
            KeyDown += OnKeyDown;
        }

        private void OnTick(object sender, EventArgs e)
        {
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // Smoke test: press F9 in-game to confirm the script is loaded.
            if (e.KeyCode == Keys.F9)
            {
                Screen.DisplaySubtitle("AI Playground is loaded");
            }
        }
    }
}
