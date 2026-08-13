using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using Control = GTA.Control;


namespace AnimeCharacterMod
{
    public partial class LuffyController : Script
    {
        private float maxStretchDistance = 120.0f;
        private int attackCooldown = 0;

        // Animation Sync variables
        private int punchDelayTimer = 0;
        private bool isWaitingToStretch = false;

        // Rendering and Extension state trackers
        private int armDisplayTimer = 0;
        private Vector3 currentArmStartPos;
        private Vector3 currentArmEndPos;

        // Special State Flags
        private bool isHeavyArmoredPunch = false;
        private bool isGatlingActive = false;
        private bool isCurrentlyExtending = false;
        private Vector3 extensionDirection;
        private float extensionCurrentLength = 0.0f;
        private float extensionTargetLength = 0.0f;

        // Rocket State Engine Variables
        private bool isRocketActive = false;
        private Vector3 rocketTargetPos;
        private int rocketTimeoutTimer = 0;

        // Spear State Engine Variables
        private bool isSpearActive = false;
        private Vector3 spearTargetPos;
        private int spearTimeoutTimer = 0;

        // Giant Stamp State Engine Variables
        private bool isGiantStampActive = false;
        private Vector3 giantStampTargetPos;

        // FIX TRACKER: Remembers if our active lingering display belongs to a leg move
        private bool wasLastAttackLeg = false;

        // Continuous Input and Interval Trackers
        private int controlHoldTracker = 0;
        private int gatlingIntervalTimer = 0;

        // Alternator variable for dual shoulder tracking
        private bool useLeftShoulderNext = false;

        // Whip State Engine Variables
        private bool isWhipActive = false;
        private int whipTimer = 0;
        private float whipCurrentAngle = 0.0f;


        public LuffyController()
        {
            Tick += OnTick;
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!MainMenuController.IsLuffyActive) return;

            int playerPedHandle = Function.Call<int>(Hash.PLAYER_PED_ID);
            int playerHandle = Function.Call<int>(Hash.PLAYER_ID);
            if (playerPedHandle == 0) return;

            // Ground & Air Chaining Attacks Matrix
            if (!isCurrentlyExtending && !isWaitingToStretch && !isSpearActive && !isGiantStampActive)
            {
                // FREEDOM AIR FIX: Removed all vertical velocity checks (Z restrictions) completely
                if (Function.Call<bool>(Hash.IS_ENTITY_IN_AIR, playerPedHandle))
                {
                    // Trigger Spear by hitting Jump key instantly at ANY point in the air
                    if (IsControlJustPressed(Control.Jump) && !isRocketActive)
                    {
                        wasLastAttackLeg = true;
                        InitializeSpearTrajectory(playerPedHandle);
                    }

                    // Trigger Giant Stamp by pressing DPAD Left while airborne
                    if (IsControlJustPressed(Control.PhoneLeft) && !isRocketActive)
                    {
                        wasLastAttackLeg = true;
                        InitializeGiantStampTrajectory(playerPedHandle);
                    }

                    // CHAIN ROCKET ACTION: Allows you to shoot another rocket mid-air
                    if (IsControlJustPressed(Control.Context) && !isRocketActive)
                    {
                        wasLastAttackLeg = false;
                        InitializeRocketTrajectory(playerPedHandle);
                    }
                }
                // Standard Ground Actions Matrix
                else
                {
                    if (IsControlJustPressed(Control.Context) && !isRocketActive)
                    {
                        wasLastAttackLeg = false;
                        InitializeRocketTrajectory(playerPedHandle);
                    }

                    // NEW: Trigger Gum-Gum Whip by pressing DPAD Down on the ground
                    if (IsControlJustPressed(Control.PhoneDown) && !isCurrentlyExtending && !isWaitingToStretch && !isWhipActive)
                    {
                        wasLastAttackLeg = true;
                        InitializeWhipAttack(playerPedHandle);
                    }
                }

                // Melee Attack Input Matrix
                if (!isRocketActive && !isSpearActive && !isWhipActive)
                {
                    if (IsControlPressed(Control.Attack))
                    {
                        controlHoldTracker++;

                        if (controlHoldTracker >= 90)
                        {
                            isGatlingActive = true;
                            isHeavyArmoredPunch = false;
                            wasLastAttackLeg = false;

                            if (gatlingIntervalTimer <= 0)
                            {
                                PerformGatlingShot(playerPedHandle);
                                gatlingIntervalTimer = 2;
                            }
                        }
                    }
                    else
                    {
                        if (controlHoldTracker > 0 && controlHoldTracker < 90)
                        {
                            if (attackCooldown <= 0)
                            {
                                isHeavyArmoredPunch = false;
                                punchDelayTimer = 42;
                                isWaitingToStretch = true;
                                attackCooldown = 55;
                                wasLastAttackLeg = false;
                            }
                        }

                        controlHoldTracker = 0;
                        isGatlingActive = false;

                        if (IsControlJustPressed(Control.MeleeAttackLight))
                        {
                            if (attackCooldown <= 0)
                            {
                                isHeavyArmoredPunch = true;
                                punchDelayTimer = 42;
                                isWaitingToStretch = true;
                                attackCooldown = 110;
                                wasLastAttackLeg = false;
                            }
                        }
                    }
                }
            }

            if (attackCooldown > 0) attackCooldown--;
            if (gatlingIntervalTimer > 0) gatlingIntervalTimer--;

            // Flight Paths Physics Evaluators
            if (isRocketActive) ProcessRocketMovement(playerPedHandle);
            if (isSpearActive) ProcessSpearMovement(playerPedHandle);
            if (isGiantStampActive) ProcessGiantStampExtension(playerPedHandle);
            if (isWhipActive) ProcessWhipSweep(playerPedHandle);

            // Wind-up Timing Delays
            if (isWaitingToStretch)
            {
                punchDelayTimer--;
                if (punchDelayTimer <= 0)
                {
                    InitializeAttackTrajectory(playerPedHandle);
                    isWaitingToStretch = false;
                }
            }

            // Linear Extension Calculators for Standard Punches
            if (isCurrentlyExtending && !wasLastAttackLeg)
            {
                currentArmStartPos = Function.Call<Vector3>(Hash.GET_PED_BONE_COORDS, playerPedHandle, 6286, 0.0f, 0.0f, 0.0f);

                if (isHeavyArmoredPunch)
                {
                    extensionCurrentLength += 1.6f;
                    currentArmEndPos = currentArmStartPos + (extensionDirection * extensionCurrentLength);

                    ProcessProximityDestruction(playerPedHandle, currentArmEndPos);

                    if (extensionCurrentLength >= extensionTargetLength || extensionCurrentLength >= maxStretchDistance)
                    {
                        isCurrentlyExtending = false;
                        armDisplayTimer = 12;
                    }
                }
                else
                {
                    currentArmEndPos = currentArmStartPos + (extensionDirection * extensionTargetLength);
                    isCurrentlyExtending = false;
                    armDisplayTimer = 45;
                }
            }

            if (armDisplayTimer > 0 || isCurrentlyExtending || isGatlingActive || isRocketActive || isSpearActive || isGiantStampActive || isWhipActive)
            {
                RenderStretchArmGraphics(playerPedHandle);
            }
        }
        private bool IsControlPressed(Control control)
        {
            return Function.Call<bool>(Hash.IS_DISABLED_CONTROL_PRESSED, 0, (int)control);
        }

        private bool IsControlJustPressed(Control control)
        {
            return Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 0, (int)control);
        }

    }
}
