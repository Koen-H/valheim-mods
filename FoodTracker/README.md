# FoodTracker

*The Gourmet* asks you to eat every edible item in the game, and *The Master Chef* to cook every dish. Which
ones are you still missing? FoodTracker tells you, right where you see the food.

- **Tooltips.** Food you haven't eaten yet says *"You haven't eaten this yet. I wonder what it tastes like..."*,
  and dishes you have never made say *"You haven't made this yet."* This works everywhere the game shows an item
  tooltip: inventory, chests, the cooking station and the trader.
- **Progress.** Food tooltips also show how far you are, like *The Gourmet: 45 / 97*.
- **Inventory marker.** A small orange diamond on the left of every slot that holds food you haven't eaten yet,
  in your inventory and in chests.
- The lists come straight from the game's achievements, so they always match what the game counts, including
  raw foods like berries and in-between steps like bread dough.
- Client-side only; works on vanilla servers.

Only food eaten while achievements can be earned counts, the same as for the achievement itself. Valheim turns
achievements off in a game marked as modded (Jotunn and several other mods do this); install
[ModdedAchievements](https://thunderstore.io/c/valheim/p/koendertjh/ModdedAchievements/) to turn them back on.

## Config (`com.koendertjh.foodtracker.cfg`)

| Section | Setting | Default | Description |
| --- | --- | --- | --- |
| General | `TooltipHints` | `true` | Tooltips say when you haven't eaten or made an item yet. |
| General | `ShowProgress` | `true` | Food tooltips show your progress for The Gourmet and The Master Chef. |
| Inventory | `InventoryMarkers` | `true` | A diamond on slots with food you haven't eaten yet. |
| Inventory | `MarkerColor` | orange | Colour of the diamond. |
