# AchievementPeek

Going for every achievement? The achievement screen tells you that you are missing something, but not what:
every part you haven't done shows as **??? / ???**. This mod shows you, with as many spoilers as you want.

- **Missing parts show up.** Open an achievement's details and the parts you haven't done show their name and
  your progress in grey: the foods you haven't eaten for *The Gourmet*, the creatures for *The Hunter*, the fish
  for *The Fisherman*, the ways to die for *Are You Alright?*, and real numbers for the grinds
  (`12,345 / 65,973` sailed for *The Captain*). Missing parts are listed first.
- **Only what you have come across.** By default a missing part only shows when you have run into it: you know
  the item, recipe or piece, or you killed the creature or picked up something it drops. The rest is counted in
  one row: *Only Odin knows what else is hidden here (14)*.
- **Secret achievements.** The *Concealed* achievements show their name and description, tinted purple, and can
  be opened. Each boss achievement appears once you have defeated the boss before it, so the next boss is never
  spoiled.
- **Combined achievements** (*Kings and Queens of Valheim*, *Child of Odin*) list the achievements they need.
- Client-side only; works on vanilla servers.

## Achievements in a modded game

Valheim turns achievements off when a mod marks the game as modded (Jotunn and several other mods do).
If you want to earn achievements in a modded game, you need a mod that turns them back on, such as
[AchievementEnabler](https://thunderstore.io/c/valheim/p/MidnightMods/AchievementEnabler/). AchievementPeek
itself doesn't mark the game as modded.

## Config (`com.koendertjh.achievementpeek.cfg`)

| Section | Setting | Default | Description |
| --- | --- | --- | --- |
| General | `Enabled` | `true` | Master switch. |
| Spoilers | `RequiredBoss` | `None` | The mod stays off until you have defeated this boss: `None`, `Eikthyr`, `Elder`, `Bonemass`, `Moder`, `Yagluth`, `Queen`, `Fader` or `Kall`. |
| Spoilers | `MissingRows` | `Discovered` | Missing parts of an achievement: `Off` (vanilla ???), `Discovered` (only things you have come across) or `All`. |
| Spoilers | `SecretAchievements` | `Progressive` | `Hidden` (vanilla), `Progressive` (boss achievements one boss at a time, Ashlands secrets after the Queen) or `All`. |
| Display | `MissingFirst` | `true` | List the parts you haven't done above the ones you have. |

A boss counts as defeated when your character has killed it in any world, or when it has been defeated in the
world you are playing (so a friend's kill counts too).
