using System;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace WorldMakesSense
{
    public static class Helpers
    {
        public static float? GetFactionDistanceToTile(Faction faction, int tile)
        {
            var settlements = Find.WorldObjects.Settlements
                .Where(s => s.Faction == faction)
                .ToList();
            if (!settlements.Any()) return null;

            var grid = Find.WorldGrid;
            return settlements.Min(s => grid.ApproxDistanceInTiles(s.Tile, tile));
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
            return (float)result;
        }

        public static float CalculateLossProbability(float points, float losses)
        {
            if (points <= 0f) return 1f;
            var p = (points - losses) / points;
            if (p < 0f) p = 0f;
            if (p > 1f) p = 1f;
            return p;
        }

        public static float CalculateRaidProbability(float points, float losses, float distance, float? roll = null, bool? willHappen = null)
        {
            var lossProb = CalculateLossProbability(points, losses);
            var distProb = GetDistanceProbability(distance);
            var combined = Math.Sqrt(lossProb * distProb);
            if (combined < 0f) combined = 0f;
            if (combined > 1f) combined = 1f;

            if (WorldMakesSenseMod.Settings?.debugLogging == true)
            {
                if (roll.HasValue && willHappen.HasValue)
                {
                    Log.Message($"[WorldMakesSense] points={points:0}; p={(float)combined:0.000} (losses={lossProb:0.000}, distance={distProb:0.000}), roll={roll.Value:0.000}; {(willHappen.Value ? "Raid will happen." : "Raid cancelled.")}");
                }
                else
                {
                    Log.Message($"[WorldMakesSense] points={points:0}; p={(float)combined:0.000} (losses={lossProb:0.000}, distance={distProb:0.000})");
                }
            }

            return (float)combined;
        }
    }
}
