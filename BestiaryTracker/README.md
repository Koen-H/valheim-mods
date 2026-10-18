# BestiaryTracker

*The Hunter* asks you to kill every kind of creature, over a hundred of them, and *The Master Hunter* the same
again on hard difficulty. BestiaryTracker shows you, out in the world, which ones you still need.

- **Bestiary window.** A **Bestiary** button next to Trophies in your inventory (a paw print) opens a window
  that looks like the Trophies one, with every creature you need: slain ones in colour with how often you slew
  them, the others greyed out with where they live (*Lives in the Swamp*). The title counts your progress, like
  *The Hunter 46 / 105*. Creatures of a biome only show once you have defeated the boss before it: beat Bonemass
  and the mountain creatures appear; later ones show as **?**. Want to see them all? Turn on `ShowAll`.
- **Name marker.** Creatures you haven't slain yet show *(not slain)* after their name, above their health bar.
  Spot the one Greydwarf shaman in a crowd you still need.
- **New kill message.** Slaying a new kind of creature shows *New creature slain: Troll (The Hunter: 46 / 105)*.
- **Master Hunter.** On hard difficulty or harder, the marker and message follow *The Master Hunter* instead, until
  you have it.
- The list comes straight from the game's achievements, so it matches what the game counts. Tamed creatures
  never get a marker.
- Client-side only; works on vanilla servers.

Kills only count while achievements can be earned, the same as for the achievement itself. Valheim turns
achievements off in a game marked as modded (Jotunn and several other mods do this); install
[ModdedAchievements](https://thunderstore.io/c/valheim/p/koendertjh/ModdedAchievements/) to turn them back on.

## Config (`com.koendertjh.bestiarytracker.cfg`)

| Setting | Default | Description |
| --- | --- | --- |
| `NameMarker` | `true` | A marker after the name of creatures you haven't slain yet. |
| `MarkerText` | `(not slain)` in orange | The marker. Rich text is allowed. |
| `KillMessage` | `true` | A message with your progress when you slay a new kind of creature. |
| `ShowAll` | `false` | Show every creature in the Bestiary right away instead of one boss at a time. |
| `ShowSlain` | `true` | Also list the creatures you have already slain. |
