using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.Noise;

namespace WorldMakesSense 
{
    public class RaidProbability
    {
        public static bool calculate(IncidentParms parms)
        {
            var faction = parms.faction;
            var target = Faction.OfPlayer;
            var factionTechLevel = faction?.def?.techLevel;
            if (faction != null && faction.IsPlayer) return true;
            if (parms.quest != null) return true;

            var points = parms.points;
            //var lossFactor = Helpers.CalculateLossProbability(points, losses);
            
            
            // Losses
            var losses = WorldLosses.Current.GetLosses(faction);
            var lossFactor = Math.Clamp((points-losses)/points, 0f, 1f);          
            // Calculate probability of success considering losses
            var minPLosses = WorldMakesSenseMod.Settings.raidMinProbabilityFromLosses;
            var lossesProbabilityMultiplier = minPLosses + (1f - minPLosses) * lossFactor;
            // Adjust raid points
            var minPointsLosses = WorldMakesSenseMod.Settings.raidPointsMinAdjustment;
            var maxPointsLosses = WorldMakesSenseMod.Settings.raidPointsMaxAdjustment;
            var lossesPointsMultiplier = minPointsLosses + lossFactor * (maxPointsLosses - minPointsLosses);

            // Tech level difference
            var techLevelProbabilityMultiplier = 1f;
            var techLevelPointsMultiplier = 0.1f;
            

            if (factionTechLevel != null) {
                var techLevelDifference = (int)factionTechLevel - (int)target.def.techLevel;
                // Probability for tech level difference
                if (techLevelDifference > 0) {
                    var mp = Math.Clamp(WorldMakesSenseMod.Settings.probabilityMultiplierPerTechLevelBelow, 0.1f, 1f);
                    techLevelProbabilityMultiplier = (float)Math.Pow(mp, Math.Pow(techLevelDifference, 2));
                } else if (techLevelDifference < 0)
                {
                    var mp = Math.Max(WorldMakesSenseMod.Settings.probabilityMultiplierPerTechLevelAbove, 1f);
                    techLevelProbabilityMultiplier = (float)Math.Pow(mp, Math.Pow(-techLevelDifference, 2));
                }
                // Points adjustment for tech level difference
                if (techLevelDifference > 0)
                {
                    var mp = Math.Clamp(WorldMakesSenseMod.Settings.raidPointsMultiplierPerTechLevelBelow, 0.1f, 1f);
                    techLevelPointsMultiplier = (float)Math.Pow(mp, Math.Pow(techLevelDifference, 2));
                } else if (techLevelDifference < 0)
                {
                    var mp = Math.Max(WorldMakesSenseMod.Settings.raidPointsMultiplierPerTechLevelAbove, 1f);
                    techLevelPointsMultiplier = (float)Math.Pow(mp, Math.Pow(-techLevelDifference, 2));
                }
            }
            
            parms.points *= lossesPointsMultiplier;
            parms.points *= techLevelPointsMultiplier;
            parms.points *= WorldMakesSenseMod.Settings.globalRaidPointsMultiplier;
            float adjustedPoints = parms.points;

            // Calculate probability of success considering distance
            float distanceProbabilityMultiplier = Helpers.GetDistanceProbability(faction, parms.target.Tile, out var distance);
            float probability = lossesProbabilityMultiplier * distanceProbabilityMultiplier * techLevelProbabilityMultiplier;
            
            float roll = Rand.Value;
            bool raidWillProceed = roll < probability;

            if (WorldMakesSenseMod.Settings?.notifyIncidentLetters == true)
            {
                TechLevel playerTech = Faction.OfPlayer?.def?.techLevel ?? TechLevel.Undefined;
                string label = raidWillProceed ? "Raid proceeding" : "Raid prevented";
                var letterDef = raidWillProceed ? LetterDefOf.ThreatBig : LetterDefOf.NeutralEvent;
                string body = BuildRaidLetterText(
                        raidWillProceed,
                        faction,
                        roll,
                        probability,
                        points,
                        parms.points,
                        lossesPointsMultiplier,
                        techLevelPointsMultiplier,
                        lossesProbabilityMultiplier,
                        techLevelProbabilityMultiplier,
                        distanceProbabilityMultiplier,
                        losses,
                        factionTechLevel,
                        Faction.OfPlayer.def.techLevel
                );
                Helpers.SendIncidentLetter(label, body, parms, faction, letterDef);
            }

            if (raidWillProceed)
                return true;

            return false;
            
        }

        private static string BuildRaidLetterText(
            bool raidWillProceed, 
            Faction faction, 
            float roll, 
            float probability, 
            float basePoints, 
            float adjustedPoints, 
            float lossesPointsMultiplier,
            float techLevelPointsMultiplier,
            float lossesProbabilityMultiplier,
            float techLevelProbabilityMultiplier,
            float distanceProbabilityMultiplier,
            float attackerLosses,
            TechLevel? attackerTech,
            TechLevel defenderTech
        )
        {
            var sb = new StringBuilder();
            sb.AppendLine(raidWillProceed
                ? "A raid is going ahead despite the odds."
                : "A raid was called off due to insufficient odds.");
            
            if (faction != null)
                sb.AppendLine($"Faction: {faction?.Name ?? "Unknown"}");
            sb.AppendLine($"Roll {roll:0.000} vs required {probability:0.000}");
            sb.AppendLine($"Attacker losses: {attackerLosses:0.#}");
            sb.AppendLine($"Tech level difference: {attackerTech} - {defenderTech}");
            sb.AppendLine();

            sb.AppendLine("Probability breakdown:");
            sb.AppendLine($" - Distance impact: {distanceProbabilityMultiplier:0.##}");
            sb.AppendLine($" - Loss impact: {lossesProbabilityMultiplier:0.##}");
            if (attackerTech != null)
                sb.AppendLine($" - Tech level difference impact: {techLevelProbabilityMultiplier:0.##}");
            sb.AppendLine();

            sb.AppendLine("Raid points:");
            sb.AppendLine($"Base: {basePoints:0.#}; Adjusted: {adjustedPoints:0.##}");
            sb.AppendLine($" - Loss impact: {lossesPointsMultiplier:0.##}");
            sb.AppendLine($" - Tech level difference impact: {techLevelPointsMultiplier:0.##}");
            sb.AppendLine($"Global multiplier: {WorldMakesSenseMod.Settings.globalRaidPointsMultiplier:0.#}");

            return sb.ToString().TrimEnd();
        }
    }
}