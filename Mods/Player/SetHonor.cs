using System;
using System.Linq;
using RDR2.Native;

namespace RDR2ModMenu
{
    // Player row: "Set Honor  < -35 >". Shows Arthur's honor as it is now (see Honor); Left/Right rolls it down or up by
    // 5 at once, and holding the key keeps it rolling. It stops at the range the story allows (the help text shows it),
    // since the game would clamp anything further at its next honor change anyway.
    //
    // Writing the value changes honor for real (the game's next honor change counts from it), and the HONOR_CURRENT stat
    // is set to match, as the game's own honor function does. The on-screen honor meter isn't updated: showing it and
    // writing its rank the way the game does still left the needle at an old level, so it isn't shown at all. It catches
    // up at the game's next honor change.
    public class SetHonor : ModChoice
    {
        protected override string Category => "Player";

        protected override string Description => $"Your honor. Left/Right rolls it down or up by {Step}; hold to keep rolling. The story currently allows -{Honor.Limit} to {Honor.Limit}.";

        // Every whole value honor can take, so the row can show it exactly.
        private const int Lowest = -320;
        private const int Highest = 320;
        private static readonly string[] Values = Enumerable.Range(Lowest, Highest - Lowest + 1).Select(v => v.ToString()).ToArray();

        private const int Step = 5;

        private static readonly uint HonorCurrentStat = Natives.Hash("HONOR_CURRENT");

        protected override string[] Choices => Values;

        protected override int StepSize => Step;

        protected override bool Wraps => false;

        protected override int InitialChoice => -Lowest;

        protected override int LiveChoice => Math.Max(Lowest, Math.Min(Highest, Honor.Value)) - Lowest;

        protected override string Apply(int index)
        {
            int before = Honor.Value;
            int limit = Honor.Limit;
            int asked = index + Lowest;
            int wanted = Math.Max(-limit, Math.Min(limit, asked));
            Honor.Value = wanted;
            SetHonorStat(wanted);

            Log.Write($"Honor set: {before} -> {wanted} (asked for {asked}, story limit {limit})");
            return wanted == asked ? $"Honor: {wanted}" : $"Honor: {wanted} (the story's limit for now)";
        }

        // The HONOR_CURRENT stat, which the game's honor function keeps equal to honor.
        private static unsafe void SetHonorStat(int honor)
        {
            ulong* statId = stackalloc ulong[2];
            statId[0] = HonorCurrentStat;
            statId[1] = 0;
            STATS.STAT_ID_SET_INT(statId, honor, true);
        }
    }
}
