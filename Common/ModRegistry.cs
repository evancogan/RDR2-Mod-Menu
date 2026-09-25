using System;
using System.Collections.Generic;
using System.Linq;
using RegistryEntry = System.Tuple<string, string, System.Func<bool>, System.Action>;

namespace AIPlayground
{
    // The list of loaded mods that the mod menu shows.
    //
    // Each mod is its own DLL with its own copy of Common, so a normal static list wouldn't be shared.
    // All scripts run in the same AppDomain, though, so the list is stored there using only framework
    // types (name, description, is-enabled check, toggle request), which every DLL sees as the same types.
    public static class ModRegistry
    {
        private const string DataKey = "AIPlayground.ModRegistry";

        // Returns a token to pass to Unregister when the mod unloads.
        public static object Register(string name, string description, Func<bool> isEnabled, Action requestToggle)
        {
            var entry = Tuple.Create(name, description, isEnabled, requestToggle);
            lock (SyncRoot)
            {
                GetList().Add(entry);
            }
            return entry;
        }

        public static void Unregister(object token)
        {
            lock (SyncRoot)
            {
                GetList().Remove((RegistryEntry)token);
            }
        }

        public static List<ModInfo> GetAll()
        {
            lock (SyncRoot)
            {
                return GetList().Select(e => new ModInfo(e)).OrderBy(m => m.Name).ToList();
            }
        }

        // The AppDomain object is the one thing every mod DLL shares, so it doubles as the lock.
        private static object SyncRoot => AppDomain.CurrentDomain;

        private static List<RegistryEntry> GetList()
        {
            var list = AppDomain.CurrentDomain.GetData(DataKey) as List<RegistryEntry>;
            if (list == null)
            {
                list = new List<RegistryEntry>();
                AppDomain.CurrentDomain.SetData(DataKey, list);
            }
            return list;
        }
    }

    public sealed class ModInfo
    {
        private readonly RegistryEntry entry;

        internal ModInfo(RegistryEntry entry)
        {
            this.entry = entry;
        }

        public string Name => entry.Item1;

        public string Description => entry.Item2;

        public bool IsEnabled => entry.Item3();

        // The mod flips itself on its next frame, so the change happens on its own script thread.
        public void RequestToggle() => entry.Item4();
    }
}
