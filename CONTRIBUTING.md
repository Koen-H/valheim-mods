# Building and releasing

## Building

Needs the .NET SDK (6 or newer), Valheim, and BepInExPack_Valheim in an r2modman profile. The mods compile
against the game's own DLLs, which are never copied into this repo.

```
dotnet build BuildTracker -c Release
```

This also copies the DLL into the r2modman **Default** profile. Close Valheim first (it locks the DLL), or add
`-p:Deploy=false` to only build. Other locations: `-p:R2Profile=MyProfile`, `-p:ValheimDir="X:\path\to\Valheim"`.

Shared build settings are in `Directory.Build.props` / `.targets`. Each mod's version lives only in its
`manifest.json`; the plugin's `[BepInPlugin]` version is generated from it.

Pull requests are welcome. Every push is checked by the **Check** workflow (Thunderstore files, no DLLs
committed). The mods themselves can't be built on GitHub, so build and test in game before opening one.

## Releasing (maintainer)

1. Bump `version_number` in `<Mod>/manifest.json` and add a `## <version>` section to `<Mod>/CHANGELOG.md`.
2. Dry run (builds, packs and checks; publishes nothing): `.\release.ps1 <Mod>`
3. Commit and push to `main`, then: `.\release.ps1 <Mod> -Publish`
   This creates the GitHub Release `<Mod>-v<version>` with the zip attached.
4. The **Publish to Thunderstore** action checks the zip again and waits for approval under
   *Actions*. Approve it to upload.

One-time setup: GitHub CLI (`winget install GitHub.cli`, `gh auth login`); a GitHub environment
`thunderstore` with yourself as required reviewer and the secret `THUNDERSTORE_TOKEN` (a Thunderstore
service-account token of the `koendertjh` team).

A published Thunderstore version can't be changed: README fixes for a mod need a new version.
This repo's own README can be changed any time.

Icons: edit `tools/icons/make_icons.py`, then run `tools\render_icons.ps1` (uses Chrome or Edge).
