using GTA;
using GTA.Native;
using System;

namespace AnimeCharacterMod
{
    public partial class DoflamingoController : Script
    {
        public DoflamingoController() { Tick += OnTick; }

        private void OnTick(object sender, EventArgs e)
        {
            if (!MainMenuController.IsDoflamingoActive) return;

            // Future Doflamingo button inputs go here
        }
    }
}
