using System;

namespace RDR2ModMenu
{
    // Debug mod, read only: shows Arthur's honor (see Honor) at the top right and logs every change. It's how the honor
    // address was checked against the in-game meter before Set Honor ever wrote to it.
    public class HonorWatch : ModScript
    {
        protected override string Category => "Debug";

        protected override string Description => "Shows your honor at the top right and logs every change. Read only.";

        private const int CheckIntervalMs = 250;

        private int nextCheck;
        private int? lastHonor;
        private string onScreen = "Honor: ?";

        protected override void OnEnabledTick()
        {
            int now = Environment.TickCount;
            if (now >= nextCheck)
            {
                nextCheck = now + CheckIntervalMs;
                int honor = Honor.Value;
                if (honor != lastHonor)
                {
                    Log.Write(lastHonor.HasValue ? $"Honor {lastHonor} -> {honor} ({honor - lastHonor:+0;-0})" : $"Honor is {honor}");
                    lastHonor = honor;
                    onScreen = $"Honor: {honor} (range -{Honor.Limit} to {Honor.Limit})";
                }
            }
            ScreenText.Draw(onScreen, 0.7f, 0.07f, 0.35f, 255, 255, 255);
        }

        protected override void OnDisable()
        {
            lastHonor = null;
            nextCheck = 0;
        }
    }
}
