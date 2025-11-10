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
        public List<Faction> debugLastKeys;
        public List<float> debugLastValues;

        public WorldLosses(World world) : base(world) { }

        public override void ExposeData()
        {
            base.ExposeData();

            if (losses == null) losses = new Dictionary<Faction, float>();

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                debugLastKeys = new List<Faction>(losses.Count);
                debugLastValues = new List<float>(losses.Count);
                foreach (var kv in losses)
                {
                    if (kv.Key != null && !kv.Key.IsPlayer)
                    {
                        debugLastKeys.Add(kv.Key);
                        debugLastValues.Add(kv.Value);
                    }
                }
            }

            Scribe_Collections.Look(ref debugLastKeys, "losses_keys", LookMode.Reference);
            Scribe_Collections.Look(ref debugLastValues, "losses_values", LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                int kc = debugLastKeys?.Count ?? 0;
                int vc = debugLastValues?.Count ?? 0;
                Log.Message($"[WorldMakesSense] PostLoadInit: losses_keys={kc}, losses_values={vc}");
                for (int i = 0; i < kc; i++)
                {
                    var f = debugLastKeys[i];
                    float val = (debugLastValues != null && i < vc) ? debugLastValues[i] : float.NaN;
                    string fid = f != null ? f.GetUniqueLoadID() : "null";
                    string fname = f?.Name ?? "null";
                    Log.Message($"[WorldMakesSense] losses[{i}]: factionId={fid}, name={fname}, value={val}");
                }

                losses = new Dictionary<Faction, float>();

                if (debugLastKeys != null && debugLastValues != null)
                {
                    int count = Math.Min(debugLastKeys.Count, debugLastValues.Count);
                    for (int i = 0; i < count; i++)
                    {
                        var f = debugLastKeys[i];
                        if (f != null && !f.IsPlayer)
                        {
                            losses[f] = debugLastValues[i];
                        }
                    }
                }

                Log.Message($"[WorldMakesSense] PostLoadInit: rebuilt losses count={losses.Count}");
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
