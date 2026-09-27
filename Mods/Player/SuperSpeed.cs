using System;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace RDR2ModMenu
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
        protected override string Category => "Player";

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

        // How recent the last boost must be for a jump to reuse its direction and gait. Moving him by placement
        // sometimes leaves his measured velocity near zero, and on the jump's first frame he may no longer read as
        // sprinting, so either alone could send him straight up.
        private const int RecentBoostMs = 300;

        // Swimming: speed along the way he's swimming, in m/s (normal swimming is about 1.5), with sprint held and
        // without. The game's own swim-speed multiplier is capped low, so it just helps his strokes keep up.
        private const float SwimSpeed = 15f;
        private const float SwimSprintSpeed = 25f;
        private const float SwimAnimationMultiplier = 1.49f;

        // Stop boosting once the bottom ahead is this close below him, so he wades out instead of ramming the bank.
        private const float MinSwimDepth = 1f;

        private bool wasJumping;
        private bool superJumping;
        private bool launching;
        private int launchUntil;
        private int groundedSince;
        private Vector3 jumpVelocity;
        private Vector3 lastBoostDirection;
        private float lastBoostGait;
        private int lastBoostTime;

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

            if (player.IsSwimming)
            {
                Swim(player, position, velocity);
                return;
            }

            // On the water surface the game thinks he's in the air, so keep boosting while running on water.
            bool canBoost = gaitSpeed > 0f && moving > MinMovingSpeed && !player.IsSwimming && (!player.IsInAir || runningOnWater);
            if (!canBoost)
            {
                runningOnWater = false;
                wetLineZ = null;
                if (gaitSpeed == 0f && !player.IsInAir && !player.IsSwimming)
                {
                    standingHeight = player.HeightAboveGround;
                }
                return;
            }

            // Along the way he's actually moving, not the way he faces: while aiming he can walk sideways.
            float extra = gaitSpeed * (SpeedMultiplier - 1f) * Math.Min(Game.FrameTime, 0.1f);
            float nextX = position.X + velocity.X / moving * extra;
            float nextY = position.Y + velocity.Y / moving * extra;

            lastBoostDirection = new Vector3(velocity.X / moving, velocity.Y / moving, 0f);
            lastBoostGait = gaitSpeed;
            lastBoostTime = Environment.TickCount;

            // What he'd be standing on there: the ground, or the water's surface wherever it's above the ground.
            float groundZ;
            if (!Natives.TryGetGroundZ(nextX, nextY, position.Z + 2f, out groundZ))
            {
                groundZ = position.Z - standingHeight;
            }
            float surfaceZ = groundZ;
            bool onWater = false;

            float waterZ;
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
                }
            }

            float nextZ = surfaceZ + standingHeight;
            if (nextZ < position.Z - MaxStepDown || nextZ > position.Z + MaxStepUp)
            {
                // A ledge (let him drop naturally) or a steep rise (let the slope stop him).
                runningOnWater = false;
                return;
            }

            runningOnWater = onWater;

            ENTITY.SET_ENTITY_COORDS_NO_OFFSET(player.Handle, new Vector3(nextX, nextY, nextZ), true, false, false);
        }

        // Jetski swimming: placed along the way he's swimming, like the on-foot boost. At the surface only horizontally,
        // so he stays at the surface; underwater along his full swimming direction.
        private void Swim(Ped player, Vector3 position, Vector3 velocity)
        {
            PLAYER.SET_SWIM_MULTIPLIER_FOR_PLAYER(Game.Player.Handle, SwimAnimationMultiplier);
            runningOnWater = false;
            wetLineZ = null;

            bool underwater = player.IsSwimmingUnderwater;
            Vector3 direction = underwater ? velocity : new Vector3(velocity.X, velocity.Y, 0f);
            float moving = (float)Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y + direction.Z * direction.Z);
            if (moving < MinMovingSpeed)
            {
                return;
            }

            bool sprint = Game.IsControlPressed(eInputType.Sprint);
            float step = (sprint ? SwimSprintSpeed : SwimSpeed) * Math.Min(Game.FrameTime, 0.1f);
            Vector3 next = position + direction * (step / moving);

            float groundZ;
            if (!underwater && Natives.TryGetGroundZ(next.X, next.Y, position.Z + 2f, out groundZ) && groundZ > position.Z - MinSwimDepth)
            {
                return;
            }

            ENTITY.SET_ENTITY_COORDS_NO_OFFSET(player.Handle, next, true, false, false);
        }

        // The placement boost only works on the ground, so a normal jump leaves with un-boosted speed. In the air, speed
        // we set does take effect: launch him upward and carry his full boosted speed through the jump.
        private void StartSuperJump(Ped player, float gaitSpeed, Vector3 velocity, float moving)
        {
            bool recentBoost = lastBoostTime != 0 && Environment.TickCount - lastBoostTime <= RecentBoostMs;
            if (recentBoost)
            {
                jumpVelocity = lastBoostDirection * (lastBoostGait * SpeedMultiplier);
            }
            else if (moving > MinMovingSpeed && gaitSpeed > 0f)
            {
                float speed = gaitSpeed * SpeedMultiplier;
                jumpVelocity = new Vector3(velocity.X / moving * speed, velocity.Y / moving * speed, 0f);
            }
            else
            {
                jumpVelocity = new Vector3(velocity.X, velocity.Y, 0f);
            }

            superJumping = true;
            launching = true;
            launchUntil = Environment.TickCount + LaunchWindowMs;
            groundedSince = 0;
            runningOnWater = false;
            wetLineZ = null;

            // A 10 m jump would hurt on landing.
            player.IsInvincible = true;
        }

        private void UpdateSuperJump(Ped player)
        {
            int now = Environment.TickCount;
            bool inAir = player.IsInAir;

            if (launching)
            {
                // The jump animation starts on the ground, where set speed is ignored, so keep launching until he's
                // actually rising. Being in the air isn't enough: running on water he already counts as in the air,
                // and stopping there left him with only a normal jump's height.
                bool launched = inAir && player.Velocity.Z > JumpUpSpeed / 2f;
                if (!launched && now <= launchUntil)
                {
                    player.Velocity = new Vector3(jumpVelocity.X, jumpVelocity.Y, JumpUpSpeed);
                    return;
                }
                launching = false;
                if (!launched)
                {
                    Log.Write($"Super jump launch timed out, vertical speed {player.Velocity.Z:F1} m/s");
                }
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
            }
        }


        protected override void OnDisable()
        {
            Ped player = Game.Player.Character;
            player.CanRagdoll = true;
            PED.SET_PED_MOVE_RATE_OVERRIDE(player.Handle, 1f);
            PLAYER.SET_SWIM_MULTIPLIER_FOR_PLAYER(Game.Player.Handle, 1f);
            runningOnWater = false;
            if (superJumping)
            {
                superJumping = false;
                player.IsInvincible = false;
            }
        }
    }
}
