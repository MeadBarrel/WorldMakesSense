using System;
using System.Runtime.InteropServices;
using System.Text;
using LudeonTK;
using RimWorld;
using Verse;
using WorldMakesSense;

namespace YourModNamespace
{
    public static class DebugLosses
    {
        [DebugAction("WorldMakesSens", "Show faction losses", allowedGameStates = AllowedGameStates.Playing)]
        public static void ShowLosses()
        {
            var wl = WorldLosses.Current;
            if (wl == null)
            {
                Log.Warning("[WorldMakesSense] WorldLosses not available");
                return;
            }

            var sb = new StringBuilder();

            foreach (var f in Find.FactionManager.AllFactionsListForReading)
            {
                if (f == null || f.IsPlayer) continue;
                float n = wl.GetLosses(f);
                sb.AppendLine($"{f.Name}: {n:0}");

            }

            Find.WindowStack.Add(new Dialog_MessageBox(
                text: sb.Length > 0 ? sb.ToString() : "No NPC faction losses recorded",
                title: "Faction losses"
            ));
        }
        
        [DebugAction("CancelRaids", "Reset faction losses", allowedGameStates = AllowedGameStates.Playing)]
        public static void ResetLosses()
        {
            var wl = WorldLosses.Current;
            if (wl == null) return;
            wl.losses.Clear();
            Messages.Message("Faction losses cleared.", MessageTypeDefOf.TaskCompletion);
        }        
    }
}