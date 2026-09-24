using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using RDR2;
using Screen = RDR2.UI.Screen;

namespace AIPlayground
{
    // Base class for every AI Playground mod. Provides the standard toggle: pressing ToggleKey
    // turns the mod on or off, shows "<Mod Name>: ON/OFF" as a subtitle, and logs the change.
    //
    // Override OnEnable/OnDisable for setup and cleanup (OnEnable can return false to refuse),
    // OnEnabledTick for per-frame work while on, and OnDisabledTick if anything must run while off.
    public abstract class ModScript : Script
    {
        protected abstract Keys ToggleKey { get; }

        protected virtual bool EnabledOnStart => false;

        // "FlyingHorse" -> "Flying Horse"
        protected string DisplayName => Regex.Replace(GetType().Name, "(?<=[a-z])(?=[A-Z])", " ");

        public bool IsEnabled { get; private set; }

        protected ModScript()
        {
            IsEnabled = EnabledOnStart;
            Tick += OnTickInternal;
            KeyDown += OnKeyDownInternal;
            Aborted += (sender, e) => OnAborted();
            Log.Write($"{DisplayName} started, {(IsEnabled ? "ON" : "OFF")}, toggle with {ToggleKey}");
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
            Screen.DisplaySubtitle($"{DisplayName}: ON");
            Log.Write("Toggled ON");
        }

        // Mods can call this themselves, e.g. when the player does something that ends the effect.
        protected void Disable(string reason = "toggled off")
        {
            if (!IsEnabled)
            {
                return;
            }
            IsEnabled = false;
            OnDisable();
            Screen.DisplaySubtitle($"{DisplayName}: OFF");
            Log.Write($"Toggled OFF ({reason})");
        }

        private void OnKeyDownInternal(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != ToggleKey)
            {
                return;
            }

            if (IsEnabled)
            {
                Disable();
            }
            else
            {
                Enable();
            }
        }

        private void OnTickInternal(object sender, EventArgs e)
        {
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
