using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;
using GTA.Math;

namespace AnimeCharacterMod
{
    public partial class AokijiController : Script
    {
        // Dictionary tracking: 1. Target Ped object, 2. Anchor position, 3. Expiration timestamp
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

            // 1. Manually apply direct sub-zero health damage instantly
            target.Health -= 35; // Deducts 35 HP on freeze strike

            // 2. Drop a zero-damage white mist burst right at their feet
            World.AddExplosion(target.Position, ExplosionType.Steam, 2.0f, 0.0f);

            // 3. Clear their ongoing AI pathfinding tasks
            target.Task.ClearAll();

            // Record target and anchor them tightly to stop ambient breathing/running on the spot
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

                // Tight coordinate clamp running every tick
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
                    // Thaw out complete: Return them to the engine AI loop
                    target.Task.ClearAll();
                    frozenPedsList.RemoveAt(i);
                }
            }
        }

        // TOGGLE METHOD 1: Fired once right when you turn Ice Age ON
        public void InitializeIceAgeToggle(Ped player)
        {
            player.Task.PlayAnimation("melee@unarmed@streamed_variations", "plyr_unarmed_kick", 8.0f, -8.0f, 2000, AnimationFlags.None, 0.0f);

            // FIX 1: SHVDN3 Nightly Native Call for instant ground snow accumulation and storm weather
            Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, "XMAS");

            // Massive white shockwave blast at Aokiji's feet
            World.AddExplosion(player.Position, ExplosionType.Steam, 6.0f, 0.0f);
        }

        // TOGGLE METHOD 2: Runs indefinitely every tick while active
        public void ProcessOngoingIceAgeFrame(Ped player)
        {
            // Constantly capture pedestrians within an explicit 22-meter radius
            Ped[] nearbyPeds = World.GetNearbyPeds(player.Position, 22.0f);
            foreach (Ped target in nearbyPeds)
            {
                if (target == player) continue;
                TryFreezeEntity(target);
            }

            // Spawn continuous cold venting steam clouds directly on top of the trapped targets
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

        // TOGGLE METHOD 3: Fired once right when you turn Ice Age OFF
        public void TerminateIceAgeToggle()
        {
            // FIX 2: SHVDN3 Nightly Native Call to clear the winter overlay and restore default sunny skies
            Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, "CLEAR");
        }

        // UPDATED RT MOVE: Fires the freezing beam AND creates heavy visible steam puffs bursting away from Aokiji
        public void ExecuteIceStrike(Ped player)
        {
            // VISIBLE STEAM BURST: We chain multiple zero-damage steam explosions moving directly away from Aokiji's palms.
            // This generates a thick, travelling cloud tunnel of ice mist shooting forward visually!
            Vector3 puff1 = player.Position + (player.ForwardVector * 1.5f) + new Vector3(0f, 0f, 0.3f);
            Vector3 puff2 = player.Position + (player.ForwardVector * 3.5f) + new Vector3(0f, 0f, 0.3f);
            Vector3 puff3 = player.Position + (player.ForwardVector * 5.5f) + new Vector3(0f, 0f, 0.3f);

            World.AddExplosion(puff1, ExplosionType.Steam, 2.0f, 0.0f);
            World.AddExplosion(puff2, ExplosionType.Steam, 2.5f, 0.0f);
            World.AddExplosion(puff3, ExplosionType.Steam, 3.0f, 0.0f);

            // Process freezing and knockbacks to peds standing within the frontal cone zone
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

        public void ExecutePureSnowballBlast(Ped player)
        {
            Vector3 originPos = player.Position + (player.ForwardVector * 1.2f) + new Vector3(0f, 0f, 0.5f);
            uint snowballHash = (uint)WeaponHash.Snowball;
            Function.Call(Hash.REQUEST_WEAPON_ASSET, snowballHash, 31, 0);

            // Spray 12 snowballs forward across the map (deals 0 damage, zero environmental status shifts)
            for (int i = 0; i < 12; i++)
            {
                float spreadX = (float)(randomGenerator.NextDouble() - 0.5f) * 4.0f;
                float spreadY = (float)(randomGenerator.NextDouble() - 0.5f) * 4.0f;
                float spreadZ = (float)(randomGenerator.NextDouble() - 0.5f) * 1.5f;

                Vector3 targetDestination = player.Position + (player.ForwardVector * 20.0f) + new Vector3(spreadX, spreadY, spreadZ);

                Function.Call(Hash.SHOOT_SINGLE_BULLET_BETWEEN_COORDS,
                    originPos.X, originPos.Y, originPos.Z,
                    targetDestination.X, targetDestination.Y, targetDestination.Z,
                    0, true, snowballHash, player.Handle, true, false, 100.0f);
            }
        }
    }
}
