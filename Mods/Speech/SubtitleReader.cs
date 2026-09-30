using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace RDR2ModMenu
{
    // Reads the subtitles the game shows, straight from its memory, without changing anything.
    //
    // The game keeps its subtitles in a list of Slots entries, each starting with a pointer to its text (spoken lines are
    // marked with "~z~"). The way to find the list comes from Rdr2TcpSubtitles (github.com/Kanawanagasaki/Rdr2TcpSubtitles):
    // search the game's code for Pattern, an instruction sequence that loads the list's address, so it doesn't depend on
    // one game version. A new subtitle shows up as an entry whose text pointer changed.
    //
    // Every text pointer is checked with Windows (VirtualQuery) before it's read, so a stale one can't crash the game.
    internal static unsafe class SubtitleReader
    {
        private const int Slots = 512;
        private const int SlotSize = 0x30;
        private const int MaxTextBytes = 1024;

        // mov ecx, [rip+?]; lea rdi, [rip+list]; movsxd rax, [rip+?]; cmp ecx, eax; jb. -1 matches any byte.
        private static readonly int[] Pattern = { 0x8B, 0x0D, -1, -1, -1, -1, 0x48, 0x8D, 0x3D, -1, -1, -1, -1, 0x48, 0x63, 0x05, -1, -1, -1, -1, 0x3B, 0xC8, 0x72 };

        // The game's formatting codes, like ~z~.
        private static readonly Regex Codes = new Regex("~[^~]*~");

        private static byte* list;
        private static readonly IntPtr[] seen = new IntPtr[Slots];

        // Looks for the list. Returns null if it was found, otherwise why not.
        public static string Find()
        {
            byte* exe = (byte*)GetModuleHandleW(null);
            byte* nt = exe + *(int*)(exe + 0x3C);
            int sectionCount = *(ushort*)(nt + 6);
            byte* section = nt + 24 + *(ushort*)(nt + 20);
            for (int s = 0; s < sectionCount && list == null; s++, section += 40)
            {
                const uint Executable = 0x20000000;
                if ((*(uint*)(section + 36) & Executable) == 0)
                {
                    continue;
                }
                uint size = Math.Max(*(uint*)(section + 8), *(uint*)(section + 16));
                byte* match = Search(exe + *(uint*)(section + 12), size);
                if (match != null)
                {
                    list = match + 13 + *(int*)(match + 9);
                }
            }
            if (list == null)
            {
                return "couldn't find the game's subtitles";
            }
            for (int i = 0; i < Slots; i++)
            {
                seen[i] = TextPointer(i);
            }
            return null;
        }

        // Adds the text of every spoken subtitle that appeared since the last call to lines, without the game's formatting
        // codes.
        public static void ReadNew(List<string> lines)
        {
            if (list == null)
            {
                return;
            }
            for (int i = 0; i < Slots; i++)
            {
                IntPtr text = TextPointer(i);
                if (text == seen[i])
                {
                    continue;
                }
                seen[i] = text;
                string raw = ReadText(text);
                if (raw == null || !raw.Contains("~z~"))
                {
                    continue;
                }
                string clean = Codes.Replace(raw, "").Trim();
                if (clean.Length > 0 && !lines.Contains(clean))
                {
                    lines.Add(clean);
                }
            }
        }

        private static IntPtr TextPointer(int slot) => *(IntPtr*)(list + slot * SlotSize);

        // The text at pointer, or null if it isn't readable or doesn't end within MaxTextBytes.
        private static string ReadText(IntPtr pointer)
        {
            int readable = pointer == IntPtr.Zero ? 0 : ReadableBytes(pointer, MaxTextBytes);
            byte* bytes = (byte*)pointer;
            int length = 0;
            while (length < readable && bytes[length] != 0)
            {
                length++;
            }
            return length == readable ? null : Encoding.UTF8.GetString(bytes, length);
        }

        private static byte* Search(byte* start, uint size)
        {
            int last = Pattern.Length - 1;
            for (long offset = 0; offset + last < size; offset++)
            {
                byte* p = start + offset;
                if (*p != Pattern[0])
                {
                    continue;
                }
                int i = 1;
                while (i <= last && (Pattern[i] < 0 || p[i] == Pattern[i]))
                {
                    i++;
                }
                if (i > last)
                {
                    return p;
                }
            }
            return null;
        }

        // How many bytes from pointer on can safely be read, up to most: 0 unless the memory is committed and readable.
        private static int ReadableBytes(IntPtr pointer, int most)
        {
            if (VirtualQuery(pointer, out MemoryInfo info, (UIntPtr)sizeof(MemoryInfo)) == UIntPtr.Zero)
            {
                return 0;
            }
            const uint Committed = 0x1000;
            const uint NoAccess = 0x01;
            const uint Guard = 0x100;
            if (info.State != Committed || info.Protect == 0 || (info.Protect & (NoAccess | Guard)) != 0)
            {
                return 0;
            }
            long left = (long)info.BaseAddress + (long)info.RegionSize - (long)pointer;
            return (int)Math.Min(most, left);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MemoryInfo
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public uint Padding1;
            public UIntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
            public uint Padding2;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandleW(string name);

        [DllImport("kernel32.dll")]
        private static extern UIntPtr VirtualQuery(IntPtr address, out MemoryInfo info, UIntPtr length);
    }
}
