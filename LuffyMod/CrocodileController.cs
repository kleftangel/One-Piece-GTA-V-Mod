using Control = GTA.Control;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using GTA.Math;

namespace AnimeCharacterMod
{
    public partial class CrocodileController : Script
    {
        // Stacking array collection processing multiple tornadoes concurrently
        private readonly List<Tuple<Vector3, Vector3, int, float>> activeTwisterInstances = new List<Tuple<Vector3, Vector3, int, float>>();

        private bool isSandSlashActive = false;
        private Vector3 sandSlashCurrentPos;
        private Vector3 sandSlashForwardVector;
        private int sandSlashLifetimeTicks = 0;
        private const int SAND_SLASH_MAX_LIFETIME = 95;
        private const float SAND_SLASH_PROJECTILE_SPEED = 45.0f;

        private const int SAND_TWISTER_DURATION_TICKS = 180;
        private const float SAND_TWISTER_SPEED = 3.0f;
        private const float SAND_TWISTER_SPAWN_DISTANCE = 10.0f;

        // NEW: Ground Sand-Blast Sequential Line State Control Variables
        private int groundBlastStepIndex = -1;
        private int nextGroundBlastTime = 0;
        private Vector3 baseGroundBlastOrigin;
        private float lockedGroundBlastHeading = 0f;
        private DateTime lastGroundBlastTime = DateTime.MinValue;
        private const int GroundBlastCooldownMs = 1200; // Battlefield move cooldown floor

        public CrocodileController()
        {
            Tick += OnTick;
            Aborted += (s, e) => { activeTwisterInstances.Clear(); isSandSlashActive = false; groundBlastStepIndex = -1; };
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!MainMenuController.IsCrocodileActive)
            {
                activeTwisterInstances.Clear();
                isSandSlashActive = false;
                groundBlastStepIndex = -1;
                return;
            }

            Function.Call(Hash.REQUEST_NAMED_PTFX_ASSET, "core");

            int playerPedId = Function.Call<int>(Hash.PLAYER_PED_ID);
            Ped playerPed = Game.Player.Character;
            if (playerPedId == 0 || playerPed == null || !playerPed.Exists() || playerPed.IsDead) return;

            // 1. Process active state-machines frame loops
            UpdateActiveFrameLoops(playerPed, playerPedId);

            // 2. Process active projectile logic (Sand Slash Desert Spada)
            if (isSandSlashActive)
            {
                ExecuteSandSlashLogic(playerPed, playerPedId);
            }

            // 3. Multi-Instance Twister Processing Loop
            if (Function.Call<bool>(Hash.HAS_NAMED_PTFX_ASSET_LOADED, "core"))
            {
                for (int i = activeTwisterInstances.Count - 1; i >= 0; i--)
                {
                    var instance = activeTwisterInstances[i];
                    int remainingTicks = instance.Item3 - 1;

                    if (remainingTicks <= 0)
                    {
                        activeTwisterInstances.RemoveAt(i);
                        continue;
                    }

                    Vector3 newPos = instance.Item1 + (instance.Item2 * (SAND_TWISTER_SPEED * Game.LastFrameTime));
                    float newAngle = instance.Item4 + 35.0f;
                    if (newAngle > 360.0f) newAngle -= 360.0f;

                    activeTwisterInstances[i] = new Tuple<Vector3, Vector3, int, float>(newPos, instance.Item2, remainingTicks, newAngle);
                    ExecuteSandTwisterInstanceLogic(playerPed, playerPedId, newPos, newAngle);
                }
            }

            // 4. NEW FIX: RT Input Listener maps to the Ground Sand-Blast Line (Desert Girasole)
            if (IsControlJustPressed(Control.Attack))
            {
                TriggerGroundSandBlastLine(playerPed);
            }

            // 5. B Button Listener -- Desert Spada Muted Triple Wave Combo
            if (IsControlJustPressed(Control.MeleeAttackLight))
            {
                InitializeSandSlash(playerPed, playerPedId);
            }

            // 6. PhoneDown Input Trigger -- Instantiates tornadoes
            if (IsControlJustPressed(Control.PhoneDown))
            {
                TriggerNewSandTwisterInstance(playerPed);
            }

            // 7. PhoneRight Listener -- Triggers Ground Death Dehydration
            if (IsControlJustPressed(Control.PhoneRight))
            {
                TriggerGroundDeathDehydration(playerPed, playerPedId);
            }

            // 8. PhoneLeft Button Listener — Triggers the Ultimate Sables giant sand vortex surrounding Crocodile
            // Continues the heavy vortex loop as long as the button is pinned down
            if (IsControlPressed(Control.PhoneLeft))
            {
                TriggerUltimateSables(playerPed, playerPedId, false);
            }
            // FIXED: The exact frame the player releases the button, trigger a massive outward kinetic fling
            else if (IsControlJustReleased(Control.PhoneLeft))
            {
                TriggerUltimateSables(playerPed, playerPedId, true);
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

        private bool IsControlJustReleased(Control control)
        {
            return Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_RELEASED, 0, (int)control);
        }

    }
}
