using RDR2;

namespace RDR2ModMenu
{
    // Makes a ped (Arthur or his horse) impossible to hurt or knock down, or undoes it: invincible, proof against every
    // damage type, and no ragdoll. Shared by God Mode and Horse God Mode, which re-apply it a few times a second, since
    // Flying Horse and Super Speed switch invincibility and ragdoll back off when they finish.
    public static class Protection
    {
        public static void Set(Ped ped, bool on)
        {
            ped.IsInvincible = on;
            ped.CanRagdoll = !on;
            ped.IsBulletProof = on;
            ped.IsFlameProof = on;
            ped.IsExplosionProof = on;
            ped.IsCollisionProof = on;
            ped.IsMeleeProof = on;
            ped.IsSteamProof = on;
            ped.IsSmokeProof = on;
            ped.IsHeadshotProof = on;
            ped.IsProjectileProof = on;
        }
    }
}
