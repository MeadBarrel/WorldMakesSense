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

            float amount = 10.0f;
            WorldLosses.Current.AddLoss(faction, amount);
            Log.Message($"Added {amount:0} losses to faction");
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_RaidEnemy), "TryExecuteWorker")]
    public static class Patch_Raid_TryExecuteWorker_Block
    {
        public static bool Prefix(IncidentParms parms, ref bool __result)
        {
            if (parms == null) return true;
            if (parms.quest != null) return true;
            if (parms.points == 0) return true;

            var faction = parms.faction;
            var losses = WorldLosses.Current.GetLosses(faction);

            var probability = (parms.points - losses) / parms.points;
            if (probability < 0) probability = 0;

            var roll = Rand.Value;

            if (roll < probability)
            {
                Log.Message($"[WorldMakesSense] points={parms.points:0}; p={probability:0}, roll={roll:0}; Raid will happen.");
                return true;
            }
            Log.Message($"[WorldMakesSense] points={parms.points:0}; p={probability:0}, roll={roll:0}; Raid cancelled;");
            __result = true;
            return false;
        }
    }

}
