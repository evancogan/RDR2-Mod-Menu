using System;
using System.Collections.Generic;

namespace RDR2ModMenu
{
    // Speech row: "Ban a Line  < Latest >". Left/Right go back through the last few subtitles the game showed, the
    // description shows the chosen one, and Enter bans it (or unbans it, if it's already banned). Banned lines are only
    // cut off while Skip Banned Lines is on.
    public class BanALine : ModChoice
    {
        private static readonly string[] Ages = { "Latest", "2nd latest", "3rd latest", "4th latest", "5th latest", "6th latest", "7th latest", "8th latest" };

        protected override string Category => "Speech";

        protected override string DisplayName => "Ban a Line";

        protected override string[] Choices => Ages;

        protected override bool CanUse => true;

        protected override string Description
        {
            get
            {
                if (SpeechLines.Problem != null)
                {
                    return SpeechLines.Problem;
                }
                SpeechLines.Heard heard = Chosen();
                if (heard == null)
                {
                    return "The last subtitles show here, newest first. Talk to someone, then come back.";
                }
                int seconds = (Environment.TickCount - heard.At) / 1000;
                string action = SpeechLines.IsBanned(heard.Text) ? "Banned. Enter unbans it." : "Enter bans it.";
                return $"\"{heard.Text}\", {seconds} s ago. {action}";
            }
        }

        protected override string Apply(int index) => null;

        protected override string Use()
        {
            SpeechLines.Heard heard = Chosen();
            if (heard == null)
            {
                return "No line to ban yet";
            }
            return SpeechLines.ToggleBan(heard.Text) ? $"Banned: \"{heard.Text}\"" : $"Unbanned: \"{heard.Text}\"";
        }

        private SpeechLines.Heard Chosen()
        {
            List<SpeechLines.Heard> recent = SpeechLines.Recent();
            return Current < recent.Count ? recent[Current] : null;
        }
    }
}
