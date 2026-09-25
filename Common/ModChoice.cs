using System;
using System.Linq;
using RDR2;
using Screen = RDR2.UI.Screen;

namespace RDR2ModMenu
{
    // Base class for a scrolling row in the mod menu, listed in its Category's section, e.g. "Body Type   < Medium >".
    // Left/Right move through Choices and apply the new one straight away.
    //
    // Optional extras: override CanUse and Use() so Enter does something (like "refill now"), override
    // OnChoiceTick() for per-frame work that depends on the current choice, and set RememberChoice to keep the
    // choice across Insert reloads and game restarts (saved in RDR2ModMenu.ini).
    public abstract class ModChoice : Script
    {
        // One line shown in the mod menu.
        protected abstract string Description { get; }

        // The menu section it's listed in, e.g. "Player".
        protected abstract string Category { get; }

        protected abstract string[] Choices { get; }

        // Which choice the row shows before anything has been picked (or saved).
        protected virtual int InitialChoice => 0;

        // Whether the current choice is saved and restored on load.
        protected virtual bool RememberChoice => false;

        // Whether Enter does something on this row (Use); otherwise Enter just moves to the next choice.
        protected virtual bool CanUse => false;

        // Defaults to the class name split into words; override for a custom name.
        protected virtual string DisplayName => ModRegistry.DisplayNameOf(GetType());

        protected int Current { get; private set; }

        private string SettingsKey => GetType().Name;

        private readonly object registration;
        private volatile int requested = -1;
        private volatile bool useRequested;

        protected ModChoice()
        {
            Current = InitialChoice;
            if (RememberChoice)
            {
                int saved = Array.IndexOf(Choices, ModSettings.GetValue(SettingsKey));
                if (saved >= 0)
                {
                    Current = saved;
                }
            }

            registration = ModRegistry.RegisterChoice(DisplayName, Description, Category, Choices, () => Current,
                index => requested = index, CanUse ? () => useRequested = true : (Action)null);
            Tick += OnTickInternal;
            Aborted += (sender, e) => ModRegistry.Unregister(registration);
            Log.Write($"{DisplayName} loaded (choice in {Category}, {Choices[Current]})");
        }

        // Applies the choice at index. Returns the subtitle to show afterwards.
        protected abstract string Apply(int index);

        // What Enter does, when CanUse is true. Returns the subtitle to show afterwards.
        protected virtual string Use() => null;

        // Runs every frame, whatever the choice.
        protected virtual void OnChoiceTick()
        {
        }

        private void OnTickInternal(object sender, EventArgs e)
        {
            int index = requested;
            if (index >= 0)
            {
                requested = -1;
                Current = index;
                if (RememberChoice)
                {
                    ModSettings.SetValue(SettingsKey, Choices[index]);
                }
                Show($"{DisplayName} -> {Choices[index]}", Apply(index));
            }

            if (useRequested)
            {
                useRequested = false;
                Show($"{DisplayName} used", Use());
            }

            OnChoiceTick();
        }

        private static void Show(string logPrefix, string result)
        {
            if (result != null)
            {
                Screen.DisplaySubtitle(result);
            }
            Log.Write($"{logPrefix}: {result}");
        }
    }
}
