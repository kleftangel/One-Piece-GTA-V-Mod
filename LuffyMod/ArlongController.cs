using GTA;
using GTA.Native;
using System;

namespace AnimeCharacterMod
{
    public partial class ArlongController : Script
    {
        public ArlongController() { Tick += OnTick; }

        private void OnTick(object sender, EventArgs e)
        {
            if (!MainMenuController.IsArlongActive) return;

            // Future Arlong button inputs go here
        }
    }
}
