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

        public static float? GetIncidentProbability(IncidentParms parms, float p_hostile, float p_neutral, float p_ally, bool use_distance=true, bool use_losses=true)
        {
            if (parms == null) return null;
            if (parms.quest != null) return null;
            
            var points = parms.points;
            var target = parms.target;

            if (points == 0) return null;

            var faction = parms.faction;
            if (faction == null)
            {
                Log.Message("Faction is null");
                return null;
            }
            if (faction.IsPlayer) return null;

            List<float> components = new List<float>();
            if (use_distance)
            {
                var distanceToFaction = Helpers.GetFactionDistanceToTile(faction, target.Tile);
                if (distanceToFaction == null) return null;
                if (distanceToFaction <= 0) return null;
                var distProb = GetDistanceProbability(distanceToFaction.Value);
                components.Add(distProb);
            }
            
            if (use_losses)
            {
                var losses = WorldLosses.Current.GetLosses(faction);
                var lossProb = CalculateLossProbability(points, losses);
                components.Add(lossProb);
            }

            float probability = 1;
            foreach (float v in components) probability *= v;
            probability = (float)Math.Sqrt(probability);

            float multiplier = 1f;
            if (WorldMakesSenseMod.Settings != null)
            {
                multiplier = Mathf.Max(0f, WorldMakesSenseMod.Settings.raidRollMultiplier);
            }

            probability *= multiplier;

            // Apply enemy factions factor
            int enemies = 0;
            try { enemies = Find.FactionManager?.AllFactionsListForReading?.Count(f => f != null && !f.IsPlayer && !f.Hidden && f.HostileTo(Faction.OfPlayer)) ?? 0; } catch { enemies = 0; }
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

            Log.Message($"Probability: {probability};");
            probability *= relationFactor;
            Log.Message($"Probability with relation factor: {probability}; enemies: {enemies}, neutrals: {neutrals}, allies: {allies}");
            
            // float perEnemy = WorldMakesSenseMod.Settings?.enemyRaidPerEnemyFactor ?? 0f;
            // float enemyFactor = 1f + enemies * perEnemy;
            //if (enemyFactor < 0f) enemyFactor = 0f;
            //probability *= enemyFactor;
            
            if (probability < 0f) probability = 0f;
            if (probability > 1f) probability = 1f;

            return probability;

        }

    }
}

