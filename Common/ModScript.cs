using System;
using RDR2;
using Screen = RDR2.UI.Screen;

namespace AIPlayground
{
    // Base class for every AI Playground mod.
    //
    // Mods are turned on and off from the mod menu (F9), start off until you enable them there, and remember
    // their on/off state in AIPlayground.ini across Insert reloads and game restarts. Turning a mod on or off
    // shows "<Mod Name>: ON/OFF" as a subtitle and logs the change.
    //
    // Override OnEnable/OnDisable for setup and cleanup (OnEnable can return false to refuse),
    // OnEnabledTick for per-frame work while on, and OnDisabledTick if anything must run while off.
    // A mod's own hotkeys should only do something while IsEnabled.
    public abstract class ModScript : Script
    {
        // One line shown in the mod menu and in the subtitle when the mod is turned on.
        protected abstract string Description { get; }

        protected string DisplayName => ModRegistry.DisplayNameOf(GetType());

        private string SettingsKey => GetType().Name;

        public bool IsEnabled { get; private set; }

        private readonly object registration;
        private readonly bool startEnabled;
        private bool started;
        private volatile bool toggleRequested;

        protected ModScript()
        {
            bool? saved = ModSettings.GetEnabled(SettingsKey);
            startEnabled = saved ?? false;

            registration = ModRegistry.Register(DisplayName, Description, () => IsEnabled, () => toggleRequested = true);

            Tick += OnTickInternal;
            Aborted += (sender, e) =>
            {
                ModRegistry.Unregister(registration);
                OnAborted();
            };
            Log.Write($"{DisplayName} loaded, starting {(startEnabled ? "ON" : "OFF")} ({(saved.HasValue ? "saved" : "default")})");
        }

        protected virtual bool OnEnable() => true;

        protected virtual void OnDisable()
        {
        }

        protected abstract void OnEnabledTick();

        protected virtual void OnDisabledTick()
        {
        }

        // Scripts are being unloaded (Insert reload or game exit). Undo anything that would outlive the script.
        protected virtual void OnAborted()
        {
            if (IsEnabled)
            {
                OnDisable();
            }
        }

        protected void Enable()
        {
            if (IsEnabled || !OnEnable())
            {
                return;
            }
            IsEnabled = true;
            ModSettings.SetEnabled(SettingsKey, true);
            Screen.DisplaySubtitle($"{DisplayName}: ON. {Description}");
            Log.Write("Turned ON");
        }

        // Mods can call this themselves, e.g. when the player does something that ends the effect.
        protected void Disable(string reason)
        {
            if (!IsEnabled)
            {
                return;
            }
            IsEnabled = false;
            ModSettings.SetEnabled(SettingsKey, false);
            OnDisable();
            Screen.DisplaySubtitle($"{DisplayName}: OFF");
            Log.Write($"Turned OFF ({reason})");
        }

        private void OnTickInternal(object sender, EventArgs e)
        {
            // Restore the saved state on the first frame rather than at load, so OnEnable runs with the game ready.
            if (!started)
            {
                started = true;
                if (startEnabled && OnEnable())
                {
                    IsEnabled = true;
                    Log.Write("Restored ON");
                }
            }

            if (toggleRequested)
            {
                toggleRequested = false;
                if (IsEnabled)
                {
                    Disable("turned off in menu");
                }
                else
                {
                    Enable();
                }
            }

            if (IsEnabled)
            {
                OnEnabledTick();
            }
            else
            {
                OnDisabledTick();
            }
        }
    }
}
