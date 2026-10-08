param([switch]$RequirePass,[string]$InstalledPath='D:/Games/MingluCity')
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $project 'outputs/release-current'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$binary = Join-Path $project 'Builds/MingluCity/MingluCity.exe'
$sourceFiles = @(Get-ChildItem (Join-Path $project 'Assets') -Recurse -File)
$sourceFiles += @(Get-ChildItem (Join-Path $project 'ProjectSettings') -Recurse -File)
# Art, shaders, imported-font data and .meta importer settings are build inputs
# too. A portrait-only edit must invalidate an older successful delivery.
foreach ($packageFile in @('Packages/manifest.json','Packages/packages-lock.json')) {
    $packagePath = Join-Path $project $packageFile
    if (Test-Path -LiteralPath $packagePath) { $sourceFiles += Get-Item -LiteralPath $packagePath }
}
$latestSourceFile = $sourceFiles | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
$latestSource = if ($null -ne $latestSourceFile) { $latestSourceFile.LastWriteTimeUtc } else { [DateTime]::MinValue }
$builtEvidence=@($binary,(Join-Path $project 'Builds/MingluCity/MingluCity_Data/Managed/Assembly-CSharp.dll'),(Join-Path $project 'Builds/MingluCity/MingluCity_Data/resources.assets'),(Join-Path $project 'Builds/MingluCity/MingluCity_Data/resources.assets.resS'))
$buildTime = if (Test-Path -LiteralPath $binary) { ($builtEvidence | Where-Object { Test-Path -LiteralPath $_ } | Get-Item | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1).LastWriteTimeUtc } else { [DateTime]::MinValue }
$rows = @()
$freshBuild = $null -ne $latestSourceFile -and @($builtEvidence | Where-Object { !(Test-Path -LiteralPath $_) }).Count -eq 0 -and $buildTime -ge $latestSource
$rows += [PSCustomObject]@{ id='build-includes-current-sources'; passed=$freshBuild; evidence=if ($null -ne $latestSourceFile) { $latestSourceFile.FullName } else { 'Assets / ProjectSettings' }; detail='Current build must postdate every imported source, including shaders, art, fonts and importer settings.' }
function Read-Gate($id, $file, $marker, $after) {
    $path = Join-Path $project $file
    $ok = $false
    $detail = 'No current verification evidence.'
    if (Test-Path -LiteralPath $path) {
        $text = [IO.File]::ReadAllText($path)
        $ok = $text.Contains($marker) -and $text -notmatch 'Exception:|error CS[0-9]+' -and (Get-Item -LiteralPath $path).LastWriteTimeUtc -ge $after
        $detail = if ($ok) { $marker } else { 'Missing success marker, error in log, or evidence predates the current code/build.' }
    }
    [PSCustomObject]@{ id=$id; passed=$ok; evidence=$file; detail=$detail }
}
$rows += Read-Gate 'rules-and-release-build' 'Logs/release-quality-build.log' 'MINGLU_BUILD_SUCCESS' $latestSource
$rows += Read-Gate 'sixty-complete-campaigns' 'Logs/release-quality-build.log' 'MINGLU_CAMPAIGN_BALANCE_VERIFY_SUCCESS' $latestSource
$rows += Read-Gate 'save-recovery' 'Logs/release-quality-build.log' 'MINGLU_SAVE_RECOVERY_VERIFY_SUCCESS' $latestSource
$rows += Read-Gate 'campaign-ui-1600x900' 'Logs/release-campaign-ui-1600.log' 'MINGLU_CAMPAIGN_EXPERIENCE_UI_SUCCESS' $buildTime
$rows += Read-Gate 'campaign-ui-1280x720' 'Logs/release-campaign-ui-720.log' 'MINGLU_CAMPAIGN_EXPERIENCE_UI_SUCCESS' $buildTime
$rows += Read-Gate 'notice-history-ui' 'Logs/release-notice-ui.log' 'MINGLU_NOTICE_ARCHIVE_UI_SUCCESS' $buildTime
$rows += Read-Gate 'map-and-existing-workflows' 'Logs/release-map-ui.log' 'MINGLU_MAP_DESK_UI_SUCCESS' $buildTime
$rows += Read-Gate 'installed-game-ui' 'Logs/release-installed-ui.log' 'MINGLU_CAMPAIGN_EXPERIENCE_UI_SUCCESS' $buildTime
$rows += Read-Gate 'keyboard-modal-isolation' 'Logs/qa-keyboard-ui.log' 'MINGLU_KEYBOARD_MODAL_UI_SUCCESS' $buildTime
$rows += Read-Gate 'persistence-failure-feedback' 'Logs/qa-persistence-ui.log' 'MINGLU_PERSISTENCE_FAILURE_UI_SUCCESS' $buildTime
$rows += Read-Gate 'keyboard-scroll-visibility' 'Logs/qa-scroll-ui.log' 'MINGLU_KEYBOARD_SCROLL_UI_SUCCESS' $buildTime
$rows += Read-Gate 'work-overview-1600x900' 'Logs/qa-work-ui.log' 'MINGLU_WORK_OVERVIEW_UI_SUCCESS' $buildTime
$rows += Read-Gate 'work-overview-1280x720' 'Logs/qa-work-ui-720.log' 'MINGLU_WORK_OVERVIEW_UI_SUCCESS' $buildTime
$rows += Read-Gate 'grooming-fit-render-coverage' 'Logs/qa-grooming-fit.log' 'MINGLU_GROOMING_FIT_RENDER_SUCCESS' $buildTime
$installed = $InstalledPath
$matching = Test-Path -LiteralPath $binary
foreach ($relative in @('MingluCity.exe','MingluCity_Data/Managed/Assembly-CSharp.dll','MingluCity_Data/resources.assets','MingluCity_Data/resources.assets.resS')) {
    $built = Join-Path (Join-Path $project 'Builds/MingluCity') $relative
    $target = Join-Path $installed $relative
    if (!(Test-Path -LiteralPath $built) -or !(Test-Path -LiteralPath $target)) { $matching=$false; continue }
    if ((Get-FileHash -LiteralPath $built -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash) { $matching=$false }
}
$rows += [PSCustomObject]@{ id='installed-files-match-build'; passed=$matching; evidence=$installed; detail='Compare executable, managed gameplay code and portrait/resource data.' }
$baseline = Join-Path $output 'saves-before.json'
$preserved = Test-Path -LiteralPath $baseline
if ($preserved) {
    foreach ($save in @(Get-Content -LiteralPath $baseline -Raw -Encoding UTF8 | ConvertFrom-Json)) {
        if (!(Test-Path -LiteralPath $save.path) -or (Get-FileHash -LiteralPath $save.path -Algorithm SHA256).Hash -ne $save.sha256) { $preserved=$false }
    }
}
$rows += [PSCustomObject]@{ id='existing-user-saves-preserved'; passed=$preserved; evidence='outputs/release-current/saves-before.json'; detail='Existing user saves must remain byte-identical during isolated testing and deployment.' }
$installer=Join-Path $project 'Builds/Installer/MingluSetup.exe'
$packaged=(Test-Path -LiteralPath $installer) -and (Get-Item -LiteralPath $installer).LastWriteTimeUtc -ge $buildTime
$rows += [PSCustomObject]@{ id='current-fixed-installer'; passed=$packaged; evidence='Builds/Installer/MingluSetup.exe'; detail='Installer must be rebuilt after the current game.' }
$head = & git -C $project rev-parse --short HEAD
$artifacts = @()
foreach ($file in @($builtEvidence + @($installer))) {
    if (Test-Path -LiteralPath $file) {
        $item = Get-Item -LiteralPath $file
        $artifacts += [PSCustomObject]@{ path=$item.FullName; bytes=$item.Length; modifiedUtc=$item.LastWriteTimeUtc.ToString('o'); sha256=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash }
    }
}
$report = [PSCustomObject]@{
    version='0.17.3'; checkedAtUtc=[DateTime]::UtcNow.ToString('o'); gitHead=$head
    sourceFreshness=[PSCustomObject]@{ latestPath=if ($null -ne $latestSourceFile) { $latestSourceFile.FullName } else { $null }; latestModifiedUtc=$latestSource.ToString('o'); sourceFiles=$sourceFiles.Count; scope='All Assets files (including shader/png/meta/font), ProjectSettings and package manifests' }
    artifacts=$artifacts
    automaticChecks=$rows; automaticPassed=@($rows | Where-Object passed).Count; automaticTotal=$rows.Count
    commerciallyApproved=$false
    manualGates=@('External first-session playtests','Full-session pacing and content repetition','Audio and visual polish review','Target hardware and widescreen QA','Art/font commercial provenance and credits','Steam store materials, content survey and build review')
    note='Simulation win counts are diagnostic, not player win rates. Grooming fit coverage verifies actual rendering and unchanged state, not visual quality. Passing automatic checks does not establish Steam release readiness.'
}
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $output 'quality-status.json') -Encoding UTF8
$rows | Format-Table id,passed,detail -Wrap -AutoSize
Write-Output ('Automatic checks: '+$report.automaticPassed+'/'+$report.automaticTotal+'. Manual release gates remain open.')
if ($RequirePass -and $report.automaticPassed -ne $report.automaticTotal) { exit 1 }
