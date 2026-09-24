[CmdletBinding()]
param(
    [string]$UnityEditor = 'D:\Unity\Editor\6000.6.2f1\Editor\Unity.exe'
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$buildRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot 'Builds\Competition-Demo'))
$packageName = 'YingYunDemo-Windows-x64'
$packageDir = [IO.Path]::GetFullPath((Join-Path $buildRoot $packageName))
$playerPath = Join-Path $packageDir 'YingYunDemo.exe'
$zipPath = Join-Path $buildRoot ($packageName + '.zip')
$hashPath = $zipPath + '.sha256'
$buildLog = Join-Path $projectRoot 'Logs\M10-competition-demo-build.log'

function Assert-ChildPath([string]$Path, [string]$Parent) {
    $normalizedParent = [IO.Path]::GetFullPath($Parent).TrimEnd('\') + '\'
    $normalizedPath = [IO.Path]::GetFullPath($Path)
    if (-not $normalizedPath.StartsWith($normalizedParent, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to modify path outside build root: $normalizedPath"
    }
}

if (-not (Test-Path -LiteralPath $UnityEditor -PathType Leaf)) {
    throw "Unity Editor not found: $UnityEditor"
}

New-Item -ItemType Directory -Force -Path $buildRoot | Out-Null
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $buildLog) | Out-Null
Assert-ChildPath -Path $packageDir -Parent $buildRoot
if (Test-Path -LiteralPath $packageDir) {
    Remove-Item -LiteralPath $packageDir -Recurse -Force
}
foreach ($artifact in @($zipPath, $hashPath)) {
    Assert-ChildPath -Path $artifact -Parent $buildRoot
    if (Test-Path -LiteralPath $artifact) {
        Remove-Item -LiteralPath $artifact -Force
    }
}
New-Item -ItemType Directory -Force -Path $packageDir | Out-Null

$unityArguments = "-batchmode -projectPath `"$projectRoot`" -buildWindows64Player `"$playerPath`" -logFile `"$buildLog`" -quit"
$unityProcess = Start-Process -FilePath $UnityEditor -ArgumentList $unityArguments -Wait -PassThru -WindowStyle Hidden
if ($unityProcess.ExitCode -ne 0) {
    throw "Unity build failed with exit code $($unityProcess.ExitCode). See $buildLog"
}

$requiredPaths = @(
    $playerPath,
    (Join-Path $packageDir 'YingYunDemo_Data'),
    (Join-Path $packageDir 'MonoBleedingEdge'),
    (Join-Path $packageDir 'UnityPlayer.dll'),
    (Join-Path $packageDir 'YingYunDemo_Data\StreamingAssets\YingYunBeatAnalyzer\Windows-x64\beat_this_cpp.exe'),
    (Join-Path $packageDir 'YingYunDemo_Data\StreamingAssets\YingYunBeatAnalyzer\Windows-x64\beat_this.onnx'),
    (Join-Path $packageDir 'YingYunDemo_Data\StreamingAssets\YingYunBeatAnalyzer\Windows-x64\msvcp140.dll'),
    (Join-Path $packageDir 'YingYunDemo_Data\StreamingAssets\YingYunBeatAnalyzer\Windows-x64\msvcp140_1.dll'),
    (Join-Path $packageDir 'YingYunDemo_Data\StreamingAssets\YingYunBeatAnalyzer\Windows-x64\vcruntime140.dll'),
    (Join-Path $packageDir 'YingYunDemo_Data\StreamingAssets\YingYunBeatAnalyzer\Windows-x64\vcruntime140_1.dll'),
    (Join-Path $packageDir 'YingYunDemo_Data\StreamingAssets\YingYunBeatAnalyzer\Windows-x64\Licenses\BeatThis-Model-MIT.txt')
)
foreach ($requiredPath in $requiredPaths) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required player file is missing: $requiredPath"
    }
}

$backupFolder = Join-Path $packageDir 'YingYunDemo_BackUpThisFolder_ButDontShipItWithYourGame'
if (Test-Path -LiteralPath $backupFolder) {
    Assert-ChildPath -Path $backupFolder -Parent $buildRoot
    Remove-Item -LiteralPath $backupFolder -Recurse -Force
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'CompetitionDemoREADME.txt') -Destination (Join-Path $packageDir 'README.txt')

$forbidden = Get-ChildItem -LiteralPath $packageDir -Recurse -Force | Where-Object {
    $_.Name -like '*BackUpThisFolder*' -or $_.Name -eq 'Logs' -or $_.Name -eq '.git'
}
if ($forbidden) {
    throw "Forbidden development-only files remain in package: $($forbidden.FullName -join ', ')"
}

Compress-Archive -LiteralPath $packageDir -DestinationPath $zipPath -CompressionLevel Optimal
$hash = Get-FileHash -LiteralPath $zipPath -Algorithm SHA256
"$($hash.Hash)  $([IO.Path]::GetFileName($zipPath))" | Set-Content -LiteralPath $hashPath -Encoding ascii

Write-Host "Package: $zipPath"
Write-Host "SHA-256: $($hash.Hash)"
