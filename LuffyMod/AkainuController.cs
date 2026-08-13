using Control = GTA.Control;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using GTA.UI;

namespace AnimeCharacterMod
{
    public partial class AkainuController : Script
    {
        public bool isAkainuModeActive = false;
        private DateTime lastPunchTime = DateTime.MinValue;
        private const int PunchCooldownMs = 400;

        private const string PtfxLibrary = "core";
        private const string MudAsset = "exp_grd_mud";
        private const int RightHandBoneId = 62862;
        private readonly List<int> activeDecals = new List<int>();

        public AkainuController()
        {
            Tick += OnTick;
            Aborted += OnAborted;
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (MainMenuController.IsAkainuActive)
            {
                if (!isAkainuModeActive)
                {
                    isAkainuModeActive = true;
                    InitializeCharacterState(Game.Player.Character);
                }
            }
            else
            {
                if (isAkainuModeActive)
                {
                    isAkainuModeActive = false;
                    ResetCharacterState(Game.Player.Character);
                }
                return;
            }

            Ped playerPed = Game.Player.Character;
            if (playerPed == null || !playerPed.IsAlive) return;

            UpdateActiveFrameLoops(playerPed);

            // 6. Input Matrix Listening Phase MATCHING LUFFY EXACTLY
            if (IsControlJustPressed(Control.Attack) || Game.IsKeyPressed(Keys.E))
            {
                TriggerLavaPunchCombo(playerPed);
            }
        }

        private void OnAborted(object sender, EventArgs e)
        {
            ResetCharacterState(Game.Player.Character);
        }

        private bool IsControlPressed(Control control)
        {
            return Function.Call<bool>(
                Hash.IS_DISABLED_CONTROL_PRESSED,
                0,
                (int)control);
        }

        private bool IsControlJustPressed(Control control)
        {
            return Function.Call<bool>(
                Hash.IS_DISABLED_CONTROL_JUST_PRESSED,
                0,
                (int)control);
        }
    }
}
