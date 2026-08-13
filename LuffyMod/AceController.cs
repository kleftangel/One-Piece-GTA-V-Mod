using System;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using GTA.Math;
using Control = GTA.Control;

namespace AnimeCharacterMod
{
    public partial class AceController : Script
    {
        private bool isTrailAttacking = false;
        private bool isMassiveWaveAttacking = false;
        private bool isFlamePillarActive = false;

        private int trailTickStart = 0;
        private int waveTickStart = 0;
        private int pillarTickStart = 0;

        private const int FireFistWindUpTicks = 25;

        // Add these variables alongside your active state checks inside AceController.cs:
        private int twisterDurationTimer = 0;
        private int spinDelayTimer = 0;
        private float twisterSpinAngle = 0.0f;

        public AceController()
        {
            Tick += OnTick;
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!MainMenuController.IsAceActive) return;

            // Strict 5-second garbage collector running every tick to keep entity pools clear
            ProcessFireCleanup();

            Ped playerPed = Game.Player.Character;
            if (playerPed == null || playerPed.IsDead) return;

            // RT (Attack) triggers your baseline Molotov Trail
            if (IsControlJustPressed(Control.Attack) && !isTrailAttacking && !isMassiveWaveAttacking && !isFlamePillarActive && playerPed.IsInMeleeCombat)
            {
                isTrailAttacking = true;
                trailTickStart = Game.GameTime;
            }

            // B Button (MeleeAttackLight) triggers the 5-shot rapid explosion punch
            if ((IsControlJustPressed(Control.MeleeAttackLight) || Game.IsKeyPressed(Keys.B)) && !isMassiveWaveAttacking && !isTrailAttacking && !isFlamePillarActive)
            {
                isMassiveWaveAttacking = true;
                waveTickStart = Game.GameTime;
            }

            // DPAD DOWN triggers the Flame Pillar vortex
            if ((IsControlJustPressed(Control.PhoneDown) || Game.IsKeyPressed(Keys.Down)) && !isFlamePillarActive && !isTrailAttacking && !isMassiveWaveAttacking)
            {
                isFlamePillarActive = true;
                pillarTickStart = Game.GameTime;      // FIX: this was declared but never actually set before
                twisterDurationTimer = 220;            // Exact duration match
                spinDelayTimer = 15;                   // Exact physics relaxation delay gateway match
                twisterSpinAngle = playerPed.Heading;
            }

            if (isTrailAttacking)
            {
                if (Game.GameTime - trailTickStart >= (FireFistWindUpTicks * 16))
                {
                    ExecuteMolotovTrail(playerPed);
                    isTrailAttacking = false;
                }
            }

            if (isMassiveWaveAttacking)
            {
                if (Game.GameTime - waveTickStart >= (FireFistWindUpTicks * 16))
                {
                    // Run the rapid-fire explosion execution thread
                    StartRapidExplosionBarrage(playerPed);
                    isMassiveWaveAttacking = false;
                }
            }

            if (isFlamePillarActive)
            {
                // Once the wind-up window has elapsed, this stays true every frame afterward
                // (pillarTickStart never changes), so the pillar now runs continuously every
                // tick -- exactly like Dragon Twister -- instead of firing once and shutting off.
                // ExecuteFlamePillar owns its own lifespan and will set isFlamePillarActive
                // back to false itself once twisterDurationTimer runs out.
                if (Game.GameTime - pillarTickStart >= (FireFistWindUpTicks * 16))
                {
                    ExecuteFlamePillar(playerPed);
                }
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