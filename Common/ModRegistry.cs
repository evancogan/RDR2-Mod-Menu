using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RegistryEntry = System.Collections.Generic.Dictionary<string, object>;

namespace AIPlayground
{
    // The list of loaded mods, actions and choices that the mod menu shows.
    //
    // Each mod is its own DLL with its own copy of Common, so a normal static list wouldn't be shared. All scripts run
    // in the same AppDomain, though, so the list is stored there. Each entry is a dictionary holding only framework
    // types (strings, delegates, arrays), which every DLL sees as the same types.
    public static class ModRegistry
    {
        private const string DataKey = "AIPlayground.ModRegistry";

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

        // An on/off mod on the main menu page.
        public static object RegisterToggle(string name, string description, Func<bool> isEnabled, Action toggle)
        {
            return Register(new RegistryEntry
            {
                [NameKey] = name, [DescriptionKey] = description, [KindKey] = ToggleKind,
                [IsEnabledKey] = isEnabled, [ActivateKey] = toggle,
            });
        }

        // A one-press button in an Actions category.
        public static object RegisterAction(string name, string description, string category, Action run)
        {
            return Register(new RegistryEntry
            {
                [NameKey] = name, [DescriptionKey] = description, [KindKey] = ActionKind,
                [CategoryKey] = category, [ActivateKey] = run,
            });
        }

        // A row in an Actions category that scrolls through choices with Left/Right.
        public static object RegisterChoice(string name, string description, string category, string[] choices, Func<int> current, Action<int> choose)
        {
            return Register(new RegistryEntry
            {
                [NameKey] = name, [DescriptionKey] = description, [KindKey] = ChoiceKind, [CategoryKey] = category,
                [ChoicesKey] = choices, [CurrentChoiceKey] = current, [ChooseKey] = choose,
            });
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

        // Toggles live on the main page; actions and choices live in Actions categories.
        public bool IsToggle => Kind == ModRegistry.ToggleKind;

        public bool IsChoice => Kind == ModRegistry.ChoiceKind;

        public string Category => entry.TryGetValue(ModRegistry.CategoryKey, out object category) ? (string)category : null;

        public bool IsEnabled => IsToggle && ((Func<bool>)entry[ModRegistry.IsEnabledKey])();

        public string[] Choices => IsChoice ? (string[])entry[ModRegistry.ChoicesKey] : new string[0];

        public int CurrentChoice => IsChoice ? ((Func<int>)entry[ModRegistry.CurrentChoiceKey])() : 0;

        // Toggles a mod or runs an action. The mod acts on its next frame, on its own script thread.
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
