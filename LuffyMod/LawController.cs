using GTA;
using GTA.Native;
using System;

namespace AnimeCharacterMod
{
    public partial class LawController : Script
    {
        public LawController() { Tick += OnTick; }

        private void OnTick(object sender, EventArgs e)
        {
            if (!MainMenuController.IsLawActive) return;

            // Future Law button inputs go here
        }
    }
}
