using GTA;
using GTA.Native;
using System;

namespace AnimeCharacterMod
{
    public partial class GarpController : Script
    {
        public GarpController() { Tick += OnTick; }

        private void OnTick(object sender, EventArgs e)
        {
            if (!MainMenuController.IsGarpActive) return;

            // Future Garp button inputs go here
        }
    }
}
