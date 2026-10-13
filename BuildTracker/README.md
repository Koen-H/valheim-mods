# BuildTracker

Adds a **Not built** tab to the build menu (after Favorites) that lists every piece of the tool in your hand
(hammer, hoe, cultivator, ...) that this character has never placed. Place a piece once and it drops off the list.

- Uses the character's own save data: the per-piece "placed" counts the game keeps for the building achievements.
  Nothing extra is saved and nothing is written.
- The category list on the left only shows categories that still have unbuilt pieces.
- A bar under the pieces shows **"N not discovered yet"**: pieces of the tool (or of the selected category)
  you haven't unlocked, so you know there is more to find. Hidden when there are none.
- Built everything? The tab says **"Congrats! You have built every piece of it!"** (or, while some pieces are still undiscovered, that everything discovered so far is built and there
  are more pieces left to discover).
- Client-side only; works on vanilla servers.

## Limitations

- The game only records placed pieces since it added these stats (a newer character save version). On an
  older character, pieces placed before that still count as "not built" until you place them once more.
- Pieces that share a name (some variants) count as built together.

## Config (`com.koendertjh.buildtracker.cfg`)

| Setting | Default | Description |
| --- | --- | --- |
| `TabName` | `Not built` | Label of the tab (restart the game to apply). |
| `ShowCategories` | `true` | Show the category list on the left of the tab. |
| `ShowUndiscovered` | `true` | Show the "not discovered yet" bar. |
| `UndiscoveredText` | `{0} not discovered yet` | Text of that bar; `{0}` is the number. |
| `CongratsText` | `Congrats! You have built every piece of it!` | Shown when every piece of the tool is built. |
| `CongratsSoFarText` | `Congrats! You have built every piece you have discovered so far. There are still more pieces to build that you have not discovered yet!` | Shown when everything discovered is built. |
