using UnityEngine;
using Verse;

namespace WorldMakesSense
{
    public class WorldMakesSenseSettings : ModSettings
    {
        public float raidRollMultiplier = 1f;
        // Back-compat for code referencing raidPointsMultiplier
        public float raidPointsMultiplier { get => raidRollMultiplier; set => raidRollMultiplier = value; }
        public int maxRaidDistance = 100;
        public int distanceClose = 5;
        public int distanceFar = 50;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref raidRollMultiplier, "raidRollMultiplier", 1f);
            Scribe_Values.Look(ref maxRaidDistance, "maxRaidDistance", 100);
            Scribe_Values.Look(ref distanceClose, "distanceClose", 5);
            Scribe_Values.Look(ref distanceFar, "distanceFar", 50);
        }
    }

    public class WorldMakesSenseMod : Mod
    {
        public static WorldMakesSenseSettings Settings;
        private static string maxRaidDistanceBuffer;

        public WorldMakesSenseMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<WorldMakesSenseSettings>();
            maxRaidDistanceBuffer = Settings?.maxRaidDistance.ToString() ?? "0";
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
            // Integer textbox: Max raid distance (tiles)
            list.Gap(6f);
            var row = list.GetRect(28f);
            float labelWidth = 260f;
            Widgets.Label(new Rect(row.x, row.y, labelWidth, row.height), "Max raid distance (tiles, 0 = unlimited):");
            var fieldRect = new Rect(row.x + labelWidth + 8f, row.y, row.width - labelWidth - 8f, row.height);
            if (maxRaidDistanceBuffer == null) maxRaidDistanceBuffer = Settings.maxRaidDistance.ToString();
            Widgets.TextFieldNumeric<int>(fieldRect, ref Settings.maxRaidDistance, ref maxRaidDistanceBuffer, 0, 100000);

            // Int range slider: distanceClose..distanceFar (tiles)
            list.Gap(6f);
            var rangeLabel = $"Distance range (tiles): {Settings.distanceClose} - {Settings.distanceFar}";
            list.Label(rangeLabel);
            var rangeRect = list.GetRect(28f);
            var range = new IntRange(Settings.distanceClose, Settings.distanceFar);
            Widgets.IntRange(rangeRect, 172936215, ref range, 0, 1000);
            Settings.distanceClose = range.min;
            Settings.distanceFar = range.max;

            list.End();
        }
    }
}
