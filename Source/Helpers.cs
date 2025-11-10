using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using Verse.Noise;
using Verse.Grammar;
using RimWorld;
using RimWorld.Planet;
using RimWorld.Planet;

namespace WorldMakesSense
{
    public class Helpers
    {
        public static float? GetFactionDistanceToTile(Faction faction, PlanetTile tile)
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