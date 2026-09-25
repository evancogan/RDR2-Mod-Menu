using System;
using RDR2;
using RDR2.Native;

namespace RDR2ModMenu
{
    // Crime mod: while it's on, the law never comes after Arthur.
    //
    // _SET_DISABLE_PLAYER_WANTED_LEVEL is documented as stopping lawmen from wanting the player. As a backstop, any
    // wanted score that still builds up is cleared a couple of times a second. Bounties aren't touched.
    public class NeverWanted : ModScript
    {
        protected override string Category => "Crime";

        protected override string Description => "The law never comes after you while it's on. Bounties aren't touched.";

        private const int ClearIntervalMs = 500;

        private int nextClear;

        protected override bool OnEnable()
        {
            int player = Game.Player.Handle;
            PLAYER._SET_DISABLE_PLAYER_WANTED_LEVEL(player, true);
            LAW.CLEAR_WANTED_SCORE(player);
            LAW._SET_BOUNTY_HUNTER_PURSUIT_CLEARED();
            return true;
        }

        protected override void OnEnabledTick()
        {
            int now = Environment.TickCount;
            if (now < nextClear)
            {
                return;
            }
            nextClear = now + ClearIntervalMs;

            int player = Game.Player.Handle;
            PLAYER._SET_DISABLE_PLAYER_WANTED_LEVEL(player, true);
            LAW.CLEAR_WANTED_SCORE(player);
        }

        protected override void OnDisable()
        {
            PLAYER._SET_DISABLE_PLAYER_WANTED_LEVEL(Game.Player.Handle, false);
        }
    }
}
