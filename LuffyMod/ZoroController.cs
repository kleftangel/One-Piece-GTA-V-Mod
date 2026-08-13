using System;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using GTA.Math;
using Control = GTA.Control;

namespace AnimeCharacterMod
{
    public partial class ZoroController : Script
    {
        // Global State Control Variables (Leopard Shot)
        private bool isLeopardSpinningShotActive = false;
        private int leopardShotDurationTimer = 0;
        private int spinDelayTimer = 0;
        private float currentSpinAngle = 0.0f;

        // Flying Slash State Control Variables
        private bool isFlyingSlashActive = false;
        private Vector3 slashCurrentPos;
        private Vector3 slashForwardVector;
        private int slashLifetimeTicks = 0;
        private int slashChargeTimer = 0; 

        // Configuration Parameters
        private const int ATTACK_DURATION_TICKS = 100;
        private const int SPIN_DELAY_TICKS = 5;
        private const float PROPULSION_SPEED = 35.0f;
        
        private const int SLASH_MAX_LIFETIME = 45; 
        private const float SLASH_PROJECTILE_SPEED = 45.0f;

        // Dragon Twister State Control Variables
        private bool isDragonTwisterActive = false;
        private int twisterDurationTimer = 0;
        private float twisterSpinAngle = 0.0f;

        // Configuration Parameters
        private const int TWISTER_DURATION_TICKS = 120; // Lasts ~4 seconds at 30 ticks/sec

        // Onigiri (Demon Cutter) State Control Variables
        private bool isOnigiriActive = false;
        private int onigiriTimer = 0;

        // Configuration Parameters
        private const int ONIGIRI_DURATION = 45; // 30 ticks for the stationary arm cross wind-up + 15 ticks for the high-speed slice dash

        private bool isUpwardSlashActive = false;
        private int upwardSlashTimer = 0;
        private int upwardSlashProgressTicks = 0;
        private Vector3 upwardSlashSpawnPos; // Locks the projectile altitude plane on launch
        private const int UPWARD_SLASH_DURATION = 55;

        public ZoroController()
        {
            Tick += OnTick;
        }

        private void OnTick(object sender, EventArgs e)
        {
            // 1. Menu Gatekeeping
            if (!MainMenuController.IsZoroActive)
            {
                isLeopardSpinningShotActive = false;
                isFlyingSlashActive = false;
                slashChargeTimer = 0;
                return;
            }

            // 2. Resolve Active Ped Identifiers
            int playerPedId = Function.Call<int>(Hash.PLAYER_PED_ID);
            Ped playerPed = Game.Player.Character;

            if (playerPedId == 0 || playerPed == null || !playerPed.Exists()) return;

            // 3. Active Leopard Spinning Shot Logic Execution Phase
            if (isLeopardSpinningShotActive)
            {
                ExecuteLeopardSpinningShotLogic(playerPed, playerPedId);
                return; 
            }

            // 4. Delayed Slash Trigger Pipeline (RT Press)
            if (slashChargeTimer > 0)
            {
                slashChargeTimer--;
                if (slashChargeTimer == 0)
                {
                    InitializeFlyingSlash(playerPed, playerPedId);
                }
            }

            // 5. Active Flying Slash Projectile Travel Phase
            if (isFlyingSlashActive)
            {
                ExecuteFlyingSlashLogic(playerPed, playerPedId);
            }

            // 6. Input Matrix Listening Phase MATCHING LUFFY EXACTLY
            // These clean methods now resolve perfectly via the helpers in ZoroLogic
            if (IsControlJustPressed(Control.PhoneRight) || Game.IsKeyPressed(Keys.X))
            {
                TriggerLeopardSpinningShot();
      
            }

            // 7. Melee Attack Overhaul Listening Matrix (Right Trigger / Left Click)
            if (IsControlJustPressed(Control.Attack) && slashChargeTimer == 0)
            {
                slashChargeTimer = 32; 
            }

            // 8. Active Dragon Twister Logic Execution Phase
            if (isDragonTwisterActive)
            {
                ExecuteDragonTwisterLogic(playerPed, playerPedId);
                return; // Suppress other actions while channeling the tornado vortex
            }

            // 9. Input Matrix Listening Phase for Dragon Twister (D-Pad Down / Keyboard C)
            if (IsControlJustPressed(Control.PhoneDown) || Game.IsKeyPressed(Keys.C))
            {
                TriggerDragonTwister();
            }

            // Active Onigiri Logic Execution Phase
            if (isOnigiriActive)
            {
                ExecuteOnigiriLogic(playerPed, playerPedId);
                return; // Prevent user commands from breaking the rush trajectory
            }

            if (isUpwardSlashActive)
            {
                ExecuteUpwardSlashLogic(playerPed, playerPedId);
                return; // Lock frame states completely during active upward channeling
            }

            // Input Matrix Listening Phase for Onigiri (B Button / Keyboard R)
            if (IsControlJustPressed(Control.MeleeAttackLight) || Game.IsKeyPressed(Keys.R))
            {
                TriggerOnigiri();
            }

            if (IsControlJustPressed(Control.PhoneLeft) || Game.IsKeyPressed(Keys.Left))
            {
                TriggerUpwardSlash();
            }
        }

        private void TriggerLeopardSpinningShot()
        {
            isLeopardSpinningShotActive = true;
            leopardShotDurationTimer = ATTACK_DURATION_TICKS;
            spinDelayTimer = SPIN_DELAY_TICKS;
            currentSpinAngle = 0.0f;
            slashChargeTimer = 0; 
        }
        private void TriggerDragonTwister()
        {
            isDragonTwisterActive = true;
            twisterDurationTimer = TWISTER_DURATION_TICKS;
            twisterSpinAngle = 0.0f;
            slashChargeTimer = 0; // Wipe out active slash queues to secure state transitions
        }

        private void TriggerOnigiri()
        {
            isOnigiriActive = true;
            onigiriTimer = ONIGIRI_DURATION;
            slashChargeTimer = 0; // Wipe out active slash queues to secure state transitions
        }

        private void TriggerUpwardSlash()
        {
            isUpwardSlashActive = true;
            upwardSlashTimer = UPWARD_SLASH_DURATION;
            upwardSlashProgressTicks = 0; // Reset travel tracker index
            slashChargeTimer = 0;
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
