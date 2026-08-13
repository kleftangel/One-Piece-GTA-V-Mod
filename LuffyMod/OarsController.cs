using System;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using Control = GTA.Control;

namespace AnimeCharacterMod
{
    public partial class OarsController : Script
    {
        private bool isSlamAttacking = false;
        private bool isThrowAttacking = false;
        private bool isStompActive = false;

        private int slamTickStart = 0;
        private int throwTickStart = 0;
        private int stompTickStart = 0;

        private const int WindUpTicks = 25;

        public OarsController()
        {
            Tick += OnTick;
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!MainMenuController.IsOarsActive) return;

            Ped playerPed = Game.Player.Character;
            if (playerPed == null || playerPed.IsDead) return;

            // RT (Attack) -- TODO: replace with Oars' giant fist slam
            if (IsControlJustPressed(Control.Attack) && !isSlamAttacking && !isThrowAttacking && !isStompActive && playerPed.IsInMeleeCombat)
            {
                isSlamAttacking = true;
                slamTickStart = Game.GameTime;
            }

            // B Button (MeleeAttackLight) -- TODO: replace with a grab-and-throw attack
            if ((IsControlJustPressed(Control.MeleeAttackLight) || Game.IsKeyPressed(Keys.B)) && !isThrowAttacking && !isSlamAttacking && !isStompActive)
            {
                isThrowAttacking = true;
                throwTickStart = Game.GameTime;
            }

            // DPAD DOWN -- TODO: replace with a ground-shaking stomp / shockwave
            if ((IsControlJustPressed(Control.PhoneDown) || Game.IsKeyPressed(Keys.Down)) && !isStompActive && !isSlamAttacking && !isThrowAttacking)
            {
                isStompActive = true;
                stompTickStart = Game.GameTime;
            }

            if (isSlamAttacking)
            {
                if (Game.GameTime - slamTickStart >= (WindUpTicks * 16))
                {
                    ExecuteGiantSlamPlaceholder(playerPed);
                    isSlamAttacking = false;
                }
            }

            if (isThrowAttacking)
            {
                if (Game.GameTime - throwTickStart >= (WindUpTicks * 16))
                {
                    ExecuteGrabAndThrowPlaceholder(playerPed);
                    isThrowAttacking = false;
                }
            }

            if (isStompActive)
            {
                if (Game.GameTime - stompTickStart >= (WindUpTicks * 16))
                {
                    ExecuteGroundStompPlaceholder(playerPed);
                    isStompActive = false;
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
