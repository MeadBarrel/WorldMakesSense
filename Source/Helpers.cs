using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace WorldMakesSense
{
    public static class Helpers
    {
        public static void OnPawnKilled(Pawn pawn)
        {
            var faction = pawn?.Faction;
            if (faction == null || faction.IsPlayer) return;

            float amount = WorldLosses.GetDeathLoss(pawn);
            if (amount > 0f)
            {
                WorldLosses.Current.AddLoss(faction, amount);
                if (WorldMakesSenseMod.Settings?.debugLogging == true)
                {
                    Log.Message($"[WorldMakesSense] Added {amount:0} losses to faction {faction?.Name ?? "<null>"}");
                }
            }
            
        }

        public static float GetTileDistanceProbability(PlanetTile fTile, PlanetTile tile, out float? distance)
        {
            var grid = Find.WorldGrid;
            distance = grid.ApproxDistanceInTiles(fTile, tile);
            if (distance == null) return 1f;
            var distanceFactor = GetDistanceProbabilityRaw(distance.Value);
            var minPDistance = WorldMakesSenseMod.Settings.raidMinProbabilityFromDistance;
            return minPDistance + (1 - minPDistance) * distanceFactor;
        }
        public static float GetDistanceProbability(Faction faction, PlanetTile tile, out float? distance)
        {
            if (faction == null || tile == null)
            {
                distance = null;
                return 1f;
            }
            distance = GetFactionDistanceToTile(faction, tile);
            if (distance == null) return 1f;
            var distanceFactor = GetDistanceProbabilityRaw(distance.Value);
            var minPDistance = WorldMakesSenseMod.Settings.raidMinProbabilityFromDistance;
            return minPDistance + (1 - minPDistance) * distanceFactor;
        }

        public static float? GetFactionDistanceToTile(Faction faction, PlanetTile tile)
        {
            var settlements = Find.WorldObjects.Settlements
                .Where(s => s.Faction == faction)
                .ToList();
            if (!settlements.Any()) return null;

            var grid = Find.WorldGrid;
            var result = settlements.Min(s => grid.ApproxDistanceInTiles(s.Tile, tile));
            if (WorldMakesSenseMod.Settings?.debugLogging == true)
                Log.Message($"[WorldMakesSense] distance to faction: {result:0.000}");
            return result;
        }

        public static float GetDistanceProbabilityRaw(float distance)
        {
            var settings = WorldMakesSenseMod.Settings;
            var distanceClose = settings.distanceClose;
            var distanceFar = settings.distanceFar;
            var divider = distanceFar - distanceClose;
            if (divider == 0)
                return 1.0f;
            var result = Math.Pow(0.5, (distance - distanceClose)/divider);
            if (result > 1.0f) result = 1.0f;
            if (WorldMakesSenseMod.Settings?.debugLogging == true)
                Log.Message($"[WorldMakesSense] distance probability: {result:0.000}");
            return (float)result;
        }

        public static void SendIncidentLetter(string label, string text, IncidentParms parms, Faction relatedFaction = null, LetterDef letterDef = null)
        {
            if (WorldMakesSenseMod.Settings?.notifyIncidentLetters != true)
            {
                return;
            }

            var lookTargets = ResolveLookTargets(parms?.target);
            var def = letterDef ?? LetterDefOf.NeutralEvent;
            Find.LetterStack.ReceiveLetter(label, text, def, lookTargets, relatedFaction);
        }

        private static LookTargets ResolveLookTargets(IIncidentTarget target)
        {
            if (target == null)
            {
                return LookTargets.Invalid;
            }

            if (target is Map map)
            {
                return new LookTargets(map.Center, map);
            }

            if (target is WorldObject worldObject)
            {
                return new LookTargets(worldObject);
            }

            if (target is Thing thing)
            {
                return new LookTargets(thing);
            }

            return LookTargets.Invalid;
        }

    }
}
