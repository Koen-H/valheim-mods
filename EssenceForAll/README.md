# EssenceForAll

Essences (**Thunderblood Essence**, **Frostfire Essence**) always drop from the creatures that carry them, and
there is enough for the whole group: the amount is multiplied by the number of players that were near the
creature when it spawned.

- Every essence in a creature's drop list drops 100% of the time.
- Two players nearby when it spawned? Twice the essence. Three players, three times, and so on.
- The player count is saved on the creature, so it still counts if someone leaves or logs out before the kill.
- Creatures that spawned before you installed the mod use the players nearby when they die.
- Starred creatures keep the game's own extra drops for stars, which are multiplied too.
- Want to know which creatures drop essence? The BepInEx log lists them every time the game starts
  (`... drops OrbThunderBlood 1-1 at 10%`).

## Multiplayer

Loot is rolled by the game that "owns" the creature, which is usually the player closest to it. Everyone in
your group should install the mod, or the drops are only boosted when a player with the mod owns the creature.
It works on vanilla servers; nothing needs to be installed on the server.

## Config (`com.koendertjh.essenceforall.cfg`)

| Setting | Default | Description |
| --- | --- | --- |
| `Enabled` | `true` | Master switch. |
| `EssenceItems` | `OrbThunderBlood,OrbFrostFire` | Item prefab names that always drop and are multiplied. |
| `MultiplyByPlayers` | `true` | Multiply the essence amount by the player count. Off = always drops, normal amount. |
| `PlayerRange` | `200` | Players within this many meters of the creature when it spawns are counted. `0` = every player in the world. |
| `LogDrops` | `false` | Log every boosted essence drop (for troubleshooting). |
