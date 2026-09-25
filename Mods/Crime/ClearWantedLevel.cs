using RDR2;
using RDR2.Native;

namespace AIPlayground
{
    // Crime section button: ends the law's current pursuit of Arthur. His bounty is left as is.
    //
    // CLEAR_PLAYER_WANTED_LEVEL and SET_PLAYER_WANTED_LEVEL do nothing in RDR2 (per alloc8or's rdr3-nativedb), so this
    // clears the wanted score and force-clears the pursuit instead.
    public class ClearWantedLevel : ModAction
    {
        protected override string Category => "Crime";

        protected override string Description => "Ends the law's current pursuit. Your bounty stays as it is.";

        protected override string Run()
        {
            int player = Game.Player.Handle;
            LAW.CLEAR_WANTED_SCORE(player);
            LAW._SET_BOUNTY_HUNTER_PURSUIT_CLEARED();
            return "Wanted level cleared";
        }
    }
}
