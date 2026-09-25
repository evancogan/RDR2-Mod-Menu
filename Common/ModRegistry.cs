using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RegistryEntry = System.Collections.Generic.Dictionary<string, object>;

namespace RDR2ModMenu
{
    // The list of loaded mods, actions and choices that the mod menu shows.
    //
    // Each mod is its own DLL with its own copy of Common, so a normal static list wouldn't be shared. All scripts run
    // in the same AppDomain, though, so the list is stored there. Each entry is a dictionary holding only framework
    // types (strings, delegates, arrays), which every DLL sees as the same types.
    public static class ModRegistry
    {
        private const string DataKey = "RDR2ModMenu.ModRegistry";

        internal const string NameKey = "Name";
        internal const string DescriptionKey = "Description";
        internal const string KindKey = "Kind";
        internal const string CategoryKey = "Category";
        internal const string IsEnabledKey = "IsEnabled";
        internal const string ActivateKey = "Activate";
        internal const string ChoicesKey = "Choices";
        internal const string CurrentChoiceKey = "CurrentChoice";
        internal const string ChooseKey = "Choose";

        internal const string ToggleKind = "toggle";
        internal const string ActionKind = "action";
        internal const string ChoiceKind = "choice";

        // "FlyingHorse" -> "Flying Horse"
        public static string DisplayNameOf(Type type) => Regex.Replace(type.Name, "(?<=[a-z])(?=[A-Z])", " ");

        // An on/off mod, listed in its menu section.
        public static object RegisterToggle(string name, string description, string category, Func<bool> isEnabled, Action toggle)
        {
            return Register(new RegistryEntry
            {
                [NameKey] = name, [DescriptionKey] = description, [KindKey] = ToggleKind,
                [CategoryKey] = category, [IsEnabledKey] = isEnabled, [ActivateKey] = toggle,
            });
        }

        // A one-press button, listed in its menu section.
        public static object RegisterAction(string name, string description, string category, Action run)
        {
            return Register(new RegistryEntry
            {
                [NameKey] = name, [DescriptionKey] = description, [KindKey] = ActionKind,
                [CategoryKey] = category, [ActivateKey] = run,
            });
        }

        // A row that scrolls through choices with Left/Right, listed in its menu section. If use isn't null,
        // Enter calls it (e.g. "refill now"); otherwise Enter moves to the next choice.
        public static object RegisterChoice(string name, string description, string category, string[] choices, Func<int> current, Action<int> choose, Action use = null)
        {
            var entry = new RegistryEntry
            {
                [NameKey] = name, [DescriptionKey] = description, [KindKey] = ChoiceKind, [CategoryKey] = category,
                [ChoicesKey] = choices, [CurrentChoiceKey] = current, [ChooseKey] = choose,
            };
            if (use != null)
            {
                entry[ActivateKey] = use;
            }
            return Register(entry);
        }

        // Pass the token Register returned, when the mod unloads.
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

        private static object Register(RegistryEntry entry)
        {
            lock (SyncRoot)
            {
                GetList().Add(entry);
            }
            return entry;
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

        public string Name => (string)entry[ModRegistry.NameKey];

        public string Description => (string)entry[ModRegistry.DescriptionKey];

        public bool IsToggle => Kind == ModRegistry.ToggleKind;

        public bool IsChoice => Kind == ModRegistry.ChoiceKind;

        // The menu section it's listed in, e.g. "Needs".
        public string Category => entry.TryGetValue(ModRegistry.CategoryKey, out object category) ? (string)category : null;

        public bool IsEnabled => IsToggle && ((Func<bool>)entry[ModRegistry.IsEnabledKey])();

        public string[] Choices => IsChoice ? (string[])entry[ModRegistry.ChoicesKey] : new string[0];

        public int CurrentChoice => IsChoice ? ((Func<int>)entry[ModRegistry.CurrentChoiceKey])() : 0;

        // Whether Enter does something of its own on a choice row (instead of moving to the next choice).
        public bool CanUse => entry.ContainsKey(ModRegistry.ActivateKey);

        // Toggles a mod, runs an action, or uses a choice row. The mod acts on its next frame, on its own script thread.
        public void Activate()
        {
            if (entry.TryGetValue(ModRegistry.ActivateKey, out object activate))
            {
                ((Action)activate)();
            }
        }

        // Moves a choice by step (wrapping around), also applied on the mod's next frame.
        public void Step(int step)
        {
            int count = Choices.Length;
            if (count > 0)
            {
                ((Action<int>)entry[ModRegistry.ChooseKey])(((CurrentChoice + step) % count + count) % count);
            }
        }

        private string Kind => (string)entry[ModRegistry.KindKey];
    }
}
