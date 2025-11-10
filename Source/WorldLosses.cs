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

        public float GetLosses(Faction f)
        {
            if (f == null) return 0;
            return losses.TryGetValue(f, out var n) ? n : 0;
        } 

        public static WorldLosses Current => Find.World.GetComponent<WorldLosses>();
    }    
}
