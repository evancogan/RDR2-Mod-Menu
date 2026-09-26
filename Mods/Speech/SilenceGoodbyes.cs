using System;
using RDR2;
using RDR2.Native;

namespace RDR2ModMenu
{
    // Speech row: "Silence Goodbyes  < Off | Arthur | Arthur + camp >". Stops Arthur's goodbye at the end of a
    // conversation ("Okay, I'll catch you later then" and the rest), and optionally the other person's reply.
    //
    // How the goodbye is recognized, found with Speech Logger: in a conversation Arthur and the other person take
    // turns, and his next line comes about 2 s or more after his last, once they've replied. The goodbye is the one
    // time he speaks twice in a row: it starts about half a second after his previous line ends, before they've said
    // anything. Then they answer it. This matched both goodbyes that were marked, and none of the other lines logged.
    //
    // Blocking the goodbye's speech contexts (GREET_<NAME>_THIRD_FAREWELL_... with _BLOCK_SPEECH_CONTEXT) was tried
    // first and made no difference, so instead the goodbye is stopped the moment it starts. The first few hundredths of
    // a second may still be heard.
    public class SilenceGoodbyes : ModChoice
    {
        private const int Off = 0;
        private const int ArthurAndCamp = 2;

        // A line of Arthur's starting this soon after his last one, with no reply in between, is the goodbye. Logged
        // goodbyes started 0.33 and 0.54 s after his previous line; his ordinary next lines, 1.7 s or more.
        private const int GoodbyeGapMs = 1000;

        // How long after a stopped goodbye the other person's reply is also stopped, on Arthur + camp.
        private const int ReplyWindowMs = 4000;

        protected override string Category => "Speech";

        protected override string Description => "Stops Arthur's goodbye at the end of a conversation (\"Okay, I'll catch you later then\"). Arthur + camp also stops their reply.";

        protected override string[] Choices => new[] { "Off", "Arthur", "Arthur + camp" };

        protected override bool RememberChoice => true;

        private bool arthurWasSpeaking;
        private int arthurEndedAt;

        // Who Arthur was focused on when his last line ended, and whether they've spoken since.
        private int partner;
        private bool partnerReplied;

        // Arthur's goodbye is being stopped (it can take a frame to go quiet).
        private bool stoppingGoodbye;
        private int goodbyeStartedAt;

        private int replyPed;
        private int stopReplyUntil;
        private bool replyStopped;

        protected override string Apply(int index)
        {
            return index == Off ? "Goodbyes: normal" : index == ArthurAndCamp ? "Goodbyes: Arthur's and their replies stopped" : "Goodbyes: Arthur's stopped";
        }

        protected override void OnChoiceTick()
        {
            if (Current == Off)
            {
                arthurWasSpeaking = false;
                partner = 0;
                replyPed = 0;
                return;
            }

            int now = Environment.TickCount;
            int player = Game.Player.Character.Handle;
            bool speaking = IsSpeaking(player);

            // Only the moment right after Arthur's line matters. Letting go of the partner after that also means never
            // asking about someone the game has since unloaded (leaving camp), whose ID may belong to something else.
            if (partner != 0 && !stoppingGoodbye && now - arthurEndedAt > GoodbyeGapMs)
            {
                partner = 0;
            }
            if (partner != 0 && !partnerReplied && IsSpeaking(partner))
            {
                partnerReplied = true;
            }

            if (speaking && !arthurWasSpeaking)
            {
                int gap = now - arthurEndedAt;
                if (partner != 0 && !partnerReplied && gap <= GoodbyeGapMs)
                {
                    Log.Write($"Goodbye: Arthur spoke again {gap} ms after his last line with no reply from ped {partner}. Stopping it.");
                    stoppingGoodbye = true;
                    goodbyeStartedAt = now;
                    if (Current == ArthurAndCamp)
                    {
                        replyPed = partner;
                        stopReplyUntil = now + ReplyWindowMs;
                        replyStopped = false;
                    }
                }
            }

            if (stoppingGoodbye && speaking)
            {
                AUDIO.STOP_CURRENT_PLAYING_AMBIENT_SPEECH(player, 0);
            }

            if (!speaking && arthurWasSpeaking)
            {
                if (stoppingGoodbye)
                {
                    // Keep the same partner, so a retry of the goodbye is caught too.
                    stoppingGoodbye = false;
                    Log.Write($"Goodbye stopped after {now - goodbyeStartedAt} ms");
                }
                else
                {
                    // Focus can also land on a horse or an object; only people answer.
                    partner = Natives.GetInteractionTarget();
                    if (!IsPed(partner))
                    {
                        partner = 0;
                    }
                    partnerReplied = partner != 0 && IsSpeaking(partner);
                }
                arthurEndedAt = now;
            }
            arthurWasSpeaking = speaking;

            StopReply(now);
        }

        private void StopReply(int now)
        {
            if (replyPed == 0)
            {
                return;
            }
            if (now > stopReplyUntil)
            {
                replyPed = 0;
                return;
            }
            if (!IsPed(replyPed))
            {
                replyPed = 0;
                return;
            }
            if (IsSpeaking(replyPed))
            {
                if (!replyStopped)
                {
                    replyStopped = true;
                    Log.Write($"Stopping ped {replyPed}'s reply to the goodbye");
                }
                AUDIO.STOP_CURRENT_PLAYING_AMBIENT_SPEECH(replyPed, 0);
            }
        }

        // Checks the ped still exists first: speech natives are only asked about peds.
        private static bool IsSpeaking(int ped)
        {
            return IsPed(ped) && (AUDIO.IS_AMBIENT_SPEECH_PLAYING(ped) || AUDIO.IS_ANY_SPEECH_PLAYING(ped));
        }

        private static bool IsPed(int entity)
        {
            return entity != 0 && ENTITY.DOES_ENTITY_EXIST(entity) && ENTITY.IS_ENTITY_A_PED(entity);
        }
    }
}
