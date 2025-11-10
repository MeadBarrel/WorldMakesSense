using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
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

        [DebugAction("WorldMakesSens", "Add losses", allowedGameStates = AllowedGameStates.Playing)]
        public static void AddLosses()
        {
            var wl = WorldLosses.Current;
            if (wl == null)
            {
                Log.Warning("[WorldMakesSense] WorldLosses not available");
                return;
            }

            var factionOptions = new List<DebugMenuOption>();
            foreach (var f in Find.FactionManager.AllFactionsListForReading)
            {
                if (f == null || f.IsPlayer) continue;
                var label = f.Name ?? f.GetUniqueLoadID();
                factionOptions.Add(new DebugMenuOption(label, DebugMenuOptionMode.Action, delegate
                {
                    var amountOptions = new List<DebugMenuOption>();
                    int[] amounts = new[] { 10, 50, 100, 200, 500, 1000 };
                    foreach (var amt in amounts)
                    {
                        amountOptions.Add(new DebugMenuOption($"+{amt}", DebugMenuOptionMode.Action, delegate
                        {
                            wl.AddLoss(f, amt);
                            Messages.Message($"Added {amt} losses to {f.Name}", MessageTypeDefOf.TaskCompletion);
                        }));
                    }
                    Find.WindowStack.Add(new Dialog_DebugOptionListLister(amountOptions));
                }));
            }

            if (factionOptions.Count == 0)
            {
                Messages.Message("No NPC factions available.", MessageTypeDefOf.RejectInput);
                return;
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(factionOptions));
        }

        [DebugAction("WorldMakesSens", "Reset losses (faction)", allowedGameStates = AllowedGameStates.Playing)]
        public static void ResetLossesForFaction()
        {
            var wl = WorldLosses.Current;
            if (wl == null)
            {
                Log.Warning("[WorldMakesSense] WorldLosses not available");
                return;
            }

            var options = new List<DebugMenuOption>();
            foreach (var f in Find.FactionManager.AllFactionsListForReading)
            {
                if (f == null || f.IsPlayer) continue;
                var label = f.Name ?? f.GetUniqueLoadID();
                options.Add(new DebugMenuOption(label, DebugMenuOptionMode.Action, delegate
                {
                    if (wl.losses.Remove(f))
                    {
                        Messages.Message($"Reset losses for {f.Name}", MessageTypeDefOf.TaskCompletion);
                    }
                    else
                    {
                        Messages.Message($"No recorded losses for {f.Name}", MessageTypeDefOf.RejectInput);
                    }
                }));
            }

            if (options.Count == 0)
            {
                Messages.Message("No NPC factions available.", MessageTypeDefOf.RejectInput);
                return;
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
        }

        [DebugAction("WorldMakesSens", "Reset losses (all)", allowedGameStates = AllowedGameStates.Playing)]
        public static void ResetAllLossesAction()
        {
            var wl = WorldLosses.Current;
            if (wl == null) return;
            wl.losses.Clear();
            Messages.Message("All faction losses cleared.", MessageTypeDefOf.TaskCompletion);
        }

        // Removed duplicate reset action under CancelRaids to avoid confusion.

        [DebugAction("WorldMakesSens", "Deteriorate losses now", allowedGameStates = AllowedGameStates.Playing)]
        public static void DeteriorateLossesNow()
        {
            var wl = WorldLosses.Current;
            if (wl == null)
            {
                Log.Warning("[WorldMakesSense] WorldLosses not available");
                return;
            }
            wl.DeteriorateOnce();
            var pct = WorldMakesSenseMod.Settings?.lossDeteriorationPercent ?? 10f;
            Messages.Message($"Deteriorated losses by {pct:0.#}%.", MessageTypeDefOf.TaskCompletion);
        }
    }
}
