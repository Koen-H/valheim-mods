# ComfortHelper

*Comfort is King* asks for a comfort level of 20. Comfort comes from the furniture within 10 m, but only the
best piece of each kind counts (one bed, one chair, one table...). ComfortHelper tells you what to add.

- **A Comfort tab in the build menu.** Every comfort piece your hammer (or cultivator) can build, with one
  category per comfort group: Beds, Seats, Tables, Fires, Banners, Rugs... Within a group only the best piece
  counts, so a second chair adds nothing. A category shows its bonus in green when you already have one nearby,
  like *Seats +2*. Pieces without a group sit in *Each counts*: every different one adds comfort.
- **Check marks** on the pieces within 10 m of you: **green** = it counts, **grey** = it is here, but a better
  piece of the same group counts instead.
- **Piece descriptions** say what a comfort piece is worth and what it would add where you stand:
  *Raven throne counts here now: +1 more with this one.*
- **When you start resting**, a message shows your comfort and the pieces that would raise it most:
  *Comfort 12 ... To raise it: +2 Raven throne (instead of Chair), +1 Banner, ...*
- **`/comfort` in chat** lists every piece you know that would raise your comfort where you stand, the most
  you can reach with the pieces you know, and how much more comes from pieces you haven't unlocked yet.
- Both show the best comfort level you have reached so far, the number the achievement looks at.
- Client-side only; works on vanilla servers.

Only comfort reached while achievements can be earned counts for the achievement. Valheim turns achievements off
in a game marked as modded (Jotunn and several other mods do this); install
[ModdedAchievements](https://thunderstore.io/c/valheim/p/koendertjh/ModdedAchievements/) to turn them back on.

## Config (`com.koendertjh.comforthelper.cfg`)

| Setting | Default | Description |
| --- | --- | --- |
| `RestingMessage` | `true` | Show the message when you start resting. |
| `MessageCooldown` | `600` | Seconds before the resting message shows again. |
| `MaxSuggestions` | `4` | Pieces listed in the resting message (`/comfort` lists all). |
| `ComfortTab` | `true` | The Comfort tab in the build menu. Needs a restart. |
| `TabName` | `Comfort` | Name of the tab. Needs a restart. |
| `ChecksInAllTabs` | `false` | Also show the check marks in the other build menu tabs. |
| `HoverInfo` | `true` | Comfort pieces show their comfort, group and what they would add here. |
