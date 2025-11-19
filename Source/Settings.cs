using System;
using System.Collections.Generic;
using RimWorld;
using Unity.Properties;
using UnityEngine;
using Verse;

namespace WorldMakesSense
{
    public class WorldMakesSenseSettings : ModSettings
    {
        public float raidPointsMinAdjustment = 0.7f;
        public float raidPointsMaxAdjustment = 1.25f;
        public float raidMinProbabilityFromLosses = 0.7f;
        public float raidMinProbabilityFromDistance = 0.0f;

        public int distanceClose = 5;
        public int distanceFar = 50;
        
        public bool debugLogging = false;
        public bool notifyIncidentLetters = false;
        public float lossMultiplier = 0.12f;
        public float onPlayerMapLossMultiplier = 0.25f;

        public float lossDeteriorationPercent = 0.954841614f;
        public float lossDeteriorationDays = 1f;
        // Per-enemy raid probability factor. Negative reduces, positive increases.
        public float probabilityPerRelation = 0.10f;
        
        public float probabilityMultiplierPerTechLevelBelow = 0.95f;
        public float probabilityMultiplierPerTechLevelAbove = 1.05f;
        
        public float raidPointsMultiplierPerTechLevelBelow = 0.9f;
        public float raidPointsMultiplierPerTechLevelAbove = 1.1f;
        public float globalRaidPointsMultiplier = 1f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref raidPointsMinAdjustment, "raidPointsMinAdjustment", 0.7f);
            Scribe_Values.Look(ref raidPointsMaxAdjustment, "raidPointsMaxAdjustment", 1.25f);
            Scribe_Values.Look(ref raidMinProbabilityFromLosses, "raidMinProbabilityFromLosses", 0.7f);
            Scribe_Values.Look(ref raidMinProbabilityFromDistance, "raidMinProbabilityFromDistance", 0.0f);

            Scribe_Values.Look(ref lossMultiplier, "lossMultiplier", 1f);
            Scribe_Values.Look(ref onPlayerMapLossMultiplier, "onPlayerMapLossMultipler", 0.25f);

            Scribe_Values.Look(ref distanceClose, "distanceClose", 5);
            Scribe_Values.Look(ref distanceFar, "distanceFar", 150);
            Scribe_Values.Look(ref debugLogging, "debugLogging", false);
            Scribe_Values.Look(ref lossDeteriorationPercent, "lossDeteriorationPercent", 10f);
            Scribe_Values.Look(ref probabilityPerRelation, "probabilityPerRelation", 0.10f);
            Scribe_Values.Look(ref notifyIncidentLetters, "notifyIncidentLetters", false);
            
            Scribe_Values.Look(ref probabilityMultiplierPerTechLevelBelow, "probabilityMultiplierPerTechLevelBelow", 0.75f);
            Scribe_Values.Look(ref probabilityMultiplierPerTechLevelAbove, "probabilityMultiplierPerTechLevelAbove", 0.75f);
            
            Scribe_Values.Look(ref raidPointsMultiplierPerTechLevelBelow, "raidPointsMultiplierPerTechLevelBelow", 0.8f);
            Scribe_Values.Look(ref raidPointsMultiplierPerTechLevelAbove, "raidPointsMultiplierPerTechLevelAbove", 1.0f);
            Scribe_Values.Look(ref globalRaidPointsMultiplier, "globalRaidPointsMultiplier", 1f);
        }
    }

    public class WorldMakesSenseMod : Mod
    {
        public static WorldMakesSenseSettings Settings;
        public WorldMakesSenseMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<WorldMakesSenseSettings>();
        }

        public override string SettingsCategory()
        {
            return "WorldMakesSense";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var list = new Listing_Standard();
            list.Begin(inRect);
            list.Gap(12f);
            list.GapLine();

            distanceRangeWidget(list);
            lossMultiplierWidget(list);
            onPlayerMapLossMultiplerWidget(list);

            // Raids  ============================
            list.GapLine();
            list.Gap(12f);
            Widgets.Label(list.GetRect(24f), "Raids");
            raidPointsAdjustmentRangeWidget(list);
            raidMinProbabilityFromLossesWidget(list);
            probabilityMultiplierPerTechLevelBelowWidget(list);
            probabilityMultiplierPerTechLevelAboveWidget(list);
            raidPointsMultiplierPerTechLevelBelowWidget(list);
            raidPointsMultiplierPerTechLevelAboveWidget(list);
            globalRaidPointsMultiplierWidget(list);

            list.GapLine();
            lossesHalfLifeWidget(list);


            // Verbose logging toggle
            list.Gap(12f);
            Widgets.CheckboxLabeled(list.GetRect(24f), "Send raid/caravan outcome letters", ref Settings.notifyIncidentLetters);
            Widgets.CheckboxLabeled(list.GetRect(24f), "Verbose debug logging", ref Settings.debugLogging);

            list.End();
        }
        
        protected void lossMultiplierWidget(Listing_Standard list)
        {
            list.Gap(12f);
            var lossLabel = $"Loss multiplier: {Settings.lossMultiplier:0.00}x";
            var lossRect = list.GetRect(24f);
            Settings.lossMultiplier = Widgets.HorizontalSlider(
                lossRect,
                Settings.lossMultiplier,
                0.01f,
                1.0f,
                middleAlignment: false,
                label: lossLabel,
                leftAlignedLabel: "0.01x",
                rightAlignedLabel: "1x",
                roundTo: 0.01f
            );
        }

        protected void onPlayerMapLossMultiplerWidget(Listing_Standard list)
        {
            list.Gap(12f);
            var lossLabel = $"Loss multiplier on player map: {Settings.onPlayerMapLossMultiplier:0.00}x";
            var lossRect = list.GetRect(24f);
            Settings.onPlayerMapLossMultiplier = Widgets.HorizontalSlider(
                lossRect,
                Settings.onPlayerMapLossMultiplier,
                0.01f,
                1.0f,
                middleAlignment: false,
                label: lossLabel,
                leftAlignedLabel: "0.01x",
                rightAlignedLabel: "1x",
                roundTo: 0.01f
            );

        }

        protected void raidPointsAdjustmentRangeWidget(Listing_Standard list)
        {
            list.Gap(12f);
            list.Label($"Raid points multiplier range: {Settings.raidPointsMinAdjustment:0.00} - {Settings.raidPointsMaxAdjustment:0.00}");
            var range = new FloatRange(Settings.raidPointsMinAdjustment, Settings.raidPointsMaxAdjustment);
            Widgets.FloatRange(list.GetRect(28f), 1712548129, ref range, 0.0f, 2.0f);
            Settings.raidPointsMinAdjustment= range.min;
            Settings.raidPointsMaxAdjustment= range.max;

        }

        protected void raidMinProbabilityFromLossesWidget(Listing_Standard list)
        {
            list.Gap(12f);
            Settings.raidMinProbabilityFromLosses = Widgets.HorizontalSlider(
                list.GetRect(24f),
                Settings.raidMinProbabilityFromLosses,
                0.0f,
                1.0f,
                middleAlignment: false,
                label: "Minimal raid probability due to losses",
                leftAlignedLabel: "0",
                rightAlignedLabel: "1",
                roundTo: 0.01f
            );
        }
        protected void raidMinProbabilityFromDistanceWidget(Listing_Standard list)
        {
            list.Gap(12f);
            Settings.raidMinProbabilityFromDistance = Widgets.HorizontalSlider(
                list.GetRect(24f),
                Settings.raidMinProbabilityFromDistance,
                0.0f,
                1.0f,
                middleAlignment: false,
                label: "Minimal raid probability due to distance",
                leftAlignedLabel: "0",
                rightAlignedLabel: "1",
                roundTo: 0.01f
            );
        }

        protected void lossesHalfLifeWidget(Listing_Standard list)
        {
            list.Gap(12f);
            var currentHalfLife = (float)Math.Log(0.5f, Settings.lossDeteriorationPercent);
            currentHalfLife = (float)Math.Floor(currentHalfLife + 0.5f);
            float halfLife = Widgets.HorizontalSlider(
                list.GetRect(24f),
                currentHalfLife,
                1,
                100,
                middleAlignment: false,
                label: $"Losses half-life: {currentHalfLife:0}",
                leftAlignedLabel: "1 day",
                rightAlignedLabel: "100 days",
                roundTo: 1f
            );
            float lossDeteriorationPercent = (float)Math.Pow(0.5, 1 / halfLife);
            Settings.lossDeteriorationPercent = lossDeteriorationPercent;

        }
        
        protected void distanceRangeWidget(Listing_Standard list)
        {
            // Int range slider: distanceClose..distanceFar (tiles)
            list.Gap(12f);
            var rangeLabel = $"Distance range (tiles): {Settings.distanceClose} - {Settings.distanceFar}";
            list.Label(rangeLabel);
            var rangeRect = list.GetRect(28f);
            var range = new IntRange(Settings.distanceClose, Settings.distanceFar);
            Widgets.IntRange(rangeRect, 172936215, ref range, 0, 1000);
            Settings.distanceClose = range.min;
            Settings.distanceFar = range.max;
        }

        protected void probabilityMultiplierPerTechLevelBelowWidget(Listing_Standard list)
        {
            list.Gap(12f);
            Settings.probabilityMultiplierPerTechLevelBelow= Widgets.HorizontalSlider(
                list.GetRect(24f),
                Settings.probabilityMultiplierPerTechLevelBelow,
                0.5f,
                1f,
                middleAlignment: false,
                label: $"Probability adjustment for each tech level below incident faction: {Settings.probabilityMultiplierPerTechLevelBelow:0.00}",
                leftAlignedLabel: "05x",
                rightAlignedLabel: "1x (disable)",
                roundTo: 0.01f
            );
            
        }
        protected void probabilityMultiplierPerTechLevelAboveWidget(Listing_Standard list)
        {
            list.Gap(12f);
            Settings.probabilityMultiplierPerTechLevelAbove = Widgets.HorizontalSlider(
                list.GetRect(24f),
                Settings.probabilityMultiplierPerTechLevelAbove,
                1f,
                1.5f,
                middleAlignment: false,
                label: $"Probability adjustment for each tech level above incident faction: {Settings.probabilityMultiplierPerTechLevelAbove:0.00}",
                leftAlignedLabel: "1x (disable)",
                rightAlignedLabel: "1.5x",
                roundTo: 0.01f
            );
            
        }


        protected void raidPointsMultiplierPerTechLevelBelowWidget(Listing_Standard list)
        {
            list.Gap(12f);
            Settings.raidPointsMultiplierPerTechLevelBelow = Widgets.HorizontalSlider(
                list.GetRect(24f),
                Settings.raidPointsMultiplierPerTechLevelBelow,
                0.5f,
                1f,
                middleAlignment: false,
                label: $"Raid points adjustment for each tech level below incident faction: {Settings.raidPointsMultiplierPerTechLevelBelow:0.00}",
                leftAlignedLabel: "05x",
                rightAlignedLabel: "1x (disable)",
                roundTo: 0.01f
            );
            
        }

        protected void raidPointsMultiplierPerTechLevelAboveWidget(Listing_Standard list)
        {
            list.Gap(12f);
            Settings.raidPointsMultiplierPerTechLevelAbove = Widgets.HorizontalSlider(
                list.GetRect(24f),
                Settings.raidPointsMultiplierPerTechLevelAbove,
                1f,
                1.5f,
                middleAlignment: false,
                label: $"Raid points adjustment for each tech level above incident faction: {Settings.raidPointsMultiplierPerTechLevelAbove:0.00}",
                leftAlignedLabel: "1x (disable)",
                rightAlignedLabel: "1.5x",
                roundTo: 0.01f
            );
            
        }

        protected void globalRaidPointsMultiplierWidget(Listing_Standard list)
        {
            list.Gap(12f);
            Settings.globalRaidPointsMultiplier = Widgets.HorizontalSlider(
                list.GetRect(24f),
                Settings.globalRaidPointsMultiplier,
                0.1f,
                4.00f,
                middleAlignment: false,
                label: $"Global raid points multiplier: {Settings.globalRaidPointsMultiplier:0.00}",
                leftAlignedLabel: "0.1x",
                rightAlignedLabel: "4x",
                roundTo: 0.01f
            );
        }
    }
}
