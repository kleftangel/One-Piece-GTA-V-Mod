using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;
using GTA.Math;

namespace AnimeCharacterMod
{
    public partial class AceController : Script
    {
        private static List<Tuple<int, int>> activeFireHandles = new List<Tuple<int, int>>();
        private const int FireLifetimeMs = 5000;

        // GTA V's engine only allows a limited number of simultaneous script fires across
        // the whole world (ambient fires included). Past that cap, START_SCRIPT_FIRE silently
        // returns handle 0 and nothing spawns. We self-limit well under that ceiling so our
        // own abilities never blindly burn through the pool and starve each other out.
        private const int MaxConcurrentFires = 35;

        // Centralized fire creation used by every ability. Refuses to request a new fire once
        // we're at budget, and only tracks successful (non-zero) handles for cleanup.
        private static int TryStartTrackedFire(float x, float y, float z, int fireLevel = 25, bool isGasFire = true)
        {
            if (activeFireHandles.Count >= MaxConcurrentFires)
            {
                return 0;
            }

            int fireHandle = Function.Call<int>(Hash.START_SCRIPT_FIRE, x, y, z, fireLevel, isGasFire);
            if (fireHandle != 0)
            {
                activeFireHandles.Add(new Tuple<int, int>(fireHandle, Game.GameTime));
            }

            return fireHandle;
        }

        private static void ProcessFireCleanup()
        {
            int currentTime = Game.GameTime;
            for (int i = activeFireHandles.Count - 1; i >= 0; i--)
            {
                if (currentTime - activeFireHandles[i].Item2 >= FireLifetimeMs)
                {
                    int handle = activeFireHandles[i].Item1;
                    if (handle != 0)
                    {
                        Function.Call(Hash.REMOVE_SCRIPT_FIRE, handle);
                    }
                    activeFireHandles.RemoveAt(i);
                }
            }
        }

        // LOCKED TO RT: Baseline Molotov Trail
        private void ExecuteMolotovTrail(Ped playerPed)
        {
            Vector3 bonePos = Function.Call<Vector3>(Hash.GET_PED_BONE_COORDS, playerPed.Handle, 28422);
            Vector3 gameplayCamDir = GameplayCamera.Direction;
            Vector3 elevatedDirection = new Vector3(gameplayCamDir.X, gameplayCamDir.Y, gameplayCamDir.Z + 0.15f).Normalized;
            Vector3 targetPosition = bonePos + (elevatedDirection * 150.0f);

            RaycastResult raycast = World.Raycast(bonePos, targetPosition, IntersectFlags.Everything, playerPed);
            Vector3 endPoint = raycast.DidHit ? raycast.HitPosition : targetPosition;

            Vector3 currentPos = bonePos;
            Vector3 travelDirection = (endPoint - bonePos).Normalized;
            float totalDistance = Vector3.Distance(bonePos, endPoint);
            float stepDistance = 2.0f;
            int steps = (int)(totalDistance / stepDistance);

            Function.Call(Hash.REQUEST_NAMED_PTFX_ASSET, "core");
            if (Function.Call<bool>(Hash.HAS_NAMED_PTFX_ASSET_LOADED, "core"))
            {
                Function.Call(Hash.USE_PARTICLE_FX_ASSET, "core");
                Function.Call(Hash.START_PARTICLE_FX_NON_LOOPED_AT_COORD,
                    "fire_wrecked_car",
                    bonePos.X, bonePos.Y, bonePos.Z,
                    0f, 0f, 0f, 5.0f, false, false, false);
            }

            for (int i = 0; i < steps; i++)
            {
                currentPos += travelDirection * stepDistance;

                // Only drop an actual ground fire every 3rd step -- the particle trail below still
                // renders every step, so the trail still looks continuous, but we're no longer
                // requesting 50-75 real fires in a single swing.
                if (i % 3 == 0)
                {
                    TryStartTrackedFire(currentPos.X, currentPos.Y, currentPos.Z, 25, true);
                }

                Ped[] nearbyPeds = World.GetNearbyPeds(currentPos, 15.0f);
                foreach (Ped ped in nearbyPeds)
                {
                    if (ped != playerPed)
                    {
                        Function.Call(Hash.START_ENTITY_FIRE, ped.Handle);
                        ped.Health -= 50;

                        Vector3 pushDirection = (ped.Position - currentPos).Normalized + new Vector3(0f, 0f, 0.2f);
                        Function.Call(Hash.APPLY_FORCE_TO_ENTITY, ped.Handle, 1,
                            pushDirection.X * 1.5f, pushDirection.Y * 1.5f, pushDirection.Z * 0.8f,
                            0f, 0f, 0f, 0, false, true, true, true, true);
                    }
                }

                Vehicle[] nearbyVehicles = World.GetNearbyVehicles(currentPos, 15.0f);
                foreach (Vehicle veh in nearbyVehicles)
                {
                    Function.Call(Hash.START_ENTITY_FIRE, veh.Handle);
                    veh.BodyHealth -= 150f;

                    Vector3 pushDirection = (veh.Position - currentPos).Normalized + new Vector3(0f, 0f, 0.3f);
                    Function.Call(Hash.APPLY_FORCE_TO_ENTITY, veh.Handle, 1,
                        pushDirection.X * 4.0f, pushDirection.Y * 4.0f, pushDirection.Z * 1.8f,
                        0f, 0f, 0f, 0, false, true, true, true, true);
                }

                if (Function.Call<bool>(Hash.HAS_NAMED_PTFX_ASSET_LOADED, "core"))
                {
                    Function.Call(Hash.USE_PARTICLE_FX_ASSET, "core");
                    Function.Call(Hash.START_PARTICLE_FX_NON_LOOPED_AT_COORD,
                        "ent_ray_prologue_lightning",
                        currentPos.X, currentPos.Y, currentPos.Z,
                        0f, 0f, 0f, 6.0f, false, false, false);
                }
            }
        }

        // LOCKED TO B: Fires exactly 5 delayed consecutive explosions expanding outwards
        private void StartRapidExplosionBarrage(Ped playerPed)
        {
            Vector3 bonePos = Function.Call<Vector3>(Hash.GET_PED_BONE_COORDS, playerPed.Handle, 28422);
            Vector3 travelDirection = GameplayCamera.Direction.Normalized;

            for (int blastIndex = 1; blastIndex <= 5; blastIndex++)
            {
                Vector3 targetBlastPos = bonePos + (travelDirection * (blastIndex * 6.0f));

                Function.Call(Hash.ADD_EXPLOSION, targetBlastPos.X, targetBlastPos.Y, targetBlastPos.Z, 23, 6.0f, true, false, 1.0f);
                TryStartTrackedFire(targetBlastPos.X, targetBlastPos.Y, targetBlastPos.Z, 25, true);

                Script.Yield();
            }
        }

         // LOCKED TO DPAD DOWN: Ace's flame pillar vortex -- everything caught inside ignites
        private void ExecuteFlamePillar(Ped playerPed)
        {
            int playerPedId = playerPed.Handle;

            // ============================================================
            // 1. TIMER / STATE
            // ============================================================

            twisterDurationTimer--;

            if (spinDelayTimer > 0)
                spinDelayTimer--;

            Vector3 currentPos = playerPed.Position;

            // ============================================================
            // 2. TERMINAL DISPERSAL
            // ============================================================

            if (twisterDurationTimer <= 0 || playerPed.IsDead)
            {
                isFlamePillarActive = false;

                Function.Call(
                    Hash.RESET_PED_RAGDOLL_TIMER,
                    playerPedId
                );

                float explosionRadius = 15.0f;

                // Final ground fire
                TryStartTrackedFire(
                    currentPos.X,
                    currentPos.Y,
                    currentPos.Z,
                    40,
                    true
                );

                // Final ignition for peds
                Ped[] finalPeds = World.GetNearbyPeds(
                    currentPos,
                    explosionRadius
                );

                foreach (Ped targetPed in finalPeds)
                {
                    int targetPedId = targetPed.Handle;

                    if (targetPedId == playerPedId)
                        continue;

                    Function.Call(
                        Hash.START_ENTITY_FIRE,
                        targetPedId
                    );
                }

                // Final ignition for vehicles
                Vehicle[] finalVehicles = World.GetNearbyVehicles(
                    currentPos,
                    explosionRadius
                );

                foreach (Vehicle targetVehicle in finalVehicles)
                {
                    Function.Call(
                        Hash.START_ENTITY_FIRE,
                        targetVehicle.Handle
                    );
                }

                return;
            }

            // ============================================================
            // 3. STATIONARY SPINNING PLAYER
            // ============================================================

            Function.Call(
                Hash.SET_PED_TO_RAGDOLL,
                playerPedId,
                1000,
                1000,
                1,
                true,
                true,
                false
            );

            // Keep Ace fixed in place while the vortex spins around him.
            Function.Call(
                Hash.SET_ENTITY_VELOCITY,
                playerPedId,
                0.0f,
                0.0f,
                0.0f
            );

            // Faster visual rotation than the original version.
            twisterSpinAngle += 32.0f;

            if (twisterSpinAngle >= 360.0f)
                twisterSpinAngle -= 360.0f;

            Function.Call(
                Hash.SET_ENTITY_ROTATION,
                playerPedId,
                0.0f,
                0.0f,
                twisterSpinAngle,
                2,
                true
            );

            // ============================================================
            // 4. DELAYED PHYSICS / VISUAL EFFECT
            // ============================================================

            if (spinDelayTimer <= 0)
            {
                // --------------------------------------------------------
                // HAND FORCE
                // --------------------------------------------------------

                Vector3 rightHandPos = Function.Call<Vector3>(
                    Hash.GET_PED_BONE_COORDS,
                    playerPedId,
                    28422,
                    0f,
                    0f,
                    0f
                );

                Vector3 leftHandPos = Function.Call<Vector3>(
                    Hash.GET_PED_BONE_COORDS,
                    playerPedId,
                    18905,
                    0f,
                    0f,
                    0f
                );

                Vector3 rightPullDir = rightHandPos - currentPos;
                rightPullDir.Z = 0.0f;

                if (rightPullDir.Length() > 0.001f)
                    rightPullDir.Normalize();

                Vector3 leftPullDir = leftHandPos - currentPos;
                leftPullDir.Z = 0.0f;

                if (leftPullDir.Length() > 0.001f)
                    leftPullDir.Normalize();

                float armExtendForceFactor = 450.0f;

                Function.Call(
                    Hash.APPLY_FORCE_TO_ENTITY,
                    playerPedId,
                    1,
                    rightPullDir.X * armExtendForceFactor,
                    rightPullDir.Y * armExtendForceFactor,
                    0.0f,
                    rightHandPos.X,
                    rightHandPos.Y,
                    rightHandPos.Z,
                    0,
                    false,
                    true,
                    true,
                    false,
                    true
                );

                Function.Call(
                    Hash.APPLY_FORCE_TO_ENTITY,
                    playerPedId,
                    1,
                    leftPullDir.X * armExtendForceFactor,
                    leftPullDir.Y * armExtendForceFactor,
                    0.0f,
                    leftHandPos.X,
                    leftHandPos.Y,
                    leftHandPos.Z,
                    0,
                    false,
                    true,
                    true,
                    false,
                    true
                );

                // ========================================================
                // 5. LOAD FLAME PARTICLE ASSET
                // ========================================================

                // Request the asset every tick until GTA has actually
                // finished streaming it in.
                Function.Call(
                    Hash.REQUEST_NAMED_PTFX_ASSET,
                    "core"
                );

                bool flameFxLoaded = Function.Call<bool>(
                    Hash.HAS_NAMED_PTFX_ASSET_LOADED,
                    "core"
                );

                // ========================================================
                // 6. MAIN FLAME VORTEX
                // ========================================================

                if (flameFxLoaded)
                {
                    Function.Call(
                        Hash.USE_PARTICLE_FX_ASSET,
                        "core"
                    );

                    // Fewer, larger particles are much more reliable than
                    // spawning 30 tiny particles every frame.
                    const int flameCount = 18;

                    for (int i = 0; i < flameCount; i++)
                    {
                        float fraction =
                            (float)i / (float)flameCount;

                        // ------------------------------------------------
                        // HEIGHT
                        // ------------------------------------------------
                        //
                        // Pillar is approximately 7.5m tall.
                        //
                        float height =
                            -0.7f +
                            (fraction * 7.5f);

                        // ------------------------------------------------
                        // FUNNEL SHAPE
                        // ------------------------------------------------
                        //
                        // Narrow at the bottom, wide at the top.
                        //
                        float radius =
                            1.25f +
                            (fraction * 3.75f);

                        // ------------------------------------------------
                        // SPIRAL ROTATION
                        // ------------------------------------------------
                        //
                        // Each particle is separated around the circle,
                        // while the whole structure rotates with Ace.
                        //
                        float angleDegrees =
                            twisterSpinAngle +
                            (i * 20.0f) +
                            (fraction * 120.0f);

                        float angleRadians =
                            angleDegrees * 0.0174532924f;

                        // ------------------------------------------------
                        // SMALL VERTICAL WOBBLE
                        // ------------------------------------------------
                        //
                        // Prevents the pillar from looking like a perfect
                        // mathematical cone.
                        //
                        float wobble =
                            (float)Math.Sin(
                                (Game.GameTime * 0.004f) +
                                i
                            ) * 0.35f;

                        Vector3 flamePoint = new Vector3(
                            currentPos.X +
                                ((float)Math.Cos(angleRadians) * radius) +
                                wobble,

                            currentPos.Y +
                                ((float)Math.Sin(angleRadians) * radius) +
                                wobble,

                            currentPos.Z +
                                height
                        );

                        // ------------------------------------------------
                        // OUTER FLAME
                        // ------------------------------------------------

                        Function.Call(
                            Hash.USE_PARTICLE_FX_ASSET,
                            "core"
                        );

                        Function.Call(
                            Hash.START_PARTICLE_FX_NON_LOOPED_AT_COORD,
                            "ent_sht_flame",

                            flamePoint.X,
                            flamePoint.Y,
                            flamePoint.Z,

                            0.0f,
                            0.0f,
                            angleDegrees,

                            2.4f,

                            false,
                            false,
                            false
                        );

                        // ------------------------------------------------
                        // INNER FLAME
                        // ------------------------------------------------
                        //
                        // A second smaller flame layer makes the centre
                        // substantially denser instead of looking like
                        // 18 isolated flame sprites.
                        //
                        if (i % 2 == 0)
                        {
                            float innerRadius =
                                radius * 0.55f;

                            Vector3 innerPoint = new Vector3(
                                currentPos.X +
                                    ((float)Math.Cos(angleRadians + 0.8f)
                                    * innerRadius),

                                currentPos.Y +
                                    ((float)Math.Sin(angleRadians + 0.8f)
                                    * innerRadius),

                                currentPos.Z +
                                    height +
                                    0.25f
                            );

                            Function.Call(
                                Hash.USE_PARTICLE_FX_ASSET,
                                "core"
                            );

                            Function.Call(
                                Hash.START_PARTICLE_FX_NON_LOOPED_AT_COORD,
                                "ent_sht_flame",

                                innerPoint.X,
                                innerPoint.Y,
                                innerPoint.Z,

                                0.0f,
                                0.0f,
                                angleDegrees + 90.0f,

                                1.6f,

                                false,
                                false,
                                false
                            );
                        }
                    }

                    // ====================================================
                    // 7. CENTRAL FIRE COLUMN
                    // ====================================================
                    //
                    // Adds a few large flames through the centre so the
                    // vortex doesn't appear hollow from certain angles.
                    //

                    for (int i = 0; i < 5; i++)
                    {
                        float centerHeight =
                            (i * 1.45f) - 0.4f;

                        float centerAngle =
                            (twisterSpinAngle * 1.7f) +
                            (i * 72.0f);

                        float centerRadians =
                            centerAngle * 0.0174532924f;

                        float centerRadius = 0.7f;

                        Vector3 centerPoint = new Vector3(
                            currentPos.X +
                                ((float)Math.Cos(centerRadians)
                                * centerRadius),

                            currentPos.Y +
                                ((float)Math.Sin(centerRadians)
                                * centerRadius),

                            currentPos.Z +
                                centerHeight
                        );

                        Function.Call(
                            Hash.USE_PARTICLE_FX_ASSET,
                            "core"
                        );

                        Function.Call(
                            Hash.START_PARTICLE_FX_NON_LOOPED_AT_COORD,
                            "ent_sht_flame",

                            centerPoint.X,
                            centerPoint.Y,
                            centerPoint.Z,

                            0.0f,
                            0.0f,
                            centerAngle,

                            2.8f,

                            false,
                            false,
                            false
                        );
                    }
                }

                // ========================================================
                // 8. REAL GROUND FIRES
                // ========================================================
                //
                // These are separate from the visual PTFX system.
                // They are deliberately throttled so the GTA script-fire
                // pool isn't exhausted.
                //

                for (int i = 0; i < 6; i++)
                {
                    float fraction =
                        (float)i / 6.0f;

                    float groundAngle =
                        twisterSpinAngle +
                        (i * 60.0f);

                    float groundRadians =
                        groundAngle * 0.0174532924f;

                    float groundRadius =
                        1.5f +
                        (fraction * 2.5f);

                    Vector3 groundPoint = new Vector3(
                        currentPos.X +
                            ((float)Math.Cos(groundRadians)
                            * groundRadius),

                        currentPos.Y +
                            ((float)Math.Sin(groundRadians)
                            * groundRadius),

                        currentPos.Z - 0.8f
                    );

                    // Only create real GTA fires occasionally.
                    if (i % 3 == 0 &&
                        twisterDurationTimer % 8 == 0)
                    {
                        TryStartTrackedFire(
                            groundPoint.X,
                            groundPoint.Y,
                            groundPoint.Z,
                            15,
                            true
                        );
                    }
                }
            }

            // ============================================================
            // 9. CONTINUOUS VORTEX SUCTION / IGNITION
            // ============================================================

            float suctionRadius = 12.0f;
            float eyeOfTheStormRadius = 2.5f;

            // ------------------------------------------------------------
            // PEDS
            // ------------------------------------------------------------

            Ped[] nearbyPeds = World.GetNearbyPeds(
                currentPos,
                suctionRadius
            );

            foreach (Ped targetPed in nearbyPeds)
            {
                int targetPedId = targetPed.Handle;

                if (targetPedId == playerPedId)
                    continue;

                Vector3 pullVector =
                    currentPos - targetPed.Position;

                float distance =
                    pullVector.Length();

                if (distance > 0.001f)
                    pullVector.Normalize();

                // Ignite target continuously.
                Function.Call(
                    Hash.START_ENTITY_FIRE,
                    targetPedId
                );

                // Damage.
                targetPed.Health = Math.Max(
                    0,
                    targetPed.Health - 4
                );

                // Ragdoll.
                Function.Call(
                    Hash.SET_PED_TO_RAGDOLL,
                    targetPedId,
                    1000,
                    1000,
                    1,
                    true,
                    true,
                    false
                );

                Vector3 velocityForce;

                // --------------------------------------------------------
                // EYE OF THE STORM
                // --------------------------------------------------------

                if (distance <= eyeOfTheStormRadius)
                {
                    Vector3 orbitalRight =
                        Vector3.Cross(
                            pullVector,
                            Vector3.WorldUp
                        );

                    if (orbitalRight.Length() > 0.001f)
                        orbitalRight.Normalize();

                    float orbitalVelocityFactor = 18.0f;

                    velocityForce =
                        (orbitalRight * orbitalVelocityFactor) +
                        (Vector3.WorldUp * 1.5f);
                }
                else
                {
                    float pullIntensity =
                        Math.Max(
                            5.0f,
                            (suctionRadius - distance) * 4.0f
                        );

                    velocityForce =
                        (pullVector * pullIntensity) +
                        (Vector3.WorldUp * 2.0f);
                }

                Function.Call(
                    Hash.SET_ENTITY_VELOCITY,
                    targetPedId,
                    velocityForce.X,
                    velocityForce.Y,
                    velocityForce.Z
                );
            }

            // ------------------------------------------------------------
            // VEHICLES
            // ------------------------------------------------------------

            Vehicle[] nearbyVehicles =
                World.GetNearbyVehicles(
                    currentPos,
                    suctionRadius
                );

            foreach (Vehicle targetVehicle in nearbyVehicles)
            {
                int targetVehicleId =
                    targetVehicle.Handle;

                Vector3 pullVector =
                    currentPos - targetVehicle.Position;

                float distance =
                    pullVector.Length();

                if (distance > 0.001f)
                    pullVector.Normalize();

                // Ignite vehicle.
                Function.Call(
                    Hash.START_ENTITY_FIRE,
                    targetVehicleId
                );

                // Damage engine.
                float engineHealth =
                    Function.Call<float>(
                        Hash.GET_VEHICLE_ENGINE_HEALTH,
                        targetVehicleId
                    );

                Function.Call(
                    Hash.SET_VEHICLE_ENGINE_HEALTH,
                    targetVehicleId,
                    engineHealth - 8.0f
                );

                Vector3 velocityForce;

                // --------------------------------------------------------
                // VEHICLE ORBIT
                // --------------------------------------------------------

                if (distance <= eyeOfTheStormRadius + 1.0f)
                {
                    Vector3 orbitalRight =
                        Vector3.Cross(
                            pullVector,
                            Vector3.WorldUp
                        );

                    if (orbitalRight.Length() > 0.001f)
                        orbitalRight.Normalize();

                    float orbitalVelocityFactor = 22.0f;

                    velocityForce =
                        (orbitalRight * orbitalVelocityFactor) +
                        (Vector3.WorldUp * 1.0f);
                }
                else
                {
                    float pullIntensity =
                        Math.Max(
                            8.0f,
                            (suctionRadius - distance) * 5.0f
                        );

                    velocityForce =
                        (pullVector * pullIntensity) +
                        (Vector3.WorldUp * 1.5f);
                }

                Function.Call(
                    Hash.SET_ENTITY_VELOCITY,
                    targetVehicleId,
                    velocityForce.X,
                    velocityForce.Y,
                    velocityForce.Z
                );
            }

            // ============================================================
            // 10. CLEAN UP OLD REAL FIRE HANDLES
            // ============================================================

            ProcessFireCleanup();
        }
    }
}