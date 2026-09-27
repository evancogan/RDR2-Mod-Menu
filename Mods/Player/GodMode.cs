using System;
using RDR2;

namespace RDR2ModMenu
{
    // Player mod: Arthur can't be hurt or knocked down (see Protection), and his health, stamina and Dead Eye stay full,
    // re-applied a few times a second.
    public class GodMode : ModScript
    {
        protected override string Category => "Player";

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

            Protection.Set(Game.Player.Character, true);
            Needs.RefillAll();
        }

        protected override void OnDisable()
        {
            Protection.Set(Game.Player.Character, false);
            nextRefresh = 0;
        }
    }
}
