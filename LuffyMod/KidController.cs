using GTA;
using GTA.Native;
using System;

namespace AnimeCharacterMod
{
    public partial class KidController : Script
    {
        public KidController() { Tick += OnTick; }

        private void OnTick(object sender, EventArgs e)
        {
            if (!MainMenuController.IsKidActive) return;

            // Future Kid button inputs go here
        }
    }
}
