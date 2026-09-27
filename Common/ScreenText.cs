using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using RDR2.Native;

namespace RDR2ModMenu
{
    // Draws text with the same game natives as V2's Drawing.DrawText, but from string buffers we own.
    // V2 hands strings to the game in temporary buffers that are freed right after the call, while the game
    // only reads them when it renders the frame, which makes the text flicker.
    public static class ScreenText
    {
        private const ulong VAR_STRING = 0xFA925AC00EB830B9;
        private const ulong BG_SET_TEXT_SCALE = 0xA1253A3C870B6843;
        private const ulong BG_SET_TEXT_COLOR = 0x16FA5CE47F184F1E;
        private const ulong BG_DISPLAY_TEXT = 0x16794E044C9EFB58;

        // Approximate width of one character per unit of text scale, as a fraction of the screen width.
        // Measured in-game at 1920 wide: 75 characters at scale 0.3 spanned 583 px, about 0.0135 per scale.
        // The game has no text-width native we can use, so this is an average with headroom for wide letters.
        private const float CharWidthPerScale = 0.015f;

        // One buffer per distinct string drawn. A buffer not drawn for UnusedMs is freed (it can't still be waiting to
        // render by then), so text that keeps changing, like a live number, doesn't pile up. The rest are freed when the
        // script unloads (FreeAll).
        private const int UnusedMs = 2000;
        private const int SweepIntervalMs = 1000;

        private sealed class Buffer
        {
            public IntPtr Pointer;
            public int LastDrawn;
        }

        private static readonly Dictionary<string, Buffer> Buffers = new Dictionary<string, Buffer>();
        private static int nextSweep;

        public static unsafe void Draw(string text, float x, float y, float scale, int r, int g, int b, int a = 255)
        {
            int now = Environment.TickCount;
            ulong* literal = (ulong*)GetBuffer("LITERAL_STRING", now);
            ulong* content = (ulong*)GetBuffer(text, now);
            ulong varString = Function.Call<ulong>(VAR_STRING, 10, literal, content);

            Function.Call(BG_SET_TEXT_SCALE, scale, scale);
            Function.Call(BG_SET_TEXT_COLOR, r, g, b, a);
            Function.Call(BG_DISPLAY_TEXT, (ulong*)varString, x, y);
            FreeUnused(now);
        }

        // Roughly how wide text of this many characters is at the given scale, as a fraction of the screen width, erring
        // on the wide side.
        public static float EstimateWidth(int characters, float scale) => characters * CharWidthPerScale * scale;

        // Splits text into lines that fit within maxWidth (a fraction of the screen width) at the given scale.
        public static List<string> Wrap(string text, float maxWidth, float scale)
        {
            int maxChars = Math.Max(1, (int)(maxWidth / (CharWidthPerScale * scale)));
            var lines = new List<string>();
            var line = new StringBuilder();

            foreach (string word in text.Split(' '))
            {
                if (line.Length > 0 && line.Length + 1 + word.Length > maxChars)
                {
                    lines.Add(line.ToString());
                    line.Clear();
                }
                if (line.Length > 0)
                {
                    line.Append(' ');
                }
                line.Append(word);
            }
            if (line.Length > 0)
            {
                lines.Add(line.ToString());
            }
            return lines;
        }

        // Call when the script unloads.
        public static void FreeAll()
        {
            foreach (Buffer buffer in Buffers.Values)
            {
                Marshal.FreeHGlobal(buffer.Pointer);
            }
            Buffers.Clear();
        }

        private static IntPtr GetBuffer(string text, int now)
        {
            Buffer buffer;
            if (!Buffers.TryGetValue(text, out buffer))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(text + "\0");
                buffer = new Buffer { Pointer = Marshal.AllocHGlobal(bytes.Length) };
                Marshal.Copy(bytes, 0, buffer.Pointer, bytes.Length);
                Buffers[text] = buffer;
            }
            buffer.LastDrawn = now;
            return buffer.Pointer;
        }

        private static void FreeUnused(int now)
        {
            if (now < nextSweep)
            {
                return;
            }
            nextSweep = now + SweepIntervalMs;
            foreach (string text in Buffers.Where(entry => now - entry.Value.LastDrawn > UnusedMs).Select(entry => entry.Key).ToList())
            {
                Marshal.FreeHGlobal(Buffers[text].Pointer);
                Buffers.Remove(text);
            }
        }
    }
}
