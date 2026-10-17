# Changelog

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
