using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace AIPlayground
{
    // Workarounds for ScriptHookRDR2 .NET V2 wrappers that read native results wrong.
    public static class Natives
    {
        private const ulong GET_PED_LAST_WEAPON_IMPACT_COORD = 0x6C4D0409BA1A2BC2;

        // The game writes out-vectors with each float padded to 8 bytes (x, pad, y, pad, z, pad),
        // but V2's Ped.GetLastWeaponImpactCoords reads them packed, which returns (x, 0, y).
        public static unsafe bool TryGetLastWeaponImpact(Ped ped, out Vector3 impact)
        {
            ulong* buffer = stackalloc ulong[3];
            bool hit = Function.Call<bool>(GET_PED_LAST_WEAPON_IMPACT_COORD, ped.Handle, buffer);

            float* f = (float*)buffer;
            impact = new Vector3(f[0], f[2], f[4]);
            return hit;
        }
    }
}
