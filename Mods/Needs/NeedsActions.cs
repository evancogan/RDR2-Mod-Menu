namespace AIPlayground
{
    // Actions submenu buttons for refilling needs. Each logs the needs before and after, to confirm it took effect.

    public class RefillAllNeeds : ModAction
    {
        protected override string Description => "Fills Arthur's health, stamina and Dead Eye, bars and cores.";

        protected override string Run() => Refill(Needs.RefillAll, "Health, stamina and Dead Eye refilled");

        internal static string Refill(System.Action refill, string done)
        {
            string before = Needs.Describe();
            refill();
            Log.Write($"Before: {before}. After: {Needs.Describe()}");
            return done;
        }
    }

    public class RefillHealth : ModAction
    {
        protected override string Description => "Fills Arthur's health bar and core.";

        protected override string Run() => RefillAllNeeds.Refill(Needs.RefillHealth, "Health refilled");
    }

    public class RefillStamina : ModAction
    {
        protected override string Description => "Fills Arthur's stamina bar and core.";

        protected override string Run() => RefillAllNeeds.Refill(Needs.RefillStamina, "Stamina refilled");
    }

    public class RefillDeadEye : ModAction
    {
        protected override string Description => "Fills Arthur's Dead Eye bar and core.";

        protected override string Run() => RefillAllNeeds.Refill(Needs.RefillDeadEye, "Dead Eye refilled");
    }
}
