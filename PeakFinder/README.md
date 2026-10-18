# PeakFinder

*Top of the World* asks you to build something very high up: a piece at **432 m** (sea level is 30 m). No
mountain is that high, so you build on top of one, and the higher the mountain, the less you have to build.
PeakFinder shows you the highest peaks.

- **"Show high points" on the big map.** A toggle below the "public position" one puts pins on the highest
  peaks of your world: *Peak 401 m (+31 m)*, where the extra is what you still have to build on top.
- **Right for every world.** The peaks come from the world generator, the same heights the terrain is made of.
  They are worked out once when you load the world, which takes a few seconds the first time you turn it on.
- **No spoilers.** By default only peaks in parts of the map you have explored show up. Turn off `OnlyExplored`
  to see them all.
- **Height messages.** Placing your highest piece so far shows *Highest build so far: 401 m (Top of the World
  needs 432 m)*, and your tallest one *Tallest build so far: 31 m above the ground (Mighty Halls needs 49 m)*.
- Client-side only; the pins are only on your own map and are not saved.

Good to know: the piece has to be outdoors and part of your own build (on your own foundation), and it only
counts while achievements can be earned. Valheim turns achievements off in a game marked as modded (Jotunn and
several other mods do this); install [ModdedAchievements](https://thunderstore.io/c/valheim/p/koendertjh/ModdedAchievements/)
to turn them back on.

## Config (`com.koendertjh.peakfinder.cfg`)

| Section | Setting | Default | Description |
| --- | --- | --- | --- |
| Map | `ShowPeaks` | `false` | Show the peaks. Also switched with the toggle on the big map. |
| Map | `PeakCount` | `10` | Number of peaks to show. |
| Map | `MinDistance` | `500` | Peaks are at least this many metres apart. |
| Map | `OnlyExplored` | `true` | Only show peaks in explored parts of the map. |
| Messages | `HeightMessages` | `true` | A message for your highest and tallest piece so far. |
