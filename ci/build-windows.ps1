[CmdletBinding()]
param(
    [string]$UnityEditorPath = ''
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3.0

function Require-File {
    param([string]$Path, [string]$Description)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Missing $Description at '$Path'."
    }
}

function Show-Tail {
    param([string]$Path)
    if (Test-Path -LiteralPath $Path -PathType Leaf) {
        Write-Host "--- tail: $Path ---"
        Get-Content -LiteralPath $Path -Tail 35
        Write-Host "--- end tail ---"
    }
}

function Invoke-Unity {
    param(
        [string]$Executable,
        [string[]]$Arguments,
        [string]$LogPath,
        [string]$Label
    )

    Write-Host "Running $Label"
    $quoted = $Arguments | ForEach-Object { '"' + $_ + '"' }
    $process = Start-Process -FilePath $Executable -ArgumentList ($quoted -join ' ') -WindowStyle Hidden -Wait -PassThru
    $code = $process.ExitCode
    if ($code -ne 0) {
        Show-Tail -Path $LogPath
        throw "$Label failed with exit code $code."
    }
}

$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$versionFile = Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt'
Require-File -Path $versionFile -Description 'Unity project version'

$unityVersionLine = Select-String -LiteralPath $versionFile -Pattern '^m_EditorVersion:\s*(\S+)' | Select-Object -First 1
if ($null -eq $unityVersionLine) {
    throw "Could not read the Unity editor version from '$versionFile'."
}
$unityVersion = $unityVersionLine.Matches[0].Groups[1].Value
if ([string]::IsNullOrWhiteSpace($unityVersion)) {
    throw "Unity editor version is empty in '$versionFile'."
}

$unityCandidates = New-Object System.Collections.Generic.List[string]
if (-not [string]::IsNullOrWhiteSpace($UnityEditorPath)) {
    [void]$unityCandidates.Add($UnityEditorPath)
}
if (-not [string]::IsNullOrWhiteSpace($env:UNITY_EDITOR)) {
    [void]$unityCandidates.Add($env:UNITY_EDITOR)
}
[void]$unityCandidates.Add("C:\Program Files\Unity\Hub\Editor\$unityVersion\Editor\Unity.exe")
[void]$unityCandidates.Add("C:\Program Files\Unity Hub\Editor\$unityVersion\Editor\Unity.exe")
$unity = $unityCandidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($unity)) {
    throw "Unity $unityVersion was not found. Set UNITY_EDITOR to the full path of Unity.exe."
}
Write-Host "Project: $projectRoot"
Write-Host "Unity:   $unity"

$runnerTemp = $env:RUNNER_TEMP
if ([string]::IsNullOrWhiteSpace($runnerTemp)) {
    $runnerTemp = [IO.Path]::GetTempPath()
}
$jobRoot = Join-Path $runnerTemp ("burst-parade-" + [Guid]::NewGuid().ToString('N'))
$playerRoot = Join-Path $jobRoot 'player'
$qaRoot = Join-Path $jobRoot 'qa'
$buildLog = Join-Path $jobRoot 'unity-build.log'
$qaLog = Join-Path $jobRoot 'qa.log'
$resumeLog = Join-Path $jobRoot 'qa-resume.log'
$playerExe = Join-Path $playerRoot 'BurstParade.exe'

New-Item -ItemType Directory -Path $playerRoot -Force | Out-Null
New-Item -ItemType Directory -Path $qaRoot -Force | Out-Null
if (Test-Path -LiteralPath $playerExe) {
    throw "Fresh build directory unexpectedly already contains '$playerExe'."
}

$previousBuildPathPresent = Test-Path -LiteralPath 'Env:PARADE_BUILD_PATH'
$previousBuildPath = $null
if ($previousBuildPathPresent) {
    $previousBuildPath = $env:PARADE_BUILD_PATH
}

try {
    $env:PARADE_BUILD_PATH = $playerExe
    $buildArgs = @(
        '-batchmode',
        '-nographics',
        '-quit',
        '-projectPath', $projectRoot,
        '-executeMethod', 'BuildParade.Build',
        '-logFile', $buildLog
    )
    Invoke-Unity -Executable $unity -Arguments $buildArgs -LogPath $buildLog -Label 'Unity Windows build'
}
finally {
    if ($previousBuildPathPresent) {
        $env:PARADE_BUILD_PATH = $previousBuildPath
    }
    else {
        Remove-Item -LiteralPath 'Env:PARADE_BUILD_PATH' -ErrorAction SilentlyContinue
    }
}

Require-File -Path $buildLog -Description 'Unity build log'
if (-not (Select-String -LiteralPath $buildLog -Pattern 'PARADE_BUILD_OK' -SimpleMatch -Quiet)) {
    Show-Tail -Path $buildLog
    throw "Unity returned success without the PARADE_BUILD_OK marker."
}
Require-File -Path $playerExe -Description 'fresh Windows player executable'
$playerInfo = Get-Item -LiteralPath $playerExe
if ($playerInfo.Length -le 0) {
    throw "The freshly built executable is empty."
}
if (-not (Test-Path -LiteralPath (Join-Path $playerRoot 'BurstParade_Data') -PathType Container)) {
    throw "The fresh Windows player is missing BurstParade_Data."
}

$qaArgs = @(
    '-paradeQA',
    '-paradeOut', $qaRoot,
    '-screen-width', '1280',
    '-screen-height', '720',
    '-screen-fullscreen', '0',
    '-force-d3d11',
    '-logFile', $qaLog
)
Invoke-Unity -Executable $playerExe -Arguments $qaArgs -LogPath $qaLog -Label 'Windows integration QA'
Require-File -Path $qaLog -Description 'Windows integration QA log'
if (-not (Select-String -LiteralPath $qaLog -Pattern 'PARADE_QA PASS' -SimpleMatch -Quiet)) {
    Show-Tail -Path $qaLog
    throw 'Windows integration QA completed without PARADE_QA PASS.'
}

$resumeArgs = @(
    '-paradeQA',
    '-paradeResume',
    '-paradeOut', $qaRoot,
    '-screen-fullscreen', '0',
    '-logFile', $resumeLog
)
Invoke-Unity -Executable $playerExe -Arguments $resumeArgs -LogPath $resumeLog -Label 'Windows save-resume QA'
Require-File -Path $resumeLog -Description 'Windows save-resume QA log'
if (-not (Select-String -LiteralPath $resumeLog -Pattern 'PARADE_QA resume=PASS' -SimpleMatch -Quiet)) {
    Show-Tail -Path $resumeLog
    throw 'Windows save-resume QA completed without PARADE_QA resume=PASS.'
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$distRoot = Join-Path $projectRoot 'dist'
New-Item -ItemType Directory -Path $distRoot -Force | Out-Null
$archivePath = Join-Path $distRoot 'BurstParade-Windows-x64.zip'
$archiveTemp = Join-Path $jobRoot 'BurstParade-Windows-x64.zip'
$sumsPath = Join-Path $distRoot 'SHA256SUMS.txt'

$zip = [IO.Compression.ZipFile]::Open($archiveTemp, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in Get-ChildItem -LiteralPath $playerRoot -Recurse -File) {
        $relative = $file.FullName.Substring($playerRoot.Length + 1).Replace('\', '/')
        [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $file.FullName, $relative, [IO.Compression.CompressionLevel]::Optimal)
    }
} finally {
    $zip.Dispose()
}
Require-File -Path $archiveTemp -Description 'fresh Windows archive'
$archive = [IO.Compression.ZipFile]::OpenRead($archiveTemp)
try {
    if ($null -eq $archive.GetEntry('BurstParade.exe')) {
        throw 'The Windows archive does not contain BurstParade.exe at its root.'
    }
    if ($null -eq ($archive.Entries | Where-Object { $_.FullName -like 'BurstParade_Data/*' } | Select-Object -First 1)) {
        throw 'The Windows archive does not contain BurstParade_Data at its root.'
    }
}
finally {
    $archive.Dispose()
}

Move-Item -LiteralPath $archiveTemp -Destination $archivePath -Force
$hash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToUpperInvariant()
[IO.File]::WriteAllText($sumsPath, "$hash  BurstParade-Windows-x64.zip`n", [Text.Encoding]::ASCII)

Write-Host "Release archive: $archivePath"
Write-Host "SHA256:          $hash"
Write-Host "QA logs:         $qaLog and $resumeLog"
