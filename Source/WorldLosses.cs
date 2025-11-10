using System;
using System.Collections.Generic;
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

        public WorldLosses(World world) : base(world) { }

        public override void ExposeData()
        {
            base.ExposeData();

            if (losses == null) losses = new Dictionary<Faction, float>();

            Scribe_Collections.Look(ref losses, "losses", LookMode.Reference, LookMode.Value, ref tmpFactions, ref tmpFloats);

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
