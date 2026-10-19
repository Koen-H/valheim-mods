# FishTracker

*The Fisherman* wants every kind of fish, and *A Catch Worthy of Njord* a big one. FishTracker shows what you
are missing and gives you a way to bring the fish to you.

- **Fish tooltips** say *"You haven't caught this fish yet."* and show your progress, like *The Fisherman: 5 / 12*.
- **Bait tooltips** list the fish that bite on that bait, with the ones you haven't caught yet in orange.
  Want to find out for yourself? `HideUnknownFish` shows `???` for fish you have never had.
- **Chum.** Every fishing bait can be mashed into chum by hand (5 bait for 1 chum, in the crafting menu
  without a workbench): Fishing Bait becomes Chum, Mossy Fishing Bait becomes Mossy Chum, and so on.
  Face the water and use it (right click it in your inventory, or put it on your hotbar): a few fish that
  take that bait and live in that biome show up in front of you. Then catch them with your rod as usual.
  - Fish you haven't caught yet are more likely to show up.
  - Some come with stars. A *Catch Worthy of Njord* needs a fish of exactly level 4 (3 stars); a 4-star
    fish (level 5) does not count. Chum fish go up to level 4.
  - Wrong biome or not deep enough? You get a message and keep your chum.
- **Fishing indicator.** While your bait is in the water, a word under the crosshair tells you what to do:
  - *Wait for a bite*: nothing is biting yet; pulling only reels in your line.
  - **Pull!** (green): a fish bites. Pull (block) now to hook it, you only have half a second.
  - **Pull** (green): the hooked fish is calm, reel it in.
  - **Hold** (red): the hooked fish is fighting. Pulling now costs twice the stamina for half the line, and
    running out of stamina loses the fish.

Fish only count for the achievements when you catch them with a rod while achievements can be earned. Valheim
turns achievements off in a game marked as modded (Jotunn and several other mods do this); install
[ModdedAchievements](https://thunderstore.io/c/valheim/p/koendertjh/ModdedAchievements/) to turn them back on.

## Multiplayer

Chum fish are normal fish: everyone sees and can catch them. Chum itself is a new item, so only players with
the mod can see it on the ground or in a chest. **If you remove the mod, chum disappears** from inventories
and chests.

## Config (`com.koendertjh.fishtracker.cfg`)

| Section | Setting | Default | Description |
| --- | --- | --- | --- |
| Tooltips | `TooltipHints` | `true` | Fish tooltips show whether you caught it; bait tooltips list the fish that bite. |
| Tooltips | `HideUnknownFish` | `false` | Bait tooltips show `???` for fish you have never had. |
| Chum | `Enabled` | `true` | Add chum. Needs a restart. |
| Chum | `BaitPerChum` | `5` | Bait for one chum. Needs a restart. |
| Chum | `FishPerChum` | `3` | Fish that show up for one chum. |
| Chum | `UncaughtWeight` | `3` | Fish you haven't caught yet are this many times more likely. |
| Chum | `LevelUpChance` | `0.25` | Chance for a chum fish to gain a star, rolled again for each star. |
| Chum | `MaxLevel` | `4` | Highest level of chum fish (4 = 3 stars). |
| Fishing | `Indicator` | `true` | Show under the crosshair when to pull and when to hold while fishing. |
