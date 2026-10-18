"""Prints a thunderstore.toml for publishing an already built package zip with the Thunderstore CLI:

    python tools/tcli_config.py dist/koendertjh-BuildTracker-1.0.0.zip > thunderstore.toml
    tcli publish --config-path thunderstore.toml --file dist/koendertjh-BuildTracker-1.0.0.zip

tcli takes the package's namespace, name and version from this file; the zip's own manifest.json is
what Thunderstore shows. The categories below are the slugs of the Valheim community on Thunderstore.
"""
import json
import sys
import zipfile

NAMESPACE = "koendertjh"
CATEGORIES = {
    "AchievementPeek": ["mods", "client-side", "utility", "ai-generated"],
    "BestiaryTracker": ["mods", "client-side", "utility", "ai-generated"],
    "BossReset": ["mods", "client-side", "tweaks", "utility", "ai-generated"],
    "BuildTracker": ["mods", "client-side", "building", "utility", "ai-generated"],
    "ComfortHelper": ["mods", "client-side", "building", "utility", "ai-generated"],
    "CraftTracker": ["mods", "client-side", "crafting", "utility", "ai-generated"],
    "EssenceForAll": ["mods", "client-side", "tweaks", "ai-generated"],
    "FishTracker": ["mods", "client-side", "crafting", "tweaks", "utility", "ai-generated"],
    "FoodTracker": ["mods", "client-side", "utility", "ai-generated"],
    "MaterialTracker": ["mods", "client-side", "crafting", "utility", "ai-generated"],
    "MerchantTracker": ["mods", "client-side", "utility", "ai-generated"],
    "ModdedAchievements": ["mods", "client-side", "tweaks", "ai-generated"],
    "PeakFinder": ["mods", "client-side", "building", "utility", "ai-generated"],
    "SummonTweaks": ["mods", "client-side", "tweaks", "ai-generated"],
    "TrophyAssister": ["mods", "client-side", "tweaks", "utility", "ai-generated"],
    "UnlockAwareness": ["mods", "client-side", "crafting", "building", "utility", "ai-generated"],
    "WorldSeed": ["mods", "client-side", "utility", "ai-generated"],
}


def toml_string(value):
    return json.dumps(value, ensure_ascii=False)  # a JSON string is a valid TOML basic string


def main():
    with zipfile.ZipFile(sys.argv[1]) as z:
        manifest = json.loads(z.read("manifest.json").decode("utf-8-sig"))
    name = manifest["name"]
    if name not in CATEGORIES:
        sys.exit(f"No Thunderstore categories set for {name} in tools/tcli_config.py")

    dependencies = "\n".join(
        f"{toml_string(dep.rsplit('-', 1)[0])} = {toml_string(dep.rsplit('-', 1)[1])}" for dep in manifest["dependencies"])
    print(f"""[config]
schemaVersion = "0.0.1"

[package]
namespace = {toml_string(NAMESPACE)}
name = {toml_string(name)}
versionNumber = {toml_string(manifest["version_number"])}
description = {toml_string(manifest["description"])}
websiteUrl = {toml_string(manifest["website_url"])}
containsNsfwContent = false

[package.dependencies]
{dependencies}

[build]
icon = "./icon.png"
readme = "./README.md"
outdir = "./build"

[publish]
repository = "https://thunderstore.io"
communities = ["valheim"]

[publish.categories]
valheim = {json.dumps(CATEGORIES[name])}
""")


if __name__ == "__main__":
    main()
