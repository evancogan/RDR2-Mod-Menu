using System;
using RDR2;
using Screen = RDR2.UI.Screen;

namespace AIPlayground
{
    // Base class for a scrolling row in the mod menu, listed in its Category's section, e.g. "Body Type   < Medium >".
    // Left/Right move through Choices and apply the new one straight away.
    public abstract class ModChoice : Script
    {
        // One line shown in the mod menu.
        protected abstract string Description { get; }

        // The menu section it's listed in, e.g. "Player".
        protected abstract string Category { get; }

        protected abstract string[] Choices { get; }

        // Which choice the row shows before anything has been picked.
        protected virtual int InitialChoice => 0;

        // Defaults to the class name split into words; override for a custom name.
        protected virtual string DisplayName => ModRegistry.DisplayNameOf(GetType());

        protected int Current { get; private set; }

        private readonly object registration;
        private volatile int requested = -1;

        protected ModChoice()
        {
            Current = InitialChoice;
            registration = ModRegistry.RegisterChoice(DisplayName, Description, Category, Choices, () => Current, index => requested = index);
            Tick += OnTickInternal;
            Aborted += (sender, e) => ModRegistry.Unregister(registration);
            Log.Write($"{DisplayName} loaded (choice in {Category})");
        }

        // Applies the choice at index. Returns the subtitle to show afterwards.
        protected abstract string Apply(int index);

        private void OnTickInternal(object sender, EventArgs e)
        {
            int index = requested;
            if (index < 0)
            {
                return;
            }
            requested = -1;

            Current = index;
            string result = Apply(index);
            Screen.DisplaySubtitle(result);
            Log.Write($"{DisplayName} -> {Choices[index]}: {result}");
        }
    }
}
