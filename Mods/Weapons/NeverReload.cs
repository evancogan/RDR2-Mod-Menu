using System;
using RDR2;
using RDR2.Native;

namespace RDR2ModMenu
{
    // Weapons mod: Arthur's guns never need reloading. The loaded clip never runs down, so the ammo he carries isn't
    // used either.
    //
    // Uses _SET_PED_INFINITE_AMMO_CLIP, re-applied once a second in case the game resets it (after a cutscene, say).
    public class NeverReload : ModScript
    {
        protected override string Category => "Weapons";

        protected override string Description => "Your guns never need reloading: the clip never runs down.";

        private const int RefreshIntervalMs = 1000;

        private int nextRefresh;

        protected override void OnEnabledTick()
        {
            int now = Environment.TickCount;
            if (now < nextRefresh)
            {
                return;
            }
            nextRefresh = now + RefreshIntervalMs;
            WEAPON._SET_PED_INFINITE_AMMO_CLIP(Game.Player.Character.Handle, true);
        }

        protected override void OnDisable()
        {
            WEAPON._SET_PED_INFINITE_AMMO_CLIP(Game.Player.Character.Handle, false);
            nextRefresh = 0;
        }
    }
}
