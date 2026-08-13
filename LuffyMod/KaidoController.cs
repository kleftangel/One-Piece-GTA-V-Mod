using GTA;
using GTA.Native;
using System;

namespace AnimeCharacterMod
{
    public partial class KaidoController : Script
    {
        public KaidoController() { Tick += OnTick; }

        private void OnTick(object sender, EventArgs e)
        {
            if (!MainMenuController.IsKaidoActive) return;

            // Future Kaido button inputs go here
        }
    }
}
