using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;
using GTA.Math;

namespace AnimeCharacterMod
{
    public partial class CrocodileController : Script
    {
        public void UpdateActiveFrameLoops(Ped player, int playerPedId)
        {
            int currentGameTime = Game.GameTime;

            // DYNAMIC SEQUENTIAL GROUND BLAST LINE ENGINE
            if (groundBlastStepIndex >= 0 && currentGameTime >= nextGroundBlastTime)
            {
                // Calculate absolute positions forward along the mathematically locked heading path axis
                float headingRadians = (float)(lockedGroundBlastHeading * (Math.PI / 180.0));
                float distancePerStep = 3.2f * (groundBlastStepIndex + 1);

                // FIX: Changed headingRad to headingRadians to match line 19 exactly
                float targetX = baseGroundBlastOrigin.X - ((float)Math.Sin(headingRadians) * distancePerStep);
                float targetY = baseGroundBlastOrigin.Y + ((float)Math.Cos(headingRadians) * distancePerStep);

                Vector3 blastStepCoords = new Vector3(targetX, targetY, baseGroundBlastOrigin.Z);

                ExecuteSingleGroundBlastStep(player, playerPedId, blastStepCoords);

                groundBlastStepIndex++;
                nextGroundBlastTime = currentGameTime + 130; // 130ms stagger delay between exploding craters

                // End the wave cycle after 10 consecutive eruptions are deployed
                if (groundBlastStepIndex >= 10)
                {
                    groundBlastStepIndex = -1; // Reset parameters and release thread lock boundaries
                }
            }
        }

        public void TriggerGroundSandBlastLine(Ped player)
        {
            if (player == null || player.IsInVehicle() || groundBlastStepIndex >= 0) return;

            double timePassed = (DateTime.Now - lastGroundBlastTime).TotalMilliseconds;
            if (timePassed <= GroundBlastCooldownMs) return;

            if (player.Weapons.Current.Hash != WeaponHash.Unarmed)
            {
                player.Weapons.Select(WeaponHash.Unarmed);
            }

            // Fire powerful punch swing animation on the main game thread execution pipeline
            Function.Call(Hash.TASK_PLAY_ANIM, player.Handle, "melee@knife@streamed_core", "knife_v_attack_b", 8.0f, -8.0f, -1, 0, 0.0f, false, false, false);

            // Lock structural root origins at the microsecond of button activation
            baseGroundBlastOrigin = player.Position;
            lockedGroundBlastHeading = player.Heading;

            // Configure state parameters to wait 420ms for the animation windup to throw full extension
            groundBlastStepIndex = 0;
            nextGroundBlastTime = Game.GameTime + 420;

            lastGroundBlastTime = DateTime.Now;
        }

        private void ExecuteSingleGroundBlastStep(Ped player, int playerPedId, Vector3 targetPos)
        {
            if (player == null || !player.IsAlive) return;

            // Fetch true floor plane elevation under coordinates to prevent flying particles
            float groundZ = targetPos.Z;
            OutputArgument outZ = new OutputArgument();
            if (Function.Call<bool>(Hash.GET_GROUND_Z_FOR_3D_COORD, targetPos.X, targetPos.Y, targetPos.Z, outZ, false))
            {
                groundZ = outZ.GetResult<float>();
            }
            Vector3 anchorCoords = new Vector3(targetPos.X, targetPos.Y, groundZ);

            // Visual: Render thick vertical desert dust geysers shooting upward
            if (Function.Call<bool>(Hash.HAS_NAMED_PTFX_ASSET_LOADED, "core"))
            {
                Function.Call(Hash.USE_PARTICLE_FX_ASSET, "core");
                Function.Call(Hash.START_PARTICLE_FX_NON_LOOPED_AT_COORD,
                    "ent_dst_dust",
                    anchorCoords.X, anchorCoords.Y, anchorCoords.Z + 0.1f,
                    -90.0f, 0.0f, 0.0f, // Force particle orientation to spray directly upward
                    3.0f, // Scale size multiplier
                    false, false, false
                );
            }

            // FIXED: Changed Explosion Type from 0 to 31 (Pure Dirt/Dust blast) to remove fire sparks
            Function.Call(Hash.ADD_EXPLOSION, anchorCoords.X, anchorCoords.Y, anchorCoords.Z + 0.2f, 31, 4.0f, true, false, 1.2f, false);

            // FIXED: Swapped Decal ID from 1110 (Molten Magma) to 1115 (Dry, cracked desert dirt texture)
            int decalId = Function.Call<int>(Hash.ADD_DECAL, 1115, anchorCoords.X, anchorCoords.Y, anchorCoords.Z + 0.02f, 0f, 0f, -1f, 0f, 1f, 0f, 2.2f, 2.2f, 1f, 0.15f, 0f, 1f, 15f, false, false, false);
        }

        // FIX: Re-adding the missing multi-instance tornado spawner method to the partial logic context
        private void TriggerNewSandTwisterInstance(Ped playerPed)
        {
            Vector3 camRot = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_ROT, 2);
            float headingRad = camRot.Z * 0.0174532924f;

            Vector3 twisterDirection = new Vector3(-(float)Math.Sin(headingRad), (float)Math.Cos(headingRad), 0.0f);
            twisterDirection.Normalize();

            Vector3 spawnPosition = playerPed.Position + (twisterDirection * SAND_TWISTER_SPAWN_DISTANCE);
            spawnPosition.Z = playerPed.Position.Z - 1.0f;

            activeTwisterInstances.Add(new Tuple<Vector3, Vector3, int, float>(spawnPosition, twisterDirection, SAND_TWISTER_DURATION_TICKS, 0.0f));
        }
        private void ExecuteSandTwisterInstanceLogic(Ped playerPed, int playerPedId, Vector3 twisterPos, float twisterAngle)
        {
            const string particleAsset = "core";
            const string particleEffect = "ent_dst_dust";

            if (Function.Call<bool>(Hash.HAS_NAMED_PTFX_ASSET_LOADED, particleAsset))
            {
                Function.Call(Hash.USE_PARTICLE_FX_ASSET, particleAsset);
                int dustCount = 18;

                for (int i = 0; i < dustCount; i++)
                {
                    float fraction = (float)i / dustCount;
                    float height = fraction * 6.5f;
                    float radius = 1.2f + (fraction * 4.0f);
                    float angle = (twisterAngle + (i * 20.0f)) * 0.0174532924f;

                    Vector3 dustPosition = twisterPos + new Vector3((float)Math.Cos(angle) * radius, (float)Math.Sin(angle) * radius, height);
                    Function.Call(Hash.USE_PARTICLE_FX_ASSET, particleAsset);
                    Function.Call(Hash.START_PARTICLE_FX_NON_LOOPED_AT_COORD, particleEffect, dustPosition.X, dustPosition.Y, dustPosition.Z, 0.0f, 0.0f, twisterAngle, 1.0f, false, false, false);
                }

                for (int i = 0; i < 5; i++)
                {
                    float angle = (twisterAngle + (i * 72.0f)) * 0.0174532924f;
                    float radius = 1.2f;
                    Vector3 basePosition = twisterPos + new Vector3((float)Math.Cos(angle) * radius, (float)Math.Sin(angle) * radius, 0.0f);
                    Function.Call(Hash.USE_PARTICLE_FX_ASSET, particleAsset);
                    Function.Call(Hash.START_PARTICLE_FX_NON_LOOPED_AT_COORD, particleEffect, basePosition.X, basePosition.Y, basePosition.Z, 0.0f, 0.0f, twisterAngle, 1.3f, false, false, false);
                }
            }

            // Suction Vortex capture mechanics
            float suctionRadius = 12.0f;
            float eyeOfTheStormRadius = 2.5f;

            Ped[] nearbyPeds = World.GetNearbyPeds(twisterPos, suctionRadius);
            foreach (Ped targetPed in nearbyPeds)
            {
                int targetPedId = targetPed.Handle;
                if (targetPedId == playerPedId || !targetPed.Exists() || targetPed.IsDead) continue;

                Vector3 pullVector = twisterPos - targetPed.Position;
                float distance = pullVector.Length();
                if (distance <= 0.01f) continue;
                pullVector.Normalize();

                targetPed.Health = Math.Max(0, targetPed.Health - 2);
                Function.Call(Hash.SET_PED_TO_RAGDOLL, targetPedId, 1000, 1000, 1, true, true, false);

                Vector3 velocityForce;
                if (distance <= eyeOfTheStormRadius)
                {
                    Vector3 orbitalRight = Vector3.Cross(pullVector, Vector3.WorldUp);
                    orbitalRight.Normalize();
                    velocityForce = (orbitalRight * 18.0f) + (Vector3.WorldUp * 1.5f);
                }
                else
                {
                    float pullIntensity = Math.Max(5.0f, (suctionRadius - distance) * 4.0f);
                    velocityForce = (pullVector * pullIntensity) + (Vector3.WorldUp * 2.0f);
                }
                Function.Call(Hash.SET_ENTITY_VELOCITY, targetPedId, velocityForce.X, velocityForce.Y, velocityForce.Z);
            }

            Vehicle[] nearbyVehicles = World.GetNearbyVehicles(twisterPos, suctionRadius);
            foreach (Vehicle targetVehicle in nearbyVehicles)
            {
                int targetVehicleId = targetVehicle.Handle;
                if (!targetVehicle.Exists()) continue;

                Vector3 pullVector = twisterPos - targetVehicle.Position;
                float distance = pullVector.Length();
                if (distance <= 0.01f) continue;
                pullVector.Normalize();

                targetVehicle.EngineHealth -= 5.0f;

                Vector3 velocityForce;
                if (distance <= eyeOfTheStormRadius + 1.0f)
                {
                    Vector3 orbitalRight = Vector3.Cross(pullVector, Vector3.WorldUp);
                    orbitalRight.Normalize();
                    velocityForce = (orbitalRight * 22.0f) + (Vector3.WorldUp * 1.0f);
                }
                else
                {
                    float pullIntensity = Math.Max(8.0f, (suctionRadius - distance) * 5.0f);
                    velocityForce = (pullVector * pullIntensity) + (Vector3.WorldUp * 1.5f);
                }
                Function.Call(Hash.SET_ENTITY_VELOCITY, targetVehicleId, velocityForce.X, velocityForce.Y, velocityForce.Z);
            }
        }
        private void InitializeSandSlash(Ped playerPed, int playerPedId)
        {
            Function.Call(Hash.TASK_PLAY_ANIM, playerPed.Handle, "melee@knife@streamed_core", "knife_v_attack_b", 8.0f, -8.0f, -1, 0, 0.0f, false, false, false);
            Vector3 handPos = Function.Call<Vector3>(Hash.GET_PED_BONE_COORDS, playerPedId, 28422, 0f, 0f, 0f);
            Vector3 camRot = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_ROT, 2);
            float headingRad = camRot.Z * 0.0174532924f;
            float pitchRad = camRot.X * 0.0174532924f;
            Vector3 forwardDirection = new Vector3((float)-Math.Sin(headingRad) * (float)Math.Cos(pitchRad), (float)Math.Cos(headingRad) * (float)Math.Cos(pitchRad), (float)Math.Sin(pitchRad));
            forwardDirection.Normalize();

            sandSlashCurrentPos = handPos;
            sandSlashForwardVector = forwardDirection;
            sandSlashLifetimeTicks = SAND_SLASH_MAX_LIFETIME;
            isSandSlashActive = true;
        }

        private void ExecuteSandSlashLogic(Ped playerPed, int playerPedId)
        {
            sandSlashLifetimeTicks--;
            if (sandSlashLifetimeTicks <= 0 || playerPed.IsDead) { isSandSlashActive = false; return; }
            sandSlashCurrentPos += sandSlashForwardVector * (SAND_SLASH_PROJECTILE_SPEED * Game.LastFrameTime);

            Function.Call(Hash.REQUEST_NAMED_PTFX_ASSET, "core");
            bool isPtfxLoaded = Function.Call<bool>(Hash.HAS_NAMED_PTFX_ASSET_LOADED, "core");

            Vector3 verticalAxisVector = Vector3.WorldUp;
            float radius = 6.5f;
            int sliceSteps = 20;

            for (int wave = 0; wave < 3; wave++)
            {
                float waveOffsetDistance = wave * -2.5f;
                Vector3 waveCenterOrigin = sandSlashCurrentPos + (sandSlashForwardVector * waveOffsetDistance);
                Vector3 previousPoint = Vector3.Zero;

                for (int i = 0; i <= sliceSteps; i++)
                {
                    float angleRadians = (-90f + ((float)i / sliceSteps * 180f)) * 0.0174532924f;
                    Vector3 currentPoint = waveCenterOrigin + (sandSlashForwardVector * (float)Math.Cos(angleRadians) * radius) + (verticalAxisVector * (float)Math.Sin(angleRadians) * radius);

                    if (i > 0)
                    {
                        Function.Call(Hash.DRAW_LINE, previousPoint.X, previousPoint.Y, previousPoint.Z, currentPoint.X, currentPoint.Y, currentPoint.Z, 195, 160, 110, 220);
                    }
                    previousPoint = currentPoint;

                    if (wave == 0 && isPtfxLoaded && i % 2 == 0)
                    {
                        Function.Call(Hash.USE_PARTICLE_FX_ASSET, "core");
                        Function.Call(Hash.START_PARTICLE_FX_NON_LOOPED_AT_COORD, "ent_dst_dust", currentPoint.X, currentPoint.Y, currentPoint.Z, 0.0f, 0.0f, playerPed.Heading, 1.4f, false, false, false);
                    }
                }
            }

            float hitRadius = 7.5f;

            foreach (Ped targetPed in World.GetNearbyPeds(sandSlashCurrentPos, hitRadius))
            {
                if (targetPed.Handle == playerPedId || !targetPed.Exists() || targetPed.IsDead) continue;
                targetPed.Health -= 90;
                Function.Call(Hash.SET_PED_TO_RAGDOLL, targetPed.Handle, 1500, 1500, 0, true, true, false);

                Vector3 pedLaunchVelocity = (sandSlashForwardVector * 32.0f) + (Vector3.WorldUp * 8.0f);
                Function.Call(Hash.SET_ENTITY_VELOCITY, targetPed.Handle, pedLaunchVelocity.X, pedLaunchVelocity.Y, pedLaunchVelocity.Z);
            }

            foreach (Vehicle targetVehicle in World.GetNearbyVehicles(sandSlashCurrentPos, hitRadius))
            {
                if (!targetVehicle.Exists()) continue;
                targetVehicle.EngineHealth -= 180.0f;
                Function.Call(Hash.SET_VEHICLE_DAMAGE, targetVehicle.Handle, 0.0f, 2.0f, 0.5f, 300.0f, 600.0f, true);

                Vector3 vehicleBlastForce = (sandSlashForwardVector * 95.0f) + (Vector3.WorldUp * 15.0f);
                Function.Call(Hash.SET_ENTITY_VELOCITY, targetVehicle.Handle, vehicleBlastForce.X, vehicleBlastForce.Y, vehicleBlastForce.Z);
            }
        }

        public void TriggerGroundDeathDehydration(Ped player, int playerPedId)
        {
            if (player == null || player.IsInVehicle()) return;

            // 1. Play ground strike animation
            Function.Call(Hash.TASK_PLAY_ANIM, player.Handle, "melee@knife@streamed_core", "knife_v_attack_b", 8.0f, -8.0f, -1, 0, 0.0f, false, false, false);

            Vector3 playerPos = player.Position;
            float shockwaveRadius = 2.5f;
            int particleCount = 16;

            // 2. Spawn expanding sand dust shockwave around feet
            if (Function.Call<bool>(Hash.HAS_NAMED_PTFX_ASSET_LOADED, "core"))
            {
                Function.Call(Hash.USE_PARTICLE_FX_ASSET, "core");
                for (int i = 0; i < particleCount; i++)
                {
                    float angleRadians = (float)(i * (2.0 * Math.PI / particleCount));
                    float targetX = playerPos.X + ((float)Math.Cos(angleRadians) * shockwaveRadius);
                    float targetY = playerPos.Y + ((float)Math.Sin(angleRadians) * shockwaveRadius);
                    float headingDegrees = (float)(angleRadians * (180.0 / Math.PI));

                    Function.Call(Hash.START_PARTICLE_FX_NON_LOOPED_AT_COORD,
                        "ent_dst_dust", targetX, targetY, playerPos.Z - 1.0f,
                        90.0f, 0.0f, headingDegrees,
                        1.5f, false, false, false
                    );
                }
            }

            // 3. VARIETY ARRAY: Safe base game corpse/zombie styles (no hospital gowns)
            string[] corpseModels = new string[] {
        "u_m_y_zombie_01",
        "u_m_y_corpse_02"
    };
            Random rand = new Random();

            float dehydrateRadius = 15.0f;
            Ped[] victims = World.GetNearbyPeds(player.Position, dehydrateRadius);

            foreach (Ped victim in victims)
            {
                if (victim.Handle == playerPedId || !victim.Exists() || victim.IsDead) continue;

                Vector3 spawnPos = victim.Position;
                float victimHeading = victim.Heading;

                // 4. FIXED VEHICLE EJECTION: Native bail-out flag pulls drivers out safely without missing hashes
                if (victim.IsInVehicle())
                {
                    Function.Call(Hash.CLEAR_PED_TASKS_IMMEDIATELY, victim.Handle);
                    int vehicleHandle = Function.Call<int>(Hash.GET_VEHICLE_PED_IS_IN, victim.Handle, false);

                    // Flag 4160 instructs the engine to force a rapid jumping/falling bail out animation
                    Function.Call(Hash.TASK_LEAVE_VEHICLE, victim.Handle, vehicleHandle, 4160);
                    Script.Yield();
                    spawnPos = victim.Position;
                }

                // 5. Sand impact particle over the victim
                if (Function.Call<bool>(Hash.HAS_NAMED_PTFX_ASSET_LOADED, "core"))
                {
                    Function.Call(Hash.USE_PARTICLE_FX_ASSET, "core");
                    Function.Call(Hash.START_PARTICLE_FX_NON_LOOPED_AT_COORD,
                        "ent_dst_dust", spawnPos.X, spawnPos.Y, spawnPos.Z,
                        0.0f, 0.0f, 0.0f, 2.5f, false, false, false
                    );
                }

                string randomModelName = corpseModels[rand.Next(corpseModels.Length)];
                Model corpseModel = new Model(randomModelName);

                corpseModel.Request();
                if (corpseModel.IsInCdImage && corpseModel.IsValid)
                {
                    DateTime timeout = DateTime.Now.AddMilliseconds(500);
                    while (!corpseModel.IsLoaded && DateTime.Now < timeout)
                    {
                        Script.Yield();
                    }

                    if (corpseModel.IsLoaded)
                    {
                        victim.Delete();

                        Ped deadPed = Ped.Create(corpseModel, spawnPos, victimHeading);
                        if (deadPed != null && deadPed.Exists())
                        {
                            deadPed.Health = 0;
                            Function.Call(Hash.SET_PED_DIES_IN_WATER, deadPed.Handle, true);
                            Function.Call(Hash.SET_PED_TO_RAGDOLL, deadPed.Handle, -1, -1, 0, true, true, false);
                        }
                    }
                }
                corpseModel.MarkAsNoLongerNeeded();
            }
        }
        public void TriggerUltimateSables(Ped player, int playerPedId, bool isReleaseFrame)
        {
            if (player == null || player.IsInVehicle()) return;

            Vector3 centerPos = player.Position;
            float stormRadius = 30.0f;
            float eyeOfStormRadius = 8.0f;
            float inwardPullStrength = 9.0f;
            float spinSpeed = 35.0f;

            if (!isReleaseFrame && Function.Call<bool>(Hash.HAS_NAMED_PTFX_ASSET_LOADED, "core"))
            {
                Function.Call(Hash.USE_PARTICLE_FX_ASSET, "core");
                for (int i = 0; i < 24; i++)
                {
                    float heightOffset = i * 0.5f;
                    float particleScale = 3.5f + (i * 0.15f);

                    Function.Call(Hash.START_PARTICLE_FX_NON_LOOPED_AT_COORD,
                        "ent_dst_dust",
                        centerPos.X, centerPos.Y, (centerPos.Z - 1.0f) + heightOffset,
                        0.0f, 0.0f, (float)(Game.GameTime * 0.4),
                        particleScale, false, false, false
                    );
                }
            }

            // ==========================================
            // 1. VEHICLE VORTEX & FLING ENGINE
            // ==========================================
            Vehicle[] cars = World.GetNearbyVehicles(centerPos, stormRadius);
            foreach (Vehicle car in cars)
            {
                if (!car.Exists()) continue;

                Vector3 pullDir = centerPos - car.Position;
                float distance = pullDir.Length();
                pullDir.Normalize();

                if (isReleaseFrame)
                {
                    // FIXED: Cut exit force in half again from 18.0f to 9.0f
                    Vector3 flingForce = (-pullDir * 9.0f) + new Vector3(0.0f, 0.0f, 2.5f);
                    Function.Call(Hash.SET_ENTITY_VELOCITY, car.Handle, flingForce.X, flingForce.Y, flingForce.Z);
                }
                else if (distance <= eyeOfStormRadius)
                {
                    Vector3 deflectDir = -pullDir;
                    Vector3 spinDir = new Vector3(-pullDir.Y, pullDir.X, 0.0f);
                    Vector3 force = (deflectDir * 16.0f) + (spinDir * spinSpeed) + new Vector3(0.0f, 0.0f, 4.0f);
                    Function.Call(Hash.SET_ENTITY_VELOCITY, car.Handle, force.X, force.Y, force.Z);
                }
                else if (distance > 2.0f)
                {
                    Vector3 spinDir = new Vector3(-pullDir.Y, pullDir.X, 0.0f);
                    Vector3 force = (pullDir * inwardPullStrength) + (spinDir * spinSpeed) + new Vector3(0.0f, 0.0f, 6.0f);
                    Function.Call(Hash.SET_ENTITY_VELOCITY, car.Handle, force.X, force.Y, force.Z);
                }
            }

            // ==========================================
            // 2. PEDESTRIAN VORTEX & FLING ENGINE
            // ==========================================
            Ped[] victims = World.GetNearbyPeds(centerPos, stormRadius);
            foreach (Ped victim in victims)
            {
                if (victim.Handle == playerPedId || !victim.Exists()) continue;

                Vector3 pullDir = centerPos - victim.Position;
                float distance = pullDir.Length();
                pullDir.Normalize();

                if (!victim.IsRagdoll)
                {
                    Function.Call(Hash.SET_PED_TO_RAGDOLL, victim.Handle, 2000, 2000, 0, true, true, false);
                }

                if (isReleaseFrame)
                {
                    // FIXED: Cut exit force in half again from 20.0f to 10.0f
                    Vector3 flingForce = (-pullDir * 10.0f) + new Vector3(0.0f, 0.0f, 3.0f);
                    Function.Call(Hash.SET_ENTITY_VELOCITY, victim.Handle, flingForce.X, flingForce.Y, flingForce.Z);
                }
                else if (distance <= eyeOfStormRadius)
                {
                    Vector3 deflectDir = -pullDir;
                    Vector3 spinDir = new Vector3(-pullDir.Y, pullDir.X, 0.0f);
                    Vector3 force = (deflectDir * 18.0f) + (spinDir * (spinSpeed * 1.2f)) + new Vector3(0.0f, 0.0f, 5.0f);
                    Function.Call(Hash.SET_ENTITY_VELOCITY, victim.Handle, force.X, force.Y, force.Z);
                }
                else if (distance > 2.0f)
                {
                    Vector3 spinDir = new Vector3(-pullDir.Y, pullDir.X, 0.0f);
                    Vector3 force = (pullDir * (inwardPullStrength * 1.2f)) + (spinDir * spinSpeed) + new Vector3(0.0f, 0.0f, 7.0f);
                    Function.Call(Hash.SET_ENTITY_VELOCITY, victim.Handle, force.X, force.Y, force.Z);
                }
            }

            // ==========================================
            // 3. MAP PROP VORTEX & FLING ENGINE
            // ==========================================
            int objectHandle = 0;
            objectHandle = Function.Call<int>(Hash.GET_CLOSEST_OBJECT_OF_TYPE, centerPos.X, centerPos.Y, centerPos.Z, stormRadius, 0, false, false, false);

            if (objectHandle != 0)
            {
                Function.Call(Hash.DETACH_ENTITY, objectHandle, true, true);

                Vector3 objPos = Function.Call<Vector3>(Hash.GET_ENTITY_COORDS, objectHandle, true);
                Vector3 pullDir = centerPos - objPos;
                float distance = pullDir.Length();
                pullDir.Normalize();

                if (isReleaseFrame)
                {
                    // FIXED: Cut exit force in half again from 15.0f to 7.5f
                    Vector3 flingForce = (-pullDir * 7.5f) + new Vector3(0.0f, 0.0f, 2.5f);
                    Function.Call(Hash.SET_ENTITY_VELOCITY, objectHandle, flingForce.X, flingForce.Y, flingForce.Z);
                }
                else if (distance <= eyeOfStormRadius)
                {
                    Vector3 deflectDir = -pullDir;
                    Vector3 spinDir = new Vector3(-pullDir.Y, pullDir.X, 0.0f);
                    Vector3 force = (deflectDir * 16.0f) + (spinDir * spinSpeed) + new Vector3(0.0f, 0.0f, 5.0f);
                    Function.Call(Hash.SET_ENTITY_VELOCITY, objectHandle, force.X, force.Y, force.Z);
                }
                else if (distance > 2.0f)
                {
                    Vector3 spinDir = new Vector3(-pullDir.Y, pullDir.X, 0.0f);
                    Vector3 force = (pullDir * inwardPullStrength) + (spinDir * spinSpeed) + new Vector3(0.0f, 0.0f, 8.0f);
                    Function.Call(Hash.SET_ENTITY_VELOCITY, objectHandle, force.X, force.Y, force.Z);
                }
            }
        }
    }
}
