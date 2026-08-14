using GTA;
using GTA.Native;
using System;

namespace AnimeCharacterMod
{
    public partial class ChopperController : Script
    {
        public ChopperController() { Tick += OnTick; }

        private void OnTick(object sender, EventArgs e)
        {
            if (!MainMenuController.IsChopperActive) return;

            // Future Chopper button inputs go here
        }
    }
}
