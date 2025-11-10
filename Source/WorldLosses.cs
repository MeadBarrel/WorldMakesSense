using System;
using System.Collections.Generic;
using Mono.Security.X509.Extensions;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace WorldMakesSense
{
    public class WorldLosses : WorldComponent 
    {
        public Dictionary<Faction, float> losses = new Dictionary<Faction, float>();
        // Debug: capture the last loaded raw lists from save
        public List<Faction> tmpFactions;
        public List<float> tmpFloats;
        private int nextDeteriorationTick = -1;

        public WorldLosses(World world) : base(world) { }

        public override void ExposeData()
        {
            base.ExposeData();

            if (losses == null) losses = new Dictionary<Faction, float>();

            Scribe_Collections.Look(ref losses, "losses", LookMode.Reference, LookMode.Value, ref tmpFactions, ref tmpFloats);
            Scribe_Values.Look(ref nextDeteriorationTick, "nextDeteriorationTick", -1);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                var toRemove = new List<Faction>();
                var existingFactions = Find.FactionManager.AllFactionsListForReading;

                foreach (var kv in losses)
                {
                    var f = kv.Key;
                    var v = kv.Value;
                    if (f == null) { toRemove.Add(f); continue; }
                    if (f.IsPlayer) { toRemove.Add(f); continue; }
                    if (!existingFactions.Contains(f)) { toRemove.Add(f); continue; }
                }

                foreach (var f in toRemove) losses.Remove(f);

                tmpFactions = null;
                tmpFloats = null;
            }

        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            TickDeteriorateLosses();
        }
        
        private void TickDeteriorateLosses()
        {
            int now = Find.TickManager.TicksGame;
            int interval = GetDeteriorationIntervalTicks();
            if (interval <= 0) return;

            if (nextDeteriorationTick < 0)
            {
                nextDeteriorationTick = now + interval;
                return;
            }

            if (now >= nextDeteriorationTick)
            {
                DeteriorateOnce();
                // Schedule next
                nextDeteriorationTick = now + interval;
            }
        }
        
        private int GetDeteriorationIntervalTicks()
        {
            var s = WorldMakesSenseMod.Settings;
            float days = s != null ? Math.Max(0f, s.lossDeteriorationDays) : 1f;
            return (int)Math.Max(60f, days * 60000f);
        }

        public void DeteriorateOnce()
        {
            var s = WorldMakesSenseMod.Settings;
            float percent = s != null ? Math.Max(0f, s.lossDeteriorationPercent) : 10f;
            float fraction = Math.Min(1f, percent / 100f);

            if (losses == null || losses.Count == 0) return;
            var keys = new List<Faction>(losses.Keys);
            foreach (var f in keys)
            {
                if (f == null || f.IsPlayer) { losses.Remove(f); continue; }
                float v = losses[f];
                float nv = v * (1f - fraction);
                if (nv <= 0.01f)
                {
                    losses.Remove(f);
                }
                else
                {
                    losses[f] = nv;
                }
            }
            if (WorldMakesSenseMod.Settings?.debugLogging == true)
            {
                Log.Message($"[WorldMakesSense] Deteriorated faction losses by {percent:0.#}%");
            }
        }

        public void AddLoss(Faction f, float amount)
        {
            if (f == null || f.IsPlayer) return;
            losses.TryGetValue(f, out var n);
            losses[f] = n + amount;
        }

        public static float GetDeathLoss(Pawn pawn)
        {
            if (pawn == null) return 0f;
            var f = pawn.Faction;
            if (f == null || f.IsPlayer) return 0f;

            // Sum of pawn skill levels (0-20 each). Pawns without skills contribute 0.
            var tracker = pawn.skills;
            if (tracker == null || tracker.skills == null) return 0f;

            int sum = 0;
            for (int i = 0; i < tracker.skills.Count; i++)
            {
                var sr = tracker.skills[i];
                if (sr != null)
                {
                    sum += sr.Level;
                }
            }
            return sum;
        }

        public float GetLosses(Faction f)
        {
            if (f == null) return 0;
            return losses.TryGetValue(f, out var n) ? n : 0;
        } 

        public static WorldLosses Current => Find.World.GetComponent<WorldLosses>();
    }    
}
