using UnityEngine;
using Verse;

namespace WorldMakesSense
{
    public class WorldMakesSenseSettings : ModSettings
    {
        public float raidPointsMultiplier = 1f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref raidPointsMultiplier, "raidPointsMultiplier", 1f);
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

            // Raid points multiplier slider
            var label = $"Raid points multiplier: {Settings.raidPointsMultiplier:0.00}x";
            var sliderRect = list.GetRect(24f);
            Settings.raidPointsMultiplier = Widgets.HorizontalSlider(
                sliderRect,
                Settings.raidPointsMultiplier,
                0.10f,
                100.0f,
                middleAlignment: false,
                label: label,
                leftAlignedLabel: "0.1x",
                rightAlignedLabel: "5x",
                roundTo: 0.01f
            );

            list.End();
        }
    }
}

