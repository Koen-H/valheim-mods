# Changelog

## 1.1.1

- Performance: dry kills are saved 30 seconds after a change, on logout and when the game closes, instead of
  writing the file on every kill.
- Performance: the dry-kill message below the top-left messages only redraws when it changes.

## 1.1.0

- Multiplayer: the pity roll now goes to the player who got the kill, also when the creature is owned by
  another player's game (before, only the owner's game rolled pity, so kills often didn't count). The
  owner needs the mod too; it sends the kill to the killer, whose game rolls the pity and drops the trophy.
- `OnlyCountMyKills = false` no longer counts other players' kills for you; they count for them.

## 1.0.2

- Added screenshots to the README.

## 1.0.1

- Updated for Valheim 1.0.20, which raises trophy chances for starred creatures.
- Fixed: a trophy from the pity roll dropped 2 or 4 times from starred creatures instead of once.
- New `StarChanceMultiplier` (Base boost): the trophy chance multiplier per star. 2 = vanilla; raise it
  to make starred creatures drop trophies more often.
- Removed `StarKillBonus` and `ExtraKillsPerStar`: a starred kill now counts as one dry kill, since the
  game itself rewards stars. Use `StarChanceMultiplier` instead.

## 1.0.0

- First public release: pity system, trophy chances and missing trophies in the Trophies tab, `trophypity` and `trophyreset` commands.
