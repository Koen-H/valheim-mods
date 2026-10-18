<#
.SYNOPSIS
    Builds, packages and checks the mods; with -Publish also starts the Thunderstore release.

.DESCRIPTION
    Without -Publish this is a dry run: it builds each mod (never copies it into the game), creates
    dist\koendertjh-<Mod>-<version>.zip and checks the zip. Nothing leaves the PC.

    With -Publish it then creates a GitHub Release "<Mod>-v<version>" with the zip attached. The
    GitHub Action checks the zip again and uploads it to Thunderstore.

    The version comes from <Mod>\manifest.json. Bump it there and add a matching section to
    <Mod>\CHANGELOG.md before publishing.

.EXAMPLE
    .\release.ps1                          # dry run for all mods
    .\release.ps1 BuildTracker             # dry run for one mod
    .\release.ps1 BuildTracker -Publish    # release BuildTracker
#>
param(
    [string[]]$Mod,
    [switch]$Publish,
    # Skip the "are you sure" question (for scripted use).
    [switch]$Yes
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$namespace = 'koendertjh'
$dist = Join-Path $root 'dist'

function Fail($message) { Write-Host "ERROR: $message" -ForegroundColor Red; exit 1 }

function Invoke-Native([string]$exe, [string[]]$arguments) {
    & $exe @arguments
    if ($LASTEXITCODE -ne 0) { Fail "$exe $($arguments -join ' ') failed (exit code $LASTEXITCODE)" }
}

# Text of the "## <version>" section in a changelog, used as the GitHub release notes.
function Get-ChangelogSection([string]$path, [string]$version) {
    $text = [IO.File]::ReadAllText($path)
    $match = [regex]::Match($text, "(?ms)^##\s+v?$([regex]::Escape($version))\b[^\n]*\n(.*?)(?=^##\s|\z)")
    if (-not $match.Success) { Fail "$path has no '## $version' section" }
    return $match.Groups[1].Value.Trim()
}

$allMods = Get-ChildItem $root -Directory | Where-Object { Test-Path (Join-Path $_.FullName 'manifest.json') } |
    ForEach-Object Name
if (-not $Mod) { $Mod = $allMods }
foreach ($m in $Mod) { if ($allMods -notcontains $m) { Fail "Unknown mod '$m'. Known: $($allMods -join ', ')" } }

if ($Publish) {
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) { Fail 'GitHub CLI (gh) is not installed: winget install GitHub.cli, then gh auth login' }
    $dirty = git -C $root status --porcelain
    if ($dirty) { Fail "Commit or stash your changes first:`n$dirty" }
    $branch = git -C $root rev-parse --abbrev-ref HEAD
    if ($branch -ne 'main') { Fail "Release from the main branch (now on '$branch')." }
    Invoke-Native git @('-C', $root, 'fetch', '--quiet', '--tags', 'origin')
    if ((git -C $root rev-parse HEAD) -ne (git -C $root rev-parse origin/main)) { Fail 'Push main first: local main differs from origin/main.' }
}

New-Item -ItemType Directory -Force $dist | Out-Null
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$packages = @()

foreach ($m in $Mod) {
    $dir = Join-Path $root $m
    $manifest = Get-Content (Join-Path $dir 'manifest.json') -Raw | ConvertFrom-Json
    $version = $manifest.version_number
    Write-Host "`n== $m $version" -ForegroundColor Cyan

    # Deploy=false: never touch the game's plugin folder from a release build.
    Invoke-Native dotnet @('build', $dir, '-c', 'Release', '-p:Deploy=false', '-p:TreatWarningsAsErrors=true', '-nologo', '-v', 'q')

    $zip = Join-Path $dist "$namespace-$m-$version.zip"
    Remove-Item $zip -ErrorAction SilentlyContinue
    $files = [ordered]@{
        'manifest.json' = Join-Path $dir 'manifest.json'
        'icon.png'      = Join-Path $dir 'icon.png'
        'README.md'     = Join-Path $dir 'README.md'
        'CHANGELOG.md'  = Join-Path $dir 'CHANGELOG.md'
        "$m.dll"        = Join-Path $dir "bin\Release\$m.dll"
    }
    $archive = [IO.Compression.ZipFile]::Open($zip, 'Create')
    try {
        foreach ($entry in $files.Keys) {
            if (-not (Test-Path $files[$entry])) { Fail "$($files[$entry]) is missing" }
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $files[$entry], $entry) | Out-Null
        }
    } finally { $archive.Dispose() }

    $tag = "$m-v$version"
    Invoke-Native python @((Join-Path $root 'tools\validate_package.py'), $zip, '--tag', $tag)
    $packages += [pscustomobject]@{ Mod = $m; Version = $version; Tag = $tag; Zip = $zip; Dir = $dir }
}

if (-not $Publish) {
    Write-Host "`nDry run OK. Packages are in $dist. Nothing was published." -ForegroundColor Green
    exit 0
}

foreach ($p in $packages) {
    if (git -C $root ls-remote --tags origin "refs/tags/$($p.Tag)") { Fail "$($p.Tag) was already released. Bump version_number in $($p.Mod)\manifest.json." }
    Get-ChangelogSection (Join-Path $p.Dir 'CHANGELOG.md') $p.Version | Out-Null
}

Write-Host "`nAbout to create GitHub releases (each is uploaded to Thunderstore right after):"
$packages | ForEach-Object { Write-Host "  $($_.Tag)" }
if (-not $Yes) {
    $answer = Read-Host 'Continue? (y/n)'
    if ($answer -ne 'y') { Write-Host 'Cancelled.'; exit 1 }
}

foreach ($p in $packages) {
    $notesFile = Join-Path $dist "$($p.Tag)-notes.md"
    [IO.File]::WriteAllText($notesFile, (Get-ChangelogSection (Join-Path $p.Dir 'CHANGELOG.md') $p.Version))
    Invoke-Native gh @('release', 'create', $p.Tag, $p.Zip, '--target', 'main', '--title', "$($p.Mod) $($p.Version)", '--notes-file', $notesFile)
}

$repo = gh repo view --json url --jq .url
Write-Host "`nReleased. Follow the Thunderstore upload at $repo/actions" -ForegroundColor Green
