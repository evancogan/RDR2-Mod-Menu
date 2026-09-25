using RDR2.Native;

namespace RDR2ModMenu
{
    // Player button: adds $1000 to Arthur's cash. The game counts money in cents. Logs the balance before and after.
    public class GiveMoney : ModAction
    {
        protected override string DisplayName => "Give $1000";

        protected override string Category => "Player";

        protected override string Description => "Adds $1000 to Arthur's cash.";

        private const int AmountInCents = 1000 * 100;

        protected override string Run()
        {
            int before = MONEY._MONEY_GET_CASH_BALANCE();
            MONEY._MONEY_INCREMENT_CASH_BALANCE(AmountInCents, Natives.Hash("ADD_REASON_DEFAULT"));
            Log.Write($"Cash {before} -> {MONEY._MONEY_GET_CASH_BALANCE()} (in cents)");
            return "+$1000";
        }
    }
}
