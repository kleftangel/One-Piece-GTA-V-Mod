using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;
using GTA.Math;
using Control = GTA.Control;

namespace AnimeCharacterMod
{
    public partial class ZoroController : Script
    {
        private static readonly Random randomizer = new Random();


        private void InitializeFlyingSlash(Ped playerPed, int playerPedId)
        {
            Vector3 handPos = Function.Call<Vector3>(Hash.GET_PED_BONE_COORDS, playerPedId, 28422, 0f, 0f, 0f);

            Vector3 camRot = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_ROT, 2);
            float headingRad = camRot.Z * 0.0174532924f;
            float pitchRad = camRot.X * 0.0174532924f;

            float cosPitch = (float)Math.Cos(pitchRad);
            float sinPitch = (float)Math.Sin(pitchRad);
            float cosHeading = (float)Math.Cos(headingRad);
            float sinHeading = (float)Math.Sin(headingRad);

            Vector3 forwardDirection = new Vector3(-sinHeading * cosPitch, cosHeading * cosPitch, sinPitch);
            forwardDirection.Normalize();

            slashCurrentPos = handPos;
            slashForwardVector = forwardDirection;
            slashLifetimeTicks = SLASH_MAX_LIFETIME;
            isFlyingSlashActive = true;
        }

        private void ExecuteFlyingSlashLogic(Ped playerPed, int playerPedId)
        {
            slashLifetimeTicks--;

            if (slashLifetimeTicks <= 0)
            {
                isFlyingSlashActive = false;
                return;
            }

            slashCurrentPos += slashForwardVector * (SLASH_PROJECTILE_SPEED * Game.LastFrameTime);

            // --- VISUAL EFFECT LAYER: VERTICAL 90-DEGREE STANDING CRESCENT ---
            Vector3 verticalAxisVector = Vector3.WorldUp;
            float radius = 3.5f;
            int arcSegments = 12;
            Vector3 previousPoint = Vector3.Zero;

            for (int i = 0; i <= arcSegments; i++)
            {
                float fraction = (float)i / arcSegments;
                float angleDegrees = -90f + (fraction * 180f);
                float angleRadians = angleDegrees * 0.0174532924f;

                float forwardOffset = (float)Math.Cos(angleRadians) * radius;
                float verticalOffset = (float)Math.Sin(angleRadians) * radius;

                Vector3 currentPoint = slashCurrentPos + (slashForwardVector * forwardOffset) + (verticalAxisVector * verticalOffset);

                if (i > 0)
                {
                    Function.Call(Hash.DRAW_LINE,
                        previousPoint.X, previousPoint.Y, previousPoint.Z,
                        currentPoint.X, currentPoint.Y, currentPoint.Z,
                        0, 255, 120, 230
                    );
                }

                previousPoint = currentPoint;
            }

            // --- DAMAGE MATRIX LAYER ---
            float hitRadius = 3.5f;

            Ped[] nearbyPeds = World.GetNearbyPeds(slashCurrentPos, hitRadius);
            foreach (Ped targetPed in nearbyPeds)
            {
                int targetPedId = targetPed.Handle;
                if (targetPedId == playerPedId) continue;

                targetPed.Health = Math.Max(0, targetPed.Health - 90);

                Vector3 blastForce = (slashForwardVector * 30.0f) + (Vector3.WorldUp * 8.0f);
                Function.Call(Hash.SET_PED_TO_RAGDOLL, targetPedId, 1500, 1500, 0, true, true, false);
                Function.Call(Hash.SET_ENTITY_VELOCITY, targetPedId, blastForce.X, blastForce.Y, blastForce.Z);
            }

            Vehicle[] nearbyVehicles = World.GetNearbyVehicles(slashCurrentPos, hitRadius);
            foreach (Vehicle targetVehicle in nearbyVehicles)
            {
                int targetVehicleId = targetVehicle.Handle;

                float currentEngineHealth = Function.Call<float>(Hash.GET_VEHICLE_ENGINE_HEALTH, targetVehicleId);
                Function.Call(Hash.SET_VEHICLE_ENGINE_HEALTH, targetVehicleId, currentEngineHealth - 150.0f);
                Function.Call(Hash.SET_VEHICLE_DAMAGE, targetVehicleId, 0.0f, 2.0f, 0.5f, 300.0f, 600.0f, true);

                Vector3 vehicleBlastForce = (slashForwardVector * 35.0f) + (Vector3.WorldUp * 6.0f);
                Function.Call(Hash.SET_ENTITY_VELOCITY, targetVehicleId, vehicleBlastForce.X, vehicleBlastForce.Y, vehicleBlastForce.Z);
            }
        }

        private void ExecuteLeopardSpinningShotLogic(Ped playerPed, int playerPedId)
        {
            leopardShotDurationTimer--;
            if (spinDelayTimer > 0) spinDelayTimer--;

            if (leopardShotDurationTimer <= 0 || playerPed.IsDead)
            {
                isLeopardSpinningShotActive = false;
                Function.Call(Hash.RESET_PED_RAGDOLL_TIMER, playerPedId);
                return;
            }

            Function.Call(Hash.SET_PED_TO_RAGDOLL, playerPedId, 1000, 1000, 1, true, true, false);

            Vector3 camRot = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_ROT, 2);
            float headingRad = camRot.Z * 0.0174532924f;
            float pitchRad = camRot.X * 0.0174532924f;

            float cosPitch = (float)Math.Cos(pitchRad);
            float sinPitch = (float)Math.Sin(pitchRad);
            float cosHeading = (float)Math.Cos(headingRad);
            float sinHeading = (float)Math.Sin(headingRad);

            Vector3 forwardDirection = new Vector3(-sinHeading * cosPitch, cosHeading * cosPitch, sinPitch);
            forwardDirection.Normalize();

            Vector3 linearVelocityVector = new Vector3(forwardDirection.X * PROPULSION_SPEED, forwardDirection.Y * PROPULSION_SPEED, 0.0f);
            Function.Call(Hash.SET_ENTITY_VELOCITY, playerPedId, linearVelocityVector.X, linearVelocityVector.Y, linearVelocityVector.Z);

            Vector3 currentPos = playerPed.Position;

            if (spinDelayTimer <= 0)
            {
                currentSpinAngle += 65.0f;
                if (currentSpinAngle > 360.0f) currentSpinAngle -= 360.0f;

                float targetPitch = 0.0f;
                float targetYaw = camRot.Z;
                Function.Call(Hash.SET_ENTITY_ROTATION, playerPedId, targetPitch, currentSpinAngle, targetYaw, 2, true);

                for (int i = 0; i < 4; i++)
                {
                    float sideOffset = (float)(randomizer.NextDouble() * 7.0 - 3.5);
                    float heightOffset = (float)(randomizer.NextDouble() * 3.6 - 1.8);

                    Vector3 spinUpVector = Vector3.WorldUp;
                    Vector3 lateralVector = Vector3.Cross(forwardDirection, spinUpVector);
                    lateralVector.Normalize();

                    Vector3 lineStart = currentPos + (lateralVector * sideOffset) + (spinUpVector * heightOffset);
                    float lineLengthMultiplier = 2.5f;
                    Vector3 lineEnd = lineStart + (forwardDirection * lineLengthMultiplier);

                    Function.Call(Hash.DRAW_LINE, lineStart.X, lineStart.Y, lineStart.Z, lineEnd.X, lineEnd.Y, lineEnd.Z, 255, 255, 255, 220);
                }
            }

            float sweepHitRadius = 4.5f;

            Ped[] nearbyPeds = World.GetNearbyPeds(currentPos, sweepHitRadius);
            foreach (Ped targetPed in nearbyPeds)
            {
                int targetPedId = targetPed.Handle;
                if (targetPedId == playerPedId) continue;

                int currentlyCalculatedHealth = targetPed.Health;
                targetPed.Health = Math.Max(0, currentlyCalculatedHealth - 85);

                Vector3 blastVector = targetPed.Position - currentPos;
                blastVector.Normalize();
                Vector3 blastForce = (blastVector * 25.0f) + (Vector3.WorldUp * 12.0f);

                Function.Call(Hash.SET_PED_TO_RAGDOLL, targetPedId, 2000, 2000, 0, true, true, false);
                Function.Call(Hash.SET_ENTITY_VELOCITY, targetPedId, blastForce.X, blastForce.Y, blastForce.Z);
            }

            Vehicle[] nearbyVehicles = World.GetNearbyVehicles(currentPos, sweepHitRadius);
            foreach (Vehicle targetVehicle in nearbyVehicles)
            {
                int targetVehicleId = targetVehicle.Handle;

                float currentEngineHealth = Function.Call<float>(Hash.GET_VEHICLE_ENGINE_HEALTH, targetVehicleId);
                float newEngineHealth = Math.Max(-400.0f, currentEngineHealth - 30.0f);
                Function.Call(Hash.SET_VEHICLE_ENGINE_HEALTH, targetVehicleId, newEngineHealth);

                Function.Call(Hash.SET_VEHICLE_DAMAGE, targetVehicleId, 0.0f, 2.0f, 0.5f, 250.0f, 500.0f, true);

                Vector3 vehicleBlastVector = targetVehicle.Position - currentPos;
                vehicleBlastVector.Normalize();
                Vector3 vehicleBlastForce = (vehicleBlastVector * 30.0f) + (Vector3.WorldUp * 10.0f);
                Function.Call(Hash.SET_ENTITY_VELOCITY, targetVehicleId, vehicleBlastForce.X, vehicleBlastForce.Y, vehicleBlastForce.Z);
            }
        }

        private void ExecuteDragonTwisterLogic(Ped playerPed, int playerPedId)
        {
            // 1. Decrement runtime tick lifespan
            twisterDurationTimer--;

            Vector3 currentPos = playerPed.Position;

            // --- TERMINAL DISPERSAL PHASE (Tornado Explodes Outward) ---
            if (twisterDurationTimer <= 0 || playerPed.IsDead)
            {
                isDragonTwisterActive = false;
                Function.Call(Hash.RESET_PED_RAGDOLL_TIMER, playerPedId);

                float explosionRadius = 15.0f;

                // Blast Peds Away
                Ped[] finalPeds = World.GetNearbyPeds(currentPos, explosionRadius);
                foreach (Ped targetPed in finalPeds)
                {
                    int targetPedId = targetPed.Handle;
                    if (targetPedId == playerPedId) continue;

                    Vector3 blastVector = targetPed.Position - currentPos;
                    blastVector.Normalize();
                    Vector3 blastForce = (blastVector * 45.0f) + (Vector3.WorldUp * 25.0f);

                    Function.Call(Hash.SET_PED_TO_RAGDOLL, targetPedId, 3000, 3000, 0, true, true, false);
                    Function.Call(Hash.SET_ENTITY_VELOCITY, targetPedId, blastForce.X, blastForce.Y, blastForce.Z);
                }

                // Blast Vehicles Away
                Vehicle[] finalVehicles = World.GetNearbyVehicles(currentPos, explosionRadius);
                foreach (Vehicle targetVehicle in finalVehicles)
                {
                    int targetVehicleId = targetVehicle.Handle;

                    Vector3 blastVector = targetVehicle.Position - currentPos;
                    blastVector.Normalize();
                    Vector3 blastForce = (blastVector * 55.0f) + (Vector3.WorldUp * 20.0f);

                    Function.Call(Hash.SET_ENTITY_VELOCITY, targetVehicleId, blastForce.X, blastForce.Y, blastForce.Z);
                }

                return;
            }

            // --- STATIONARY PHYSICS SPIN LOOP ---
            Function.Call(Hash.SET_PED_TO_RAGDOLL, playerPedId, 1000, 1000, 1, true, true, false);

            // FORCEFULLY ZERO VELOCITY MATRIX: Locks Zoro strictly on the spot
            Function.Call(Hash.SET_ENTITY_VELOCITY, playerPedId, 0.0f, 0.0f, 0.0f);

            // Accumulate spin rotation
            twisterSpinAngle += 40.0f;
            if (twisterSpinAngle > 360.0f) twisterSpinAngle -= 360.0f;

            // Setting Roll = 0, Pitch = 0, and Yaw/Heading = twisterSpinAngle forces upright rotation
            Function.Call(Hash.SET_ENTITY_ROTATION, playerPedId, 0.0f, 0.0f, twisterSpinAngle, 2, true);

            // --- DELAYED PHYSICS & GRAPHICS RENDER MATRIX ---
            // DELAY GATEWAY: Forces arms to stay limp and keeps lines invisible until the ragdoll fully detaches
            if (spinDelayTimer <= 0)
            {
                // --- HANDS EXTENDED OUTWARD FORCE PHYSICS ---
                Vector3 rightHandPos = Function.Call<Vector3>(Hash.GET_PED_BONE_COORDS, playerPedId, 28422, 0f, 0f, 0f);
                Vector3 leftHandPos = Function.Call<Vector3>(Hash.GET_PED_BONE_COORDS, playerPedId, 18905, 0f, 0f, 0f);

                // Calculate horizontal pull directions relative to Zoro's core spine axis center
                Vector3 rightPullDir = rightHandPos - currentPos; rightPullDir.Z = 0.0f; rightPullDir.Normalize();
                Vector3 leftPullDir = leftHandPos - currentPos; leftPullDir.Z = 0.0f; leftPullDir.Normalize();

                float armExtendForceFactor = 450.0f; // Force multiplier to lift bones outward

                // Pull the hands outward cleanly 
                Function.Call(Hash.APPLY_FORCE_TO_ENTITY, playerPedId, 1,
                    rightPullDir.X * armExtendForceFactor, rightPullDir.Y * armExtendForceFactor, 0.0f,
                    rightHandPos.X, rightHandPos.Y, rightHandPos.Z,
                    0, false, true, true, false, true);

                Function.Call(Hash.APPLY_FORCE_TO_ENTITY, playerPedId, 1,
                    leftPullDir.X * armExtendForceFactor, leftPullDir.Y * armExtendForceFactor, 0.0f,
                    leftHandPos.X, leftHandPos.Y, leftHandPos.Z,
                    0, false, true, true, false, true);

                // --- 2D WHITE TORNADO SPEED STREAK FUNNEL SYSTEM ---
                int tornadoShellLineCount = 30;
                for (int i = 0; i < tornadoShellLineCount; i++)
                {
                    float stepFraction = (float)i / tornadoShellLineCount;
                    float heightOffset = stepFraction * 6.5f;

                    float funnelRadiusModifier = 1.8f + (stepFraction * 3.8f);
                    float lineJitterAngleDeg = (float)(randomizer.NextDouble() * 30.0 - 15.0);

                    float startAngleRad = (twisterSpinAngle + (i * 12.0f)) * 0.0174532924f;
                    float endAngleRad = (twisterSpinAngle + (i * 12.0f) + 35.0f + lineJitterAngleDeg) * 0.0174532924f;

                    Vector3 lineStart = new Vector3(
                        currentPos.X + (float)Math.Cos(startAngleRad) * funnelRadiusModifier,
                        currentPos.Y + (float)Math.Sin(startAngleRad) * funnelRadiusModifier,
                        currentPos.Z - 1.0f + heightOffset
                    );

                    Vector3 lineEnd = new Vector3(
                        currentPos.X + (float)Math.Cos(endAngleRad) * funnelRadiusModifier,
                        currentPos.Y + (float)Math.Sin(endAngleRad) * funnelRadiusModifier,
                        currentPos.Z - 0.8f + heightOffset
                    );

                    Function.Call(Hash.DRAW_LINE,
                        lineStart.X, lineStart.Y, lineStart.Z,
                        lineEnd.X, lineEnd.Y, lineEnd.Z,
                        255, 255, 255, 220
                    );
                }
            }

            // --- CONTINUOUS VORTEX SUCTION LOOP WITH ORBITAL EYE OF THE STORM ---
            float suctionRadius = 12.0f;
            float eyeOfTheStormRadius = 2.5f;

            // A. Vacuum Pull & Orbit Peds
            Ped[] nearbyPeds = World.GetNearbyPeds(currentPos, suctionRadius);
            foreach (Ped targetPed in nearbyPeds)
            {
                int targetPedId = targetPed.Handle;
                if (targetPedId == playerPedId) continue;

                Vector3 pullVector = currentPos - targetPed.Position;
                float distance = pullVector.Length();
                pullVector.Normalize();

                targetPed.Health = Math.Max(0, targetPed.Health - 2);
                Function.Call(Hash.SET_PED_TO_RAGDOLL, targetPedId, 1000, 1000, 1, true, true, false);

                Vector3 velocityForce;

                if (distance <= eyeOfTheStormRadius)
                {
                    Vector3 orbitalRight = Vector3.Cross(pullVector, Vector3.WorldUp);
                    orbitalRight.Normalize();

                    float orbitalVelocityFactor = 18.0f;
                    velocityForce = (orbitalRight * orbitalVelocityFactor) + (Vector3.WorldUp * 1.5f);
                }
                else
                {
                    float pullIntensity = Math.Max(5.0f, (suctionRadius - distance) * 4.0f);
                    velocityForce = (pullVector * pullIntensity) + (Vector3.WorldUp * 2.0f);
                }

                Function.Call(Hash.SET_ENTITY_VELOCITY, targetPedId, velocityForce.X, velocityForce.Y, velocityForce.Z);
            }

            // B. Vacuum Pull & Orbit Vehicles
            Vehicle[] nearbyVehicles = World.GetNearbyVehicles(currentPos, suctionRadius);
            foreach (Vehicle targetVehicle in nearbyVehicles)
            {
                int targetVehicleId = targetVehicle.Handle;

                Vector3 pullVector = currentPos - targetVehicle.Position;
                float distance = pullVector.Length();
                pullVector.Normalize();

                float engineHealth = Function.Call<float>(Hash.GET_VEHICLE_ENGINE_HEALTH, targetVehicleId);
                Function.Call(Hash.SET_VEHICLE_ENGINE_HEALTH, targetVehicleId, engineHealth - 5.0f);

                Vector3 velocityForce;

                if (distance <= eyeOfTheStormRadius + 1.0f)
                {
                    Vector3 orbitalRight = Vector3.Cross(pullVector, Vector3.WorldUp);
                    orbitalRight.Normalize();

                    float orbitalVelocityFactor = 22.0f;
                    velocityForce = (orbitalRight * orbitalVelocityFactor) + (Vector3.WorldUp * 1.0f);
                }
                else
                {
                    float pullIntensity = Math.Max(8.0f, (suctionRadius - distance) * 5.0f);
                    velocityForce = (pullVector * pullIntensity) + (Vector3.WorldUp * 1.5f);
                }

                Function.Call(Hash.SET_ENTITY_VELOCITY, targetVehicleId, velocityForce.X, velocityForce.Y, velocityForce.Z);
            }
        }

        private void ExecuteOnigiriLogic(Ped playerPed, int playerPedId)
        {
            // 1. Decrement the active state run timer
            onigiriTimer--;

            Vector3 currentPos = playerPed.Position;

            // Termination conditions checking
            if (onigiriTimer <= 0 || playerPed.IsDead)
            {
                isOnigiriActive = false;
                Function.Call(Hash.RESET_PED_RAGDOLL_TIMER, playerPedId);
                return;
            }

            // 2. Enforce Continual Ragdoll Physics Frame-Lock to manipulate the skeleton limbs
            Function.Call(Hash.SET_PED_TO_RAGDOLL, playerPedId, 1000, 1000, 1, true, true, false);

            // Calculate directional forward vector matching camera angles
            Vector3 camRot = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_ROT, 2);
            float headingRad = camRot.Z * 0.0174532924f;
            float pitchRad = camRot.X * 0.0174532924f;

            float cosPitch = (float)Math.Cos(pitchRad);
            float sinPitch = (float)Math.Sin(pitchRad);
            float cosHeading = (float)Math.Cos(headingRad);
            float sinHeading = (float)Math.Sin(headingRad);

            Vector3 forwardDirection = new Vector3(-sinHeading * cosPitch, cosHeading * cosPitch, sinPitch);
            forwardDirection.Normalize();

            // Fetch physical bone coordinates
            Vector3 rightHandPos = Function.Call<Vector3>(Hash.GET_PED_BONE_COORDS, playerPedId, 28422, 0f, 0f, 0f);
            Vector3 leftHandPos = Function.Call<Vector3>(Hash.GET_PED_BONE_COORDS, playerPedId, 18905, 0f, 0f, 0f);
            Vector3 spinePos = Function.Call<Vector3>(Hash.GET_PED_BONE_COORDS, playerPedId, 24817, 0f, 0f, 0f);

            // Fetch horizontal cross vectors for clean lateral spacing multipliers
            Vector3 lateralRight = Vector3.Cross(forwardDirection, Vector3.WorldUp);
            lateralRight.Normalize();

            // --- STAGE 1: THE 1-SECOND WIND-UP & ARM CROSSING PHASE (Ticks 45 down to 16) ---
            if (onigiriTimer > 15)
            {
                // FORCEFULLY ZERO VELOCITY MATRIX: Zoro stands completely still on the spot while preparing the attack
                Function.Call(Hash.SET_ENTITY_VELOCITY, playerPedId, 0.0f, 0.0f, 0.0f);

                // FIXED BODY ROTATION RESTRAINT: Explicitly locks his body angles to match the camera direction
                // Forcing Roll = 0.0, Pitch = 0.0, and Yaw/Heading = camRot.Z pins his stance straight forward
                Function.Call(Hash.SET_ENTITY_ROTATION, playerPedId, 0.0f, 0.0f, camRot.Z, 2, true);

                // Give the ragdoll joints a brief 4-frame window to relax before applying heavy force vectors
                if (onigiriTimer <= 41)
                {
                    // INWARD CROSSING TARGETS: Pull left hand to right side, right hand to left side across chest center
                    Vector3 rightInwardTarget = spinePos + (lateralRight * -0.6f) + (forwardDirection * 0.4f);
                    Vector3 leftInwardTarget = spinePos + (lateralRight * 0.6f) + (forwardDirection * 0.4f);

                    Vector3 rightHandPullVector = rightInwardTarget - rightHandPos; rightHandPullVector.Normalize();
                    Vector3 leftHandPullVector = leftInwardTarget - leftHandPos; leftHandPullVector.Normalize();

                    float crossForceIntensity = 950.0f;
                    Function.Call(Hash.APPLY_FORCE_TO_ENTITY, playerPedId, 1, rightHandPullVector.X * crossForceIntensity, rightHandPullVector.Y * crossForceIntensity, rightHandPullVector.Z * crossForceIntensity, rightHandPos.X, rightHandPos.Y, rightHandPos.Z, 0, false, true, true, false, true);
                    Function.Call(Hash.APPLY_FORCE_TO_ENTITY, playerPedId, 1, leftHandPullVector.X * crossForceIntensity, leftHandPullVector.Y * crossForceIntensity, leftHandPullVector.Z * crossForceIntensity, leftHandPos.X, leftHandPos.Y, leftHandPos.Z, 0, false, true, true, false, true);
                }
            }
            // --- STAGE 2: HIGH-SPEED PROPULSION LAUNCH & EXPLOSIVE OUTWARD SLICE (Ticks 15 down to 0) ---
            else
            {
                // UNSTOPPABLE DASH PROPULSION: Teleport forward by a smooth increment every frame to guarantee travel distance
                float smoothTravelDistancePerFrame = 0.52f;
                Vector3 targetDisplacementCoord = currentPos + (forwardDirection * smoothTravelDistancePerFrame);

                Function.Call(Hash.SET_ENTITY_COORDS_NO_OFFSET, playerPedId,
                    targetDisplacementCoord.X, targetDisplacementCoord.Y, targetDisplacementCoord.Z,
                    true, false, false);

                // Keep his body facing straight forward even during the high-speed forward rush displacement phase
                Function.Call(Hash.SET_ENTITY_ROTATION, playerPedId, 0.0f, 0.0f, camRot.Z, 2, true);

                // OUTWARD SLICING FORCE: Explode arms outward away from his spine center toward the horizons
                Vector3 rightSliceForce = (lateralRight * 2500.0f);
                Vector3 leftSliceForce = (-lateralRight * 2500.0f);

                Function.Call(Hash.APPLY_FORCE_TO_ENTITY, playerPedId, 1, rightSliceForce.X, rightSliceForce.Y, 0.0f, rightHandPos.X, rightHandPos.Y, rightHandPos.Z, 0, false, true, true, false, true);
                Function.Call(Hash.APPLY_FORCE_TO_ENTITY, playerPedId, 1, leftSliceForce.X, leftSliceForce.Y, 0.0f, leftHandPos.X, leftHandPos.Y, leftHandPos.Z, 0, false, true, true, false, true);

                // --- ONIGIRI IMPACT VISUALS: DEMON GUST SLITS ---
                Function.Call(Hash.DRAW_LINE, currentPos.X, currentPos.Y, currentPos.Z + 0.2f, currentPos.X + (lateralRight.X * 6.5f), currentPos.Y + (lateralRight.Y * 6.5f), currentPos.Z + 0.2f, 30, 220, 90, 245);
                Function.Call(Hash.DRAW_LINE, currentPos.X, currentPos.Y, currentPos.Z + 0.2f, currentPos.X - (lateralRight.X * 6.5f), currentPos.Y - (lateralRight.Y * 6.5f), currentPos.Z + 0.2f, 30, 220, 90, 245);

                // --- VOLUMETRIC COLLISION DAMAGE SWEEP ---
                float hitRadius = 6.0f;

                Ped[] hitPeds = World.GetNearbyPeds(currentPos, hitRadius);
                foreach (Ped targetPed in hitPeds)
                {
                    int targetPedId = targetPed.Handle;
                    if (targetPedId == playerPedId) continue;

                    targetPed.Health = Math.Max(0, targetPed.Health - 160);

                    Vector3 knockbackVector = targetPed.Position - currentPos;
                    knockbackVector.Normalize();
                    Vector3 knockbackForce = (knockbackVector * 50.0f) + (Vector3.WorldUp * 12.0f);

                    Function.Call(Hash.SET_PED_TO_RAGDOLL, targetPedId, 2500, 2500, 0, true, true, false);
                    Function.Call(Hash.SET_ENTITY_VELOCITY, targetPedId, knockbackForce.X, knockbackForce.Y, knockbackForce.Z);
                }

                Vehicle[] hitVehicles = World.GetNearbyVehicles(currentPos, hitRadius);
                foreach (Vehicle targetVehicle in hitVehicles)
                {
                    int targetVehicleId = targetVehicle.Handle;

                    float engineHealth = Function.Call<float>(Hash.GET_VEHICLE_ENGINE_HEALTH, targetVehicleId);
                    Function.Call(Hash.SET_VEHICLE_ENGINE_HEALTH, targetVehicleId, engineHealth - 250.0f);
                    Function.Call(Hash.SET_VEHICLE_DAMAGE, targetVehicleId, 0.0f, 2.0f, 0.5f, 400.0f, 800.0f, true);

                    Vector3 knockbackVector = targetVehicle.Position - currentPos;
                    knockbackVector.Normalize();
                    Vector3 knockbackForce = (knockbackVector * 55.0f) + (Vector3.WorldUp * 10.0f);
                    Function.Call(Hash.SET_ENTITY_VELOCITY, targetVehicleId, knockbackForce.X, knockbackForce.Y, knockbackForce.Z);
                }
            }
        }

        private void ExecuteUpwardSlashLogic(Ped playerPed, int playerPedId)
        {
            // 1. Decrement runtime tick lifespan
            upwardSlashTimer--;
            Vector3 currentPos = playerPed.Position;

            // Termination conditions checking
            if (upwardSlashTimer <= 0 || playerPed.IsDead)
            {
                isUpwardSlashActive = false;
                upwardSlashProgressTicks = 0;
                Function.Call(Hash.RESET_PED_RAGDOLL_TIMER, playerPedId);
                return;
            }

            // 2. Enforce Continual Ragdoll Physics Frame-Lock
            Function.Call(Hash.SET_PED_TO_RAGDOLL, playerPedId, 1000, 1000, 1, true, true, false);

            // 3. Resolve Camera Orientation Forward Vectors
            Vector3 camRot = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_ROT, 2);
            float headingRad = camRot.Z * 0.0174532924f;
            float pitchRad = camRot.X * 0.0174532924f;

            float cosPitch = (float)Math.Cos(pitchRad);
            float sinPitch = (float)Math.Sin(pitchRad);
            float cosHeading = (float)Math.Cos(headingRad);
            float sinHeading = (float)Math.Sin(headingRad);

            Vector3 forwardDir = new Vector3(-sinHeading * cosPitch, cosHeading * cosPitch, sinPitch);
            forwardDir.Normalize();

            Vector3 lateralRight = Vector3.Cross(forwardDir, Vector3.WorldUp);
            lateralRight.Normalize();

            // --- STAGE 1: 0.5-SECOND LIMP STASIS PHASE (Ticks 55 down to 41) ---
            if (upwardSlashTimer > 40)
            {
                Function.Call(Hash.SET_ENTITY_VELOCITY, playerPedId, 0.0f, 0.0f, 0.0f);
            }
            // --- STAGE 2: HALVED SKYWARD LAUNCH & FLAT TRIPLE CRESCENT PROJECTILES (Ticks 40 down to 0) ---
            else
            {
                // Cache the exact height at the frame launch triggers (Tick 40)
                if (upwardSlashTimer == 40)
                {
                    upwardSlashSpawnPos = playerPed.Position;
                }

                // A. SKYWARD PROPULSION LIFT LOOP (Runs continuously from Tick 40 down to 26)
                if (upwardSlashTimer > 25)
                {
                    // HALVED LIFT SPEED: Maintained at 13.0f to reduce vertical height by half
                    float skywardLaunchSpeed = 13.0f;
                    Vector3 skywardForce = (forwardDir * 4.0f) + (Vector3.WorldUp * skywardLaunchSpeed);
                    Function.Call(Hash.SET_ENTITY_VELOCITY, playerPedId, skywardForce.X, skywardForce.Y, skywardForce.Z);

                    Vector3 rightHandPos = Function.Call<Vector3>(Hash.GET_PED_BONE_COORDS, playerPedId, 28422, 0f, 0f, 0f);
                    Vector3 leftHandPos = Function.Call<Vector3>(Hash.GET_PED_BONE_COORDS, playerPedId, 18905, 0f, 0f, 0f);

                    float armForceMultiplier = 1200.0f;
                    Function.Call(Hash.APPLY_FORCE_TO_ENTITY, playerPedId, 1, 0.0f, 0.0f, armForceMultiplier, rightHandPos.X, rightHandPos.Y, rightHandPos.Z, 0, false, true, true, false, true);
                    Function.Call(Hash.APPLY_FORCE_TO_ENTITY, playerPedId, 1, 0.0f, 0.0f, armForceMultiplier, leftHandPos.X, leftHandPos.Y, leftHandPos.Z, 0, false, true, true, false, true);
                }

                // B. PERSISTENT TRIPLE-SLASH FLIGHT ENGINE
                upwardSlashProgressTicks++;

                float projectileVelocitySpeed = 48.0f;
                float currentDistanceMultiplier = upwardSlashProgressTicks * (projectileVelocitySpeed * Game.LastFrameTime);

                // FLAT HORIZONTAL DIRECTION: Drops Z tracking components completely to force level flight
                Vector3 flatSlashForward = new Vector3(forwardDir.X, forwardDir.Y, 0.0f);
                flatSlashForward.Normalize();

                // INITIAL SEPARATION AND EXPANSION MATH
                float wideSpreadingFactor = 3.5f + (upwardSlashProgressTicks * 0.4f);

                // FIXED HEIGHT PLOTTING: Uses 'upwardSlashSpawnPos' coordinate arrays instead of moving 'currentPos'
                Vector3 baseCenterOrigin = upwardSlashSpawnPos + (flatSlashForward * currentDistanceMultiplier) + (Vector3.WorldUp * 0.5f);

                Vector3 leftWaveCenter = baseCenterOrigin - (lateralRight * wideSpreadingFactor);
                Vector3 rightWaveCenter = baseCenterOrigin + (lateralRight * wideSpreadingFactor);
                Vector3 centerWaveCenter = baseCenterOrigin; // RESTORED: Center track centerline

                // --- VISUAL EFFECT LAYER: THREE STANDING VERTICAL CRESCENTS ---
                Vector3 verticalAxisVector = Vector3.WorldUp;
                float radius = 3.5f;
                int arcSegments = 12;

                Vector3 prevLeftPoint = Vector3.Zero;
                Vector3 prevRightPoint = Vector3.Zero;
                Vector3 prevCenterPoint = Vector3.Zero; // RESTORED

                for (int i = 0; i <= arcSegments; i++)
                {
                    float fraction = (float)i / arcSegments;
                    float angleDegrees = -90f + (fraction * 180f);
                    float angleRadians = angleDegrees * 0.0174532924f;

                    float forwardOffset = (float)Math.Cos(angleRadians) * radius;
                    float verticalOffset = (float)Math.Sin(angleRadians) * radius;

                    Vector3 currentLeftPoint = leftWaveCenter + (flatSlashForward * forwardOffset) + (verticalAxisVector * verticalOffset);
                    Vector3 currentRightPoint = rightWaveCenter + (flatSlashForward * forwardOffset) + (verticalAxisVector * verticalOffset);
                    Vector3 currentCenterPoint = centerWaveCenter + (flatSlashForward * forwardOffset) + (verticalAxisVector * verticalOffset); // RESTORED

                    if (i > 0)
                    {
                        // Draw Left Wave Segment
                        Function.Call(Hash.DRAW_LINE,
                            prevLeftPoint.X, prevLeftPoint.Y, prevLeftPoint.Z,
                            currentLeftPoint.X, currentLeftPoint.Y, currentLeftPoint.Z,
                            30, 255, 120, 230);

                        // Draw Right Wave Segment
                        Function.Call(Hash.DRAW_LINE,
                            prevRightPoint.X, prevRightPoint.Y, prevRightPoint.Z,
                            currentRightPoint.X, currentRightPoint.Y, currentRightPoint.Z,
                            30, 255, 120, 230);

                        // Draw Center Wave Segment (RESTORED)
                        Function.Call(Hash.DRAW_LINE,
                            prevCenterPoint.X, prevCenterPoint.Y, prevCenterPoint.Z,
                            currentCenterPoint.X, currentCenterPoint.Y, currentCenterPoint.Z,
                            30, 255, 120, 230);
                    }

                    prevLeftPoint = currentLeftPoint;
                    prevRightPoint = currentRightPoint;
                    prevCenterPoint = currentCenterPoint; // RESTORED
                }

                // --- COLLISION DAMAGE SWEEP MATRIX ---
                ApplyShockwaveAreaDamage(leftWaveCenter, flatSlashForward);
                ApplyShockwaveAreaDamage(rightWaveCenter, flatSlashForward);
                ApplyShockwaveAreaDamage(centerWaveCenter, flatSlashForward); // RESTORED: Center lane damage tracking
            }
        }

        private void ApplyShockwaveAreaDamage(Vector3 checkPos, Vector3 forwardDir)
        {
            float radius = 4.2f; // Volumetric damage checking radius
            int playerPedId = Game.Player.Character.Handle;

            // --- A. VOLUMETRIC PED COLLISION SWEEP ---
            Ped[] nearbyPeds = World.GetNearbyPeds(checkPos, radius);
            foreach (Ped targetPed in nearbyPeds)
            {
                int targetPedId = targetPed.Handle;
                if (targetPedId == playerPedId) continue;

                targetPed.Health = Math.Max(0, targetPed.Health - 95);
                Vector3 impactForce = (forwardDir * 35.0f) + (Vector3.WorldUp * 15.0f);

                Function.Call(Hash.SET_PED_TO_RAGDOLL, targetPedId, 2000, 2000, 0, true, true, false);
                Function.Call(Hash.SET_ENTITY_VELOCITY, targetPedId, impactForce.X, impactForce.Y, impactForce.Z);
            }

            // --- B. RESTORED VEHICLE COLLISION SWEEP ---
            // Grabs and tracks multi-ton automotive assets caught in the flat shockwave lane paths
            Vehicle[] nearbyVehicles = World.GetNearbyVehicles(checkPos, radius);
            foreach (Vehicle targetVehicle in nearbyVehicles)
            {
                int targetVehicleId = targetVehicle.Handle;

                // Deliver sudden structural engine block wear damage
                float currentEngineHealth = Function.Call<float>(Hash.GET_VEHICLE_ENGINE_HEALTH, targetVehicleId);
                Function.Call(Hash.SET_VEHICLE_ENGINE_HEALTH, targetVehicleId, currentEngineHealth - 180.0f);

                // Dent sheets and burst glass windows to simulate sword impacts
                Function.Call(Hash.SET_VEHICLE_DAMAGE, targetVehicleId, 0.0f, 2.0f, 0.5f, 300.0f, 600.0f, true);

                // Throw vehicles backward and up out of the lane path trajectory footprint
                Vector3 vehicleBlastForce = (forwardDir * 40.0f) + (Vector3.WorldUp * 8.0f);
                Function.Call(Hash.SET_ENTITY_VELOCITY, targetVehicleId, vehicleBlastForce.X, vehicleBlastForce.Y, vehicleBlastForce.Z);
            }
        }
    }
}
