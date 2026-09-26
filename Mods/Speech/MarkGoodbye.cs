namespace RDR2ModMenu
{
    // Speech button: marks Arthur's latest line in the speech log as the goodbye, so it can be found afterwards.
    public class MarkGoodbye : ModAction
    {
        protected override string Category => "Speech";

        protected override string Description => "Use right after Arthur says goodbye: marks his latest line in the speech log as the goodbye.";

        protected override string Run()
        {
            if (SpeechLogger.LastArthurLine == 0)
            {
                return "No Arthur lines logged yet. Turn on Speech Logger first.";
            }
            Log.Write($"===== MARK: goodbye is Arthur line {SpeechLogger.LastArthurSummary} =====");
            return $"Marked Arthur line {SpeechLogger.LastArthurSummary} as the goodbye";
        }
    }
}
