# UnlockAwareness

Never miss a new recipe, build piece or item again. Everything your character unlocks or picks up for the first
time gets an orange **!** marker until you have looked at it.

![Build menu with orange ! markers on newly unlocked pieces](https://raw.githubusercontent.com/Koen-H/valheim-mods/main/UnlockAwareness/screenshots/build-menu-badges.png)

![Crafting panel with a ! on a new recipe and the Mark all as seen button](https://raw.githubusercontent.com/Koen-H/valheim-mods/main/UnlockAwareness/screenshots/crafting-badges.png)

- **Crafting panel**: new recipes show a **!** on their icon in the Craft tab. Select the recipe and the marker
  disappears.
- **Build menu**: new pieces show a **!** on their button (hammer, hoe, cultivator, ...). Select the piece and the
  marker disappears.
- **Inventory**: items you pick up for the very first time show a **!** in your inventory. Click the item (or
  right-click to use or equip it) and the marker disappears. Turn it off with `InventoryBadges`.
- **Chests**: opening a chest counts as having the items inside: your character learns them (unlocking any recipes
  and pieces they lead to) and they get a **!** right away. Turn it off with `LearnFromContainers`; unknown chest
  items then show a **?** until you take them (turn that off with `ContainerBadges`).

  ![Stone and wood in the inventory, each with an orange ! marker](https://raw.githubusercontent.com/Koen-H/valheim-mods/main/UnlockAwareness/screenshots/inventory-badges.png)

- **Mark all as seen**: an eye button below the repair button in the crafting panel, and next to the search field in
  the build menu, clears every marker there at once. The tooltip shows how many are left. Dimmed when there is
  nothing new.
- Markers are saved with your character, so they survive logging out.
- Client-side only; works on vanilla servers.

## Already playing?

Only things you unlock or pick up for the first time **after** installing get a marker; items you have
owned before are never marked. Want to browse everything you already know as well?
Set `FlagAllKnownOnce = true`: the next time each character loads in, every recipe and piece it already knows
is marked as new (once per character). Clear them with **Mark all as seen**.

## Config (`com.koendertjh.unlockawareness.cfg`)

| Section | Setting | Default | Description |
| --- | --- | --- | --- |
| General | `FlagAllKnownOnce` | `false` | Mark every recipe and piece a character already knows as new, once per character. |
| General | `InventoryBadges` | `true` | Show a **!** on items you pick up for the first time, until you click them. |
| General | `LearnFromContainers` | `true` | Opening a chest learns the items inside, as if picked up. |
| General | `ContainerBadges` | `true` | With `LearnFromContainers` off: show a **?** on chest items you have never had. |
| Badge | `Color` | orange | Color of the **!** marker. |

## Notes

- The Upgrade tab never shows markers: it lists items you already own.
- Remove the mod at any time; the only thing left behind is a small list of unseen names in your character file.
