$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
$output = Join-Path $PSScriptRoot 'PackAssets.exe'
& $compiler /nologo ("/out:" + $output) /reference:System.Drawing.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll (Join-Path $PSScriptRoot 'PackAssets.cs')
if ($LASTEXITCODE -ne 0) { throw 'Asset pack compiler failed.' }
& $output $root
if ($LASTEXITCODE -ne 0) { throw 'Asset packaging failed.' }
