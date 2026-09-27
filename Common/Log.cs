using System;
using System.IO;
using System.Reflection;

namespace RDR2ModMenu
{
    // Appends lines to RDR2ModMenu.log next to RDR2.exe so we can inspect what happened after a play test.
    // Every mod writes to the same file, tagged with its own name. The menu calls RollOver when it loads, so the log
    // never grows much past MaxBytes: the previous one is kept as RDR2ModMenu.old.log.
    public static class Log
    {
        private static readonly string FilePath = GameFolder.File("RDR2ModMenu.log");
        private static readonly string OldFilePath = GameFolder.File("RDR2ModMenu.old.log");
        private const long MaxBytes = 2 * 1024 * 1024;

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

        public static void RollOver()
        {
            try
            {
                var file = new FileInfo(FilePath);
                if (file.Exists && file.Length > MaxBytes)
                {
                    File.Delete(OldFilePath);
                    file.MoveTo(OldFilePath);
                }
            }
            catch (IOException)
            {
            }
        }
    }
}
