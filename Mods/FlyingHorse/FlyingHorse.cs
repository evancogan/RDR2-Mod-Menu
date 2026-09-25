using System;
using System.Windows.Forms;
using RDR2;
using RDR2.Math;
using RDR2.Native;
using Screen = RDR2.UI.Screen;

namespace AIPlayground
{
    // While enabled, press F7 on a horse to take off or land. The horse flies where the camera looks:
    // W/S forward/back, Shift for speed, Space to climb. Let go of everything to hover.
    //
    // Flight freezes the horse's physics and moves it directly each frame, so the game's falling
    // behavior never kicks in. It stays at least GroundClearance above the ground but can pass through
    // buildings and trees.
    public class FlyingHorse : ModScript
    {
        protected override string Description => "On a horse, press F7 to take off or land. Fly where you look: W/S, Shift faster, Space climb.";

        private const Keys FlyKey = Keys.F7;

        private const float CruiseSpeed = 15f;
        private const float SprintSpeed = 40f;
        private const float ClimbSpeed = 10f;
        private const float TakeoffLift = 2f;

        // How quickly the horse reaches its target speed, per second. Lower feels floatier.
        private const float Acceleration = 4f;

        // Minimum height above the ground while flying, on top of the horse's normal standing height.
        private const float GroundClearance = 0.5f;

        // Landing protection ends once horse and rider have both been this slow vertically for LandedHoldMs.
        private const float LandedVerticalSpeed = 1f;
        private const int LandedHoldMs = 1000;
        private const int LandingTimeoutMs = 15000;

        private enum FlightState { Grounded, Flying, Landing }

        private FlightState state = FlightState.Grounded;
        private Ped horse;
        private Vector3 position;
        private Vector3 velocity;
        private float standingHeight;
        private int landingStartedAt;
        private int stillSince;
        private int nextLogTime;

        public FlyingHorse()
        {
            KeyDown += OnKeyDown;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!IsEnabled || e.KeyCode != FlyKey)
            {
                return;
            }

            if (state == FlightState.Flying)
            {
                Land("F7");
            }
            else
            {
                TakeOff();
            }
        }

        protected override void OnEnabledTick()
        {
            if (state == FlightState.Flying)
            {
                Ped player = Game.Player.Character;
                if (!player.IsOnMount || player.CurrentMount.Handle != horse.Handle)
                {
                    Land("dismounted");
                    return;
                }
                Fly();
            }
            else if (state == FlightState.Landing)
            {
                CheckLanded();
            }
        }

        protected override void OnDisabledTick()
        {
            // Turned off mid-landing: keep protecting until touchdown.
            if (state == FlightState.Landing)
            {
                CheckLanded();
            }
        }

        protected override void OnDisable()
        {
            if (state == FlightState.Flying)
            {
                Land("mod turned off");
            }
        }

        protected override void OnAborted()
        {
            // Scripts reloaded mid-flight or mid-landing: put everything back to normal.
            if (state != FlightState.Grounded && horse != null)
            {
                ENTITY.FREEZE_ENTITY_POSITION(horse.Handle, false);
                SetProtection(false);
            }
            state = FlightState.Grounded;
        }

        private void TakeOff()
        {
            Ped player = Game.Player.Character;
            if (!player.IsOnMount)
            {
                Screen.DisplaySubtitle("Get on a horse first");
                return;
            }

            horse = player.CurrentMount;
            standingHeight = horse.HeightAboveGround;
            position = horse.Position + new Vector3(0f, 0f, TakeoffLift);
            velocity = Vector3.Zero;

            SetProtection(true);
            ENTITY.FREEZE_ENTITY_POSITION(horse.Handle, true);
            state = FlightState.Flying;

            Screen.DisplaySubtitle("Taking off");
            Log.Write($"Takeoff at {horse.Position}, standing height {standingHeight:F2}, cam dir {GameplayCamera.Direction}");
        }

        private void Land(string reason)
        {
            ENTITY.FREEZE_ENTITY_POSITION(horse.Handle, false);
            horse.Velocity = velocity;

            state = FlightState.Landing;
            landingStartedAt = Environment.TickCount;
            stillSince = 0;

            Screen.DisplaySubtitle("Landing");
            Log.Write($"Landing ({reason}) from {horse.Position}, height {horse.HeightAboveGround:F1}");
        }

        private void Fly()
        {
            // Space climbs instead of making the horse jump.
            Game.DisableControlThisFrame(eInputType.HorseJump);

            float dt = Math.Min(Game.FrameTime, 0.1f);
            float forward = Game.GetControlNormal(eInputType.HorseMoveUpOnly) - Game.GetControlNormal(eInputType.HorseMoveDownOnly);
            bool sprint = Game.IsControlPressed(eInputType.HorseSprint);
            bool climb = Game.IsDisabledControlPressed(eInputType.HorseJump);

            float speed = sprint ? SprintSpeed : CruiseSpeed;
            Vector3 target = GameplayCamera.Direction * (forward * speed);
            if (climb)
            {
                target = target + new Vector3(0f, 0f, ClimbSpeed);
            }

            velocity = velocity + (target - velocity) * Math.Min(1f, Acceleration * dt);
            Vector3 next = position + velocity * dt;

            // Keep above the ground: work out the ground's Z under the horse from its height above it.
            float height = horse.HeightAboveGround;
            float minZ = position.Z - height + standingHeight + GroundClearance;
            if (next.Z < minZ)
            {
                next = new Vector3(next.X, next.Y, minZ);
                if (velocity.Z < 0f)
                {
                    velocity = new Vector3(velocity.X, velocity.Y, 0f);
                }
            }

            position = next;
            ENTITY.SET_ENTITY_COORDS_NO_OFFSET(horse.Handle, position, false, false, false);
            // Face where the camera looks. Worked out from the camera's direction rather than its rotation,
            // which pointed the horse backwards. Skipped when looking nearly straight up or down.
            Vector3 look = GameplayCamera.Direction;
            float heading = horse.Heading;
            if (look.X * look.X + look.Y * look.Y > 0.01f)
            {
                heading = HeadingFromDirection(look);
                horse.Heading = heading;
            }

            if (Environment.TickCount > nextLogTime)
            {
                nextLogTime = Environment.TickCount + 1000;
                Log.Write($"Flying: pos {position}, vel {velocity}, height {height:F1}, forward {forward:F2}, sprint {sprint}, climb {climb}, dt {dt:F3}");
                Log.Write($"Facing: set heading {heading:F0}, horse reports {horse.Heading:F0}, horse forward {horse.ForwardVector}, cam dir {look}, cam rot Z {GameplayCamera.Rotation.Z:F0}");
            }
        }

        // The game's heading is degrees counterclockwise from north (+Y): heading 0 faces +Y, 90 faces -X.
        private static float HeadingFromDirection(Vector3 direction)
        {
            float degrees = (float)(Math.Atan2(-direction.X, direction.Y) * 180.0 / Math.PI);
            return degrees < 0f ? degrees + 360f : degrees;
        }

        private void CheckLanded()
        {
            int now = Environment.TickCount;
            Ped player = Game.Player.Character;
            bool still = Math.Abs(horse.Velocity.Z) < LandedVerticalSpeed && Math.Abs(player.Velocity.Z) < LandedVerticalSpeed;

            if (!still)
            {
                stillSince = 0;
            }
            else if (stillSince == 0)
            {
                stillSince = now;
            }

            bool landed = stillSince != 0 && now - stillSince > LandedHoldMs;
            if (landed || now - landingStartedAt > LandingTimeoutMs)
            {
                SetProtection(false);
                state = FlightState.Grounded;
                Log.Write(landed ? $"Landed at {horse.Position}" : "Landing protection timed out");
            }
        }

        private void SetProtection(bool on)
        {
            Ped player = Game.Player.Character;
            player.IsInvincible = on;
            player.CanRagdoll = !on;
            horse.IsInvincible = on;
            horse.CanRagdoll = !on;
        }
    }
}
