namespace RDR2ModMenu
{
    // Player rows: "Refill Health  < Once | Always >", and the same for Stamina and Dead Eye (see NeedRefill).
    public abstract class ArthurNeedRefill : NeedRefill
    {
        protected override string Category => "Player";

        protected override string Describe() => Needs.Describe();
    }

    public class RefillHealth : ArthurNeedRefill
    {
        protected override string Need => "Health";

        protected override bool Refill()
        {
            Needs.RefillHealth();
            return true;
        }
    }

    public class RefillStamina : ArthurNeedRefill
    {
        protected override string Need => "Stamina";

        protected override bool Refill()
        {
            Needs.RefillStamina();
            return true;
        }
    }

    public class RefillDeadEye : ArthurNeedRefill
    {
        protected override string Need => "Dead Eye";

        protected override bool Refill()
        {
            Needs.RefillDeadEye();
            return true;
        }
    }
}
