using System;

namespace RDR2ModMenu
{
    // Keeps Arthur's health, stamina and Dead Eye full, bars and cores, for as long as it's on.
    public class KeepNeedsFilled : ModScript
    {
        protected override string Category => "Needs";

        protected override string Description => "Keeps Arthur's health, stamina and Dead Eye full while it's on.";

        // Refilling a few times a second is plenty and keeps it cheap.
        private const int RefillIntervalMs = 250;

        private int nextRefill;

        protected override void OnEnabledTick()
        {
            int now = Environment.TickCount;
            if (now < nextRefill)
            {
                return;
            }
            nextRefill = now + RefillIntervalMs;
            Needs.RefillAll();
        }
    }
}
