# WorldSeed

Want to look your world up on a seed map, or share it with a friend? WorldSeed shows the world seed in game,
also when you play on someone else's server.

- **Big map.** The seed shows below the biome name when you open the big map.
- **`/seed` in chat** shows the seed and copies it to your clipboard.
- **No spoilers?** Set `RequiredBoss` and the seed stays secret (*Seed: ???*) until you have defeated that boss.
  A boss counts as defeated when your character has killed it in any world, or when it has been defeated in the
  world you are playing.
- Client-side only; works on vanilla servers (the server sends every player the seed, the game needs it to build
  the terrain).

## Config (`com.koendertjh.worldseed.cfg`)

| Setting | Default | Description |
| --- | --- | --- |
| `ShowOnMap` | `true` | Show the seed on the big map. |
| `RequiredBoss` | `None` | Keep the seed secret until you have defeated this boss: `None`, `Eikthyr`, `Elder`, `Bonemass`, `Moder`, `Yagluth`, `Queen`, `Fader` or `Kall`. |
