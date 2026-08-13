using GTA;
using GTA.Native;
using System;

namespace AnimeCharacterMod
{
    public partial class SanjiController : Script
    {
        public SanjiController() { Tick += OnTick; }

        private void OnTick(object sender, EventArgs e)
        {
            if (!MainMenuController.IsSanjiActive) return;

            // Future Sanji button inputs go here
        }
    }
}
