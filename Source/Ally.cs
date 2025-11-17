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
    public class AllyProbability
    {
        public static bool calculate(IncidentParms parms)
        {
            var faction = parms.faction;
            var target = Faction.OfPlayer;
            if (faction == null) return true;
            if (faction.IsPlayer) return true;
            if (parms.quest != null) return true;

            // Tech level difference
            var techLevelDifference = (int)faction.def.techLevel - (int)target.def.techLevel;
            // Probability for tech level difference
            var techLevelProbabilityMultiplier = 1f;
            if (techLevelDifference > 0) {
                var mp = Math.Clamp(WorldMakesSenseMod.Settings.probabilityMultiplierPerTechLevelBelow, 0.1f, 1f);
                techLevelProbabilityMultiplier = (float)Math.Pow(mp, Math.Pow(techLevelDifference, 2));
            } else if (techLevelDifference < 0)
            {
                var mp = Math.Max(WorldMakesSenseMod.Settings.probabilityMultiplierPerTechLevelAbove, 1f);
                techLevelProbabilityMultiplier = (float)Math.Pow(mp, Math.Pow(-techLevelDifference, 2));
            }
            
            // Calculate probability of success considering distance
            float distanceProbabilityMultiplier = Helpers.GetDistanceProbability(faction, parms.target.Tile, out var distance);
            float probability = distanceProbabilityMultiplier * techLevelProbabilityMultiplier;
            
            float roll = Rand.Value;
            bool raidWillProceed = roll < probability;

            if (WorldMakesSenseMod.Settings?.notifyIncidentLetters == true)
            {
                TechLevel playerTech = Faction.OfPlayer?.def?.techLevel ?? TechLevel.Undefined;
                string label = raidWillProceed ? "Raid proceeding" : "Raid prevented";
                var letterDef = raidWillProceed ? LetterDefOf.ThreatBig : LetterDefOf.NeutralEvent;
                string body = BuildAllyLetterText(
                        raidWillProceed,
                        faction,
                        roll,
                        probability,
                        techLevelProbabilityMultiplier,
                        distanceProbabilityMultiplier,
                        faction.def.techLevel,
                        Faction.OfPlayer.def.techLevel
                );
                Helpers.SendIncidentLetter(label, body, parms, faction, letterDef);
            }

            if (raidWillProceed)
                return true;

            return false;
            
        }

        private static string BuildAllyLetterText(
            bool raidWillProceed, 
            Faction faction, 
            float roll, 
            float probability, 
            float techLevelProbabilityMultiplier,
            float distanceProbabilityMultiplier,
            TechLevel allyTech,
            TechLevel hostTech
        )
        {
            var sb = new StringBuilder();
            sb.AppendLine(raidWillProceed
                ? "A friendly decided to make the trip."
                : "A friendly declined to travel.");
            
            sb.AppendLine($"Faction: {faction?.Name ?? "Unknown"}");
            sb.AppendLine($"Roll {roll:0.000} vs required {probability:0.000}");
            sb.AppendLine($"Tech level difference: {allyTech} - {hostTech}");
            sb.AppendLine();

            sb.AppendLine("Probability breakdown:");
            sb.AppendLine($" - Distance impact: {distanceProbabilityMultiplier}");
            sb.AppendLine($" - Tech level difference impact: {techLevelProbabilityMultiplier}");
            sb.AppendLine();
            return sb.ToString().TrimEnd();
        }
    }
}