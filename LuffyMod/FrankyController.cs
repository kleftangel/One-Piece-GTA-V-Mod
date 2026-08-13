using GTA;
using GTA.Native;
using System;

namespace AnimeCharacterMod
{
    public partial class FrankyController : Script
    {
        public FrankyController() { Tick += OnTick; }

        private void OnTick(object sender, EventArgs e)
        {
            if (!MainMenuController.IsFrankyActive) return;

            // Future Franky button inputs go here
        }
    }
}
