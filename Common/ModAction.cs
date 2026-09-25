using System;
using RDR2;
using Screen = RDR2.UI.Screen;

namespace AIPlayground
{
    // Base class for a one-shot action in the mod menu (F9): a button rather than an on/off mod.
    // Selecting it and pressing Enter runs Run() once, then shows its result as a subtitle and logs it.
    // Actions have no state to save.
    public abstract class ModAction : Script
    {
        // One line shown in the mod menu.
        protected abstract string Description { get; }

        protected string DisplayName => ModRegistry.DisplayNameOf(GetType());

        private readonly object registration;
        private volatile bool runRequested;

        protected ModAction()
        {
            registration = ModRegistry.Register(DisplayName, Description, null, () => runRequested = true);
            Tick += OnTickInternal;
            Aborted += (sender, e) => ModRegistry.Unregister(registration);
            Log.Write($"{DisplayName} loaded (action)");
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
