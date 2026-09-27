using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RDR2;
using RDR2.Math;
using RDR2.Native;
using Screen = RDR2.UI.Screen;

namespace RDR2ModMenu
{
    // While enabled, press F7 on a horse to take off or land. In the air the horse rides as normal
    // (W walk, Shift faster, A/D steer, with the game's own animations); this mod only controls height:
    // Space rises, Q descends, and otherwise the horse holds its altitude. The higher it flies above the ground,
    // the faster it goes. Descending slows as the ground gets close.
    //
    // How height is controlled, and why (all found by testing in-game):
    // - The horse's own riding moves it horizontally. The mod never sets its heading or horizontal speed:
    //   doing so made the game draw the horse facing opposite its rider.
    // - Height is set by placing the horse every frame, never by vertical speed. Any downward speed above a crawl
    //   switches the horse into the game's falling state, after which it ignores the speed we set and drops.
    //   Switching between speed and placement also triggered falls.
    // - Placement keeps the horse's tasks but not its IK (see Place): clearing tasks flipped the horse around,
    //   and keeping IK let the falling state start mid-descent.
    // - Extra forward speed is added the same way, by placing the horse further along the way it's already moving.
    public class FlyingHorse : ModScript
    {
        protected override string Category => "Horse";

        protected override string Description => "On a horse, press F7 to take off or land. Ride as normal (W walk, Shift faster, A/D steer); Space rises, Q descends.";

        private const Keys FlyKey = Keys.F7;

        // Not Ctrl (the game's horse-stop control), C (turns the camera around), or X (used by the game elsewhere).
        private const Keys DescendKey = Keys.Q;

        private const float RiseSpeed = 8f;

        // Descent speed is proportional to the height above the ground, so it slows as the ground gets close: half the
        // height per second, never slower than MinDescendSpeed or faster than MaxDescendSpeed (reached from 16 m up).
        // It used to go up to 50 m/s from 100 m up, and the horse visibly shook: moved by placement, it jumped nearly a
        // metre each frame while the game thought it wasn't moving vertically. The height reading also breaks above
        // about 160 m (it reads the altitude instead), which made the target speed jump. MaxDescendSpeed matches
        // RiseSpeed, which flies smoothly.
        private const float MinDescendSpeed = 4f;
        private const float DescendSpeedPerMeter = 0.5f;
        private const float MaxDescendSpeed = 8f;

        // Forward speed grows with height: normal up to BoostStartHeight, rising steadily to MaxSpeedMultiplier times
        // normal at BoostFullHeight and above.
        private const float BoostStartHeight = 5f;
        private const float BoostFullHeight = 150f;
        private const float MaxSpeedMultiplier = 5f;

        // The boost is based on the horse's own riding speed, capped at about a full gallop, in case its measured speed
        // ever includes the boost itself and would snowball.
        private const float MaxRidingSpeed = 15f;

        // Below this, which way the horse is moving isn't reliable enough to boost along.
        private const float MinMovingSpeed = 0.3f;

        // How quickly the speed multiplier follows a change in height, per second, so flying over a cliff edge
        // doesn't suddenly jolt the speed.
        private const float BoostSmoothing = 1f;

        // How long takeoff rises on its own, in milliseconds.
        private const int TakeoffMs = 400;

        // How quickly vertical speed reaches its target, per second. Lower feels floatier.
        private const float VerticalAcceleration = 5f;

        // Minimum height above the ground while flying, on top of the horse's normal standing height.
        private const float GroundClearance = 0.5f;

        // Landing protection ends once horse and rider have both been this slow vertically for LandedHoldMs.
        private const float LandedVerticalSpeed = 1f;
        private const int LandedHoldMs = 1000;
        private const int LandingTimeoutMs = 15000;

        private enum FlightState { Grounded, Flying, Landing }

        // Space and Q are read straight from the keyboard: mounted, the game didn't report Space
        // through the horse-jump control.
        private readonly HashSet<Keys> heldKeys = new HashSet<Keys>();

        private FlightState state = FlightState.Grounded;
        private Ped horse;
        private float flyZ;
        private float verticalSpeed;
        private float standingHeight;
        private float speedMultiplier = 1f;
        private int takeoffUntil;
        private int landingStartedAt;
        private int stillSince;

        public FlyingHorse()
        {
            KeyDown += OnKeyDown;
            KeyUp += (sender, e) => heldKeys.Remove(e.KeyCode);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            heldKeys.Add(e.KeyCode);

            if (!IsEnabled)
            {
                return;
            }

            if (e.KeyCode != FlyKey)
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
                ENTITY.SET_ENTITY_HAS_GRAVITY(horse.Handle, true);
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
            flyZ = horse.Position.Z;
            verticalSpeed = RiseSpeed;
            speedMultiplier = 1f;
            takeoffUntil = Environment.TickCount + TakeoffMs;

            SetProtection(true);
            ENTITY.SET_ENTITY_HAS_GRAVITY(horse.Handle, false);
            state = FlightState.Flying;

            Screen.DisplaySubtitle("Taking off");
            Log.Write($"Takeoff at {horse.Position}, standing height {standingHeight:F2}");
        }

        private void Land(string reason)
        {
            ENTITY.SET_ENTITY_HAS_GRAVITY(horse.Handle, true);

            state = FlightState.Landing;
            landingStartedAt = Environment.TickCount;
            stillSince = 0;

            Screen.DisplaySubtitle("Landing");
            Log.Write($"Landing ({reason}) from {horse.Position}, height {horse.HeightAboveGround:F1}");
        }

        private void Fly()
        {
            // Space rises instead of making the horse jump.
            Game.DisableControlThisFrame(eInputType.HorseJump);

            int now = Environment.TickCount;
            float dt = Math.Min(Game.FrameTime, 0.1f);
            bool rise = now < takeoffUntil || heldKeys.Contains(Keys.Space) || Game.IsDisabledControlPressed(eInputType.HorseJump);
            bool descend = heldKeys.Contains(DescendKey);
            Vector3 position = horse.Position;
            Vector3 velocity = horse.Velocity;
            float height = horse.HeightAboveGround;

            float target = 0f;
            if (rise && !descend)
            {
                target = RiseSpeed;
            }
            else if (descend && !rise)
            {
                target = -DescendSpeed(height);
            }

            verticalSpeed = verticalSpeed + (target - verticalSpeed) * Math.Min(1f, VerticalAcceleration * dt);
            flyZ += verticalSpeed * dt;

            // Stay just above the ground. The ground's height comes from how high the horse is above it right now.
            float minZ = position.Z - height + standingHeight + GroundClearance;
            if (flyZ < minZ)
            {
                flyZ = minZ;
                if (verticalSpeed < 0f)
                {
                    verticalSpeed = 0f;
                }
            }

            // The horse's riding moved it horizontally since last frame; keep that, and add the height boost on top.
            float targetMultiplier = SpeedMultiplier(height);
            speedMultiplier += (targetMultiplier - speedMultiplier) * Math.Min(1f, BoostSmoothing * dt);
            float x = position.X;
            float y = position.Y;
            float moving = (float)Math.Sqrt(velocity.X * velocity.X + velocity.Y * velocity.Y);
            if (moving > MinMovingSpeed)
            {
                float extra = Math.Min(moving, MaxRidingSpeed) * (speedMultiplier - 1f) * dt;
                x += velocity.X / moving * extra;
                y += velocity.Y / moving * extra;
            }
            Place(new Vector3(x, y, flyZ), velocity);

        }

        private static float DescendSpeed(float height)
        {
            return Math.Max(MinDescendSpeed, Math.Min(MaxDescendSpeed, height * DescendSpeedPerMeter));
        }

        private static float SpeedMultiplier(float height)
        {
            float t = (height - BoostStartHeight) / (BoostFullHeight - BoostStartHeight);
            t = Math.Max(0f, Math.Min(1f, t));
            return 1f + (MaxSpeedMultiplier - 1f) * t;
        }

        // Places the horse, keeping its horizontal speed and zeroing its vertical speed so the game never sees it
        // moving down. V2 names the native's three flags xAxis/yAxis/zAxis; they behave like GTA V's
        // keepTasks/keepIK/doWarp. Tested in-game:
        //   keep tasks, no IK, no warp  -> faces forward and never falls (used)
        //   keep tasks, keep IK         -> faces forward but falls mid-descent
        //   no tasks                    -> never falls, but the horse is drawn facing opposite its rider
        private void Place(Vector3 at, Vector3 velocity)
        {
            ENTITY.SET_ENTITY_COORDS_NO_OFFSET(horse.Handle, at, true, false, false);
            horse.Velocity = new Vector3(velocity.X, velocity.Y, 0f);
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
