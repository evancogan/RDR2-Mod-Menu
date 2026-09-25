using System;
using RDR2;

namespace RDR2ModMenu
{
    // Needs mod: Arthur can't be hurt or knocked down, and his health, stamina and Dead Eye stay full.
    //
    // Invincible, proof against every damage type, and no ragdoll. These are re-applied a few times a second, since
    // other mods (Flying Horse landing, Super Speed) switch invincibility and ragdoll off when they finish.
    public class GodMode : ModScript
    {
        protected override string Category => "Needs";

        protected override string Description => "Can't be hurt or knocked down, and health, stamina and Dead Eye stay full.";

        private const int RefreshIntervalMs = 250;

        private int nextRefresh;

        protected override void OnEnabledTick()
        {
            int now = Environment.TickCount;
            if (now < nextRefresh)
            {
                return;
            }
            nextRefresh = now + RefreshIntervalMs;

            SetProtected(Game.Player.Character, true);
            Needs.RefillAll();
        }

        protected override void OnDisable()
        {
            SetProtected(Game.Player.Character, false);
        }

        private static void SetProtected(Ped arthur, bool on)
        {
            arthur.IsInvincible = on;
            arthur.CanRagdoll = !on;
            arthur.IsBulletProof = on;
            arthur.IsFlameProof = on;
            arthur.IsExplosionProof = on;
            arthur.IsCollisionProof = on;
            arthur.IsMeleeProof = on;
            arthur.IsSteamProof = on;
            arthur.IsSmokeProof = on;
            arthur.IsHeadshotProof = on;
            arthur.IsProjectileProof = on;
        }
    }
}
