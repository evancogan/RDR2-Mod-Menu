using System;
using RDR2;
using RDR2.Native;

namespace RDR2ModMenu
{
    // Speech row: "Silence Goodbyes  < Off | Arthur | Arthur + camp >". Stops Arthur's goodbye at the end of a
    // conversation ("Okay, I'll catch you later then" and the rest), and optionally the other person's reply.
    //
    // How the goodbye is recognized, found with Speech Logger. Talking to someone goes greeting, second line, farewell
    // (the game's GREET_<NAME>, ..._SECOND_..., ..._THIRD_FAREWELL_... speech), with them answering each one. So a line
    // of Arthur's is the goodbye when either:
    // - it's his third line to the same person (every marked goodbye was), or
    // - he speaks twice in a row: it starts within GoodbyeGapMs of his previous line ending, before they've said anything
    //   (how the first two marked goodbyes came, 0.33 and 0.54 s after his line; ordinary next lines came 1.7 s or more
    //   after, once they'd replied).
    // The count starts again when he turns to someone else or the conversation goes quiet for ConversationTimeoutMs.
    // Only ambient speech (the game's picked-at-random lines) counts as a goodbye; scripted lines, like mission dialogue,
    // are left alone. A third line picked with Antagonize would be stopped too; none has been checked.
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

        // The goodbye is his third line to the same person.
        private const int GoodbyeLine = 3;

        // A pause this long between Arthur's lines starts a new conversation.
        private const int ConversationTimeoutMs = 15000;

        // A line can take a frame or two to register as ambient, so a goodbye waits this long for it before being left
        // alone as scripted.
        private const int AmbientCheckMs = 200;

        // How long after a stopped goodbye the other person's reply is also stopped, on Arthur + camp.
        private const int ReplyWindowMs = 4000;

        protected override string Category => "Speech";

        protected override string Description => "Stops Arthur's goodbye at the end of a conversation (\"Okay, I'll catch you later then\"). Arthur + camp also stops their reply.";

        private static readonly string[] Modes = { "Off", "Arthur", "Arthur + camp" };

        protected override string[] Choices => Modes;

        protected override int OffChoice => Off;

        protected override bool RememberChoice => true;

        private bool arthurWasSpeaking;
        private int arthurEndedAt;

        // Who Arthur was focused on when his last line ended, and whether they've spoken since.
        private int partner;
        private bool partnerReplied;

        // Who Arthur is talking to, how many lines he's said to them, and when his last line started.
        private int conversationPartner;
        private int arthurLines;
        private int lastArthurLineAt;

        // A line that looks like a goodbye, waiting to register as ambient before it's stopped.
        private string pendingReason;
        private int pendingUntil;
        private int pendingReplyPed;

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
                conversationPartner = 0;
                arthurLines = 0;
                pendingReason = null;
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
                int line = CountLine(now);
                int gap = now - arthurEndedAt;
                string reason = null;
                if (partner != 0 && !partnerReplied && gap <= GoodbyeGapMs)
                {
                    reason = $"Arthur spoke again {gap} ms after his last line with no reply from ped {partner}";
                }
                else if (conversationPartner != 0 && line == GoodbyeLine)
                {
                    reason = $"Arthur's line {GoodbyeLine} to ped {conversationPartner}";
                }

                if (reason != null)
                {
                    pendingReason = reason;
                    pendingUntil = now + AmbientCheckMs;
                    pendingReplyPed = partner != 0 ? partner : conversationPartner;
                    arthurLines = 0;
                }
            }

            if (pendingReason != null)
            {
                if (speaking && AUDIO.IS_AMBIENT_SPEECH_PLAYING(player))
                {
                    Log.Write($"Goodbye: {pendingReason}. Stopping it.");
                    stoppingGoodbye = true;
                    goodbyeStartedAt = now;
                    if (Current == ArthurAndCamp)
                    {
                        replyPed = pendingReplyPed;
                        stopReplyUntil = now + ReplyWindowMs;
                        replyStopped = false;
                    }
                    pendingReason = null;
                }
                else if (!speaking || now > pendingUntil)
                {
                    Log.Write($"Looked like a goodbye ({pendingReason}) but isn't ambient speech; left alone");
                    pendingReason = null;
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

        // Counts the line Arthur just started toward his current conversation, starting a new one when he's turned to
        // someone else or it's been quiet a while. Focus can flicker off the person mid-conversation, so an empty focus
        // keeps the current one. Returns which line this is to them (0 when he isn't talking to anyone).
        private int CountLine(int now)
        {
            int target = Natives.GetInteractionTarget();
            if (!IsPed(target))
            {
                target = 0;
            }
            bool quiet = now - lastArthurLineAt > ConversationTimeoutMs;
            if ((target != 0 && target != conversationPartner) || quiet)
            {
                conversationPartner = target;
                arthurLines = 0;
            }
            lastArthurLineAt = now;
            if (conversationPartner == 0 || !IsPed(conversationPartner))
            {
                conversationPartner = 0;
                return 0;
            }
            return ++arthurLines;
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
