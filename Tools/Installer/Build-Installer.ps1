param([switch]$SkipGameBuild)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$unity = 'C:/Program Files/Unity/Hub/Editor/2022.3.62f3c1/Editor/Unity.exe'
$compiler = Join-Path $env:LOCALAPPDATA 'Programs/Inno Setup 6/ISCC.exe'
if (!(Test-Path $compiler)) { $compiler = 'C:/Program Files (x86)/Inno Setup 6/ISCC.exe' }
if (!(Test-Path $compiler)) { throw 'Install Inno Setup 6 before packaging.' }
if (!$SkipGameBuild) {
    $buildLog = Join-Path $projectRoot 'Logs/installer-build.log'
    $process = Start-Process $unity -ArgumentList "-batchmode -nographics -projectPath `"$projectRoot`" -executeMethod Minglu.EditorTools.InstitutionVerification.BatchDeliver -logFile `"$buildLog`"" -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "Unity build failed. See $buildLog" }
}
& $compiler (Join-Path $PSScriptRoot 'Minglu.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
Write-Output (Join-Path $projectRoot 'Builds/Installer/MingluSetup.exe')
