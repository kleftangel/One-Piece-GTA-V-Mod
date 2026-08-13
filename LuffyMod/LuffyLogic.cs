using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;

namespace AnimeCharacterMod
{
    public partial class LuffyController : Script
    {
        private Random randomSeed = new Random();


        private void RenderStretchArmGraphics(int trackingTargetPedHandle)
        {

            // SPECIAL PASSTHROUGH CASE: Gum-Gum Whip sweeps a single thin leg in a massive horizontal arc
            if (isWhipActive)
            {
                int leftFootBone = 14201;
                Vector3 legStart = Function.Call<Vector3>(Hash.GET_PED_BONE_COORDS, trackingTargetPedHandle, leftFootBone, 0.0f, 0.0f, 0.0f);

                // Draw the tight leg cylinder out to the current moving sweep tip point
                DrawSingleStretchCylinderInternal(legStart, currentArmEndPos, 0.022f, false);
                return;
            }

            // SPECIAL PASSTHROUGH CASE: Gum-Gum Giant Stamp stretches the right leg downward as a ranged strike
            if (isGiantStampActive)
            {
                int rightFootBone = 52301;
                Vector3 legStart = Function.Call<Vector3>(Hash.GET_PED_BONE_COORDS, trackingTargetPedHandle, rightFootBone, 0.0f, 0.0f, 0.0f);

                // Draw the leg cylinder extending down to current extension position
                DrawSingleStretchCylinderInternal(legStart, currentArmEndPos, 0.08f, false);

                // Marker Type 3 is a flat disc/cylinder. Perfect for footprints on the ground.
                int flatDiscMarker = 3;

                // Get player heading to rotate the foot and toes in the direction Luffy is facing
                float playerHeading = Function.Call<float>(Hash.GET_ENTITY_HEADING, trackingTargetPedHandle);
                double headingRad = (playerHeading + 90.0) * 0.0174532925; // Forward look vector
                double rightRad = (playerHeading) * 0.0174532925;       // Right strafe vector

                Vector3 forwardVec = new Vector3((float)Math.Cos(headingRad), (float)Math.Sin(headingRad), 0f);
                Vector3 rightVec = new Vector3((float)Math.Cos(rightRad), (float)Math.Sin(rightRad), 0f);

                // 1. DRAW THE SOLE/HEEL (An elongated flat oval using slightly scaled width/length)
                // Draw Marker parameters: type, pos XYZ, dir XYZ, rot XYZ, scale XYZ, R, G, B, alpha...
                Function.Call(Hash.DRAW_MARKER, flatDiscMarker,
                    currentArmEndPos.X, currentArmEndPos.Y, currentArmEndPos.Z,
                    0.0f, 0.0f, 0.0f, 0.0f, 0.0f, playerHeading,
                    3.2f, 4.8f, 0.5f, // Scale: Width 3.2m, Length 4.8m, Thickness 0.5m
                    0, 0, 0, 240, false, false, 2, false, false, false, false);

                // 2. DRAW THE 5 TOES (Ranging from Big Toe on the left/right side to Pinky)
                // We project them forward off the sole center point
                Vector3 toeRowCenter = currentArmEndPos + (forwardVec * 2.6f);

                // Big Toe (Thicker, further inside)
                Vector3 toe1 = toeRowCenter - (rightVec * 0.9f) + (forwardVec * 0.4f);
                Function.Call(Hash.DRAW_MARKER, flatDiscMarker, toe1.X, toe1.Y, toe1.Z, 0f, 0f, 0f, 0f, 0f, playerHeading, 0.9f, 1.1f, 0.4f, 0, 0, 0, 240, false, false, 2, false, false, false, false);

                // Index Toe
                Vector3 toe2 = toeRowCenter - (rightVec * 0.4f) + (forwardVec * 0.5f);
                Function.Call(Hash.DRAW_MARKER, flatDiscMarker, toe2.X, toe2.Y, toe2.Z, 0f, 0f, 0f, 0f, 0f, playerHeading, 0.7f, 0.8f, 0.4f, 0, 0, 0, 240, false, false, 2, false, false, false, false);

                // Middle Toe
                Vector3 toe3 = toeRowCenter + (forwardVec * 0.45f);
                Function.Call(Hash.DRAW_MARKER, flatDiscMarker, toe3.X, toe3.Y, toe3.Z, 0f, 0f, 0f, 0f, 0f, playerHeading, 0.65f, 0.75f, 0.4f, 0, 0, 0, 240, false, false, 2, false, false, false, false);

                // Ring Toe
                Vector3 toe4 = toeRowCenter + (rightVec * 0.4f) + (forwardVec * 0.35f);
                Function.Call(Hash.DRAW_MARKER, flatDiscMarker, toe4.X, toe4.Y, toe4.Z, 0f, 0f, 0f, 0f, 0f, playerHeading, 0.6f, 0.7f, 0.4f, 0, 0, 0, 240, false, false, 2, false, false, false, false);

                // Pinky Toe (Smallest, further outside)
                Vector3 toe5 = toeRowCenter + (rightVec * 0.85f) + (forwardVec * 0.15f);
                Function.Call(Hash.DRAW_MARKER, flatDiscMarker, toe5.X, toe5.Y, toe5.Z, 0f, 0f, 0f, 0f, 0f, playerHeading, 0.5f, 0.6f, 0.4f, 0, 0, 0, 240, false, false, 2, false, false, false, false);

                return;
            }


            // SPECIAL SPLIT CASE: Gum-Gum Spear stretches BOTH legs simultaneously
            if (isSpearActive)
            {
                int leftFootBone = 14201;
                int rightFootBone = 52301;

                Vector3 leftStart = Function.Call<Vector3>(Hash.GET_PED_BONE_COORDS, trackingTargetPedHandle, leftFootBone, 0.0f, 0.0f, 0.0f);
                DrawSingleStretchCylinderInternal(leftStart, spearTargetPos, 0.045f, false);

                Vector3 rightStart = Function.Call<Vector3>(Hash.GET_PED_BONE_COORDS, trackingTargetPedHandle, rightFootBone, 0.0f, 0.0f, 0.0f);
                DrawSingleStretchCylinderInternal(rightStart, spearTargetPos, 0.045f, false);

                return;
            }

            // --- Standard Single Limb / Shoulder Anchor Assignments ---
            if (isGatlingActive)
            {
                int activeShoulderBone = useLeftShoulderNext ? 40269 : 64729;
                currentArmStartPos = Function.Call<Vector3>(Hash.GET_PED_BONE_COORDS, trackingTargetPedHandle, activeShoulderBone, 0.0f, 0.0f, 0.0f);
            }
            else if (isRocketActive)
            {
                currentArmStartPos = Function.Call<Vector3>(Hash.GET_PED_BONE_COORDS, trackingTargetPedHandle, 6286, 0.0f, 0.0f, 0.0f);
                currentArmEndPos = rocketTargetPos;
            }
            // FIXED: Only force the foot bone calculation if the fading timer explicitly belongs to a leg move
            else if (armDisplayTimer > 0 && wasLastAttackLeg)
            {
                int rightFootBone = 52301;
                currentArmStartPos = Function.Call<Vector3>(Hash.GET_PED_BONE_COORDS, trackingTargetPedHandle, rightFootBone, 0.0f, 0.0f, 0.0f);
            }
            else if (!isCurrentlyExtending)
            {
                currentArmStartPos = Function.Call<Vector3>(Hash.GET_PED_BONE_COORDS, trackingTargetPedHandle, 6286, 0.0f, 0.0f, 0.0f);
            }

            float armRadius = isHeavyArmoredPunch ? 0.065f : 0.045f;
            DrawSingleStretchCylinderInternal(currentArmStartPos, currentArmEndPos, armRadius, isHeavyArmoredPunch && !isRocketActive);

            if (!isCurrentlyExtending && !isGatlingActive && !isRocketActive) armDisplayTimer--;
        }

        private void DrawSingleStretchCylinderInternal(Vector3 startPoint, Vector3 endPoint, float radius, bool drawHeavyMarker)
        {
            Vector3 aimDir = (endPoint - startPoint);
            float distance = aimDir.Length();
            if (distance < 0.1f) return;
            aimDir.Normalize();

            Vector3 upVec = Math.Abs(aimDir.Z) < 0.9f ? Vector3.WorldUp : new Vector3(1f, 0f, 0f);
            Vector3 rightVec = Vector3.Cross(aimDir, upVec);
            rightVec.Normalize();
            Vector3 actualUp = Vector3.Cross(rightVec, aimDir);
            actualUp.Normalize();

            // Tighter, thinner default radius profiles to close up visual gaps
            float tightRadius = radius;
            if (radius == 0.045f) tightRadius = 0.022f;
            else if (radius == 0.065f) tightRadius = 0.045f;

            // 1. High-Density Longitudinal Line Mesh Strands (72 lines)
            for (int i = 0; i < 72; i++)
            {
                double angle = (i * 5.0) * 0.0174532925;
                float cosA = (float)Math.Cos(angle) * tightRadius;
                float sinA = (float)Math.Sin(angle) * tightRadius;

                Vector3 radialOffset = (rightVec * cosA) + (actualUp * sinA);
                Vector3 finalStart = startPoint + radialOffset;
                Vector3 finalEnd = endPoint + radialOffset;

                Function.Call(Hash.DRAW_LINE, finalStart.X, finalStart.Y, finalStart.Z, finalEnd.X, finalEnd.Y, finalEnd.Z, 255, 230, 200, 255);
            }

            // 2. High-Density Cross-Sectional Rings (24 steps)
            for (int r = 1; r <= 4; r++)
            {
                float segmentFactor = (float)r / 5.0f;
                Vector3 ringCenter = startPoint + (aimDir * (distance * segmentFactor));

                for (int i = 0; i < 24; i++)
                {
                    double angle1 = (i * 15.0) * 0.0174532925;
                    double angle2 = ((i + 1) * 15.0) * 0.0174532925;

                    Vector3 p1 = ringCenter + (rightVec * (float)Math.Cos(angle1) * tightRadius) + (actualUp * (float)Math.Sin(angle1) * tightRadius);
                    Vector3 p2 = ringCenter + (rightVec * (float)Math.Cos(angle2) * tightRadius) + (actualUp * (float)Math.Sin(angle2) * tightRadius);

                    Function.Call(Hash.DRAW_LINE, p1.X, p1.Y, p1.Z, p2.X, p2.Y, p2.Z, 255, 230, 200, 255);
                }
            }

            // 3. SOLID BLACK HAKI IMPACT INDICATOR (Restored, Pure & Crash-Proof)
            if (drawHeavyMarker)
            {
                float ballRadius = 3.5f;

                // One true core sphere: Set to a deep obsidian purple-gray with 100% solid opacity
                Function.Call(Hash.DRAW_MARKER, 28,
                    endPoint.X, endPoint.Y, endPoint.Z,
                    0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f,
                    ballRadius, ballRadius, ballRadius,
                    20, 15, 30, 255, // Obsidian Haki tone (Alpha: 255 makes it completely solid)
                    false, false, 2, false, false, false, false);
            }
            // 4. STANDARD CAP LAYER
            else if (!isGiantStampActive && !isSpearActive && !isWhipActive)
            {
                float tipSphereDiameter = tightRadius * 2.4f;
                Function.Call(Hash.DRAW_MARKER, 28, endPoint.X, endPoint.Y, endPoint.Z, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, tipSphereDiameter, tipSphereDiameter, tipSphereDiameter, 255, 230, 200, 255, false, false, 2, false, false, false, false);
            }

        }



        private void PerformGatlingShot(int trackingTargetPedHandle)
        {
            int boneSourceId = useLeftShoulderNext ? 64729 : 40269;
            useLeftShoulderNext = !useLeftShoulderNext;

            Vector3 launchOriginPos = Function.Call<Vector3>(Hash.GET_PED_BONE_COORDS, trackingTargetPedHandle, boneSourceId, 0.0f, 0.0f, 0.0f);
            Vector3 camPos = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_COORD);
            Vector3 camRot = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_ROT, 2);

            double rotX = camRot.X * 0.0174532924;
            double rotZ = camRot.Z * 0.0174532924;
            rotX += 0.095;

            double scatterPitch = ((randomSeed.NextDouble() * 2.0) - 1.0) * 0.07;
            double scatterYaw = ((randomSeed.NextDouble() * 2.0) - 1.0) * 0.07;

            rotX += scatterPitch;
            rotZ += scatterYaw;

            double num = Math.Abs(Math.Cos(rotX));
            Vector3 scatterDirection = new Vector3((float)(-(float)Math.Sin(rotZ) * num), (float)(Math.Cos(rotZ) * num), (float)Math.Sin(rotX));
            Vector3 targetPos = camPos + (scatterDirection * maxStretchDistance);

            uint sniperBulletHash = 0x3656C8C1;
            Function.Call(Hash.SHOOT_SINGLE_BULLET_BETWEEN_COORDS, camPos.X, camPos.Y, camPos.Z, targetPos.X, targetPos.Y, targetPos.Z, 0, true, sniperBulletHash, trackingTargetPedHandle, true, false, -1.0f);

            int rayHandle = Function.Call<int>((Hash)0x377906D8A31E5586, launchOriginPos.X, launchOriginPos.Y, launchOriginPos.Z, targetPos.X, targetPos.Y, targetPos.Z, -1, trackingTargetPedHandle, 7);

            OutputArgument hitArg = new OutputArgument();
            OutputArgument endCoordsArg = new OutputArgument();
            OutputArgument surfaceNormalArg = new OutputArgument();
            OutputArgument entityArg = new OutputArgument();

            Function.Call((Hash)0x3D87450E15D98694, rayHandle, hitArg, endCoordsArg, surfaceNormalArg, entityArg);

            if (hitArg.GetResult<bool>())
            {
                int hitEntityHandle = entityArg.GetResult<int>();
                if (hitEntityHandle != 0 && Function.Call<bool>(Hash.DOES_ENTITY_EXIST, hitEntityHandle))
                {
                    int weaponMeleeHash = -1569042534;
                    if (Function.Call<bool>(Hash.IS_ENTITY_A_PED, hitEntityHandle))
                    {
                        Function.Call(Hash.APPLY_DAMAGE_TO_PED, hitEntityHandle, 35, true, weaponMeleeHash);
                        Function.Call(Hash.APPLY_FORCE_TO_ENTITY, hitEntityHandle, 1, scatterDirection.X * 45f, scatterDirection.Y * 45f, scatterDirection.Z * 20f, 0f, 0f, 0f, 0, false, true, true, false, true);
                    }
                    else if (Function.Call<bool>(Hash.IS_ENTITY_A_VEHICLE, hitEntityHandle))
                    {
                        Vector3 hitPt = endCoordsArg.GetResult<Vector3>();
                        Function.Call(Hash.SET_VEHICLE_DAMAGE, hitEntityHandle, hitPt.X, hitPt.Y, hitPt.Z, 80.0f, 150.0f, true);
                        Function.Call(Hash.APPLY_FORCE_TO_ENTITY, hitEntityHandle, 1, scatterDirection.X * 50f, scatterDirection.Y * 50f, scatterDirection.Z * 25f, 0f, 0f, 0f, 0, false, true, true, false, true);
                    }
                }
            }

            currentArmStartPos = launchOriginPos;
            currentArmEndPos = (entityArg.GetResult<int>() != 0) ? endCoordsArg.GetResult<Vector3>() : launchOriginPos + (scatterDirection * (maxStretchDistance * 0.4f));
        }
        private void InitializeAttackTrajectory(int playerPedHandle)
        {
            Vector3 handPos = Function.Call<Vector3>(Hash.GET_PED_BONE_COORDS, playerPedHandle, 6286, 0.0f, 0.0f, 0.0f);
            Vector3 camPos = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_COORD);
            Vector3 camRot = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_ROT, 2);

            double rotX = camRot.X * 0.0174532924;
            double rotZ = camRot.Z * 0.0174532924;
            rotX += 0.095;

            double num = Math.Abs(Math.Cos(rotX));
            extensionDirection = new Vector3((float)(-(float)Math.Sin(rotZ) * num), (float)(Math.Cos(rotZ) * num), (float)Math.Sin(rotX));

            Vector3 targetPos = camPos + (extensionDirection * maxStretchDistance);

            uint sniperBulletHash = 0x3656C8C1;
            Function.Call(Hash.SHOOT_SINGLE_BULLET_BETWEEN_COORDS, camPos.X, camPos.Y, camPos.Z, targetPos.X, targetPos.Y, targetPos.Z, 0, true, sniperBulletHash, playerPedHandle, true, false, -1.0f);

            int rayHandle = Function.Call<int>((Hash)0x377906D8A31E5586, handPos.X, handPos.Y, handPos.Z, targetPos.X, targetPos.Y, targetPos.Z, -1, playerPedHandle, 7);

            OutputArgument hitArg = new OutputArgument();
            OutputArgument endCoordsArg = new OutputArgument();
            OutputArgument surfaceNormalArg = new OutputArgument();
            OutputArgument entityArg = new OutputArgument();

            Function.Call((Hash)0x3D87450E15D98694, rayHandle, hitArg, endCoordsArg, surfaceNormalArg, entityArg);

            bool hitAnything = hitArg.GetResult<bool>();
            int hitEntityHandle = entityArg.GetResult<int>();
            Vector3 hitPoint = endCoordsArg.GetResult<Vector3>();

            Vector3 finalHitTarget = (hitAnything && hitPoint != Vector3.Zero) ? hitPoint : handPos + (extensionDirection * maxStretchDistance);

            if (hitAnything && hitEntityHandle != 0 && Function.Call<bool>(Hash.DOES_ENTITY_EXIST, hitEntityHandle))
            {
                if (Function.Call<bool>(Hash.IS_ENTITY_A_PED, hitEntityHandle))
                {
                    finalHitTarget = Function.Call<Vector3>(Hash.GET_PED_BONE_COORDS, hitEntityHandle, 31086, 0.0f, 0.0f, 0.0f);

                    int weaponMeleeHash = -1569042534;
                    Function.Call(Hash.APPLY_DAMAGE_TO_PED, hitEntityHandle, 70, true, weaponMeleeHash);
                    Function.Call(Hash.APPLY_FORCE_TO_ENTITY, hitEntityHandle, 1, extensionDirection.X * 55f, extensionDirection.Y * 55f, extensionDirection.Z * 25f, 0f, 0f, 0f, 0, false, true, true, false, true);
                }
                else if (Function.Call<bool>(Hash.IS_ENTITY_A_VEHICLE, hitEntityHandle))
                {
                    Function.Call(Hash.SET_VEHICLE_DAMAGE, hitEntityHandle, hitPoint.X, hitPoint.Y, hitPoint.Z, 200.0f, 300.0f, true);
                    Function.Call(Hash.APPLY_FORCE_TO_ENTITY, hitEntityHandle, 1, extensionDirection.X * 65f, extensionDirection.Y * 65f, extensionDirection.Z * 30f, 0f, 0f, 0f, 0, false, true, true, false, true);
                }
            }

            currentArmStartPos = handPos;
            currentArmEndPos = finalHitTarget;
            extensionCurrentLength = 0.0f;
            extensionTargetLength = (finalHitTarget - handPos).Length();

            isCurrentlyExtending = true;
        }

        private void ProcessProximityDestruction(int playerPedHandle, Vector3 sphereCenter)
        {
            Function.Call(Hash.ADD_EXPLOSION, sphereCenter.X, sphereCenter.Y, sphereCenter.Z, 32, 5.0f, 0.5f, true, true);
        }

        private void InitializeRocketTrajectory(int playerPedHandle)
        {
            Vector3 handPos = Function.Call<Vector3>(Hash.GET_PED_BONE_COORDS, playerPedHandle, 6286, 0.0f, 0.0f, 0.0f);
            Vector3 camPos = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_COORD);
            Vector3 camRot = Function.Call<Vector3>(Hash.GET_GAMEPLAY_CAM_ROT, 2);

            double rotX = camRot.X * 0.0174532924;
            double rotZ = camRot.Z * 0.0174532924;
            rotX += 0.095;

            double num = Math.Abs(Math.Cos(rotX));
            Vector3 launchDirection = new Vector3((float)(-(float)Math.Sin(rotZ) * num), (float)(Math.Cos(rotZ) * num), (float)Math.Sin(rotX));
            launchDirection.Normalize();

            Vector3 testEndPos = camPos + (launchDirection * 150.0f);
            int rayHandle = Function.Call<int>((Hash)0x377906D8A31E5586, handPos.X, handPos.Y, handPos.Z, testEndPos.X, testEndPos.Y, testEndPos.Z, 1, playerPedHandle, 7);

            OutputArgument hitArg = new OutputArgument();
            OutputArgument endCoordsArg = new OutputArgument();
            OutputArgument surfaceNormalArg = new OutputArgument();
            OutputArgument entityArg = new OutputArgument();

            Function.Call((Hash)0x3D87450E15D98694, rayHandle, hitArg, endCoordsArg, surfaceNormalArg, entityArg);

            bool hitAnything = hitArg.GetResult<bool>();
            Vector3 hitPoint = endCoordsArg.GetResult<Vector3>();

            if (hitAnything && hitPoint != Vector3.Zero && Vector3.Distance(handPos, hitPoint) > 2.0f && hitPoint.Length() > 10.0f)
            {
                rocketTargetPos = hitPoint;
            }
            else
            {
                rocketTargetPos = handPos + (launchDirection * 95.0f);
            }

            isRocketActive = true;
            rocketTimeoutTimer = 120;

            // FIX: Force reset the punch flag so a heavy attack cannot bleed into your rocket visual strands
            isHeavyArmoredPunch = false;

            currentArmStartPos = handPos;
            currentArmEndPos = rocketTargetPos;

            Function.Call(Hash.SET_PED_TO_RAGDOLL, playerPedHandle, 500, 500, 0, true, true, false);
        }

        private void ProcessRocketMovement(int playerPedHandle)
        {
            if (!isRocketActive) return;

            rocketTimeoutTimer--;

            Vector3 currentPos = Function.Call<Vector3>(Hash.GET_ENTITY_COORDS, playerPedHandle, true);
            Vector3 travelDirection = rocketTargetPos - currentPos;
            float distanceRemaining = travelDirection.Length();

            if (distanceRemaining < 4.0f || rocketTimeoutTimer <= 0)
            {
                isRocketActive = false;

                // Kill the physical ragdoll loop instantly so the character can react on touchdown
                Function.Call(Hash.SET_PED_TO_RAGDOLL, playerPedHandle, 0, 0, 0, false, false, false);
                Function.Call(Hash.CLEAR_PED_TASKS, playerPedHandle);

                armDisplayTimer = 25;
                return;
            }

            travelDirection.Normalize();

            Vector3 forceVector = (travelDirection * 45.0f) + new Vector3(0f, 0f, 8.0f);
            Function.Call(Hash.APPLY_FORCE_TO_ENTITY, playerPedHandle, 1, forceVector.X, forceVector.Y, forceVector.Z, 0f, 0f, 0f, 0, false, true, true, false, true);

            currentArmStartPos = Function.Call<Vector3>(Hash.GET_PED_BONE_COORDS, playerPedHandle, 6286, 0.0f, 0.0f, 0.0f);
            currentArmEndPos = rocketTargetPos;
        }


        private void InitializeSpearTrajectory(int playerPedHandle)
        {
            Vector3 currentPos = Function.Call<Vector3>(Hash.GET_ENTITY_COORDS, playerPedHandle, true);
            Vector3 traceEnd = currentPos + new Vector3(0f, 0f, -100f);

            int rayHandle = Function.Call<int>((Hash)0x377906D8A31E5586, currentPos.X, currentPos.Y, currentPos.Z, traceEnd.X, traceEnd.Y, traceEnd.Z, 19, playerPedHandle, 7);

            OutputArgument hitArg = new OutputArgument();
            OutputArgument endCoordsArg = new OutputArgument();
            OutputArgument surfaceNormalArg = new OutputArgument();
            OutputArgument entityArg = new OutputArgument();

            Function.Call((Hash)0x3D87450E15D98694, rayHandle, hitArg, endCoordsArg, surfaceNormalArg, entityArg);

            bool hitAnything = hitArg.GetResult<bool>();
            Vector3 hitPoint = endCoordsArg.GetResult<Vector3>();

            spearTargetPos = (hitAnything && hitPoint != Vector3.Zero) ? hitPoint : currentPos + new Vector3(0f, 0f, -30f);

            if (Vector3.Distance(currentPos, spearTargetPos) > 2.5f)
            {
                isSpearActive = true;
                spearTimeoutTimer = 60;

                Function.Call(Hash.SET_PED_TO_RAGDOLL, playerPedHandle, 1000, 1000, 0, true, true, false);
            }
        }

        private void ProcessSpearMovement(int playerPedHandle)
        {
            if (!isSpearActive) return;

            spearTimeoutTimer--;

            Vector3 currentPos = Function.Call<Vector3>(Hash.GET_ENTITY_COORDS, playerPedHandle, true);
            Vector3 travelDirection = spearTargetPos - currentPos;
            float distanceRemaining = travelDirection.Length();

            if (distanceRemaining < 2.5f || spearTimeoutTimer <= 0)
            {
                isSpearActive = false;
                Function.Call(Hash.SET_PED_TO_RAGDOLL, playerPedHandle, 0, 0, 0, false, false, false);
                Function.Call(Hash.ADD_EXPLOSION, spearTargetPos.X, spearTargetPos.Y, spearTargetPos.Z, 32, 6.0f, 1.0f, true, false);
                return;
            }

            travelDirection.Normalize();

            Vector3 velocityVector = travelDirection * 75.0f;
            Function.Call(Hash.SET_ENTITY_VELOCITY, playerPedHandle, velocityVector.X, velocityVector.Y, velocityVector.Z);
        }

        private void InitializeGiantStampTrajectory(int playerPedHandle)
        {
            int rightFootBone = 52301;
            Vector3 legStart = Function.Call<Vector3>(Hash.GET_PED_BONE_COORDS, playerPedHandle, rightFootBone, 0.0f, 0.0f, 0.0f);

            Vector3 traceEnd = legStart + new Vector3(0f, 0f, -100f);
            int rayHandle = Function.Call<int>((Hash)0x377906D8A31E5586, legStart.X, legStart.Y, legStart.Z, traceEnd.X, traceEnd.Y, traceEnd.Z, 19, playerPedHandle, 7);

            OutputArgument hitArg = new OutputArgument();
            OutputArgument endCoordsArg = new OutputArgument();
            OutputArgument surfaceNormalArg = new OutputArgument();
            OutputArgument entityArg = new OutputArgument();

            Function.Call((Hash)0x3D87450E15D98694, rayHandle, hitArg, endCoordsArg, surfaceNormalArg, entityArg);

            bool hitAnything = hitArg.GetResult<bool>();
            Vector3 hitPoint = endCoordsArg.GetResult<Vector3>();

            giantStampTargetPos = (hitAnything && hitPoint != Vector3.Zero) ? hitPoint : legStart + new Vector3(0f, 0f, -30f);

            isGiantStampActive = true;

            currentArmStartPos = legStart;
            currentArmEndPos = legStart;
            extensionCurrentLength = 0.0f;
            extensionTargetLength = (giantStampTargetPos - legStart).Length();

            // FIXED: Absolute protection. No ragdoll or animation tasks are triggered here at all.
        }

        private void ProcessGiantStampExtension(int playerPedHandle)
        {
            if (!isGiantStampActive) return;

            int rightFootBone = 52301;
            Vector3 legStart = Function.Call<Vector3>(Hash.GET_PED_BONE_COORDS, playerPedHandle, rightFootBone, 0.0f, 0.0f, 0.0f);

            Vector3 downwardDir = giantStampTargetPos - legStart;
            downwardDir.Normalize();

            extensionCurrentLength += 2.2f;
            currentArmEndPos = legStart + (downwardDir * extensionCurrentLength);

            ProcessProximityDestruction(playerPedHandle, currentArmEndPos);

            // Ground Target Stomp Impact Reached!
            if (extensionCurrentLength >= extensionTargetLength || Vector3.Distance(currentArmEndPos, giantStampTargetPos) < 1.5f)
            {
                isGiantStampActive = false;
                armDisplayTimer = 20; // Hold foot on screen briefly

                // FIXED: Removed any trailing SET_PED_TO_RAGDOLL impact trip logic completely
                Function.Call(Hash.ADD_EXPLOSION, giantStampTargetPos.X, giantStampTargetPos.Y, giantStampTargetPos.Z, 32, 12.0f, 1.0f, true, false);
            }
        }

        private void InitializeWhipAttack(int playerPedHandle)
        {
            isWhipActive = true;
            whipTimer = 42;
            whipCurrentAngle = -90.0f; // Start sweeping from Luffy's left side

            // Activate the baseline stable ragdoll framework safely
            Function.Call(Hash.SET_PED_TO_RAGDOLL, playerPedHandle, 1500, 1000, 0, true, true, false);
        }

        private void ProcessWhipSweep(int playerPedHandle)
        {
            if (!isWhipActive) return;

            whipTimer--;
            if (whipTimer <= 0)
            {
                isWhipActive = false;
                Function.Call(Hash.SET_PED_TO_RAGDOLL, playerPedHandle, 0, 0, 0, false, false, false);
                attackCooldown = 30;
                return;
            }

            // Extract center capsule physics root coordinates
            Vector3 playerPos = Function.Call<Vector3>(Hash.GET_ENTITY_COORDS, playerPedHandle, true);
            Vector3 legStart = playerPos + new Vector3(0f, 0f, -0.4f);

            // FIXED STEP 1: Strict ground clamp. Force vertical Z speed to 0.0f every single frame 
            // during the entire move sequence to completely eliminate the sky launching glitch.
            Vector3 currentVel = Function.Call<Vector3>(Hash.GET_ENTITY_VELOCITY, playerPedHandle);
            Function.Call(Hash.SET_ENTITY_VELOCITY, playerPedHandle, currentVel.X * 0.2f, currentVel.Y * 0.2f, 0.0f);

            // Once the 16-frame wind-up finishes, execute the 360 rotation and stretch sweep
            if (whipTimer <= 26)
            {
                float playerHeading = Function.Call<float>(Hash.GET_ENTITY_HEADING, playerPedHandle);

                whipCurrentAngle += 10.3f;

                float newHeading = playerHeading + 14.0f;
                if (newHeading > 360.0f) newHeading -= 360.0f;
                Function.Call(Hash.SET_ENTITY_HEADING, playerPedHandle, newHeading);

                double radAngle = (newHeading + 90.0f + whipCurrentAngle) * 0.0174532925;
                Vector3 sweepDirection = new Vector3((float)Math.Cos(radAngle), (float)Math.Sin(radAngle), 0.0f);
                sweepDirection.Normalize();

                float whipReachDistance = 25.0f;
                currentArmEndPos = legStart + (sweepDirection * whipReachDistance);

                int rayHandle = Function.Call<int>((Hash)0x377906D8A31E5586, legStart.X, legStart.Y, legStart.Z, currentArmEndPos.X, currentArmEndPos.Y, currentArmEndPos.Z, -1, playerPedHandle, 7);

                OutputArgument hitArg = new OutputArgument();
                OutputArgument endCoordsArg = new OutputArgument();
                OutputArgument surfaceNormalArg = new OutputArgument();
                OutputArgument entityArg = new OutputArgument();

                Function.Call((Hash)0x3D87450E15D98694, rayHandle, hitArg, endCoordsArg, surfaceNormalArg, entityArg);

                if (hitArg.GetResult<bool>())
                {
                    int hitEntityHandle = entityArg.GetResult<int>();
                    if (hitEntityHandle != 0 && Function.Call<bool>(Hash.DOES_ENTITY_EXIST, hitEntityHandle))
                    {
                        Vector3 pushForce = Vector3.Cross(sweepDirection, Vector3.WorldUp);
                        pushForce.Normalize();

                        int weaponMeleeHash = -1569042534;
                        if (Function.Call<bool>(Hash.IS_ENTITY_A_PED, hitEntityHandle))
                        {
                            Function.Call(Hash.APPLY_DAMAGE_TO_PED, hitEntityHandle, 60, true, weaponMeleeHash);
                            Function.Call(Hash.APPLY_FORCE_TO_ENTITY, hitEntityHandle, 1, pushForce.X * 65f, pushForce.Y * 65f, 8f, 0f, 0f, 0f, 0, false, true, true, false, true);
                        }
                        else if (Function.Call<bool>(Hash.IS_ENTITY_A_VEHICLE, hitEntityHandle))
                        {
                            Vector3 hitPt = endCoordsArg.GetResult<Vector3>();
                            Function.Call(Hash.SET_VEHICLE_DAMAGE, hitEntityHandle, hitPt.X, hitPt.Y, hitPt.Z, 120.0f, 200.0f, true);
                            Function.Call(Hash.APPLY_FORCE_TO_ENTITY, hitEntityHandle, 1, pushForce.X * 80f, pushForce.Y * 80f, 10f, 0f, 0f, 0f, 0, false, true, true, false, true);
                        }
                    }
                }

                currentArmStartPos = legStart;
            }
            else
            {
                // WIND-UP PHASE: Stable entity-offset pull mechanics
                float playerHeading = Function.Call<float>(Hash.GET_ENTITY_HEADING, playerPedHandle);
                double backRad = (playerHeading - 90.0f) * 0.0174532925;

                // FIXED STEP 2: Flattened vertical lift from 1.5f down to 0.2f 
                // to pull the legs horizontally backward instead of launching Luffy upward
                Vector3 forceDir = new Vector3((float)Math.Cos(backRad), (float)Math.Sin(backRad), 0.2f);
                forceDir.Normalize();

                float forceStrength = 24.0f; // Kept high power for a crisp, fast pull
                Vector3 finalForce = forceDir * forceStrength;

                Function.Call(Hash.APPLY_FORCE_TO_ENTITY, playerPedHandle, 1,
                    finalForce.X, finalForce.Y, finalForce.Z,
                    0.0f, 0.0f, -0.8f,
                    0, false, true, true, false, true);

                // Keep the visual line collapsed at his feet so it doesn't flash early
                currentArmStartPos = legStart;
                currentArmEndPos = legStart;
            }
        }
    }
}