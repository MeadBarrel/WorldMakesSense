using UnityEngine;
using Verse;

namespace WorldMakesSense
{
    public class WorldMakesSenseSettings : ModSettings
    {
        public float raidRollMultiplier = 1f;
        // Back-compat for code referencing raidPointsMultiplier
        public int distanceClose = 5;
        public int distanceFar = 50;
        public bool debugLogging = false;
        public float lossMultiplier = 1f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref raidRollMultiplier, "raidRollMultiplier", 1f);
            Scribe_Values.Look(ref distanceClose, "distanceClose", 5);
            Scribe_Values.Look(ref distanceFar, "distanceFar", 50);
            Scribe_Values.Look(ref debugLogging, "debugLogging", false);
            Scribe_Values.Look(ref lossMultiplier, "lossMultiplier", 1f);
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
            list.Gap(6f);

            // Raid points multiplier slider
            var label = $"Raid roll multiplier: {Settings.raidRollMultiplier:0.00}x";
            var sliderRect = list.GetRect(24f);
            Settings.raidRollMultiplier = Widgets.HorizontalSlider(
                sliderRect,
                Settings.raidRollMultiplier,
                0.10f,
                10.0f,
                middleAlignment: false,
                label: label,
                leftAlignedLabel: "0.1x",
                rightAlignedLabel: "5x",
                roundTo: 0.01f
            );

            // Loss multiplier slider
            list.Gap(6f);
            var lossLabel = $"Loss multiplier: {Settings.lossMultiplier:0.00}x";
            var lossRect = list.GetRect(24f);
            Settings.lossMultiplier = Widgets.HorizontalSlider(
                lossRect,
                Settings.lossMultiplier,
                0.10f,
                10.0f,
                middleAlignment: false,
                label: lossLabel,
                leftAlignedLabel: "0.1x",
                rightAlignedLabel: "10x",
                roundTo: 0.01f
            );

            // Int range slider: distanceClose..distanceFar (tiles)
            list.Gap(6f);
            var rangeLabel = $"Distance range (tiles): {Settings.distanceClose} - {Settings.distanceFar}";
            list.Label(rangeLabel);
            var rangeRect = list.GetRect(28f);
            var range = new IntRange(Settings.distanceClose, Settings.distanceFar);
            Widgets.IntRange(rangeRect, 172936215, ref range, 0, 1000);
            Settings.distanceClose = range.min;
            Settings.distanceFar = range.max;

            // Verbose logging toggle
            list.Gap(6f);
            Widgets.CheckboxLabeled(list.GetRect(24f), "Verbose debug logging", ref Settings.debugLogging);

            list.End();
        }
    }
}
