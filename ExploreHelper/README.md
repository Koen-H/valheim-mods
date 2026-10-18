# ExploreHelper

*The Explorer* asks you to travel far in all four directions, *The Pathfinder* the same in a world without a
map. But how far is far, and where exactly? ExploreHelper shows you.

- **Four pins on the map** where each direction counts: more than 10,350 m from the centre of the world, close
  to the edge. A pin is crossed off once you have that direction.
- **A message** when you reach a new direction: *You reached the far north! (The Explorer: 2 / 4)*. This also
  works for The Pathfinder, where you have no map.
- **Mind the compass.** The game counts "far east" on the **left** side of the map and "far west" on the
  **right**. The pins follow the game, so go where the pin is.
- One second past the line is enough.
- Client-side only; the pins are only on your own map and are not saved. Removing a pin brings it back; turn
  them off with `ShowPins`.

Only exploring while achievements can be earned counts, the same as for the achievement itself. Valheim turns
achievements off in a game marked as modded (Jotunn and several other mods do this); install
[ModdedAchievements](https://thunderstore.io/c/valheim/p/koendertjh/ModdedAchievements/) to turn them back on.

## Config (`com.koendertjh.explorehelper.cfg`)

| Setting | Default | Description |
| --- | --- | --- |
| `ShowPins` | `true` | Pins where each direction counts, crossed off once you have it. |
| `ReachedMessage` | `true` | A message when you reach a direction for the first time. |
