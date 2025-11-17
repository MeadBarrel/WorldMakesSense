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
using System.Configuration;

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

            if (RaidProbability.calculate(parms)) return true;

            __result = true;
            return false;
        }
    }
    
    [HarmonyPatch(typeof(IncidentWorker_CrashedShipPart), "TryExecuteWorker")]
    public static class Patch_CrashedShipPart
    {
        public static bool Prefix(ref bool __result, IncidentParms parms)
        {
            parms.faction = Faction.OfMechanoids;
            bool willProceed = RaidProbability.calculate(parms);
            if (willProceed) return true;
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_AnimalInsanityMass), "TryExecuteWorker")]
    public static class Patch_AnimalInsanityMass 
    {
        public static bool Prefix(ref bool __result, IncidentParms parms)
        {
            bool willProceed = RaidProbability.calculate(parms);
            if (willProceed) return true;
            __result = true;
            return false;
        }
    }
    
    [HarmonyPatch(typeof(IncidentWorker_WastepackInfestation), "TryExecuteWorker")]
    public static class Patch_WastepackInfestation
    {
        public static bool Prefix(ref bool __result, IncidentParms parms)
        {
            parms.faction = Faction.OfInsects;
            bool willProceed = RaidProbability.calculate(parms);
            if (willProceed) return true;
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
            
            bool willArrive = AllyProbability.calculate(parms);

            if (willArrive) return true;

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

