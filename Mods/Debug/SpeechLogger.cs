using System;
using System.Collections.Generic;
using System.Linq;
using RDR2;
using RDR2.Native;

namespace RDR2ModMenu
{
    // Debug mod behind Silence Goodbyes: logs every line Arthur and the people near him speak, numbered, with its kind,
    // how long it lasted, who Arthur was focused on and whether a conversation was running. Arthur's latest line number
    // is shown on screen, so a line heard in-game can be matched to its log entry (Mark Goodbye marks it).
    //
    // No native tells us which line is playing, only whether someone is speaking and whether it's ambient speech (the
    // game picks a random take from a named speech context). Speech that isn't ambient is most likely scripted (part of
    // a written conversation). IS_SCRIPTED_SPEECH_PLAYING isn't used: its argument is undocumented, and passing it a ped
    // crashed Script Hook.
    public class SpeechLogger : ModScript
    {
        protected override string Category => "Debug";

        protected override string Description => "Logs every line Arthur and people near him speak to RDR2ModMenu.log, numbered. Arthur's latest line is shown top right.";

        // How far away other speakers are logged.
        private const float Radius = 15f;

        // Latest line Arthur started, for the Mark Goodbye button (same DLL, so the static is shared).
        public static int LastArthurLine { get; private set; }
        public static string LastArthurSummary { get; private set; } = "none yet";

        private sealed class Speaker
        {
            public int Line;
            public int StartTime;
            public bool Ambient;
        }

        private readonly Dictionary<int, Speaker> speaking = new Dictionary<int, Speaker>();
        private readonly NearbyPeds nearby = new NearbyPeds();

        // Reused every frame: who's been checked this frame, and who stopped.
        private readonly HashSet<int> seen = new HashSet<int>();
        private readonly List<int> gone = new List<int>();
        private int lineCount;
        private int lastTarget;
        private bool conversationPlaying;
        private bool arthurInConversation;
        private string onScreen = "Speech log: no lines yet";

        protected override bool OnEnable()
        {
            Log.Write("===== Speech log started =====");
            return true;
        }

        protected override void OnEnabledTick()
        {
            Ped player = Game.Player.Character;
            int now = Environment.TickCount;

            LogConversationChanges(player);

            seen.Clear();
            Check(player, now);
            foreach (int handle in nearby.Find(player.Position, Radius))
            {
                if (handle != player.Handle && PED.IS_PED_HUMAN(handle))
                {
                    Check(new Ped(handle), now);
                }
            }

            // Anyone who was speaking but has gone out of range or despawned.
            gone.Clear();
            gone.AddRange(speaking.Keys.Where(handle => !seen.Contains(handle)));
            foreach (int handle in gone)
            {
                End(handle, "out of range", now);
            }

            ScreenText.Draw(onScreen, 0.7f, 0.04f, 0.35f, 255, 255, 255);
        }

        private void Check(Ped ped, int now)
        {
            int handle = ped.Handle;
            bool ambient = AUDIO.IS_AMBIENT_SPEECH_PLAYING(handle);
            bool isSpeaking = ambient || AUDIO.IS_ANY_SPEECH_PLAYING(handle);

            Speaker speaker;
            if (!speaking.TryGetValue(handle, out speaker))
            {
                if (isSpeaking)
                {
                    speaker = new Speaker { Line = ++lineCount, StartTime = now };
                    speaking[handle] = speaker;
                    speaker.Ambient |= ambient;
                    bool isArthur = handle == Game.Player.Character.Handle;
                    Log.Write($"#{speaker.Line} START {SpeakerName(ped)}: {Kind(ambient)}, {Distance(ped)}, Arthur focused on {Target()}, conversation playing {conversationPlaying}");
                    if (isArthur)
                    {
                        LastArthurLine = speaker.Line;
                        LastArthurSummary = $"#{speaker.Line}, {Kind(ambient)}, still playing";
                        onScreen = $"Speech log: Arthur line #{speaker.Line} ({Kind(ambient)})";
                    }
                    seen.Add(handle);
                }
                return;
            }

            seen.Add(handle);
            if (isSpeaking)
            {
                // Ambient can register a frame or two after the line starts, so keep checking.
                speaker.Ambient |= ambient;
            }
            else
            {
                End(handle, "finished", now);
            }
        }

        private void End(int handle, string why, int now)
        {
            Speaker speaker = speaking[handle];
            speaking.Remove(handle);
            float seconds = (now - speaker.StartTime) / 1000f;
            string kinds = Kind(speaker.Ambient);
            bool isArthur = handle == Game.Player.Character.Handle;
            Log.Write($"#{speaker.Line} END {(isArthur ? "ARTHUR" : $"ped {handle}")} ({why}) after {seconds:F2} s, kind: {kinds}");
            if (isArthur)
            {
                LastArthurSummary = $"#{speaker.Line}, {kinds}, {seconds:F1} s";
                onScreen = $"Speech log: Arthur line {LastArthurSummary}";
            }
        }

        // Logs when Arthur's focus target changes and when a conversation starts or stops.
        private void LogConversationChanges(Ped player)
        {
            int target = Natives.GetInteractionTarget();
            if (target != lastTarget)
            {
                Log.Write($"Arthur's focus: {(lastTarget == 0 ? "nobody" : $"ped {lastTarget}")} -> {Target()}");
                lastTarget = target;
            }

            bool playing = AUDIO._IS_ANY_CONVERSATION_PLAYING(false);
            if (playing != conversationPlaying)
            {
                Log.Write($"Scripted conversation {(playing ? "STARTED" : "ENDED")}");
                conversationPlaying = playing;
            }

            bool inConversation = AUDIO._IS_PED_IN_ANY_CONVERSATION(player.Handle, false);
            if (inConversation != arthurInConversation)
            {
                Log.Write($"Arthur {(inConversation ? "JOINED" : "LEFT")} a conversation");
                arthurInConversation = inConversation;
            }
        }

        private string Target()
        {
            int target = Natives.GetInteractionTarget();
            if (target == 0)
            {
                return "nobody";
            }
            return $"ped {target} (model {ENTITY.GET_ENTITY_MODEL(target):X8})";
        }

        private static string SpeakerName(Ped ped)
        {
            if (ped.Handle == Game.Player.Character.Handle)
            {
                return "ARTHUR";
            }
            return $"ped {ped.Handle} (model {(uint)ped.Model.Hash:X8})";
        }

        private static string Distance(Ped ped)
        {
            if (ped.Handle == Game.Player.Character.Handle)
            {
                return "0 m";
            }
            return $"{ped.Position.DistanceTo(Game.Player.Character.Position):F1} m away";
        }

        private static string Kind(bool ambient) => ambient ? "ambient" : "not ambient (likely scripted)";

        protected override void OnDisable()
        {
            speaking.Clear();
            nearby.Dispose();
            Log.Write("===== Speech log stopped =====");
        }
    }
}
