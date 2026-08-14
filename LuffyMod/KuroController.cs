using GTA;
using GTA.Native;
using System;

namespace AnimeCharacterMod
{
    public partial class KuroController : Script
    {
        public KuroController() { Tick += OnTick; }

        private void OnTick(object sender, EventArgs e)
        {
            if (!MainMenuController.IsKuroActive) return;

            // Future Kuro button inputs go here
        }
    }
}
