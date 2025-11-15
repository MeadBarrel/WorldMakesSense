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

// *Uncomment for Harmony*
using System.Reflection;
using HarmonyLib;
using Steamworks;
using System.Runtime.InteropServices;
using RimWorld.QuestGen;

namespace WorldMakesSense
{


    [StaticConstructorOnStartup]
    public static class Start
    {
        static Start()
        {
            Log.Message("WorldMakesSense loaded successfully!");
            Harmony harmony = new Harmony("tashka.worldmakessense");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            // Ensure WorldLosses component exists so deterioration starts ticking
        }
    }

    
    [HarmonyPatch(typeof(Settlement), "GetInspectString")]
    public class Settlement_GetInspectString
    {
        public static void Postfix(Settlement __instance, ref string __result)
        {
            var faction = __instance.Faction;
            var playerSettlements = Find.WorldObjects.Settlements.Where(s => s.Map?.IsPlayerHome ?? false);
            foreach (var s in playerSettlements)
            {
                var name = s.Name;
                var p = Helpers.GetTileDistanceProbability(__instance.Tile, s.Tile, out var distance);
                var pFaction = Helpers.GetDistanceProbability(faction, s.Tile, out var fDistance);
                string text = $"distance to {name}: {distance:0.##}/{fDistance:0.##} (Attack probability: {p:0.00}/{pFaction:0.00})";
                __result += $"\n{text}";
            }
        }
    }    
    
    [HarmonyPatch(typeof(FactionManager), "Notify_PawnKilled")]
    public static class Patch_FactionManager_Notify_PawnKilled
    {
        public static void Postfix(Pawn pawn)
        {
            Helpers.OnPawnKilled(pawn);
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_RaidEnemy), "TryExecuteWorker")]
    public static class Patch_RaidEnemy_TryExecuteWorker_Block
    {
        public static bool Prefix(
            IncidentWorker_RaidEnemy __instance,
            IncidentParms parms, ref bool __result)
        {
            object[] args = { parms };
            var mi = AccessTools.Method(typeof(IncidentWorker_RaidEnemy), "ResolveRaidPoints");
            mi.Invoke(__instance, args);
            mi = AccessTools.Method(typeof(IncidentWorker_RaidEnemy), "TryResolveRaidFaction");
            mi.Invoke(__instance, args);

            var faction = parms.faction;
            if (faction == null) return true;
            if (faction.IsPlayer) return true;
            if (parms.quest != null) return true;

            var points = parms.points;
            var losses = WorldLosses.Current.GetLosses(faction) * WorldMakesSenseMod.Settings.raidLossesMultiplier;
            var lossFactor = Helpers.CalculateLossProbability(points, losses);

            // Calculate probability of success considering losses
            var minPLosses = WorldMakesSenseMod.Settings.raidMinProbabilityFromLosses;
            var pLosses = minPLosses + (1 - minPLosses) * lossFactor;

            // Adjust raid points
            var minPointsLosses = WorldMakesSenseMod.Settings.raidPointsMinAdjustment;
            var maxPointsLosses = WorldMakesSenseMod.Settings.raidPointsMaxAdjustment;
            var pointsAdjustment = minPointsLosses + lossFactor * (maxPointsLosses - minPointsLosses);
            parms.points *= pointsAdjustment;
            float adjustedPoints = parms.points;

            // Calculate probability of success considering distance
            float pDistance = Helpers.GetDistanceProbability(faction, parms.target.Tile, out var distance);

            // Tech level difference
            var fTechLevel = faction?.def?.techLevel ?? TechLevel.Undefined;
            var pTech = Helpers.GetTechLevelProbability(faction, Faction.OfPlayer);

            float p = pLosses * pDistance * pTech;
            float pBeforeRelations = p;

            // Calculate probability of success considering number of hostile factions
            p = Helpers.GetPerEnemyProbability(Faction.OfPlayer, p, out var hostileFactions);

            float roll = Rand.Value;
            if (WorldMakesSenseMod.Settings.debugLogging)
            {
                var sb = new StringBuilder();
                if (roll < p)
                    sb.Append($"The raid will proceed. (p={p:0.000})");
                else
                    sb.Append($"The raid cancelled. (p={p:0.000})");
                sb.AppendInNewLine($"Raid points: {points} ({parms.points} adjusted)");
                sb.AppendInNewLine($"Faction losses: {losses}; p = {pLosses:0.00}");
                sb.AppendInNewLine($"Faction distance: {distance ?? 0}; p={pDistance:0.00}");
                sb.AppendInNewLine($"Number of enemies adjusted: {hostileFactions};");
                sb.AppendInNewLine($"Tech level: {fTechLevel}; p={pTech:0.00}");
                Log.Message(sb.ToString());
            }

            bool raidWillProceed = roll < p;

            if (WorldMakesSenseMod.Settings?.notifyIncidentLetters == true)
            {
                TechLevel playerTech = Faction.OfPlayer?.def?.techLevel ?? TechLevel.Undefined;
                string label = raidWillProceed ? "Raid proceeding" : "Raid prevented";
                var letterDef = raidWillProceed ? LetterDefOf.ThreatBig : LetterDefOf.NeutralEvent;
                string body = BuildRaidLetterText(
                    raidWillProceed,
                    faction,
                    roll,
                    p,
                    points,
                    adjustedPoints,
                    pointsAdjustment,
                    losses,
                    pLosses,
                    pDistance,
                    distance,
                    pTech,
                    fTechLevel,
                    playerTech,
                    pBeforeRelations,
                    hostileFactions);
                Helpers.SendIncidentLetter(label, body, parms, faction, letterDef);
            }

            if (raidWillProceed)
                return true;

            __result = true;
            return false;
        }

        private static string BuildRaidLetterText(bool raidWillProceed, Faction faction, float roll, float probability, float basePoints, float adjustedPoints, float pointsAdjustment, float losses, float pLosses, float pDistance, float? distance, float pTech, TechLevel attackerTech, TechLevel defenderTech, float pBeforeHostiles, int hostileFactionDelta)
        {
            var sb = new StringBuilder();
            sb.AppendLine(raidWillProceed
                ? "A raid is going ahead despite the odds."
                : "A raid was called off due to insufficient odds.");
            sb.AppendLine($"Faction: {faction?.Name ?? "Unknown"}");
            sb.AppendLine($"Roll {roll:0.000} vs required {probability:0.000}");
            sb.AppendLine("Breakdown:");
            sb.AppendLine($" - Loss impact: {pLosses:0.00} (losses {losses:0.#}, points {basePoints:0.#})");
            sb.AppendLine($" - Points adjusted: {basePoints:0.#} → {adjustedPoints:0.#} (x{pointsAdjustment:0.00})");
            string distanceText = distance.HasValue ? $"{distance.Value:0.#} tiles" : "unknown distance";
            sb.AppendLine($" - Distance factor: {pDistance:0.00} ({distanceText})");
            sb.AppendLine($" - Tech factor: {pTech:0.00} ({attackerTech} vs {defenderTech})");
            sb.AppendLine($" - Hostile factions delta: {hostileFactionDelta:+#;-#;0} ⇒ {pBeforeHostiles:0.000} → {probability:0.000}");
            return sb.ToString().TrimEnd();
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_TraderCaravanArrival), "TryExecuteWorker")]
    public static class Patch_TraderCaravanArrival
    {
        public static bool Prefix(IncidentWorker_TraderCaravanArrival __instance, ref bool __result, IncidentParms parms)
        {
            object[] args = { parms };
            var mi = AccessTools.Method(typeof(IncidentWorker_TraderCaravanArrival), "TryResolveParms");
            mi.Invoke(__instance, args);

            var faction = parms.faction;
            if (faction == null) return true;
            if (faction.IsPlayer) return true;
            if (parms.quest != null) return true;
            var tile = parms.target.Tile;

            // Calculate probability of success considering distance
            float pDistance = Helpers.GetDistanceProbability(faction, tile, out var distance);
            float pTech = Helpers.GetTechLevelProbability(faction, Faction.OfPlayer);
            float p = pDistance * pTech;
            float pBeforeAllies = p;
            p = Helpers.GetPerAllyProbability(Faction.OfPlayer, p, out var allies);

            float roll = Rand.Value;
            if (WorldMakesSenseMod.Settings.debugLogging)
            {
                var sb = new StringBuilder();
                if (roll < p)
                    sb.Append($"The Caravan will arrive. (p={p:0.000})");
                else
                    sb.Append($"Caravan arrival cancelled. (p={p:0.000})");
                sb.AppendInNewLine($"Faction distance: {distance ?? 0}; p={pDistance:0.00}");
                sb.AppendInNewLine($"Tech level ({faction.def.techLevel}) p={pTech:0.00}");
                Log.Message(sb.ToString());
            }

            bool caravanWillArrive = roll < p;

            if (WorldMakesSenseMod.Settings?.notifyIncidentLetters == true)
            {
                TechLevel playerTech = Faction.OfPlayer?.def?.techLevel ?? TechLevel.Undefined;
                string label = caravanWillArrive ? "Caravan arrival confirmed" : "Caravan canceled";
                var letterDef = caravanWillArrive ? LetterDefOf.PositiveEvent : LetterDefOf.NegativeEvent;
                string body = BuildCaravanLetterText(
                    caravanWillArrive,
                    faction,
                    roll,
                    p,
                    pDistance,
                    distance,
                    pTech,
                    faction.def?.techLevel ?? TechLevel.Undefined,
                    playerTech,
                    pBeforeAllies,
                    allies);
                Helpers.SendIncidentLetter(label, body, parms, faction, letterDef);
            }

            if (caravanWillArrive)
                return true;

            __result = true;
            return false;
        }

        private static string BuildCaravanLetterText(bool caravanWillArrive, Faction faction, float roll, float probability, float pDistance, float? distance, float pTech, TechLevel traderTech, TechLevel playerTech, float pBeforeAllies, int allies)
        {
            var sb = new StringBuilder();
            sb.AppendLine(caravanWillArrive
                ? "A trading caravan decided to make the trip."
                : "A trading caravan declined to travel.");
            sb.AppendLine($"Faction: {faction?.Name ?? "Unknown"}");
            sb.AppendLine($"Roll {roll:0.000} vs required {probability:0.000}");
            sb.AppendLine("Breakdown:");
            string distanceText = distance.HasValue ? $"{distance.Value:0.#} tiles" : "unknown distance";
            sb.AppendLine($" - Distance factor: {pDistance:0.00} ({distanceText})");
            sb.AppendLine($" - Tech factor: {pTech:0.00} ({traderTech} vs {playerTech})");
            sb.AppendLine($" - Ally support: {allies} allies ⇒ {pBeforeAllies:0.000} → {probability:0.000}");
            return sb.ToString().TrimEnd();
        }
    }


    /*
    [HarmonyPatch(typeof(SettlementUtility), "AttackNow")]
    public static class Patch_SettlementUtility_AttackNow
    {
        static void Postfix(Caravan caravan, Settlement settlement) 
        {
            Map map = settlement.Map;
            Faction faction = settlement.Faction;
            var lords = map.lordManager.lords.Where(l => l?.LordJob is LordJob_DefendBase && l?.faction == faction).ToList();
            if (!lords.Any())
            {
                Log.Error("Could not find lord, will continue attack as normal");
            }
            var lord = lords[0];
     
            var pawns = map.mapPawns.AllPawnsSpawned.Where(p => p.GetLord() == lord).ToList();
            foreach (var pawn in pawns) pawn.Destroy(DestroyMode.Vanish);

            PawnGroupKindDef settlementKindDef = PawnGroupKindDefOf.Settlement;
            
    var cell = CellFinderLoose.TryFindCentralCell(map, 7, 10);
    var rect = cell.RectAbout(40, 40);

    MapGenUtility.GeneratePawns(map, rect, faction, lord, settlementKindDef, points: 1000, requiresRoof: true); }
    }
    */

}

