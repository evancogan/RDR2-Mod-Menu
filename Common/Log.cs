using System;
using System.IO;
using System.Reflection;

namespace RDR2ModMenu
{
    // Appends lines to RDR2ModMenu.log next to RDR2.exe so we can inspect what happened after a play test.
    // Every mod writes to the same file, tagged with its own name.
    public static class Log
    {
        private static readonly string FilePath = GameFolder.File("RDR2ModMenu.log");

        private static readonly string ModName = Assembly.GetExecutingAssembly().GetName().Name;

        public static void Write(string message)
        {
            try
            {
                File.AppendAllText(FilePath, $"[{DateTime.Now:HH:mm:ss.fff}] [{ModName}] {message}{Environment.NewLine}");
            }
            catch (IOException)
            {
            }
        }
    }
}
