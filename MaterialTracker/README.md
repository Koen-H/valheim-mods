# MaterialTracker

Which materials have you found, and which ones are still out there? MaterialTracker adds a **Materials** window
to your inventory, right next to Trophies, and it looks just like it.

- **Every material in the game**: everything a recipe or build piece asks for (trophies excepted, those are in
  the Trophies window).
- **Found** materials show in full colour; **missing** ones are greyed out with *Not found yet*.
- **Where to find it.** Each material says where it comes from: *Dropped by Troll*, *Crafted at the forge*,
  *Made in a smelter from Copper Ore*, *Mined from rocks and deposits*, *Picked up in the wild*, *Sold by
  Haldor*...
- **No spoilers.** Missing materials of a biome only show once you have defeated the boss before it: beat
  Bonemass and the mountain materials appear. Later ones show as **?** with *Only Odin knows what is hidden
  here*. Want to see everything? Turn on `ShowAll`.
- The window title counts your progress: *Materials 85 / 210*.
- Client-side only; works on vanilla servers.

Finding every material is the first step towards *The Artisan* (craft every item);
[CraftTracker](https://thunderstore.io/c/valheim/p/koendertjh/CraftTracker/) shows what you haven't crafted yet.

A boss counts as defeated when your character has killed it in any world, or when it has been defeated in the
world you are playing.

## Config (`com.koendertjh.materialtracker.cfg`)

| Section | Setting | Default | Description |
| --- | --- | --- | --- |
| Spoilers | `ShowAll` | `false` | Show every missing material right away instead of one boss at a time. |
| Display | `ShowFound` | `true` | Also list the materials you have already found. |
| Display | `ShowHints` | `true` | Show where each material comes from. |
