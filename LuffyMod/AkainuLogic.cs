using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;

namespace AnimeCharacterMod
{
    public partial class AkainuController : Script
    {
        // Thread-safe game engine step trackers
        private int blastStepIndex = -1;
        private int nextStepGameTime = 0;
        private Vector3 initialBlastOrigin;
        private float lockedPunchHeading = 0f;

        // Visual asset tracking lists
        private readonly List<int> activeParticleHandles = new List<int>();
        private readonly List<int> particlesToRemove = new List<int>();
        private readonly Dictionary<int, int> particleExpirationTicks = new Dictionary<int, int>();

        public void InitializeCharacterState(Ped player)
        {
            if (player == null) return;
            player.CanRagdoll = false;
            Function.Call(Hash.SET_PED_CONFIG_FLAG, player.Handle, 37, true); // Fireproof flag
            LoadPtfxAsset(PtfxLibrary);
        }

        public void UpdateActiveFrameLoops(Ped player)
        {
            if (!isAkainuModeActive) return;

            // Stream asset engines hot into VRAM pipelines
            Function.Call(Hash.REQUEST_NAMED_PTFX_ASSET, PtfxLibrary);

            int currentGameTime = Game.GameTime;

            // 1. REPEATING EXPLOSION STATE MACHINE (Locked Straight Line Forward)
            if (blastStepIndex >= 0 && currentGameTime >= nextStepGameTime)
            {
                // Calculate straight-line forward steps mathematically using the locked heading
                // GTA heading uses degrees where 0 is North, counting counter-clockwise
                float headingRadians = (float)(lockedPunchHeading * (Math.PI / 180.0));
                float stepForwardDistance = 2.8f * (blastStepIndex + 1);

                // Math calculation ensuring the line travels perfectly forward from the origin point
                float targetX = initialBlastOrigin.X - ((float)Math.Sin(headingRadians) * stepForwardDistance);
                float targetY = initialBlastOrigin.Y + ((float)Math.Cos(headingRadians) * stepForwardDistance);

                Vector3 specificStepLocation = new Vector3(targetX, targetY, initialBlastOrigin.Z);

                ExecuteSingleLavaBlastStep(player, specificStepLocation);

                blastStepIndex++;
                nextStepGameTime = currentGameTime + 120; // Stagger sequential explosions by 120ms

                // Cut off the execution machine once 4 steps are achieved
                if (blastStepIndex >= 4)
                {
                    blastStepIndex = -1; // Completely reset and release the trigger lock
                }
            }

            // 2. Loop Emitter Particle Disposal Engine
            particlesToRemove.Clear();
            foreach (var kvp in particleExpirationTicks)
            {
                if (currentGameTime >= kvp.Value)
                {
                    Function.Call(Hash.REMOVE_PARTICLE_FX, kvp.Key, false);
                    particlesToRemove.Add(kvp.Key);
                }
            }

            foreach (int handle in particlesToRemove)
            {
                particleExpirationTicks.Remove(handle);
                activeParticleHandles.Remove(handle);
            }
        }

        public void TriggerLavaPunchCombo(Ped player)
        {
            if (player == null || player.IsInVehicle()) return;

            double timePassed = (DateTime.Now - lastPunchTime).TotalMilliseconds;
            if (timePassed <= PunchCooldownMs) return;

            // Guard validation safety lock: If a rolling blast wave is active, exit out
            if (blastStepIndex >= 0) return;

            if (player.Weapons.Current.Hash != WeaponHash.Unarmed)
            {
                player.Weapons.Select(WeaponHash.Unarmed);
            }

            // Force low-level task animation assignment
            Function.Call(Hash.TASK_PLAY_ANIM,
                player.Handle,
                "melee@knife@streamed_core",
                "knife_v_attack_b",
                8.0f, -8.0f, -1, 0, 0.0f,
                false, false, false
            );

            // FIX: Capture the player's core root position and absolute heading at the moment of trigger
            // This prevents animations from shifting the forward line calculations
            initialBlastOrigin = player.Position;
            lockedPunchHeading = player.Heading;

            // Configure state parameters to start firing the explosion loop after the 620ms stretch animation completes
            blastStepIndex = 0;
            nextStepGameTime = Game.GameTime + 620;

            lastPunchTime = DateTime.Now;
        }

        private void ExecuteSingleLavaBlastStep(Ped player, Vector3 blastTarget)
        {
            if (player == null || !player.IsAlive) return;

            if (!Function.Call<bool>(Hash.HAS_NAMED_PTFX_ASSET_LOADED, PtfxLibrary)) return;

            Function.Call(Hash.USE_PARTICLE_FX_ASSET, PtfxLibrary);

            // VISUAL LAYER A: FIX: Swapped purple clown fx for heavy respray smoke asset (Allows clean orange recolors)
            int ptfxHandleLava = Function.Call<int>(Hash.START_PARTICLE_FX_LOOPED_AT_COORD,
                "veh_respray_smoke", blastTarget.X, blastTarget.Y, blastTarget.Z,
                0.0f, 0.0f, 0.0f,
                3.5f, // Scaled massive to look like erupting volcanic chunks
                false, false, false
            );
            Function.Call(Hash.SET_PARTICLE_FX_LOOPED_COLOUR, ptfxHandleLava, 1.0f, 0.22f, 0.0f, false);
            Function.Call(Hash.SET_PARTICLE_FX_LOOPED_ALPHA, ptfxHandleLava, 1.0f);

            // VISUAL LAYER B: Volcanic core engine fire sparks
            int ptfxHandleFire = Function.Call<int>(Hash.START_PARTICLE_FX_LOOPED_AT_COORD,
                "exp_grd_ext_vehicle_explosion", blastTarget.X, blastTarget.Y, blastTarget.Z,
                0.0f, 0.0f, 0.0f,
                1.2f, false, false, false
            );
            Function.Call(Hash.SET_PARTICLE_FX_LOOPED_COLOUR, ptfxHandleFire, 1.0f, 0.35f, 0.0f, false);

            // Log emitters inside frame expiration arrays
            int disappearanceTime = Game.GameTime + 450;
            particleExpirationTicks[ptfxHandleLava] = disappearanceTime;
            particleExpirationTicks[ptfxHandleFire] = disappearanceTime;
            activeParticleHandles.Add(ptfxHandleLava);
            activeParticleHandles.Add(ptfxHandleFire);

            // Inject hot orange dynamic lighting matching the step location
            Function.Call(Hash.DRAW_LIGHT_WITH_RANGE,
                blastTarget.X, blastTarget.Y, blastTarget.Z,
                255, 65, 0, 15.0f, 45.0f
            );

            // PHYSICS KINETIC SHOCKWAVE: Explosion Type 0 (GRENADE EXPLOSION)
            Function.Call(Hash.ADD_EXPLOSION,
                blastTarget.X, blastTarget.Y, blastTarget.Z,
                0,     // Grenade type
                4.5f,  // Force damage radius
                true,  // Play sound
                false, // Render original texture (False, using our magma)
                1.2f,  // Camera shake intensity
                false
            );

            ApplyLavaDecal(blastTarget);
        }

        private void ApplyLavaDecal(Vector3 targetPos)
        {
            float groundZ = targetPos.Z;
            OutputArgument outZ = new OutputArgument();
            if (Function.Call<bool>(Hash.GET_GROUND_Z_FOR_3D_COORD, targetPos.X, targetPos.Y, targetPos.Z, outZ, false))
            {
                groundZ = outZ.GetResult<float>();
            }

            int decalId = Function.Call<int>(Hash.ADD_DECAL,
                1110, targetPos.X, targetPos.Y, groundZ + 0.02f,
                0.0f, 0.0f, -1.0f, 0.0f, 1.0f, 0.0f,
                2.6f, 2.6f, 1.0f, 0.15f, 0.0f, 1.0f, 15.0f,
                false, false, false
            );

            activeDecals.Add(decalId);
        }

        private void LoadPtfxAsset(string assetName)
        {
            if (!Function.Call<bool>(Hash.HAS_NAMED_PTFX_ASSET_LOADED, assetName))
            {
                Function.Call(Hash.REQUEST_NAMED_PTFX_ASSET, assetName);
                int safetyCounter = 0;
                while (!Function.Call<bool>(Hash.HAS_NAMED_PTFX_ASSET_LOADED, assetName) && safetyCounter < 1000)
                {
                    Script.Wait(10);
                    safetyCounter++;
                }
            }
        }

        public void ResetCharacterState(Ped player)
        {
            blastStepIndex = -1;

            if (player == null) return;
            player.CanRagdoll = true;
            Function.Call(Hash.SET_PED_CONFIG_FLAG, player.Handle, 37, false);

            foreach (int handle in activeParticleHandles)
            {
                Function.Call(Hash.REMOVE_PARTICLE_FX, handle, false);
            }
            activeParticleHandles.Clear();
            particleExpirationTicks.Clear();

            foreach (int decal in activeDecals)
            {
                Function.Call(Hash.REMOVE_DECAL, decal);
            }
            activeDecals.Clear();

            Function.Call(Hash.REMOVE_NAMED_PTFX_ASSET, PtfxLibrary);
        }
    }
}
