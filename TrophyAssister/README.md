# TrophyAssister

Client-side trophy helper for Valheim 1.0.

Every kill that doesn't drop a trophy makes the next one more
likely ("pity"). When the trophy drops, the counter for that creature resets.

## What counts as your kill
With `OnlyCountMyKills = true` (default) a kill adds pity when:
- you landed the last hit, or
- it died from burning/poison/other damage over time and you were the last one to hit it, or
- one of your summons killed it: Dead Raiser skeletons, summoned trolls, Staff of the Wild roots,
  or tamed creatures following you.

## Trophies tab
Open your inventory → **Trophies**:
- Each trophy shows **"Next drop: 15% - 2 dry kills"** at the top of its description
  (green ≥ 50%, yellow ≥ 20%, orange below). If several creatures drop the same trophy,
  it shows the best one and names it.
- Trophies you **haven't collected yet** are added in the slot they'll take once collected.
- With `SpoilerFree = true` (default), trophies of creatures you've never killed show a **?**
  icon and **??????** as name. After your first kill they show greyed out with their name and chance.
  Kills from before you installed the mod count too (the game's own kill statistics are used).
- `HiddenTrophies` keeps trophies out of the list; by default `TrophyDeerWhite`
  (unfinished game content without a name in the game's text files).
- Chances are for a normal (0-star) creature and update every time you reopen the tab.

## Files
Settings: `BepInEx\config\com.koendertjh.trophyassister.cfg`. Pity counters:
`BepInEx\config\TrophyAssister_pity.txt`, per character ID (a long number), so renaming a
character keeps its counters and two characters with the same name don't share them.

## How the pity works (defaults)
**Compound** mode: every dry kill multiplies the current chance by 1.10, or by 1.20 for rare
trophies (vanilla chance ≤ 5%). A 10% trophy goes 10% → 11% → 12.1% → 13.3% ...
**Flat** mode: every dry kill adds `FlatBonusPerKill` (0.05 = +5 percentage points).
`GuaranteeAfterKills` adds a hard cap (e.g. 20 = trophy guaranteed by kill 21).
**Starred creatures**: the game doubles the trophy chance per star (1 star = 2×, 2 stars = 4×), on
top of the pity chance. `StarChanceMultiplier` (default 2 = vanilla) sets that per-star multiplier;
3 makes a 1-star 3× and a 2-star 9×. It can't go below vanilla.

The game's own roll is never changed. For chances of 30% or less the game uses its own
anti-bad-luck counter, and that keeps working. When the game's roll misses, the mod rolls the
pity bonus on top, so on average the chance per kill is the "next chance" shown. Such a drop says
"pity roll" in the message. Average kills per trophy (simulated, default settings):

| Vanilla chance | Vanilla | With pity |
|---|---|---|
| 5% | 20 | 7.6 |
| 10% | 10 | 6.5 |
| 20% | 5.1 | 4.1 |
| 50% | 2.0 | 1.9 |

## Commands
Type these in chat with a leading `/` (or in the F5 console without it). They're not cheats.

| Command | What it does |
|---|---|
| `trophypity` | Lists this character's dry-kill counters |
| `trophyreset <creature>` | Resets one creature, by prefab or name (e.g. `trophyreset Wolf`) |
| `trophyreset all` | Resets all counters of this character; first copies the file to `TrophyAssister_pity.bak` |

## Settings
| Section | Setting | Default | What it does |
|---|---|---|---|
| General | Enabled | true | Master switch |
| General | NotifyOnDrop | true | "Trophy dropped!" message |
| General | ShowPityProgress | true | Shows dry kills + next chance after each kill (own line under the top-left messages, so item pickups don't push it away) |
| General | LogKills | false | Logs every trophy-creature kill (for troubleshooting) |
| Base boost | ChanceMultiplier | 1 | Extra multiplier on the base chance (1 = vanilla) |
| Base boost | OnlyUncollected | false | Only trophies you've never picked up |
| Base boost | StarChanceMultiplier | 2 | Chance multiplier per star (2 = vanilla, 2–10) |
| Pity | Enabled | true | Turn the per-kill increase on/off |
| Pity | Mode | Compound | Compound or Flat |
| Pity | GrowthPerKill | 0.10 | Compound: growth per dry kill |
| Pity | RareThreshold | 0.05 | Compound: vanilla chance at or below this counts as rare |
| Pity | RareGrowthPerKill | 0.20 | Compound: growth per dry kill for rare trophies |
| Pity | FlatBonusPerKill | 0.05 | Flat: added per dry kill |
| Pity | GuaranteeAfterKills | 0 | Hard pity (0 = off) |
| Pity | OnlyCountMyKills | true | Only your kills (see above) add pity |
| Trophy list | ShowChances | true | Chance + dry kills on each trophy |
| Trophy list | ShowMissing | true | Add uncollected trophies |
| Trophy list | SpoilerFree | true | Hide never-killed trophies behind "?" |
| Trophy list | HiddenTrophies | TrophyDeerWhite | Trophies never added as missing |

## Client-side notes
- No server install, no RPCs, no version check: works on vanilla servers, and other players
  don't need it.
- Loot is rolled by the game that "owns" the creature. Solo you always own it. In multiplayer,
  a creature owned by a friend's game rolls with normal chances (and won't add pity).
- Valheim 1.0 blocks achievements for characters/worlds flagged for cheating. This mod uses
  no cheat commands and doesn't spawn items out of thin air (a pity drop is added to the
  creature's normal loot), but Iron Gate hasn't said exactly which mods count.
