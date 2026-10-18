# BossReset

A boss fight gone wrong? A minion stuck outside the arena, the boss glitched into a wall, or you just want a
do-over? Reset the fight and get your summoning items back.

- **`/bossreset`** in chat (or `bossreset` in the F5 console) shows what will happen: which boss is removed, how
  many hostile creatures around the arena go with it, and which items you get back. Nothing changes yet.
- **`/bossreset confirm`** does it. The boss and its minions disappear (no drops, no kill), and the items used at
  the altar are put in your inventory, or dropped at your feet if it's full. Summon the boss again whenever you're ready.

Works for every boss with an altar, also bosses that were summoned before you installed the mod.

- Altars that take items from your inventory give back that many of the item.
- Altars with item stands give back the item of every empty stand.
- A boss without an altar nearby (for example one spawned with a cheat command) is removed, but nothing is given back.

Every hostile creature near the arena counts as a minion, so a wild creature that wandered in is removed too.
Turn this off with `RemoveMinions`.

## Multiplayer

Only the player using the command needs the mod; the boss and minions disappear for everyone. Works on vanilla
servers. Any player with the mod can reset a nearby fight, and the items go to that player.

## Config (`com.koendertjh.bossreset.cfg`)

| Setting | Default | Description |
| --- | --- | --- |
| `BossRange` | `150` | How close (meters) a living boss has to be to be found. |
| `RemoveMinions` | `true` | Also remove the hostile creatures around the arena. |
| `MinionRange` | `80` | Hostile creatures within this many meters of the arena are removed with the boss. |
