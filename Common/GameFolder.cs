using System.Diagnostics;
using System.IO;

namespace AIPlayground
{
    public static class GameFolder
    {
        // The folder containing RDR2.exe.
        public static readonly string Path = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

        public static string File(string name) => System.IO.Path.Combine(Path, name);
    }
}
