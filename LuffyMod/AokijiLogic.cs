using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;
using GTA.Math;

namespace AnimeCharacterMod
{
    public partial class AokijiController : Script
    {
        private static List<Tuple<Ped, Vector3, int>> frozenPedsList = new List<Tuple<Ped, Vector3, int>>();
        private const int IceFreezeLifetimeMs = 5000;
        private static Random randomGenerator = new Random();

        public static void PreloadAokijiAssets()
        {
            Function.Call(Hash.REQUEST_ANIM_DICT, "melee@unarmed@streamed_variations");
        }

        private static void TryFreezeEntity(Ped target)
        {
            if (target == null || !target.IsAlive) return;
            if (frozenPedsList.Exists(x => x.Item1 == target)) return;

            target.Health -= 35;
            World.AddExplosion(target.Position, ExplosionType.Steam, 2.0f, 0.0f);
            target.Task.ClearAll();

            frozenPedsList.Add(new Tuple<Ped, Vector3, int>(target, target.Position, Game.GameTime));
        }

        private static void ProcessIceCleanup()
        {
            int currentTime = Game.GameTime;
            for (int i = frozenPedsList.Count - 1; i >= 0; i--)
            {
                Ped target = frozenPedsList[i].Item1;
                Vector3 anchorPos = frozenPedsList[i].Item2;
                int freezeTime = frozenPedsList[i].Item3;

                if (target == null || !target.Exists())
                {
                    frozenPedsList.RemoveAt(i);
                    continue;
                }

                if (currentTime - freezeTime < IceFreezeLifetimeMs)
                {
                    target.Position = anchorPos;
                    target.Velocity = Vector3.Zero;

                    if (target.TaskSequenceProgress != -1 || target.IsInMeleeCombat)
                    {
                        target.Task.ClearAll();
                    }
                }
                else
                {
                    target.Task.ClearAll();
                    frozenPedsList.RemoveAt(i);
                }
            }
        }

        public void InitializeIceAgeToggle(Ped player)
        {
            player.Task.PlayAnimation("melee@unarmed@streamed_variations", "plyr_unarmed_kick", 8.0f, -8.0f, 2000, AnimationFlags.None, 0.0f);
            Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, "XMAS");
            World.AddExplosion(player.Position, ExplosionType.Steam, 6.0f, 0.0f);
        }

        public void ProcessOngoingIceAgeFrame(Ped player)
        {
            Ped[] nearbyPeds = World.GetNearbyPeds(player.Position, 22.0f);
            foreach (Ped target in nearbyPeds)
            {
                if (target == player) continue;
                TryFreezeEntity(target);
            }

            if (Game.FrameCount % 20 == 0)
            {
                foreach (var icePedTuple in frozenPedsList)
                {
                    Ped targetPed = icePedTuple.Item1;
                    if (targetPed != null && targetPed.IsAlive)
                    {
                        World.AddExplosion(targetPed.Position, ExplosionType.Steam, 1.5f, 0.0f);
                    }
                }
            }
        }

        public void TerminateIceAgeToggle()
        {
            Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, "CLEAR");
        }

        // B BUTTON ACTION: Pure cosmetic straight snowball shotgun blast
        public void ExecutePureSnowballBlast(Ped player)
        {
            Vector3 originPos = player.Position + (player.ForwardVector * 1.2f) + new Vector3(0f, 0f, 0.5f);
            uint snowballHash = (uint)WeaponHash.Snowball;
            Function.Call(Hash.REQUEST_WEAPON_ASSET, snowballHash, 31, 0);

            for (int i = 0; i < 12; i++)
            {
                float spreadX = (float)(randomGenerator.NextDouble() - 0.5f) * 1.5f;
                float spreadZ = (float)(randomGenerator.NextDouble() - 0.5f) * 0.4f;

                Vector3 targetDestination = originPos + (player.ForwardVector * 25.0f) + (player.RightVector * spreadX) + new Vector3(0f, 0f, spreadZ);

                Function.Call(Hash.SHOOT_SINGLE_BULLET_BETWEEN_COORDS,
                    originPos.X, originPos.Y, originPos.Z,
                    targetDestination.X, targetDestination.Y, targetDestination.Z,
                    0, true, snowballHash, player.Handle, true, false, 150.0f);
            }
        }

        // RT BUTTON ACTION: Frost Wave Freeze wave + Multi-Steam Tunnel Visuals
        public void ExecuteIceStrike(Ped player)
        {
            // Puffs expanding outward along the trajectory to create a rushing cloud tunnel
            Vector3 puff1 = player.Position + (player.ForwardVector * 1.5f) + new Vector3(0f, 0f, 0.3f);
            Vector3 puff2 = player.Position + (player.ForwardVector * 3.5f) + new Vector3(0f, 0f, 0.3f);
            Vector3 puff3 = player.Position + (player.ForwardVector * 5.5f) + new Vector3(0f, 0f, 0.3f);

            World.AddExplosion(puff1, ExplosionType.Steam, 2.0f, 0.0f);
            World.AddExplosion(puff2, ExplosionType.Steam, 2.5f, 0.0f);
            World.AddExplosion(puff3, ExplosionType.Steam, 3.0f, 0.0f);

            Ped[] forwardPeds = World.GetNearbyPeds(player.Position, 9.0f);
            foreach (Ped target in forwardPeds)
            {
                if (target == player) continue;

                Vector3 toTarget = (target.Position - player.Position).Normalized;
                float dotProduct = Vector3.Dot(player.ForwardVector, toTarget);

                if (dotProduct > 0.4f)
                {
                    TryFreezeEntity(target);
                    target.Velocity = (toTarget * 12.0f) + new Vector3(0f, 0f, 3.0f);
                }
            }
        }
    }
}
