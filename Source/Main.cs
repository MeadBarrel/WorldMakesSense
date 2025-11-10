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

namespace WorldMakesSense
{


    [DefOf]
    public class WorldMakesSenseDefOf
    {
        public static LetterDef success_letter;
    }

    public class MyMapComponent : MapComponent
    {
        public MyMapComponent(Map map) : base(map){}
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
            harmony.PatchAll( Assembly.GetExecutingAssembly() );
            // Ensure WorldLosses component exists so deterioration starts ticking
            var _ = WorldLosses.Current;
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

    [HarmonyPatch(typeof(IncidentWorker_Raid), "TryExecuteWorker")]
    public static class Patch_Raid_TryExecuteWorker_Block
    {
        public static bool Prefix(IncidentParms parms, ref bool __result)
        {
            if (parms == null) return true;
            if (parms.quest != null) return true;

            var points = parms.points;
            var target = parms.target;

            if (points == 0) return true;

            var faction = parms.faction;
            var losses = WorldLosses.Current.GetLosses(faction);

            var distanceToFaction = Helpers.GetFactionDistanceToTile(faction, target.Tile);
            if (distanceToFaction == null) return true;
            if (distanceToFaction <= 0) return true;

            float multiplier = 1f;
            if (WorldMakesSenseMod.Settings != null)
            {
                multiplier = Mathf.Max(0f, WorldMakesSenseMod.Settings.raidRollMultiplier);
            }

            var roll = Rand.Value * multiplier;

            var probability = Helpers.CalculateRaidProbability(points, losses, distanceToFaction.Value);

            if (roll < probability)
            {
                if (WorldMakesSenseMod.Settings?.debugLogging == true)
                {
                    var lossProbDbg = Helpers.CalculateLossProbability(points, losses);
                    var distProbDbg = Helpers.GetDistanceProbability(distanceToFaction.Value);
                    Log.Message($"[WorldMakesSense] points={parms.points}; p={probability:0.000} (losses={lossProbDbg:0.000}, distance={distProbDbg:0.000}), roll={roll:0.000}; Raid will happen.");
                }
                return true;
            }
            if (WorldMakesSenseMod.Settings?.debugLogging == true)
            {
                var lossProbDbg = Helpers.CalculateLossProbability(points, losses);
                var distProbDbg = Helpers.GetDistanceProbability(distanceToFaction.Value);
                Log.Message($"[WorldMakesSense] points={parms.points}; p={probability:0.000} (losses={lossProbDbg:0.000}, distance={distProbDbg:0.000}), roll={roll:0.000}; Raid cancelled.");
            }
            __result = true;
            return false;
        }
    }

}
