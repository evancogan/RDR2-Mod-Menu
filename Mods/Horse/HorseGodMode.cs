using System;
using RDR2;
using RDR2.Native;

namespace RDR2ModMenu
{
    // Horse mod: the horse Arthur is riding (or his active horse, when he's on foot) can't be hurt or knocked down, and
    // its health and stamina stay full.
    //
    // Like God Mode (see Protection), re-applied a few times a second. If he switches horses, the previous one is put
    // back to normal.
    public class HorseGodMode : ModScript
    {
        protected override string Category => "Horse";

        protected override string Description => "Your horse can't be hurt or knocked down, and its health and stamina stay full.";

        private const int RefreshIntervalMs = 250;

        private int nextRefresh;
        private int protectedHorse;

        protected override void OnEnabledTick()
        {
            int now = Environment.TickCount;
            if (now < nextRefresh)
            {
                return;
            }
            nextRefresh = now + RefreshIntervalMs;

            Ped horse = HorseNeeds.Current;
            int handle = horse?.Handle ?? 0;
            if (handle != protectedHorse)
            {
                Unprotect(protectedHorse);
                protectedHorse = handle;
                if (horse != null)
                {
                    Log.Write($"Protecting horse {handle}");
                }
            }
            if (horse == null)
            {
                return;
            }

            Protection.Set(horse, true);
            HorseNeeds.RefillHealth(horse);
            HorseNeeds.RefillStamina(horse);
        }

        protected override void OnDisable()
        {
            Unprotect(protectedHorse);
            protectedHorse = 0;
            nextRefresh = 0;
        }

        private static void Unprotect(int handle)
        {
            if (handle != 0 && ENTITY.DOES_ENTITY_EXIST(handle) && ENTITY.IS_ENTITY_A_PED(handle))
            {
                Protection.Set(new Ped(handle), false);
            }
        }
    }
}
