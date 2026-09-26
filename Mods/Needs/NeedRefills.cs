using System;

namespace RDR2ModMenu
{
    // Needs section rows: "Refill Health  < Once | Always >", and the same for Stamina and Dead Eye.
    // On Once, Enter refills it right now. On Always, it's kept full, a few times a second. Remembered across reloads.
    public abstract class NeedRefill : ModChoice
    {
        private const int Once = 0;
        private const int Always = 1;

        // Refilling a few times a second is plenty and keeps it cheap.
        private const int RefillIntervalMs = 250;

        private int nextRefill;

        // e.g. "Health"
        protected abstract string Need { get; }

        protected abstract void Refill();

        // For the log, before and after a refill.
        protected virtual string Describe() => Needs.Describe();

        protected override string Category => "Needs";

        protected override string Description => $"Once: press Enter to fill {Need.ToLowerInvariant()} now. Always: keeps it full.";

        protected override string[] Choices => new[] { "Once", "Always" };

        protected override bool RememberChoice => true;

        protected override bool CanUse => true;

        protected override string Apply(int index)
        {
            return index == Always ? $"{Need}: always full" : $"{Need}: press Enter to refill";
        }

        protected override string Use()
        {
            string before = Describe();
            Refill();
            Log.Write($"Before: {before}. After: {Describe()}");
            return $"{Need} refilled";
        }

        protected override void OnChoiceTick()
        {
            int now = Environment.TickCount;
            if (Current != Always || now < nextRefill)
            {
                return;
            }
            nextRefill = now + RefillIntervalMs;
            Refill();
        }
    }

    public class RefillHealth : NeedRefill
    {
        protected override string Need => "Health";

        protected override void Refill() => Needs.RefillHealth();
    }

    public class RefillStamina : NeedRefill
    {
        protected override string Need => "Stamina";

        protected override void Refill() => Needs.RefillStamina();
    }

    public class RefillDeadEye : NeedRefill
    {
        protected override string Need => "Dead Eye";

        protected override void Refill() => Needs.RefillDeadEye();
    }
}
