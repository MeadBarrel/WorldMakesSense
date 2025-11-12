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
        public float raidLossesMultiplier = 0.2f;
        public float raidPointsMinAdjustment = 0.7f;
        public float raidPointsMaxAdjustment = 1.25f;
        public float raidMinProbabilityFromLosses = 0.7f;
        public float raidMinProbabilityFromDistance = 0.0f;

        public int defaultNumEnemies = 5;
        public float perEnemyMultiplier = 0.1f;

        public float perAllyMultiplier = 0.2f;
        
        public int distanceClose = 5;
        public int distanceFar = 50;
        
        public bool debugLogging = false;
        public float lossMultiplier = 0.12f;

        public float lossDeteriorationPercent = 0.954841614f;
        public float lossDeteriorationDays = 1f;
        // Per-enemy raid probability factor. Negative reduces, positive increases.
        public float probabilityPerRelation = 0.10f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref raidLossesMultiplier, "incidentPointsMultiplier",1f);
            Scribe_Values.Look(ref raidPointsMinAdjustment, "raidPointsMinAdjustment", 0.7f);
            Scribe_Values.Look(ref raidPointsMaxAdjustment, "raidPointsMaxAdjustment", 1.25f);
            Scribe_Values.Look(ref raidMinProbabilityFromLosses, "raidMinProbabilityFromLosses", 0.7f);
            Scribe_Values.Look(ref raidMinProbabilityFromDistance, "raidMinProbabilityFromDistance", 0.0f);

            Scribe_Values.Look(ref defaultNumEnemies, "defaultNumEnemies", 5);
            Scribe_Values.Look(ref perEnemyMultiplier, "perEnemyMultiplier", 0.1f);
            Scribe_Values.Look(ref perAllyMultiplier, "perAllyMultiplier", 0.2f);
            

            Scribe_Values.Look(ref distanceClose, "distanceClose", 5);
            Scribe_Values.Look(ref distanceFar, "distanceFar", 50);
            Scribe_Values.Look(ref debugLogging, "debugLogging", false);
            Scribe_Values.Look(ref lossMultiplier, "lossMultiplier", 1f);
            Scribe_Values.Look(ref lossDeteriorationPercent, "lossDeteriorationPercent", 10f);
            Scribe_Values.Look(ref probabilityPerRelation, "probabilityPerRelation", 0.10f);
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
            perEnemyMultiplierWidget(list);
            defaultNumEnemiesWidget(list);
            lossMultiplierWidget(list);
            perAllyMultiplierWidget(list);

            // Raids  ============================
            list.GapLine();
            list.Gap(12f);
            Widgets.Label(list.GetRect(24f), "Raids");
            raidLossesMultiplierWidget(list);
            raidPointsAdjustmentRangeWidget(list);
            raidMinProbabilityFromLossesWidget(list);

            list.GapLine();
            lossesHalfLifeWidget(list);


            // Verbose logging toggle
            list.Gap(12f);
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

        protected void raidPointsAdjustmentRangeWidget(Listing_Standard list)
        {
            list.Gap(12f);
            list.Label($"Raid points multiplier range: {Settings.raidPointsMinAdjustment:0.00} - {Settings.raidPointsMaxAdjustment:0.00}");
            var range = new FloatRange(Settings.raidPointsMinAdjustment, Settings.raidPointsMaxAdjustment);
            Widgets.FloatRange(list.GetRect(28f), 1712548129, ref range, 0.0f, 2.0f);
            Settings.raidPointsMinAdjustment= range.min;
            Settings.raidPointsMaxAdjustment= range.max;

        }

        protected void raidLossesMultiplierWidget(Listing_Standard list)
        {
            list.Gap(12f);
            Settings.raidLossesMultiplier = Widgets.HorizontalSlider(
                list.GetRect(24f),
                Settings.raidLossesMultiplier,
                0.1f,
                1.0f,
                middleAlignment: false,
                label: "Raid losses multiplier",
                leftAlignedLabel: "0.1x",
                rightAlignedLabel: "1x",
                roundTo: 0.01f
            );
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

        protected void defaultNumEnemiesWidget(Listing_Standard list)
        {
            list.Gap(12f);
            var row = list.GetRect(28f);
            float labelWidth = 260f;
            Widgets.Label(new Rect(row.x, row.y, labelWidth, row.height), "Number of enemies a faction is expected to have on average");
            var fieldRect = new Rect(row.x + labelWidth + 8f, row.y, row.width - labelWidth - 8f, row.height);
            string buffer = Settings.defaultNumEnemies.ToString();
            Widgets.TextFieldNumeric(fieldRect, ref Settings.defaultNumEnemies, ref buffer, 0f, 1000f);
        }

        protected void perEnemyMultiplierWidget(Listing_Standard list)
        {
            list.Gap(12f);
            Settings.perEnemyMultiplier = Widgets.HorizontalSlider(
                list.GetRect(24f),
                Settings.perEnemyMultiplier,
                0.01f,
                0.25f,
                middleAlignment: false,
                label: $"Per enemy multiplier: {Settings.perEnemyMultiplier:0.00}",
                leftAlignedLabel: "0.01x",
                rightAlignedLabel: "0.25x",
                roundTo: 0.01f
            );
        }
        protected void perAllyMultiplierWidget(Listing_Standard list)
        {
            list.Gap(12f);
            Settings.perAllyMultiplier = Widgets.HorizontalSlider(
                list.GetRect(24f),
                Settings.perAllyMultiplier,
                0.01f,
                0.25f,
                middleAlignment: false,
                label: $"Per enemy multiplier: {Settings.perAllyMultiplier:0.00}",
                leftAlignedLabel: "0.01x",
                rightAlignedLabel: "0.25x",
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
    }
}
