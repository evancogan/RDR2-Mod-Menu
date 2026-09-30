using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RDR2;
using RDR2.Native;

namespace RDR2ModMenu
{
    // Behind the Speech section, not a menu row itself. Every frame it reads the subtitles the game has just shown
    // (SubtitleReader), and:
    // - logs each one;
    // - keeps the last RecentCount for Ban a Line;
    // - while Skip Banned Lines is on, cuts off a line whose subtitle is on the no-list as soon as it appears.
    //
    // A line is known by its subtitle text, exactly as the game shows it, so what's banned is what you saw and heard.
    // The no-list is RDR2ModMenu.banned.txt next to RDR2.exe, one subtitle per line.
    public class SpeechLines : Script
    {
        public const int RecentCount = 8;

        // How far away someone saying a banned line is cut off, when it isn't Arthur.
        private const float NearbyRadius = 12f;

        // The subtitle can show a moment before the voice starts. Stop waiting for it after this long.
        private const int StopWaitMs = 1500;

        private static readonly string BannedPath = GameFolder.File("RDR2ModMenu.banned.txt");

        internal sealed class Heard
        {
            public string Text;
            public int At;
        }

        // Scripts can run on different threads, so the lists the menu rows read are locked.
        private static readonly object Sync = new object();
        private static readonly List<Heard> recent = new List<Heard>();
        private static readonly List<string> banned = new List<string>();
        private static string[] bannedChoices = { "None" };

        // Why subtitles can't be read, or null when they can.
        public static string Problem { get; private set; } = "still starting up";

        // Set by the Skip Banned Lines row.
        public static volatile bool SkipBanned;

        private readonly List<string> incoming = new List<string>();
        private readonly NearbyPeds nearby = new NearbyPeds();

        // A banned line is being cut off.
        private bool stopping;
        private bool stopHeard;
        private int stopSince;

        public SpeechLines()
        {
            LoadBanned();
            string problem = SubtitleReader.Find();
            Problem = problem == null ? null : $"Can't read subtitles: {problem}.";
            Log.Write(problem == null ? "Reading subtitles" : $"Subtitles NOT readable: {problem}");

            Tick += OnTick;
            Aborted += (sender, e) => nearby.Dispose();
        }

        // The latest subtitles, newest first.
        internal static List<Heard> Recent()
        {
            lock (Sync)
            {
                return recent.ToList();
            }
        }

        // The no-list, oldest ban first.
        internal static List<string> Banned()
        {
            lock (Sync)
            {
                return banned.ToList();
            }
        }

        // "1 of 3", "2 of 3"... for the Banned Lines row, or "None". A new array only when the list changes.
        public static string[] BannedChoices
        {
            get
            {
                lock (Sync)
                {
                    return bannedChoices;
                }
            }
        }

        internal static bool IsBanned(string text)
        {
            lock (Sync)
            {
                return banned.Contains(text);
            }
        }

        // Bans the line, or unbans it if it's banned. Returns true if it's now banned.
        internal static bool ToggleBan(string text)
        {
            bool nowBanned;
            lock (Sync)
            {
                nowBanned = !banned.Remove(text);
                if (nowBanned)
                {
                    banned.Add(text);
                }
                UpdateBannedChoices();
            }
            SaveBanned();
            Log.Write($"{(nowBanned ? "Banned" : "Unbanned")} \"{text}\"");
            return nowBanned;
        }

        private void OnTick(object sender, EventArgs e)
        {
            int now = Environment.TickCount;
            incoming.Clear();
            SubtitleReader.ReadNew(incoming);
            foreach (string text in incoming)
            {
                OnSubtitle(text, now);
            }
            if (stopping)
            {
                Stop(now);
            }
        }

        private void OnSubtitle(string text, int now)
        {
            lock (Sync)
            {
                recent.Insert(0, new Heard { Text = text, At = now });
                if (recent.Count > RecentCount)
                {
                    recent.RemoveAt(recent.Count - 1);
                }
            }

            if (SkipBanned && IsBanned(text))
            {
                Log.Write($"Subtitle: \"{text}\" is banned, cutting it off");
                stopping = true;
                stopHeard = false;
                stopSince = now;
            }
            else
            {
                Log.Write($"Subtitle: \"{text}\"");
            }
        }

        // Cuts off Arthur if he's the one speaking, otherwise whoever nearby is, until the line has been cut or it's
        // clear it isn't coming.
        private void Stop(int now)
        {
            Ped player = Game.Player.Character;
            var speakers = new List<int>();
            if (IsSpeaking(player.Handle))
            {
                speakers.Add(player.Handle);
            }
            else
            {
                speakers.AddRange(nearby.Find(player.Position, NearbyRadius).Where(ped => ped != player.Handle && IsSpeaking(ped)));
            }

            if (speakers.Count > 0)
            {
                if (!stopHeard)
                {
                    stopHeard = true;
                    Log.Write($"Cut off {(speakers[0] == player.Handle ? "Arthur" : $"{speakers.Count} nearby")} {now - stopSince} ms after the subtitle showed");
                }
                foreach (int ped in speakers)
                {
                    AUDIO.STOP_CURRENT_PLAYING_AMBIENT_SPEECH(ped, 0);
                }
            }
            else if (stopHeard || now - stopSince > StopWaitMs)
            {
                if (!stopHeard)
                {
                    Log.Write("Nobody was saying the banned line");
                }
                stopping = false;
            }
        }

        private static bool IsSpeaking(int ped)
        {
            return ENTITY.DOES_ENTITY_EXIST(ped) && (AUDIO.IS_AMBIENT_SPEECH_PLAYING(ped) || AUDIO.IS_ANY_SPEECH_PLAYING(ped));
        }

        private static void UpdateBannedChoices()
        {
            bannedChoices = banned.Count == 0 ? new[] { "None" } : banned.Select((text, i) => $"{i + 1} of {banned.Count}").ToArray();
        }

        private static void LoadBanned()
        {
            lock (Sync)
            {
                banned.Clear();
                try
                {
                    if (File.Exists(BannedPath))
                    {
                        foreach (string line in File.ReadAllLines(BannedPath))
                        {
                            string text = line.Trim();
                            if (text.Length > 0 && !text.StartsWith("#", StringComparison.Ordinal) && !banned.Contains(text))
                            {
                                banned.Add(text);
                            }
                        }
                    }
                }
                catch (IOException ex)
                {
                    Log.Write($"Couldn't read the banned lines: {ex.Message}");
                }
                UpdateBannedChoices();
                Log.Write($"{banned.Count} banned lines");
            }
        }

        private static void SaveBanned()
        {
            var lines = new List<string>
            {
                "# Lines Skip Banned Lines cuts off, one subtitle per line, exactly as the game shows it.",
                "# Ban and unban them in the mod menu (Speech > Ban a Line, Banned Lines).",
            };
            lines.AddRange(Banned());
            try
            {
                File.WriteAllLines(BannedPath, lines);
            }
            catch (IOException ex)
            {
                Log.Write($"Couldn't save the banned lines: {ex.Message}");
            }
        }
    }
}
