using System;

namespace RDR2ModMenu
{
    // Base for the refill rows: "Refill Health  < Once | Always >", for Arthur (Player) and his horse (Horse).
    // On Once, Enter refills it right now. On Always, it's kept full, a few times a second. Remembered across reloads,
    // and Turn All Mods Off puts it back to Once.
    public abstract class NeedRefill : ModChoice
    {
        private const int Once = 0;
        private const int Always = 1;

        private static readonly string[] OnceOrAlways = { "Once", "Always" };

        // Refilling a few times a second is plenty and keeps it cheap.
        private const int RefillIntervalMs = 250;

        private int nextRefill;

        // e.g. "Health"
        protected abstract string Need { get; }

        // Refills it; false if there's nothing to refill (no horse, say).
        protected abstract bool Refill();

        // For the log, before and after a refill.
        protected abstract string Describe();

        protected override string Description => $"Once: press Enter to fill {Need.ToLowerInvariant()} now. Always: keeps it full.";

        protected override string[] Choices => OnceOrAlways;

        protected override bool RememberChoice => true;

        protected override bool CanUse => true;

        protected override int OffChoice => Once;

        protected override string Apply(int index)
        {
            return index == Always ? $"{Need}: always full" : $"{Need}: press Enter to refill";
        }

        protected override string Use()
        {
            string before = Describe();
            if (!Refill())
            {
                return $"Nothing to refill ({before})";
            }
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
}
