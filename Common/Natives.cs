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

        // Height of the ground below (x, y, fromZ). Out-floats are read from an 8-byte slot to be safe,
        // since the game pads out-values to 8 bytes.
        public static unsafe bool TryGetGroundZ(float x, float y, float fromZ, out float groundZ)
        {
            ulong buffer = 0;
            bool found = MISC.GET_GROUND_Z_FOR_3D_COORD(x, y, fromZ, (float*)&buffer, false);
            groundZ = *(float*)&buffer;
            return found;
        }

        // Height of the water surface at (x, y), probing straight down from 5 m above nearZ against every kind of water.
        // Tested in-game on a river: this finds the surface, while GET_WATER_HEIGHT and GET_WATER_HEIGHT_NO_WAVES
        // never reported any water there.
        public static unsafe bool TryGetWaterZ(float x, float y, float nearZ, out float waterZ)
        {
            ulong buffer = 0;
            bool found = WATER.TEST_VERTICAL_PROBE_AGAINST_ALL_WATER(x, y, nearZ + 5f, 1, (float*)&buffer) != 0;
            waterZ = *(float*)&buffer;
            return found;
        }
    }
}
