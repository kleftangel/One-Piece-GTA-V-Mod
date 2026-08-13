using System;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using GTA.Math;
using Control = GTA.Control;

namespace AnimeCharacterMod
{
    public partial class AokijiController : Script
    {
        // State tracking flags
        private bool isIceAgeActive = false;
        private bool isIceStrikeActive = false;
        private bool isSnowballBlastActive = false;

        // Timing trackers matching your layout
        private int iceAgeTickStart = 0;
        private int iceStrikeTickStart = 0;
        private int snowballTickStart = 0;
        private const int IceWindUpTicks = 25;
        private const int AttackDelayMs = 250;

        public AokijiController()
        {
            PreloadAokijiAssets();
            Tick += OnTick;
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!MainMenuController.IsAokijiActive) return;

            Ped playerPed = Game.Player.Character;
            if (playerPed == null || playerPed.IsDead) return;

            ProcessIceCleanup();

            // DPAD DOWN / Down Arrow triggers "Ice Age" Toggle
            if (IsControlJustPressed(Control.PhoneDown))
            {
                if (!isIceAgeActive)
                {
                    isIceAgeActive = true;
                    InitializeIceAgeToggle(playerPed);
                }
                else
                {
                    isIceAgeActive = false;
                    TerminateIceAgeToggle();
                }
            }

            // RT (Attack) - Frost Wave + Freeze Mechanic + Heavy Steam Visuals
            if (IsControlJustPressed(Control.Attack) && !isIceAgeActive && !isIceStrikeActive && !isSnowballBlastActive)
            {
                isIceStrikeActive = true;
                iceStrikeTickStart = Game.GameTime;
                playerPed.Task.PlayAnimation("melee@unarmed@streamed_variations", "plyr_unarmed_punch", 8.0f, -8.0f, 400, AnimationFlags.None, 0f);
            }

            // FIXED: B BUTTON mapped correctly to MeleeAttackLight (No Freeze, No Steam)
            if (IsControlJustPressed(Control.MeleeAttackLight) && !isIceAgeActive && !isIceStrikeActive && !isSnowballBlastActive)
            {
                isSnowballBlastActive = true;
                snowballTickStart = Game.GameTime;
                playerPed.Task.PlayAnimation("melee@unarmed@streamed_variations", "plyr_unarmed_punch", 8.0f, -8.0f, 400, AnimationFlags.None, 0f);
            }

            // Continuous background ticks
            if (isIceAgeActive)
            {
                ProcessOngoingIceAgeFrame(playerPed);
            }

            // Delayed Execution Gates
            if (isIceStrikeActive && (Game.GameTime - iceStrikeTickStart >= AttackDelayMs))
            {
                ExecuteIceStrike(playerPed);
                isIceStrikeActive = false;
            }

            if (isSnowballBlastActive && (Game.GameTime - snowballTickStart >= AttackDelayMs))
            {
                ExecutePureSnowballBlast(playerPed);
                isSnowballBlastActive = false;
            }
        }

        // RESTORED HELPERS: Safe input reading methods for SHVDN3
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
