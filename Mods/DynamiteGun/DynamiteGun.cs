using System;
using System.Windows.Forms;
using RDR2;
using RDR2.Math;
using Screen = RDR2.UI.Screen;

namespace AIPlayground
{
    // Every bullet the player fires explodes like dynamite where it lands.
    // Toggle with F10.
    public class DynamiteGun : Script
    {
        private const Keys ToggleKey = Keys.F10;

        // Don't blow up impacts this close to the player, so point-blank shots aren't suicide.
        private const float MinSafeDistance = 5.0f;

        private bool enabled = true;
        private bool wasShooting;
        private Vector3 lastImpact = Vector3.Zero;

        public DynamiteGun()
        {
            Tick += OnTick;
            KeyDown += OnKeyDown;
            Log.Write("DynamiteGun started");
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!enabled)
            {
                return;
            }

            Ped player = Game.Player.Character;

            bool isShooting = player.IsShooting;
            if (isShooting && !wasShooting)
            {
                Log.Write($"Shot fired from {player.Position}");
            }
            wasShooting = isShooting;

            // The last impact stays the same until a new shot lands, so only react when it changes.
            if (!Natives.TryGetLastWeaponImpact(player, out Vector3 impact) || impact == lastImpact)
            {
                return;
            }
            lastImpact = impact;

            float distance = impact.DistanceTo(player.Position);
            if (distance < MinSafeDistance)
            {
                Log.Write($"Impact at {impact} skipped, only {distance:F1}m away");
                return;
            }

            Log.Write($"Impact at {impact}, {distance:F1}m away: exploding");
            Screen.DisplaySubtitle($"BOOM ({distance:F0}m)");
            World.AddExplosion(impact, (int)eExplosionTag.Dynamite, 1.0f, 1.0f);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == ToggleKey)
            {
                enabled = !enabled;
                Log.Write($"Toggled {(enabled ? "ON" : "OFF")}");
                Screen.DisplaySubtitle(enabled ? "Dynamite rounds: ON" : "Dynamite rounds: OFF");
            }
        }
    }
}
