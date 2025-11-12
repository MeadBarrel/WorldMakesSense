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
        public static int CountHostileFactions(Faction faction, bool count_hidden = false)
        {
            return Find.FactionManager?.AllFactionsListForReading?.Count(
                f => f != null
                && f != faction
                && (!f.Hidden || count_hidden)
                && f.HostileTo(faction)
            ) ?? 0;
        }
        public static int CountAlliedFactions(Faction faction, bool count_hidden = false)
        {
            return Find.FactionManager?.AllFactionsListForReading?.Count(
                f => f != null
                && f != faction
                && (!f.Hidden || count_hidden)
                && f.GoodwillWith(faction) >= 75
            ) ?? 0;
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
        
        public static float GetTechLevelProbability(Faction incidentFaction, Faction targetFaction)
        {
            var fTechLevel = incidentFaction?.def?.techLevel ?? TechLevel.Undefined;
            if (fTechLevel == TechLevel.Undefined) return 1f;
            var tTechLevel = targetFaction?.def?.techLevel ?? TechLevel.Undefined;
            if (tTechLevel == TechLevel.Undefined) return 1f;
            return 1f / (1f + Math.Max(0, (int)fTechLevel - (int)tTechLevel));
                        
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

        public static float CalculateLossProbability(float points, float losses)
        {
            if (points <= 0f) return 1f;
            var p = (points - losses) / points;
            if (p < 0f) p = 0f;
            if (p > 1f) p = 1f;
            if (WorldMakesSenseMod.Settings?.debugLogging == true)
                Log.Message($"[WorldMakesSense] loss probability: {p:0.000}");
            return p;
        }

        
        public static float GetPerEnemyProbability(Faction faction, float p, out int enemies)
        {
            if (faction == null)
            {
                enemies = 0;
                return 1f;
            }
            enemies = Helpers.CountHostileFactions(faction) - WorldMakesSenseMod.Settings.defaultNumEnemies;
            float perEnemy = WorldMakesSenseMod.Settings.perEnemyMultiplier;
            return 1.0f - (1.0f - p) * (float)Math.Pow(1.0f - perEnemy, enemies);
        }

        public static float GetPerAllyProbability(Faction faction, float p, out int allies)
        {
            if (faction == null)
            {
                allies = 0;
                return 1f;
            }
            allies = Helpers.CountAlliedFactions(faction);
            float perAlly = WorldMakesSenseMod.Settings.perAllyMultiplier;
            return 1.0f - (1.0f - p) * (float)Math.Pow(1.0f - perAlly, allies);
        }

    }
}

