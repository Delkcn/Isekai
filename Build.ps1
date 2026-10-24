param(
    [ValidateSet('App', 'Tests', 'AnimationCheck', 'RigCheck', 'FeedbackCheck', 'CarryCheck', 'CarryAnimationCheck', 'ArtCheck', 'DesktopPaintCheck', 'GestureCheck', 'BubbleCheck', 'WaveCheck', 'RunCheck', 'LeapCheck', 'PivotCheck', 'TuningCheck', 'TransitionCheck', 'CarryMotionCheck', 'PickupCheck', 'SleepGridCheck', 'ActionCheck', 'CorrectionCheck', 'FixedParametersCheck', 'ArcheryEffectsCheck', 'ArcheryPoseCheck', 'ArcheryStanceCheck', 'ArcheryLayerCheck', 'ArcheryImpactCheck','SwordCheck','FrozenCheck','SwordPreview','RevisionCheck','InteractionCheck','CastingCheck')]
    [string]$Target = 'App',
    [switch]$Rebuild
)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

try {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    if (-not (Test-Path -LiteralPath $compiler)) {
        $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
    }
    if (-not (Test-Path -LiteralPath $compiler)) {
        throw 'Microsoft .NET Framework 4.8 compiler was not found.'
    }

    $specifications = @{
        ArcheryLayerCheck = @{ Name = 'ArcheryLayerCheck'; Main = 'ArcheryLayerCheck'; Kind = 'exe'; Test = 'ArcheryLayerCheck.cs' }
        ArcheryImpactCheck = @{ Name = 'ArcheryImpactCheck'; Main = 'ArcheryImpactCheck'; Kind = 'exe'; Test = 'ArcheryImpactCheck.cs' }
        CastingCheck = @{ Name = 'CastingCheck'; Main = 'CastingCheck'; Kind = 'exe'; Test = 'CastingCheck.cs' }
        InteractionCheck = @{ Name = 'InteractionCheck'; Main = 'InteractionCheck'; Kind = 'exe'; Test = 'InteractionCheck.cs' }
        RevisionCheck = @{ Name = 'RevisionCheck'; Main = 'RevisionCheck'; Kind = 'exe'; Test = 'RevisionCheck.cs' }
        SwordCheck = @{ Name = 'SwordCheck'; Main = 'SwordCheck'; Kind = 'exe'; Test = 'SwordCheck.cs' }
        FrozenCheck = @{ Name = 'FrozenCheck'; Main = 'FrozenCheck'; Kind = 'exe'; Test = 'FrozenCheck.cs' }
        SwordPreview = @{ Name = 'SwordPreview'; Main = 'SwordPreview'; Kind = 'exe'; Test = 'SwordPreview.cs' }
        App = @{ Name = 'IsekaijouchoPet'; Main = 'JouchoPet.Program'; Kind = 'winexe'; Test = '' }
        Tests = @{ Name = 'SelfTest'; Main = 'SelfTest'; Kind = 'exe'; Test = 'SelfTest.cs' }
        AnimationCheck = @{ Name = 'AnimationCheck'; Main = 'AnimationCheck'; Kind = 'exe'; Test = 'AnimationCheck.cs' }
        RigCheck = @{ Name = 'RigCheck'; Main = 'RigCheck'; Kind = 'exe'; Test = 'RigCheck.cs' }
        FeedbackCheck = @{ Name = 'FeedbackCheck'; Main = 'FeedbackCheck'; Kind = 'exe'; Test = 'FeedbackCheck.cs' }
        CarryCheck = @{ Name = 'CarryCheck'; Main = 'CarryCheck'; Kind = 'exe'; Test = 'CarryCheck.cs' }
        CarryAnimationCheck = @{ Name = 'CarryAnimationCheck'; Main = 'CarryAnimationCheck'; Kind = 'exe'; Test = 'CarryAnimationCheck.cs' }
        ArtCheck = @{ Name = 'ArtCheck'; Main = 'ArtCheck'; Kind = 'exe'; Test = 'ArtCheck.cs' }
        DesktopPaintCheck = @{ Name = 'DesktopPaintCheck'; Main = 'DesktopPaintCheck'; Kind = 'exe'; Test = 'DesktopPaintCheck.cs' }
        GestureCheck = @{ Name = 'GestureCheck'; Main = 'GestureCheck'; Kind = 'exe'; Test = 'GestureCheck.cs' }
        BubbleCheck = @{ Name = 'BubbleCheck'; Main = 'BubbleCheck'; Kind = 'exe'; Test = 'BubbleCheck.cs' }
        WaveCheck = @{ Name = 'WaveCheck'; Main = 'WaveCheck'; Kind = 'exe'; Test = 'WaveCheck.cs' }
        RunCheck = @{ Name = 'RunCheck'; Main = 'RunCheck'; Kind = 'exe'; Test = 'RunCheck.cs' }
        LeapCheck = @{ Name = 'LeapCheck'; Main = 'LeapCheck'; Kind = 'exe'; Test = 'LeapCheck.cs' }
        PivotCheck = @{ Name = 'PivotCheck'; Main = 'PivotCheck'; Kind = 'exe'; Test = 'PivotCheck.cs' }
        TuningCheck = @{ Name = 'TuningCheck'; Main = 'TuningCheck'; Kind = 'exe'; Test = 'TuningCheck.cs' }
        TransitionCheck = @{ Name = 'TransitionCheck'; Main = 'TransitionCheck'; Kind = 'exe'; Test = 'TransitionCheck.cs' }
        CarryMotionCheck = @{ Name = 'CarryMotionCheck'; Main = 'CarryMotionCheck'; Kind = 'exe'; Test = 'CarryMotionCheck.cs' }
        SleepGridCheck = @{ Name = 'SleepGridCheck'; Main = 'SleepGridCheck'; Kind = 'exe'; Test = 'SleepGridCheck.cs' }
        ActionCheck = @{ Name = 'ActionCheck'; Main = 'ActionCheck'; Kind = 'exe'; Test = 'ActionCheck.cs' }
        CorrectionCheck = @{ Name = 'CorrectionCheck'; Main = 'CorrectionCheck'; Kind = 'exe'; Test = 'CorrectionCheck.cs' }
        FixedParametersCheck = @{ Name = 'FixedParametersCheck'; Main = 'FixedParametersCheck'; Kind = 'exe'; Test = 'FixedParametersCheck.cs' }
        ArcheryEffectsCheck = @{ Name = 'ArcheryEffectsCheck'; Main = 'ArcheryEffectsCheck'; Kind = 'exe'; Test = 'ArcheryEffectsCheck.cs' }
        ArcheryStanceCheck = @{ Name = 'ArcheryStanceCheck'; Main = 'ArcheryStanceCheck'; Kind = 'exe'; Test = 'ArcheryStanceCheck.cs' }
        ArcheryPoseCheck = @{ Name = 'ArcheryPoseCheck'; Main = 'ArcheryPoseCheck'; Kind = 'exe'; Test = 'ArcheryPoseCheck.cs' }
        PickupCheck = @{ Name = 'PickupCheck'; Main = 'PickupCheck'; Kind = 'exe'; Test = 'PickupCheck.cs' }
    }
    $spec = $specifications[$Target]
    # One source-discovery rule for every target; adding src/*.cs needs no script edits.
    $sources = @(Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs' -Recurse |
        Sort-Object FullName | ForEach-Object { $_.FullName })
    if ($spec.Test) { $sources += Join-Path (Join-Path $root 'tests') $spec.Test }
    if ($sources.Count -eq 0) { throw 'No C# sources were found.' }

    $output = Join-Path $root ($spec.Name + '.exe')
    $cacheDir = Join-Path $root '.build'
    [IO.Directory]::CreateDirectory($cacheDir) | Out-Null
    $stamp = Join-Path $cacheDir ($Target + '.sha256')
    $inputs = @($sources) + @($PSCommandPath, (Join-Path $root 'VERSION'))
    $fingerprintInput = ($inputs | ForEach-Object {
        $_ + ':' + (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash
    }) -join "`n"
    $hash = [Security.Cryptography.SHA256]::Create()
    try { $fingerprint = [Convert]::ToBase64String($hash.ComputeHash([Text.Encoding]::UTF8.GetBytes($fingerprintInput))) }
    finally { $hash.Dispose() }

    if (-not $Rebuild -and (Test-Path -LiteralPath $output) -and (Test-Path -LiteralPath $stamp)) {
        if ([IO.File]::ReadAllText($stamp) -eq $fingerprint) {
            Write-Host "$Target is up to date."
            exit 0
        }
    }

    $temporary = Join-Path $cacheDir ($spec.Name + '.exe')
    $arguments = @('/nologo', '/optimize+', '/unsafe+', '/codepage:65001',
        ('/target:' + $spec.Kind), ('/main:' + $spec.Main), ('/out:' + $temporary),
        '/reference:System.Drawing.dll', '/reference:System.Windows.Forms.dll',
        '/reference:System.Net.Http.dll', '/reference:System.Web.Extensions.dll',
        '/reference:System.Security.dll', '/reference:System.IO.Compression.dll',
        '/reference:System.IO.Compression.FileSystem.dll') + $sources
    Write-Host "Building $Target..."
    & $compiler @arguments
    if ($LASTEXITCODE -ne 0) { throw 'C# compilation failed; the previous executable was retained.' }
    # Replace only after compilation succeeds. A running locked executable fails clearly.
    Move-Item -LiteralPath $temporary -Destination $output -Force
    [IO.File]::WriteAllText($stamp, $fingerprint)
    Write-Host "Built $output"
    exit 0
}
catch {
    Write-Host ('Build failed: ' + $_.Exception.Message) -ForegroundColor Red
    exit 1
}
