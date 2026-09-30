namespace RDR2ModMenu
{
    // Speech mod: cuts off the lines on the no-list (see SpeechLines) as soon as their subtitle shows. The list is made
    // with the Ban a Line row and edited with Banned Lines.
    public class SkipBannedLines : ModScript
    {
        protected override string Category => "Speech";

        protected override string Description => "Cuts off the lines you've banned (Ban a Line) as soon as their subtitle shows.";

        protected override bool OnEnable()
        {
            SpeechLines.SkipBanned = true;
            return true;
        }

        protected override void OnDisable()
        {
            SpeechLines.SkipBanned = false;
        }

        protected override void OnEnabledTick()
        {
        }
    }
}
