param()
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$testParent = [IO.Path]::GetFullPath((Join-Path $project 'outputs'))
$fixture = Join-Path $testParent ('quality-gate-fixture-' + [guid]::NewGuid().ToString('N'))
$checks = 0
function Assert-Gate($condition, $reason) {
    if (!$condition) { throw ('Quality gate regression: ' + $reason) }
    $script:checks++
}
function Write-FixtureFile($relative, $text, $time) {
    $path = Join-Path $fixture $relative
    New-Item -ItemType Directory -Path (Split-Path -Parent $path) -Force | Out-Null
    [IO.File]::WriteAllText($path, $text, [Text.UTF8Encoding]::new($false))
    [IO.File]::SetLastWriteTimeUtc($path, $time)
    return $path
}
function Read-FixtureReport {
    # This runs the unchanged quality script in a disposable repository, with
    # synthetic log markers and files. It is not evidence about the real game.
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $fixture 'Tools/Release/Review-Quality.ps1') -InstalledPath (Join-Path $fixture 'Installed') *> $null
    if ($LASTEXITCODE -ne 0) { throw 'Fixture quality script did not run successfully.' }
    return Get-Content -LiteralPath (Join-Path $fixture 'outputs/release-current/quality-status.json') -Raw -Encoding UTF8 | ConvertFrom-Json
}
function Gate-Passed($report,$id) {
    $gate = @($report.automaticChecks | Where-Object id -eq $id)
    if ($gate.Count -ne 1) { throw ('Expected exactly one gate: ' + $id) }
    return $gate[0].passed
}
try {
    New-Item -ItemType Directory -Path (Join-Path $fixture 'Tools/Release') -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Review-Quality.ps1') -Destination (Join-Path $fixture 'Tools/Release/Review-Quality.ps1')
    & git -C $fixture init --quiet
    & git -C $fixture -c user.name='Quality gate fixture' -c user.email='quality-fixture@example.invalid' commit --quiet --allow-empty -m 'Isolated quality gate fixture'
    if ($LASTEXITCODE -ne 0) { throw 'Could not create isolated fixture history.' }
    $stamp = [DateTime]::UtcNow.AddMinutes(-20)
    $sourceTime = $stamp.AddMinutes(-5)
    $buildTime = $stamp.AddMinutes(-3)
    $evidenceTime = $stamp.AddMinutes(-1)
    foreach ($relative in @('Assets/Minglu/Runtime/Fixture.cs','Assets/Minglu/Resources/Minglu/Art/Classic/Portrait.shader','Assets/Minglu/Resources/Minglu/Art/Classic/heads-male.png','Assets/Minglu/Resources/Minglu/Art/Classic/heads-male.png.meta','Assets/Fonts/fixture.ttf','ProjectSettings/ProjectSettings.asset','Packages/manifest.json','Packages/packages-lock.json')) {
        $null = Write-FixtureFile $relative 'Synthetic source for timestamp verification only.' $sourceTime
    }
    foreach ($relative in @('MingluCity.exe','MingluCity_Data/Managed/Assembly-CSharp.dll','MingluCity_Data/resources.assets','MingluCity_Data/resources.assets.resS')) {
        $null = Write-FixtureFile ('Builds/MingluCity/' + $relative) 'Synthetic binary for checksum verification only.' $buildTime
        $null = Write-FixtureFile ('Installed/' + $relative) 'Synthetic binary for checksum verification only.' $buildTime
    }
    $null = Write-FixtureFile 'Builds/Installer/MingluSetup.exe' 'Synthetic installer.' $evidenceTime
    $logMarkers = @{
        'Logs/release-quality-build.log'="MINGLU_BUILD_SUCCESS`nMINGLU_CAMPAIGN_BALANCE_VERIFY_SUCCESS`nMINGLU_SAVE_RECOVERY_VERIFY_SUCCESS`nMINGLU_TUTORIAL_VERIFY_SUCCESS"
        'Logs/qa-tutorial-ui.log'='MINGLU_TUTORIAL_UI_SUCCESS'
        'Logs/qa-tutorial-ui-720.log'='MINGLU_TUTORIAL_UI_SUCCESS'
        'Logs/release-campaign-ui-1600.log'='MINGLU_CAMPAIGN_EXPERIENCE_UI_SUCCESS'
        'Logs/release-campaign-ui-720.log'='MINGLU_CAMPAIGN_EXPERIENCE_UI_SUCCESS'
        'Logs/release-notice-ui.log'='MINGLU_NOTICE_ARCHIVE_UI_SUCCESS'
        'Logs/release-map-ui.log'='MINGLU_MAP_DESK_UI_SUCCESS'
        'Logs/release-installed-ui.log'='MINGLU_CAMPAIGN_EXPERIENCE_UI_SUCCESS'
        'Logs/qa-keyboard-ui.log'='MINGLU_KEYBOARD_MODAL_UI_SUCCESS'
        'Logs/qa-persistence-ui.log'='MINGLU_PERSISTENCE_FAILURE_UI_SUCCESS'
        'Logs/qa-scroll-ui.log'='MINGLU_KEYBOARD_SCROLL_UI_SUCCESS'
        'Logs/qa-work-ui.log'='MINGLU_WORK_OVERVIEW_UI_SUCCESS'
        'Logs/qa-work-ui-720.log'='MINGLU_WORK_OVERVIEW_UI_SUCCESS'
        'Logs/qa-grooming-fit.log'='MINGLU_GROOMING_FIT_RENDER_SUCCESS'
        'Logs/qa-classic-studio.log'='MINGLU_STUDIO_UI_SUCCESS'
        'Logs/qa-classic-studio-720.log'='MINGLU_STUDIO_UI_SUCCESS'
    }
    foreach ($entry in $logMarkers.GetEnumerator()) { $null = Write-FixtureFile $entry.Key $entry.Value $evidenceTime }
    $save = Write-FixtureFile 'SyntheticSave.json' '{"fixture":true}' $sourceTime
    $secondSaveText = '{"fixture":true,"slot":2}'
    $secondSave = Write-FixtureFile 'SyntheticSaveSecond.json' $secondSaveText $sourceTime
    # Read-FixtureReport uses Windows PowerShell 5.1. Keep at least two entries:
    # its ConvertFrom-Json returns the JSON array as one pipeline output object.
    $baseline = ConvertTo-Json -InputObject @(
        [PSCustomObject]@{path=$save;sha256=(Get-FileHash -LiteralPath $save -Algorithm SHA256).Hash},
        [PSCustomObject]@{path=$secondSave;sha256=(Get-FileHash -LiteralPath $secondSave -Algorithm SHA256).Hash}
    )
    $null = Write-FixtureFile 'outputs/release-current/saves-before.json' $baseline $sourceTime

    $report = Read-FixtureReport
    Assert-Gate (Gate-Passed $report 'existing-user-saves-preserved') 'Windows PowerShell 5.1 must compare each unchanged entry in a two-save JSON array.'
    Assert-Gate ($report.automaticPassed -eq $report.automaticTotal) 'A fresh complete fixture should pass every automatic check.'
    Assert-Gate (!$report.commerciallyApproved) 'Synthetic automatic success must not approve commercial release.'
    $null = Write-FixtureFile 'SyntheticSaveSecond.json' '{"fixture":true,"slot":2,"changed":true}' $sourceTime
    $report = Read-FixtureReport
    Assert-Gate (!(Gate-Passed $report 'existing-user-saves-preserved')) 'Changing only the second baseline entry must fail save preservation in Windows PowerShell 5.1.'
    $null = Write-FixtureFile 'SyntheticSaveSecond.json' $secondSaveText $sourceTime
    $report = Read-FixtureReport
    Assert-Gate (Gate-Passed $report 'existing-user-saves-preserved') 'Restoring the second save must make the same two-entry baseline pass again.'
    foreach ($relative in @('Assets/Minglu/Resources/Minglu/Art/Classic/Portrait.shader','Assets/Minglu/Resources/Minglu/Art/Classic/heads-male.png','Assets/Minglu/Resources/Minglu/Art/Classic/heads-male.png.meta','Assets/Fonts/fixture.ttf','Packages/manifest.json')) {
        $path = Join-Path $fixture $relative
        [IO.File]::SetLastWriteTimeUtc($path,$stamp)
        $report = Read-FixtureReport
        Assert-Gate (!(Gate-Passed $report 'build-includes-current-sources')) ('Changed source must reject the old build: ' + $relative)
        Assert-Gate (!(Gate-Passed $report 'rules-and-release-build')) ('Changed source must invalidate earlier verification evidence: ' + $relative)
        Assert-Gate ($report.sourceFreshness.latestPath -eq $path) ('Report must identify the newer source: ' + $relative)
        [IO.File]::SetLastWriteTimeUtc($path,$sourceTime)
    }
    $grooming = Join-Path $fixture 'Logs/qa-grooming-fit.log'
    [IO.File]::SetLastWriteTimeUtc($grooming,$sourceTime)
    $report = Read-FixtureReport
    Assert-Gate (!(Gate-Passed $report 'grooming-fit-render-coverage')) 'A grooming success log before the build is stale.'
    $null = Write-FixtureFile 'Logs/qa-grooming-fit.log' 'MINGLU_GROOMING_FIT_UI_SUCCESS' $evidenceTime
    $report = Read-FixtureReport
    Assert-Gate (!(Gate-Passed $report 'grooming-fit-render-coverage')) 'Unimplemented or wrong success markers must not satisfy rendering coverage.'
    $null = Write-FixtureFile 'Logs/qa-grooming-fit.log' "MINGLU_GROOMING_FIT_RENDER_SUCCESS`nException: synthetic rendering failure" $evidenceTime
    $report = Read-FixtureReport
    Assert-Gate (!(Gate-Passed $report 'grooming-fit-render-coverage')) 'A success marker must not hide a rendering exception.'
    $null = Write-FixtureFile 'Logs/qa-grooming-fit.log' 'MINGLU_GROOMING_FIT_RENDER_SUCCESS' $evidenceTime
    $report = Read-FixtureReport
    Assert-Gate (Gate-Passed $report 'grooming-fit-render-coverage') 'Current actual rendering marker should satisfy coverage only.'
    foreach ($tutorial in @(
        @{id='tutorial-rules';file='Logs/release-quality-build.log';marker='MINGLU_TUTORIAL_VERIFY_SUCCESS'},
        @{id='tutorial-ui-1600x900';file='Logs/qa-tutorial-ui.log';marker='MINGLU_TUTORIAL_UI_SUCCESS'},
        @{id='tutorial-ui-1280x720';file='Logs/qa-tutorial-ui-720.log';marker='MINGLU_TUTORIAL_UI_SUCCESS'}
    )) {
        $path = Join-Path $fixture $tutorial.file
        [IO.File]::SetLastWriteTimeUtc($path,$sourceTime.AddSeconds(-1))
        $report = Read-FixtureReport
        Assert-Gate (!(Gate-Passed $report $tutorial.id)) ('Tutorial evidence before its code/build is stale: ' + $tutorial.id)
        $withoutMarker = $logMarkers[$tutorial.file].Replace($tutorial.marker,'UNRELATED_SYNTHETIC_SUCCESS')
        $null = Write-FixtureFile $tutorial.file $withoutMarker $evidenceTime
        $report = Read-FixtureReport
        Assert-Gate (!(Gate-Passed $report $tutorial.id)) ('Unrelated success cannot satisfy tutorial verification: ' + $tutorial.id)
        $null = Write-FixtureFile $tutorial.file ($logMarkers[$tutorial.file] + "`nException: synthetic tutorial failure") $evidenceTime
        $report = Read-FixtureReport
        Assert-Gate (!(Gate-Passed $report $tutorial.id)) ('Tutorial success must not hide an exception: ' + $tutorial.id)
        $null = Write-FixtureFile $tutorial.file $logMarkers[$tutorial.file] $evidenceTime
        $report = Read-FixtureReport
        Assert-Gate (Gate-Passed $report $tutorial.id) ('Current tutorial marker should satisfy its own gate: ' + $tutorial.id)
    }
    foreach ($studio in @(
        @{id='classic-editor-1600x900';file='Logs/qa-classic-studio.log'},
        @{id='classic-editor-1280x720';file='Logs/qa-classic-studio-720.log'}
    )) {
        $path = Join-Path $fixture $studio.file
        Remove-Item -LiteralPath $path
        $report = Read-FixtureReport
        Assert-Gate (!(Gate-Passed $report $studio.id)) ('Missing classic editor evidence cannot pass: ' + $studio.id)
        $null = Write-FixtureFile $studio.file 'MINGLU_STUDIO_UI_SUCCESS' $sourceTime
        $report = Read-FixtureReport
        Assert-Gate (!(Gate-Passed $report $studio.id)) ('Classic editor evidence before the build is stale: ' + $studio.id)
        $null = Write-FixtureFile $studio.file 'MINGLU_GROOMING_UI_SUCCESS' $evidenceTime
        $report = Read-FixtureReport
        Assert-Gate (!(Gate-Passed $report $studio.id)) ('Grooming coverage cannot substitute for the full editor workflow: ' + $studio.id)
        $null = Write-FixtureFile $studio.file "MINGLU_STUDIO_UI_SUCCESS`nException: synthetic classic editor failure" $evidenceTime
        $report = Read-FixtureReport
        Assert-Gate (!(Gate-Passed $report $studio.id)) ('Classic editor success must not hide an exception: ' + $studio.id)
        $null = Write-FixtureFile $studio.file 'MINGLU_STUDIO_UI_SUCCESS' $evidenceTime
        $report = Read-FixtureReport
        Assert-Gate (Gate-Passed $report $studio.id) ('Current classic editor marker should pass its own resolution: ' + $studio.id)
    }
    Write-Output ('MINGLU_QUALITY_GATE_VERIFY_SUCCESS: ' + $checks + ' checks using isolated synthetic evidence; no game or player files modified.')
}
finally {
    # Resolve and verify the exact disposable target before a recursive delete.
    if (Test-Path -LiteralPath $fixture) {
        $resolved = (Resolve-Path -LiteralPath $fixture).Path
        $allowedPrefix = $testParent.TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
        if (!$resolved.StartsWith($allowedPrefix,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($resolved) -notlike 'quality-gate-fixture-*') {
            throw 'Refusing to remove a fixture outside the project outputs directory.'
        }
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}
