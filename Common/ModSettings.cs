using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RDR2ModMenu
{
    // Remembers mod states in RDR2ModMenu.ini next to RDR2.exe, one "Name=value" line each: "on"/"off" for mods,
    // or the chosen option for settings that remember their choice (e.g. "RefillHealth=Always").
    // Shared by every mod DLL, so each one only rewrites its own line.
    public static class ModSettings
    {
        private static readonly string FilePath = GameFolder.File("RDR2ModMenu.ini");

        public static bool? GetEnabled(string mod)
        {
            string value = GetValue(mod);
            return value == null ? (bool?)null : string.Equals(value, "on", StringComparison.OrdinalIgnoreCase);
        }

        public static void SetEnabled(string mod, bool enabled) => SetValue(mod, enabled ? "on" : "off");

        // The saved value for name, or null if there isn't one.
        public static string GetValue(string name)
        {
            string value;
            return ReadAll().TryGetValue(name, out value) ? value : null;
        }

        public static void SetValue(string name, string value)
        {
            try
            {
                Dictionary<string, string> settings = ReadAll();
                settings[name] = value;
                File.WriteAllLines(FilePath, settings.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}"));
            }
            catch (IOException ex)
            {
                Log.Write($"Couldn't save settings: {ex.Message}");
            }
        }

        private static Dictionary<string, string> ReadAll()
        {
            var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!File.Exists(FilePath))
                {
                    return settings;
                }
                foreach (string line in File.ReadAllLines(FilePath))
                {
                    int eq = line.IndexOf('=');
                    if (eq > 0)
                    {
                        settings[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
                    }
                }
            }
            catch (IOException ex)
            {
                Log.Write($"Couldn't read settings: {ex.Message}");
            }
            return settings;
        }
    }
}
