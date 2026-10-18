# SummonTweaks

Summons that stay out of your way, and a Spirit Caller that doesn't waste your casts.

- **Walk through your summons.** Skeletons from the Dead Raiser and spirits from the Spirit Caller no longer
  block or push you. Only the bumping between summons and players is turned off: summons still walk on the
  ground and collide with everything else, attacks and projectiles still hit them, and they still deal damage.
- **The Trollstav troll stays solid**, like before. Other summons can be made solid too (see `SolidSummons`).
- **Smarter Spirit Caller.** The staff summons a random spirit (Bjorn, Boar, Moose or Wolf), and you can only
  have as many of each kind as the staff's quality. Rolling a kind you already have makes the old one vanish.
  With this mod the staff only rolls spirits you can still add. Once every kind is at its limit, the game's
  normal roll is used again.

## Multiplayer

Collision is worked out on each player's own game: install the mod to walk through summons yourself, and it
works for every player's summons. The Spirit Caller fix only needs the mod on the caster's game. It works on
vanilla servers; nothing needs to be installed on the server.

## Config (`com.koendertjh.summontweaks.cfg`)

| Setting | Default | Description |
| --- | --- | --- |
| `WalkThroughSummons` | `true` | Players walk through summons. Attacks and projectiles still hit. |
| `SolidSummons` | `Troll_Summoned` | Creature prefab names that stay solid for players. |
| `SmartSpiritCaller` | `true` | Summoning staffs with several creatures only roll ones you can still add. |
