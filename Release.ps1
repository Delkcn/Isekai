$ErrorActionPreference = 'Stop'
$version = '0.7.24-preview'
$target = Join-Path $PSScriptRoot ("dist\IsekaijouchoPet-v" + $version)
if (Test-Path -LiteralPath $target) { throw 'Release folder already exists. Choose a new version before packaging again.' }
foreach ($name in @('IsekaijouchoPet.exe','assets')) {
    if (-not (Test-Path -LiteralPath (Join-Path $PSScriptRoot $name))) { throw "Build first: missing $name" }
}
New-Item -ItemType Directory -Path $target | Out-Null
foreach ($name in @('IsekaijouchoPet.exe','README.md','PROJECT_STATE.md','VERSION')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $target
}
# Ship the active resources as editable files, without recreating character.pack.
$resources = @(
    'v0610/anemone-i-unified-v10-eyes.png', 'v064/walk-legs.png',
    'v067/sleep-keys-eye.png', 'v0611/underarm-backplate.png',
    'v0616/greeting-underlay.png', 'v0618/greeting-palm.png',
    'v0642/ivory-bow.png', 'v0642/ivory-bowstring.png',
    'v0643/ivory-arrow.png', 'v0700/blue-silver-sword.png',
    'v0700/right-grip-hands.png', 'version.txt'
)
foreach ($name in $resources) {
    $source = Join-Path (Join-Path $PSScriptRoot 'assets') $name
    $destination = Join-Path (Join-Path $target 'assets') $name
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
    Copy-Item -LiteralPath $source -Destination $destination
}
$zip = $target + '.zip'
$preview = Join-Path $target 'preview'
New-Item -ItemType Directory -Path $preview | Out-Null
Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'qa\v0724') -File | Where-Object { $_.Extension -in @('.html','.png') -or $_.Name -in @('validation.md','agent-interface.md','FocusedCheck.txt','JourneyCheck.txt','FixedParametersCheck.txt') } | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $preview
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'qa\v0724\FocusedCheck.txt') -Destination (Join-Path $target 'CHECK_RESULTS.txt')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'qa\v0724\agent-interface.md') -Destination (Join-Path $target 'AGENT_INTERFACE.md')
Compress-Archive -LiteralPath $target -DestinationPath $zip
Get-Item -LiteralPath $zip | Select-Object FullName,Length
