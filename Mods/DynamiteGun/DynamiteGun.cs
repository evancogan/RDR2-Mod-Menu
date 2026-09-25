using RDR2;
using RDR2.Math;
using Screen = RDR2.UI.Screen;

namespace AIPlayground
{
    // Every bullet the player fires explodes like dynamite where it lands.
    public class DynamiteGun : ModScript
    {
        protected override string Description => "Every bullet explodes like dynamite where it lands (not within 5 m of you).";

        // Don't blow up impacts this close to the player, so point-blank shots aren't suicide.
        private const float MinSafeDistance = 5.0f;

        private bool wasShooting;
        private Vector3 lastImpact = Vector3.Zero;

        protected override bool OnEnable()
        {
            // Forget impacts from shots fired while off, so turning on doesn't blow up an old one.
            Natives.TryGetLastWeaponImpact(Game.Player.Character, out lastImpact);
            return true;
        }

        protected override void OnEnabledTick()
        {
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
    }
}
