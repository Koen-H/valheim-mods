# CraftTracker

Adds a **Not crafted** tab next to Craft and Upgrade at crafting stations (and when crafting by hand) that lists
every recipe there that this character has never crafted. Craft an item once and it drops off the list.
Crafting from the tab works exactly like the Craft tab.

![Not crafted tab at the forge listing recipes this character has never crafted](https://raw.githubusercontent.com/Koen-H/valheim-mods/main/CraftTracker/screenshots/not-crafted-tab.png)

- Uses the character's own save data: the per-item "crafted" counts the game keeps for the crafting achievements.
  Nothing extra is saved and nothing is written.
- A bar under the recipes shows **"N not discovered yet"**: recipes for this station you haven't unlocked yet.
  Hidden when there are none.

  ![39 not discovered yet bar](https://raw.githubusercontent.com/Koen-H/valheim-mods/main/CraftTracker/screenshots/not-discovered-count.png)

- Crafted everything? The tab says **"Congrats! You have crafted every item of it!"** (or, while some recipes are
  still undiscovered, that everything discovered so far is crafted and there are more items left to discover).
- Client-side only; works on vanilla servers. Companion to BuildTracker.

## Limitations

- Only new crafts count; upgrading an item does not (same as the game's stat).
- The game only records crafted items since it added these stats (a newer character save version). On an older
  character, items crafted before that still count as "not crafted" until you craft them once more.
- Upgrade-only stations have no Craft tab, so they don't get a Not crafted tab either.

## Config (`com.koendertjh.crafttracker.cfg`)

| Setting | Default | Description |
| --- | --- | --- |
| `TabName` | `Not crafted` | Label of the tab (restart the game to apply). |
| `ShowUndiscovered` | `true` | Show the "not discovered yet" bar. |
| `UndiscoveredText` | `{0} not discovered yet` | Text of that bar; `{0}` is the number. |
| `CongratsText` | `Congrats! You have crafted every item of it!` | Shown when every recipe of the station is crafted. |
| `CongratsSoFarText` | `Congrats! You have crafted every item you have discovered so far. There are still more items to craft that you have not discovered yet!` | Shown when everything discovered is crafted. |
