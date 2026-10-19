# BuildTracker

Adds a **Not built** tab to the build menu (after Favorites) that lists every piece of the tool in your hand
(hammer, hoe, cultivator, ...) that this character has never placed. Place a piece once and it drops off the list.

![Not built tab in the build menu listing pieces this character has never placed](https://raw.githubusercontent.com/Koen-H/valheim-mods/main/BuildTracker/screenshots/not-built-tab.png)

- Uses the character's own save data: the per-piece "placed" counts the game keeps for the building achievements.
  Nothing extra is saved and nothing is written.
- The category list on the left only shows categories that still have unbuilt pieces.
- A bar under the pieces shows **"N not discovered yet"**: pieces of the tool (or of the selected category)
  you haven't unlocked, so you know there is more to find. Hidden when there are none.

  ![242 not discovered yet bar](https://raw.githubusercontent.com/Koen-H/valheim-mods/main/BuildTracker/screenshots/not-discovered-count.png)

- Built everything? The tab says **"Congrats! You have built every piece of it!"** (or, while some pieces are still undiscovered, that everything discovered so far is built and there
  are more pieces left to discover).
- **Seasonal pieces all year** (off by default). Missed Yule, Midsummer or Halloween? Turn on `SeasonalPieces`
  and the seasonal decorations (like the Yule tree and the Jack-o-turnip) can be built all year; they then count
  in the Not built tab too. `SeasonalRecipes` does the same for seasonal recipes (like the Yule hat and the
  Midsummer crown). You still unlock them the normal way, with the materials; only the date check is gone.
  Turning a setting off puts the dates back, no restart needed.
- Client-side only; works on vanilla servers.

## Limitations

- The game only records placed pieces since it added these stats (a newer character save version). On an
  older character, pieces placed before that still count as "not built" until you place them once more.
- Pieces that share a name (some variants) count as built together.

## Config (`com.koendertjh.buildtracker.cfg`)

| Section | Setting | Default | Description |
| --- | --- | --- | --- |
| General | `TabName` | `Not built` | Label of the tab (restart the game to apply). |
| General | `ShowCategories` | `true` | Show the category list on the left of the tab. |
| General | `ShowUndiscovered` | `true` | Show the "not discovered yet" bar. |
| General | `UndiscoveredText` | `{0} not discovered yet` | Text of that bar; `{0}` is the number. |
| General | `CongratsText` | `Congrats! You have built every piece of it!` | Shown when every piece of the tool is built. |
| General | `CongratsSoFarText` | `Congrats! You have built every piece you have discovered so far. There are still more pieces to build that you have not discovered yet!` | Shown when everything discovered is built. |
| Seasonal | `SeasonalPieces` | `false` | Seasonal build pieces can be built all year. |
| Seasonal | `SeasonalRecipes` | `false` | Seasonal recipes can be crafted all year. |
