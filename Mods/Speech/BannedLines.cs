using System;
using System.Collections.Generic;

namespace RDR2ModMenu
{
    // Speech row: "Banned Lines  < 1 of 3 >". Left/Right go through the no-list, the description shows each line, and
    // Enter unbans it.
    public class BannedLines : ModChoice
    {
        protected override string Category => "Speech";

        protected override string[] Choices => SpeechLines.BannedChoices;

        protected override bool CanUse => true;

        protected override int LiveChoice => Math.Min(Current, Choices.Length - 1);

        protected override string Description
        {
            get
            {
                List<string> banned = SpeechLines.Banned();
                return banned.Count == 0 ? "No lines banned yet. Use Ban a Line right after hearing one." : $"\"{banned[LiveChoice]}\". Enter unbans it.";
            }
        }

        protected override string Apply(int index) => null;

        protected override string Use()
        {
            List<string> banned = SpeechLines.Banned();
            if (banned.Count == 0)
            {
                return "No lines banned";
            }
            string text = banned[LiveChoice];
            SpeechLines.ToggleBan(text);
            return $"Unbanned: \"{text}\"";
        }
    }
}
