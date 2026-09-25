using RDR2;
using RDR2.Native;

namespace RDR2ModMenu
{
    // Crime button: wipes Arthur's bounty and the law's record of his past crimes. Logs the bounty before and after,
    // since RDR2 keeps bounties per region and it isn't confirmed yet that this clears every region.
    public class ClearBounty : ModAction
    {
        protected override string Category => "Crime";

        protected override string Description => "Wipes your bounty and your record of past crimes.";

        protected override string Run()
        {
            int player = Game.Player.Handle;
            int before = LAW.GET_BOUNTY(player);

            LAW.CLEAR_BOUNTY(player);
            LAW.CLEAR_PLAYER_PAST_CRIMES(player);

            Log.Write($"Bounty {before} -> {LAW.GET_BOUNTY(player)} (in cents)");
            return "Bounty cleared";
        }
    }
}
