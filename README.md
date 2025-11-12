# WorldMakesSense

WorldMakesSense is a RimWorld balance mod that makes raids and caravans behave like the factions behind them actually care about losses, distance, and tech parity. NPC factions record every pawn they lose, raid strength is scaled to reflect that attrition, and traders think twice before crossing half the planet for a poor colony. Safe to add to existing saves, as well as to remove mid-playthrough.

---

## Highlights

- **Persistent faction losses** – every non-player pawn death adds a configurable amount of “losses” to its faction; losses decay over time so enemies eventually recover.
- **Sensible raids** – raids check accumulated losses, travel distance, tech level differences before deciding whether to fire. Raid points are automatically scaled inside your chosen range.
- **Logistics-aware caravans** – trader caravans run similar sanity checks (distance and tech gap) and may cancel themselves if the trip looks pointless.

> **Note:** If a raid or caravan cancels itself, the storyteller still considers that incident “spent”; it won’t immediately substitute a different threat. I.e. you won't get a mechanoid cluster just because a tribal raid got cancelled. This means, however, that the game may end up being significantly easier - the mod can only lower raid
frequency, not bump it up. Adjust your storyteller settings accordingly.

---

## Mechanics in Detail

### Faction Loss Ledger
- On pawn death, the mod sums that pawn’s skills (0–20 each) and adds the total to their faction’s losses; deaths on a player home map count at half value.
- A global `Loss multiplier` slider (0.01×–1×, default 0.12×) scales the recorded value for every faction.
- Losses half a configurable half-life; i.e. if half-life is set to 50, and a faction has 1000 losses, then after 50 days they'll be left with 500 losses, and 250 losses another 50 days later.

### Raid Evaluation
Patched `IncidentWorker_RaidEnemy.TryExecuteWorker` now:

1. Resolves raid points/faction normally.
2. Computes the loss factor: stored losses × `Raid losses multiplier` (default 0.2×) vs current raid points.
3. Scales raid points inside `Raid points multiplier range` (default 0.70×–1.25×).
4. Applies distance probability using the nearest settlement distance.
5. Applies a tech-level factor (higher tech attacking lower tech lowers success).
6. Adjusts for number hostiles: the more enemies you have, the more likely a raid is to proceed.

Enable “Verbose debug logging” to see the full calculation in the log whenever the incident fires or cancels.

### Caravan Evaluation
`IncidentWorker_TraderCaravanArrival` receives the same distance + tech + per-ally treatment:

- Distance: same sliders as raids.
- Tech: factions far ahead or behind the player adjust their willingness.
- Allies: each ally increases the chance (default per-ally multiplier 0.20). If the final roll fails, the caravan doesn’t spawn.

---

## Configuration

Open **Options → Mod Settings → WorldMakesSense** to tweak everything live in your save.

| Setting | Default | Description |
| --- | --- | --- |
| Loss multiplier | 0.12× | Scales the recorded loss per pawn death. |
| Raid losses multiplier | 0.20× | Fraction of stored losses that affect raids. |
| Raid points multiplier range | 0.70×–1.25× | Floor/ceiling for raid point adjustments. |
| Minimal raid probability (losses) | 0.70 | Ensures raids keep a baseline chance even with huge losses. |
| Minimal raid probability (distance) | 0.00 | Distance factor floor. |
| Distance close / far | 5 / 50 tiles | Normalization range for distance probability. Any settlement below left margin will have no impact on probabilities. Settlements at the right margin will reduce probabilities by 50% |
| Default number of enemies | 5 | Baseline hostile count. This is how many enemies you're expected to have on average during normal gameplay. |
| Per enemy multiplier | 0.10 | Probability adjustment per hostile faction beyond baseline. |
| Per ally multiplier | 0.20 | Probability bonus per allied faction when evaluating caravans. |
| Losses half-life | ~10 days | Choose 1–100 days; |
| Verbose debug logging | Off | Writes incident decisions and roll breakdowns to the log. |

---

## Debug Tools (Dev Mode)

Enable Dev Mode and open Debug Actions → **WorldMakesSens**:

- *Show faction losses* – list every NPC faction and its stored loss value.
- *Add losses* – pick a faction, add 10/50/100/200/500/1000 for testing.
- *Reset losses (faction)* – clear a single faction entry.
- *Reset losses (all)* – wipe the dictionary.
- *Deteriorate losses now* – run one decay tick instantly.

These tools are safe in existing saves and help verify tuning.

---

## Installation & Load Order

1. Copy this folder to `RimWorld/Mods/WorldMakesSense` or subscribe on Steam.
2. Ensure **Harmony** loads before WorldMakesSense (listed as a dependency in `About.xml`).
3. Load the mod near other storyteller/balance mods if you curate order manually.

Removing the mod mid-save reverts raids/caravans to vanilla logic; stored losses disappear but no permanent map data remains.

