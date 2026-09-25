using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RDR2;
using RDR2.Math;
using RDR2.Native;
using Screen = RDR2.UI.Screen;

namespace AIPlayground
{
    // While enabled, press F7 on a horse to take off or land. In the air the horse rides as normal
    // (W walk, Shift faster, A/D steer, with the game's own animations); this mod only controls height:
    // Space rises, X descends, and otherwise the horse holds its altitude.
    //
    // How height is controlled, and why (all found by testing in-game):
    // - The horse's own riding moves it horizontally. The mod never sets its heading or horizontal speed:
    //   doing so made the game draw the horse facing opposite its rider.
    // - Height is set by placing the horse every frame, never by vertical speed. Any downward speed above a crawl
    //   switches the horse into the game's falling state, after which it ignores the speed we set and drops.
    //   Switching between speed and placement also triggered falls.
    // - Placement keeps the horse's tasks but not its IK (see Place): clearing tasks flipped the horse around,
    //   and keeping IK let the falling state start mid-descent.
    public class FlyingHorse : ModScript
    {
        protected override string Description => "On a horse, press F7 to take off or land. Ride as normal (W walk, Shift faster, A/D steer); Space rises, X descends.";

        private const Keys FlyKey = Keys.F7;

        // Not Ctrl (the game's horse-stop control) or C (turns the camera around).
        private const Keys DescendKey = Keys.X;

        private const float RiseSpeed = 8f;

        // TEMPORARY: [ and ] adjust the descent speed in-game.
        private float descendSpeed = 4f;
        private const Keys DescendSlowerKey = Keys.OemOpenBrackets;
        private const Keys DescendFasterKey = Keys.OemCloseBrackets;

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

        // Space and X are read straight from the keyboard: mounted, the game didn't report Space
        // through the horse-jump control.
        private readonly HashSet<Keys> heldKeys = new HashSet<Keys>();

        private FlightState state = FlightState.Grounded;
        private Ped horse;
        private float flyZ;
        private float verticalSpeed;
        private float standingHeight;
        private int takeoffUntil;
        private int landingStartedAt;
        private int stillSince;
        private int nextLogTime;

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

            if (e.KeyCode == DescendSlowerKey || e.KeyCode == DescendFasterKey)
            {
                float step = e.KeyCode == DescendFasterKey ? 0.5f : -0.5f;
                descendSpeed = Math.Max(0.5f, descendSpeed + step);
                Screen.DisplaySubtitle($"Descend speed: {descendSpeed:F1} m/s");
                Log.Write($"Descend speed -> {descendSpeed:F1}");
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
                target = -descendSpeed;
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

            // The horse's riding moved it horizontally since last frame; keep that and set only the height.
            Place(new Vector3(position.X, position.Y, flyZ), velocity);

            if (now > nextLogTime)
            {
                nextLogTime = now + 1000;
                Log.Write($"Flying: pos {position}, fly Z {flyZ:F1}, vel {velocity}, vertical {verticalSpeed:F1} (target {target:F1}), height {height:F1}, rise {rise}, descend {descend} at {descendSpeed:F1}, inWater {horse.IsInWater}, inAir {horse.IsInAir}");
            }
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
