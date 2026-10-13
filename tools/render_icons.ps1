# Regenerates <Mod>\icon.svg (tools\icons\make_icons.py) and renders each to a 256x256 <Mod>\icon.png
# with headless Chrome or Edge. Run from anywhere: powershell -File tools\render_icons.ps1
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

python "$PSScriptRoot\icons\make_icons.py"
if ($LASTEXITCODE -ne 0) { throw 'make_icons.py failed' }

$browser = @(
    "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
    "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $browser) { throw 'Chrome or Edge is needed to render the icons.' }

$profileDir = Join-Path ([IO.Path]::GetTempPath()) 'valheim-mods-icon-render'
foreach ($svg in Get-ChildItem $root -Filter icon.svg -Recurse -Depth 1) {
    $png = Join-Path $svg.DirectoryName 'icon.png'
    $url = 'file:///' + ($svg.FullName -replace '\\', '/')
    Remove-Item $png -ErrorAction SilentlyContinue
    # Chrome reports progress on stderr, which Windows PowerShell would turn into an error.
    $ErrorActionPreference = 'Continue'
    & $browser --headless=new --disable-gpu --hide-scrollbars --force-device-scale-factor=1 `
        --default-background-color=00000000 --window-size=256,256 "--user-data-dir=$profileDir" `
        "--screenshot=$png" $url 2>&1 | Out-Null
    $ErrorActionPreference = 'Stop'
    if (-not (Test-Path $png)) { throw "Rendering $($svg.FullName) failed" }
    Write-Host "rendered $png"
}
