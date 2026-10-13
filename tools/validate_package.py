"""Checks a Thunderstore package (zip) or a mod's source folder against Thunderstore's rules.

    python tools/validate_package.py dist/koendertjh-BuildTracker-1.0.0.zip [--tag BuildTracker-v1.0.0]
    python tools/validate_package.py --source BuildTracker

Exits with 1 and lists every problem when something is wrong. Uses only the standard library,
so it runs the same on the release PC and on GitHub's runners.
"""
import argparse
import json
import re
import struct
import sys
import zipfile
from pathlib import PurePosixPath, Path

NAME_RE = re.compile(r"^[A-Za-z0-9_]+$")
VERSION_RE = re.compile(r"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$")
DEPENDENCY_RE = re.compile(r"^[A-Za-z0-9_]+-[A-Za-z0-9_]+-\d+\.\d+\.\d+$")
NAMESPACE = "koendertjh"
MAX_DESCRIPTION = 250
# Game, Unity and BepInEx files must never be shipped (copyright, and they would break installs).
FORBIDDEN_DLL_RE = re.compile(r"^(assembly_.*|UnityEngine.*|Unity\..*|BepInEx.*|0Harmony.*|Mono\..*|System\..*|mscorlib|netstandard)\.dll$", re.I)


class Checker:
    def __init__(self):
        self.errors = []

    def check(self, condition, message):
        if not condition:
            self.errors.append(message)
        return condition


def png_size(data):
    if data[:8] != b"\x89PNG\r\n\x1a\n" or data[12:16] != b"IHDR":
        return None
    return struct.unpack(">II", data[16:24])


def check_manifest(c, raw):
    try:
        manifest = json.loads(raw.decode("utf-8-sig"))
    except (UnicodeDecodeError, json.JSONDecodeError) as e:
        c.check(False, f"manifest.json is not valid JSON: {e}")
        return None

    for field, kind in (("name", str), ("version_number", str), ("website_url", str),
                        ("description", str), ("dependencies", list)):
        c.check(isinstance(manifest.get(field), kind), f"manifest.json: '{field}' missing or not a {kind.__name__}")
    if c.errors:
        return None

    c.check(NAME_RE.match(manifest["name"]), f"manifest.json: name '{manifest['name']}' may only use A-Z a-z 0-9 _")
    c.check(VERSION_RE.match(manifest["version_number"]), f"manifest.json: version '{manifest['version_number']}' is not like 1.2.3")
    c.check(len(manifest["description"]) <= MAX_DESCRIPTION,
            f"manifest.json: description is {len(manifest['description'])} characters (max {MAX_DESCRIPTION})")
    c.check(manifest["description"].strip() != "", "manifest.json: description is empty")
    url = manifest["website_url"]
    c.check(url == "" or url.startswith("https://"), f"manifest.json: website_url '{url}' must be empty or https://")
    for dep in manifest["dependencies"]:
        c.check(isinstance(dep, str) and DEPENDENCY_RE.match(dep), f"manifest.json: bad dependency '{dep}'")
    c.check(any(str(d).startswith("denikson-BepInExPack_Valheim-") for d in manifest["dependencies"]),
            "manifest.json: BepInExPack_Valheim dependency missing")
    return manifest


def check_common(c, files, read):
    """files: names at the package root; read(name) -> bytes."""
    manifest = None
    if c.check("manifest.json" in files, "manifest.json missing"):
        manifest = check_manifest(c, read("manifest.json"))
    if c.check("icon.png" in files, "icon.png missing"):
        size = png_size(read("icon.png"))
        c.check(size == (256, 256), f"icon.png must be a 256x256 PNG (found {size or 'not a PNG'})")
    if c.check("README.md" in files, "README.md missing"):
        c.check(len(read("README.md").strip()) > 0, "README.md is empty")
    if manifest and "CHANGELOG.md" in files:
        changelog = read("CHANGELOG.md").decode("utf-8-sig")
        c.check(re.search(rf"^##\s+v?{re.escape(manifest['version_number'])}\b", changelog, re.M),
                f"CHANGELOG.md has no '## {manifest['version_number']}' section")
    return manifest


def validate_zip(c, path, tag):
    with zipfile.ZipFile(path) as z:
        names = [n for n in z.namelist() if not n.endswith("/")]
        c.check(all("\\" not in n for n in names), "zip uses backslashes in paths; build it with forward slashes")
        root = {n for n in names if "/" not in n}
        manifest = check_common(c, root, z.read)
        if manifest is None:
            return
        dlls = [PurePosixPath(n).name for n in names if n.lower().endswith(".dll")]
        expected = manifest["name"] + ".dll"
        c.check(expected in root, f"{expected} missing at the zip root")
        for dll in dlls:
            c.check(not FORBIDDEN_DLL_RE.match(dll), f"{dll} must not be shipped (game/BepInEx file)")
            c.check(dll == expected, f"unexpected DLL in package: {dll}")

    name, version = manifest["name"], manifest["version_number"]
    expected_file = f"{NAMESPACE}-{name}-{version}.zip"
    c.check(Path(path).name == expected_file, f"zip should be named {expected_file}")
    if tag is not None:
        c.check(tag == f"{name}-v{version}", f"tag '{tag}' does not match the package ({name}-v{version})")
    if not c.errors:
        print(f"OK  {Path(path).name}  ({name} {version})")


def validate_source(c, folder):
    folder = Path(folder)
    files = {p.name for p in folder.iterdir() if p.is_file()}
    manifest = check_common(c, files, lambda n: (folder / n).read_bytes())
    if manifest:
        c.check(manifest["name"] == folder.name, f"manifest name '{manifest['name']}' should match folder '{folder.name}'")
        c.check("CHANGELOG.md" in files, "CHANGELOG.md missing")
    if not c.errors:
        print(f"OK  {folder.name} (source)")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("zip", nargs="?", help="package zip to check")
    parser.add_argument("--tag", help="release tag the zip belongs to, e.g. BuildTracker-v1.0.0")
    parser.add_argument("--source", help="check a mod's source folder instead of a zip")
    args = parser.parse_args()
    if bool(args.zip) == bool(args.source):
        parser.error("give either a zip or --source")

    c = Checker()
    if args.source:
        validate_source(c, args.source)
    else:
        validate_zip(c, args.zip, args.tag)
    for e in c.errors:
        print(f"ERROR  {args.zip or args.source}: {e}")
    sys.exit(1 if c.errors else 0)


if __name__ == "__main__":
    main()
