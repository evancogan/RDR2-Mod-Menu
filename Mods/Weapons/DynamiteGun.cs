using RDR2;
using RDR2.Math;

namespace RDR2ModMenu
{
    // Weapons mod: every bullet Arthur fires explodes like dynamite where it lands, except within MinSafeDistance of
    // him. The impact comes from Natives.TryGetLastWeaponImpact, which reads the game's padded out-vector correctly.
    public class DynamiteGun : ModScript
    {
        protected override string Category => "Weapons";

        protected override string Description => "Every bullet explodes like dynamite where it lands (not within 5 m of you).";

        // Don't blow up impacts this close to the player, so point-blank shots aren't suicide.
        private const float MinSafeDistance = 5.0f;

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

            // The last impact stays the same until a new shot lands, so only react when it changes.
            if (!Natives.TryGetLastWeaponImpact(player, out Vector3 impact) || impact == lastImpact)
            {
                return;
            }
            lastImpact = impact;

            if (impact.DistanceTo(player.Position) >= MinSafeDistance)
            {
                World.AddExplosion(impact, (int)eExplosionTag.Dynamite, 1.0f, 1.0f);
            }
        }
    }
}
