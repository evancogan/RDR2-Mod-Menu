using System;
using RDR2;
using Screen = RDR2.UI.Screen;

namespace AIPlayground
{
    // Base class for a one-press button in the mod menu's Actions submenu, listed in its Category folder
    // (or directly on the Actions page if Category is null).
    // Selecting it and pressing Enter runs Run() once, then shows its result as a subtitle and logs it.
    // Actions have no state to save.
    public abstract class ModAction : Script
    {
        // One line shown in the mod menu.
        protected abstract string Description { get; }

        // The Actions category folder it's listed in, e.g. "Needs", or null to show it directly on the Actions page.
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
            Log.Write($"{DisplayName} loaded (action in {Category ?? "Actions"})");
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
