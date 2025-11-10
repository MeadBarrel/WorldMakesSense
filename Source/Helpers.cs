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
    }
}
