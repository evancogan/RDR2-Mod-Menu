using System;
using System.Windows.Forms;
using RDR2;
using RDR2.Math;
using RDR2.Native;
using Screen = RDR2.UI.Screen;

namespace AIPlayground
{
    // Turn on while mounted to take off; turn off to land. The horse flies where the camera looks:
    // W/S forward/back, Shift for speed, Space to climb. Let go of everything to hover.
    public class FlyingHorse : ModScript
    {
        protected override Keys ToggleKey => Keys.F7;

        private const float CruiseSpeed = 15f;
        private const float SprintSpeed = 40f;
        private const float ClimbSpeed = 10f;

        // How far velocity moves toward the target each tick (0..1). Lower feels floatier.
        private const float Responsiveness = 0.1f;

        // Upper bound on how long landing protection lasts if the horse never reports touching down.
        private const int LandingProtectionMs = 10000;

        private Ped horse;
        private bool landingProtection;
        private int landingProtectionUntil;
        private int nextLogTime;

        protected override bool OnEnable()
        {
            Ped player = Game.Player.Character;
            if (!player.IsOnMount)
            {
                Screen.DisplaySubtitle("Get on a horse first");
                return false;
            }

            horse = player.CurrentMount;
            landingProtection = false;

            SetProtection(player, horse, true);
            ENTITY.SET_ENTITY_HAS_GRAVITY(horse.Handle, false);
            horse.Velocity = horse.Velocity + new Vector3(0f, 0f, ClimbSpeed);

            Log.Write($"Takeoff at {horse.Position}, cam dir {GameplayCamera.Direction}, cam rot {GameplayCamera.Rotation}, heading {horse.Heading}");
            return true;
        }

        protected override void OnDisable()
        {
            ENTITY.SET_ENTITY_HAS_GRAVITY(horse.Handle, true);

            // Stay invincible until the horse is back on the ground, so the fall doesn't kill anyone.
            landingProtection = true;
            landingProtectionUntil = Environment.TickCount + LandingProtectionMs;
            Log.Write($"Landing from {horse.Position}");
        }

        protected override void OnEnabledTick()
        {
            Ped player = Game.Player.Character;
            if (!player.IsOnMount || player.CurrentMount.Handle != horse.Handle)
            {
                Disable("dismounted");
                return;
            }

            // Space climbs instead of making the horse jump.
            Game.DisableControlThisFrame(eInputType.HorseJump);

            float forward = Game.GetControlNormal(eInputType.HorseMoveUpOnly) - Game.GetControlNormal(eInputType.HorseMoveDownOnly);
            bool sprint = Game.IsControlPressed(eInputType.HorseSprint);
            bool climb = Game.IsDisabledControlPressed(eInputType.HorseJump);

            float speed = sprint ? SprintSpeed : CruiseSpeed;
            Vector3 target = GameplayCamera.Direction * (forward * speed);
            if (climb)
            {
                target = target + new Vector3(0f, 0f, ClimbSpeed);
            }

            Vector3 velocity = horse.Velocity;
            horse.Velocity = velocity + (target - velocity) * Responsiveness;
            horse.Heading = GameplayCamera.Rotation.Z;

            if (Environment.TickCount > nextLogTime)
            {
                nextLogTime = Environment.TickCount + 1000;
                Log.Write($"Flying: pos {horse.Position}, vel {velocity}, target {target}, forward {forward:F2}, sprint {sprint}, climb {climb}, inAir {horse.IsInAir}");
            }
        }

        protected override void OnDisabledTick()
        {
            if (!landingProtection)
            {
                return;
            }

            bool landed = !horse.IsInAir;
            if (landed || Environment.TickCount > landingProtectionUntil)
            {
                SetProtection(Game.Player.Character, horse, false);
                landingProtection = false;
                Log.Write(landed ? $"Landed at {horse.Position}" : "Landing protection timed out");
            }
        }

        protected override void OnAborted()
        {
            // Scripts reloaded mid-flight or mid-landing: put everything back to normal.
            if ((IsEnabled || landingProtection) && horse != null)
            {
                ENTITY.SET_ENTITY_HAS_GRAVITY(horse.Handle, true);
                SetProtection(Game.Player.Character, horse, false);
            }
        }

        private static void SetProtection(Ped player, Ped mount, bool on)
        {
            player.IsInvincible = on;
            player.CanRagdoll = !on;
            mount.IsInvincible = on;
            mount.CanRagdoll = !on;
        }
    }
}
