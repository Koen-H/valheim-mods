# BestiaryTracker

*The Hunter* asks you to kill every kind of creature, over a hundred of them, and *The Master Hunter* the same
again on hard difficulty. BestiaryTracker shows you, out in the world, which ones you still need.

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
Want the full list of creatures you are missing? [AchievementPeek](https://thunderstore.io/c/valheim/p/koendertjh/AchievementPeek/)
shows it in the achievement details.

## Config (`com.koendertjh.bestiarytracker.cfg`)

| Setting | Default | Description |
| --- | --- | --- |
| `NameMarker` | `true` | A marker after the name of creatures you haven't slain yet. |
| `MarkerText` | `(not slain)` in orange | The marker. Rich text is allowed. |
| `KillMessage` | `true` | A message with your progress when you slay a new kind of creature. |
