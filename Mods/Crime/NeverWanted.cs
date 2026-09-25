using RDR2;
using RDR2.Native;

namespace RDR2ModMenu
{
    // Crime mod: while it's on, the law never comes after Arthur.
    //
    // The first version only disabled the wanted level and cleared it twice a second as a backstop. The law kept
    // noticing crimes, so it flipped between chasing and giving up (and the HUD with it). Instead, this stops crimes
    // being noticed and answered at all:
    // - witnesses are stopped from calling the law, every frame
    // - law dispatch is disabled, even for crimes that do get witnessed or reported
    // - disturbance crimes (brawling, noise) aren't registered
    // Any pursuit already under way is cleared once when it's turned on. Bounties aren't touched.
    public class NeverWanted : ModScript
    {
        protected override string Category => "Crime";

        protected override string Description => "The law never comes after you while it's on. Bounties aren't touched.";

        protected override bool OnEnable()
        {
            int player = Game.Player.Handle;
            SetLawIgnoring(player, true);
            LAW.CLEAR_WANTED_SCORE(player);
            LAW._SET_BOUNTY_HUNTER_PURSUIT_CLEARED();
            return true;
        }

        protected override void OnEnabledTick()
        {
            PLAYER.SUPPRESS_WITNESSES_CALLING_POLICE_THIS_FRAME(Game.Player.Handle);
        }

        protected override void OnDisable()
        {
            SetLawIgnoring(Game.Player.Handle, false);
        }

        private static void SetLawIgnoring(int player, bool ignoring)
        {
            LAW._SET_LAW_DISABLED(ignoring);
            LAW.SET_DISABLE_DISTURBANCE_CRIMES(player, ignoring);
            PLAYER._SET_DISABLE_PLAYER_WANTED_LEVEL(player, ignoring);
        }
    }
}
