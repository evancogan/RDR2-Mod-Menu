using System;
using RDR2;
using Screen = RDR2.UI.Screen;

namespace RDR2ModMenu
{
    // Base class for a one-press button in the mod menu, listed in its Category's section.
    // Selecting it and pressing Enter runs Run() once, then shows its result as a subtitle and logs it.
    // Actions have no state to save.
    public abstract class ModAction : Script
    {
        // One line shown in the mod menu.
        protected abstract string Description { get; }

        // The menu section it's listed in, e.g. "Needs".
        protected abstract string Category { get; }

        // Defaults to the class name split into words; override for a custom name (the menu sorts by it).
        protected virtual string DisplayName => ModRegistry.DisplayNameOf(GetType());

        private readonly object registration;
        private volatile bool runRequested;

        protected ModAction()
        {
            registration = ModRegistry.RegisterAction(DisplayName, Description, Category, () => runRequested = true);
            Tick += OnTickInternal;
            Aborted += (sender, e) => ModRegistry.Unregister(registration);
            Log.Write($"{DisplayName} loaded (action in {Category})");
        }

        // Does the action. Returns the subtitle to show afterwards.
        protected abstract string Run();

        private void OnTickInternal(object sender, EventArgs e)
        {
            if (!runRequested)
            {
                return;
            }
            runRequested = false;

            string result = Run();
            Screen.DisplaySubtitle(result);
            Log.Write($"Used: {result}");
        }
    }
}
