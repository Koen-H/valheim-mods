# UnlockAwareness

Never miss a new recipe or build piece again. Everything your character unlocks gets an orange **!** marker
until you have looked at it.

- **Crafting panel**: new recipes show a **!** on their icon in the Craft tab. Select the recipe and the marker
  disappears.
- **Build menu**: new pieces show a **!** on their button (hammer, hoe, cultivator, ...). Select the piece and the
  marker disappears.
- **Mark all as seen**: an eye button below the repair button in the crafting panel, and next to the search field in
  the build menu, clears every marker there at once. The tooltip shows how many are left. Dimmed when there is
  nothing new.
- Markers are saved with your character, so they survive logging out.
- Client-side only; works on vanilla servers.

## Already playing?

Only things you unlock **after** installing get a marker. Want to browse everything you already know as well?
Set `FlagAllKnownOnce = true`: the next time each character loads in, every recipe and piece it already knows
is marked as new (once per character). Clear them with **Mark all as seen**.

## Config (`com.koendertjh.unlockawareness.cfg`)

| Section | Setting | Default | Description |
| --- | --- | --- | --- |
| General | `FlagAllKnownOnce` | `false` | Mark everything a character already knows as new, once per character. |
| Badge | `Color` | orange | Color of the **!** marker. |

## Notes

- The Upgrade tab never shows markers: it lists items you already own.
- Remove the mod at any time; the only thing left behind is a small list of unseen names in your character file.
