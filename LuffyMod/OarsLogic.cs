using System;
using GTA;
using GTA.Native;
using GTA.Math;
using GTA.UI;

namespace AnimeCharacterMod
{
    public partial class OarsController : Script
    {
        // TODO: Replace with a real giant fist slam (e.g. a heavy short-range punch with
        // a large hit radius and strong knockback, reflecting Oars' size).
        private void ExecuteGiantSlamPlaceholder(Ped playerPed)
        {
            Screen.ShowSubtitle("Oars: Giant Slam (placeholder -- not yet implemented)", 2000);

            Vector3 bonePos = Function.Call<Vector3>(Hash.GET_PED_BONE_COORDS, playerPed.Handle, 28422);
            Function.Call(Hash.REQUEST_NAMED_PTFX_ASSET, "core");
            if (Function.Call<bool>(Hash.HAS_NAMED_PTFX_ASSET_LOADED, "core"))
            {
                Function.Call(Hash.USE_PARTICLE_FX_ASSET, "core");
                Function.Call(Hash.START_PARTICLE_FX_NON_LOOPED_AT_COORD,
                    "ent_dst_dust",
                    bonePos.X, bonePos.Y, bonePos.Z,
                    0f, 0f, 0f, 2.0f, false, false, false);
            }
        }

        // TODO: Replace with a grab-and-throw attack on the nearest ped/vehicle.
        private void ExecuteGrabAndThrowPlaceholder(Ped playerPed)
        {
            Screen.ShowSubtitle("Oars: Grab & Throw (placeholder -- not yet implemented)", 2000);
        }

        // TODO: Replace with a ground-shaking stomp / shockwave, similar in structure to
        // Ace's ExecuteFlamePillar in AceLogic.cs (radial knockback instead of ignition).
        private void ExecuteGroundStompPlaceholder(Ped playerPed)
        {
            Screen.ShowSubtitle("Oars: Ground Stomp (placeholder -- not yet implemented)", 2000);
        }
    }
}