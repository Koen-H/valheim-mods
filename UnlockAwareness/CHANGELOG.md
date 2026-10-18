# Changelog

## 1.2.1

- Performance: the inventory and chest markers do much less work every frame while the inventory is open.

## 1.2.0

- New: opening a chest (or other container) counts as having the items inside. Your character learns them as if
  picked up, which also unlocks the recipes and pieces they lead to, and they get a "!" right away (in the chest
  too). Click the item to clear it. Turn this off with the new `LearnFromContainers` setting (General, on by default).
- New: with `LearnFromContainers` off, chest items you have never had show a "?" instead, until you take them.
  Turn that off with the new `ContainerBadges` setting (General, on by default).

## 1.1.0

- New: items you pick up for the first time get a "!" marker in your inventory. Click the item (or right-click
  to use or equip it) and the marker disappears.
- New `InventoryBadges` setting (General, on by default) to turn the inventory markers off.
- Items you already had before updating are not marked.

## 1.0.0

- First public release: "!" markers on new recipes and build pieces, "Mark all as seen" buttons.
