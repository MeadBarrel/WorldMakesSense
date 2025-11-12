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


    [DefOf]
    public class WorldMakesSenseDefOf
    {
        public static LetterDef success_letter;
    }

    public class MyMapComponent : MapComponent
    {
        public MyMapComponent(Map map) : base(map) { }
        public override void FinalizeInit()
        {
            Messages.Message("Success", null, MessageTypeDefOf.PositiveEvent);
            Find.LetterStack.ReceiveLetter(new TaggedString("Success"), new TaggedString("Success message"), WorldMakesSenseDefOf.success_letter, "", 0);
        }
    }

    [StaticConstructorOnStartup]
    public static class Start
    {
        static Start()
        {
            Log.Message("WorldMakesSense loaded successfully!");
            Harmony harmony = new Harmony("username.worldmakessense");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            // Ensure WorldLosses component exists so deterioration starts ticking
        }
    }

    // *Uncomment for Harmony*
    // [HarmonyPatch(typeof(LetterStack), "ReceiveLetter")]
    // [HarmonyPatch(new Type[] {typeof(TaggedString), typeof(TaggedString), typeof(LetterDef), typeof(string), typeof(int), typeof(bool)})]
    public static class LetterTextChange
    {
        public static bool Prefix(ref TaggedString text)
        {
            text += new TaggedString(" with harmony");
            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), "SetDead")]
    public static class Patch_SetDead_IncrementFactionLosses
    {
        public static void Postfix(Pawn_HealthTracker __instance, Pawn ___pawn)
        {
            var pawn = ___pawn;
            var map = pawn.Map;
            var faction = ___pawn?.Faction;
            if (faction == null || faction.IsPlayer) return;

            float amount = WorldLosses.GetDeathLoss(pawn);
            var lm = WorldMakesSenseMod.Settings != null ? Mathf.Max(0f, WorldMakesSenseMod.Settings.lossMultiplier) : 1f;
            amount *= lm;
            if (amount > 0f)
            {
                WorldLosses.Current.AddLoss(faction, amount);
                if (WorldMakesSenseMod.Settings?.debugLogging == true)
                {
                    Log.Message($"[WorldMakesSense] Added {amount:0} losses (x{lm:0.00}) to faction {faction?.Name ?? "<null>"}");
                }
            }
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

            // Calculate probability of success considering distance
            float pDistance = Helpers.GetDistanceProbability(faction, parms.target.Tile, out var distance);

            // Tech level difference
            var fTechLevel = faction?.def?.techLevel ?? TechLevel.Undefined;
            var pTech = Helpers.GetTechLevelProbability(faction, Faction.OfPlayer);

            float p = pLosses * pDistance * pTech;

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

            if (roll < p)
                return true;

            __result = true;
            return false;
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

            if (roll < p)
                return true;

            __result = true;
            return false;
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

