using System;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace AIPlayground
{
    // Arthur moves SpeedMultiplier times faster on foot, and can run across water (he sinks when he stops).
    // Walk, run and sprint as normal.
    //
    // On the ground his animation moves him and any speed we set is ignored, so each frame the mod instead places him
    // a little further along the way he's already moving (keeping his tasks, as with the flying horse, so his
    // animation isn't disturbed), at standing height above the ground or water surface there. His movement animations
    // also play faster so his legs keep up. At a ledge or a steep rise the boost pauses, so he drops off naturally or
    // is stopped by the slope rather than being snapped down or up it. Placement skips collision, so at speed he can
    // still clip into walls. Jumps launch him much higher and keep his boosted speed through the air.
    public class SuperSpeed : ModScript
    {
        protected override string Description => "Arthur moves 5x faster on foot, jumps much higher, and can run on water (he sinks when he stops). He won't ragdoll while it's on.";

        private const float SpeedMultiplier = 5f;

        // How much faster his movement animations play.
        private const float AnimationRate = 1.5f;

        // Arthur's normal on-foot speeds, in m/s. The extra distance is based on these rather than his measured
        // speed, which would include the boost and snowball.
        private const float WalkSpeed = 1.6f;
        private const float RunSpeed = 4f;
        private const float SprintSpeed = 6.5f;

        // Below this, which way he's moving isn't reliable enough to boost along.
        private const float MinMovingSpeed = 0.3f;

        // How far the surface ahead may drop or rise in one step before the boost pauses (a ledge or a wall/steep rise).
        private const float MaxStepDown = 1f;
        private const float MaxStepUp = 1f;

        // His height above the ground when standing (his position is at his hips). Measured while he isn't being
        // boosted; this is the starting guess.
        private float standingHeight = 1f;

        private bool runningOnWater;

        // Backup for when the water probe finds nothing: the height of the water line, recorded the moment
        // his feet got wet while being boosted. He's kept at least this high until he stops or is back on land.
        private float? wetLineZ;

        // Super jump: upward launch speed (about 10 m high), how long to keep trying to launch while the jump animation
        // is still on the ground, and how long he must be back on the ground before the jump counts as landed.
        private const float JumpUpSpeed = 14f;
        private const int LaunchWindowMs = 500;
        private const int LandedMs = 300;

        private bool wasJumping;
        private bool superJumping;
        private bool launching;
        private int launchUntil;
        private int groundedSince;
        private Vector3 jumpVelocity;
        private Vector3 lastLoggedPosition;
        private int nextLogTime;

        protected override void OnEnabledTick()
        {
            Ped player = Game.Player.Character;
            if (!player.IsOnFoot)
            {
                return;
            }

            // At this speed every fence and bystander would knock him over.
            player.CanRagdoll = false;
            PED.SET_PED_MOVE_RATE_OVERRIDE(player.Handle, AnimationRate);

            float gaitSpeed = player.IsSprinting ? SprintSpeed : player.IsRunning ? RunSpeed : player.IsWalking ? WalkSpeed : 0f;
            Vector3 position = player.Position;
            Vector3 velocity = player.Velocity;
            float moving = (float)Math.Sqrt(velocity.X * velocity.X + velocity.Y * velocity.Y);

            bool jumping = player.IsJumping;
            if (jumping && !wasJumping && !superJumping && !player.IsVaulting && !player.IsClimbing)
            {
                StartSuperJump(player, gaitSpeed, velocity, moving);
            }
            wasJumping = jumping;
            if (superJumping)
            {
                UpdateSuperJump(player);
                return;
            }

            // On the water surface the game thinks he's in the air, so keep boosting while running on water.
            bool canBoost = gaitSpeed > 0f && moving > MinMovingSpeed && !player.IsSwimming && (!player.IsInAir || runningOnWater);
            if (!canBoost)
            {
                if (runningOnWater)
                {
                    Log.Write($"Stopped on water at {position}, sinking");
                }
                runningOnWater = false;
                wetLineZ = null;
                if (gaitSpeed == 0f && !player.IsInAir && !player.IsSwimming)
                {
                    standingHeight = player.HeightAboveGround;
                }
                LogSpeed(player, gaitSpeed, "");
                return;
            }

            // Along the way he's actually moving, not the way he faces: while aiming he can walk sideways.
            float extra = gaitSpeed * (SpeedMultiplier - 1f) * Math.Min(Game.FrameTime, 0.1f);
            float nextX = position.X + velocity.X / moving * extra;
            float nextY = position.Y + velocity.Y / moving * extra;

            // What he'd be standing on there: the ground, or the water's surface wherever it's above the ground.
            float groundZ;
            if (!Natives.TryGetGroundZ(nextX, nextY, position.Z + 2f, out groundZ))
            {
                groundZ = position.Z - standingHeight;
            }
            float surfaceZ = groundZ;
            bool onWater = false;

            float waterZ;
            string waterMethod = "probe";
            if (Natives.TryGetWaterZ(nextX, nextY, position.Z, out waterZ) && waterZ > groundZ)
            {
                surfaceZ = waterZ;
                onWater = true;
            }

            // Backup: his feet just got wet, so the water line is at his feet. Hold him there as the bottom drops away.
            // In testing the probe usually got there first, but this covered the moments it didn't.
            if (player.IsInWater && wetLineZ == null)
            {
                wetLineZ = position.Z - standingHeight;
                Log.Write($"Feet wet at {position}: water line {wetLineZ:F1}, ground ahead {groundZ:F1}");
            }
            if (wetLineZ.HasValue)
            {
                if (groundZ > wetLineZ.Value + 0.2f)
                {
                    wetLineZ = null;
                }
                else if (wetLineZ.Value > surfaceZ)
                {
                    surfaceZ = wetLineZ.Value;
                    onWater = true;
                    waterMethod = "wet line";
                }
            }

            float nextZ = surfaceZ + standingHeight;
            if (nextZ < position.Z - MaxStepDown || nextZ > position.Z + MaxStepUp)
            {
                // A ledge (let him drop naturally) or a steep rise (let the slope stop him).
                runningOnWater = false;
                LogSpeed(player, gaitSpeed, $"paused: surface ahead {nextZ - position.Z:+0.0;-0.0} m");
                return;
            }

            if (onWater && !runningOnWater)
            {
                Log.Write($"Running on water at {position}, surface {surfaceZ:F1} (from {waterMethod}), ground {groundZ:F1}");
            }
            runningOnWater = onWater;

            ENTITY.SET_ENTITY_COORDS_NO_OFFSET(player.Handle, new Vector3(nextX, nextY, nextZ), true, false, false);
            LogSpeed(player, gaitSpeed, $"surface {(onWater ? "water" : "ground")} {surfaceZ:F1}, standing height {standingHeight:F2}");
        }

        // The placement boost only works on the ground, so a normal jump leaves with un-boosted speed. In the air, speed
        // we set does take effect: launch him upward and carry his full boosted speed through the jump.
        private void StartSuperJump(Ped player, float gaitSpeed, Vector3 velocity, float moving)
        {
            float speed = gaitSpeed * SpeedMultiplier;
            jumpVelocity = moving > MinMovingSpeed && speed > 0f
                ? new Vector3(velocity.X / moving * speed, velocity.Y / moving * speed, 0f)
                : new Vector3(velocity.X, velocity.Y, 0f);

            superJumping = true;
            launching = true;
            launchUntil = Environment.TickCount + LaunchWindowMs;
            groundedSince = 0;
            runningOnWater = false;
            wetLineZ = null;

            // A 10 m jump would hurt on landing.
            player.IsInvincible = true;
            Log.Write($"Super jump from {player.Position}, forward speed {speed:F1} m/s");
        }

        private void UpdateSuperJump(Ped player)
        {
            int now = Environment.TickCount;
            bool inAir = player.IsInAir;

            if (launching)
            {
                // The jump animation starts on the ground, where set speed is ignored, so keep launching until he's up.
                player.Velocity = new Vector3(jumpVelocity.X, jumpVelocity.Y, JumpUpSpeed);
                if (inAir || now > launchUntil)
                {
                    launching = false;
                }
                return;
            }

            if (inAir)
            {
                // Keep his forward speed; leave the vertical to gravity.
                groundedSince = 0;
                player.Velocity = new Vector3(jumpVelocity.X, jumpVelocity.Y, player.Velocity.Z);
                return;
            }

            if (groundedSince == 0)
            {
                groundedSince = now;
            }
            else if (now - groundedSince > LandedMs)
            {
                superJumping = false;
                player.IsInvincible = false;
                Log.Write($"Super jump landed at {player.Position}");
            }
        }

        private void LogSpeed(Ped player, float gaitSpeed, string detail)
        {
            int now = Environment.TickCount;
            if (now <= nextLogTime)
            {
                return;
            }

            Vector3 position = player.Position;
            float dx = position.X - lastLoggedPosition.X;
            float dy = position.Y - lastLoggedPosition.Y;
            float actual = (float)Math.Sqrt(dx * dx + dy * dy) / ((now - nextLogTime + 1000) / 1000f);
            string gait = player.IsSprinting ? "sprint" : player.IsRunning ? "run" : player.IsWalking ? "walk" : "still";
            Log.Write($"{gait}: actual speed {actual:F1} m/s (target {gaitSpeed * SpeedMultiplier:F1}), inAir {player.IsInAir}, swimming {player.IsSwimming}. {detail}");
            lastLoggedPosition = position;
            nextLogTime = now + 1000;
        }

        protected override void OnDisable()
        {
            Ped player = Game.Player.Character;
            player.CanRagdoll = true;
            PED.SET_PED_MOVE_RATE_OVERRIDE(player.Handle, 1f);
            runningOnWater = false;
            if (superJumping)
            {
                superJumping = false;
                player.IsInvincible = false;
            }
        }
    }
}
