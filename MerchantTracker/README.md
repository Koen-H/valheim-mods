# MerchantTracker

Haldor, Hildir and the Bog Witch sell a lot, and some of it only after you have made progress. MerchantTracker
shows what is new to you, and how much is still to come.

- **"new" marker.** Wares you have never owned show *new* after their name in the shop.
- **Locked wares.** A last row in the shop counts the wares the trader will only sell later:
  *Only Odin knows what else Hildir will sell (6)*. It doesn't say what they are. For Haldor and the Bog Witch
  they come with defeating bosses; for Hildir with returning the chests her minibosses guard (the bosses of
  the *Odin is Pleased* achievement).
- Client-side only; works on vanilla servers.

## Config (`com.koendertjh.merchanttracker.cfg`)

| Setting | Default | Description |
| --- | --- | --- |
| `NewMarker` | `true` | A marker after wares you have never owned. |
| `MarkerText` | `new` in orange | The marker. Rich text is allowed. |
| `LockedWaresRow` | `true` | A last row counting the wares the trader will only sell later. |
