using System;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace WorldMakesSense
{
    public static class Helpers
    {
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

        public static float GetDistanceProbability(float distance)
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

        public static bool RollIncidentProbability(IncidentParms parms, float p_hostile, float p_neutral, float p_ally)
        {
            if (parms == null) return true;
            if (parms.quest != null) return true;
            
            var points = parms.points;
            var target = parms.target;

            if (points == 0) return true;

            var faction = parms.faction;
            if (faction.IsPlayer) return true;
            var losses = WorldLosses.Current.GetLosses(faction);

            var distanceToFaction = Helpers.GetFactionDistanceToTile(faction, target.Tile);
            if (distanceToFaction == null) return true;
            if (distanceToFaction <= 0) return true;

            float multiplier = 1f;
            if (WorldMakesSenseMod.Settings != null)
            {
                multiplier = Mathf.Max(0f, WorldMakesSenseMod.Settings.raidRollMultiplier);
            }

            var lossProb = CalculateLossProbability(points, losses);
            var distProb = GetDistanceProbability(distanceToFaction.Value);
            var probability = Math.Sqrt(lossProb * distProb);
            
            // Apply enemy factions factor
            int enemies = 0;
            try { enemies = Find.FactionManager?.AllFactionsListForReading?.Count(f => f != null && !f.IsPlayer && f.HostileTo(Faction.OfPlayer)) ?? 0; } catch { enemies = 0; }
            int neutrals = 0;
            try {
                neutrals = Find.FactionManager?.AllFactionsListForReading?.Count(
                    f => f != null
                    && !f.IsPlayer
                    && f.AllyOrNeutralTo(Faction.OfPlayer)
                ) ?? 0;
            }   catch { neutrals = 0;  }
            int allies = 0;
            try
            {
                allies = Find.FactionManager?.AllFactionsListForReading?.Count(
                    f => f != null
                    && !f.IsPlayer
                    && f.GoodwillWith(Faction.OfPlayer) >= 75
                ) ?? 0;
            }
            catch { allies = 0; }

            float perRelation = WorldMakesSenseMod.Settings?.probabilityPerRelation ?? 0f;
            float relationFactor = 1f + perRelation * (enemies * p_hostile + neutrals * p_neutral + allies * p_ally);

            probability *= relationFactor;
            
            // float perEnemy = WorldMakesSenseMod.Settings?.enemyRaidPerEnemyFactor ?? 0f;
            // float enemyFactor = 1f + enemies * perEnemy;
            //if (enemyFactor < 0f) enemyFactor = 0f;
            //probability *= enemyFactor;
            
            if (probability < 0f) probability = 0f;
            if (probability > 1f) probability = 1f;

            var roll = Rand.Value * multiplier;

            bool willHappen = roll < probability;

            if (WorldMakesSenseMod.Settings?.debugLogging == true)
            {
                Log.Message($"[WorldMakesSense] points={points:0}; p={(float)probability:0.000} (losses={lossProb:0.000}, distance={distProb:0.000}, relations={relationFactor:0.000}, roll={roll:0.000}; {(willHappen ? "Raid will happen." : "Raid cancelled.")}");
            }

            return willHappen;
        }

    }
}

