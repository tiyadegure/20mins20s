param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $root
$solution = Join-Path $root '20min20s.sln'
$updater = Join-Path $root '20min20sUp\20min20sUp.csproj'
$appOutput = Join-Path $root "20min20s\bin\$Configuration"
$updaterOutput = Join-Path $root "20min20sUp\bin\$Configuration"
$dist = Join-Path $repoRoot 'dist'
$distExe = Join-Path $dist '20min20s.exe'
$assemblyInfo = Join-Path $root '20min20s\Properties\AssemblyInfo.cs'
$versionMatch = Select-String -Path $assemblyInfo -Pattern '^\s*\[assembly:\s*AssemblyVersion\("([0-9.]+)"\)\]' | Select-Object -First 1
if (-not $versionMatch) {
    throw "Could not determine version from $assemblyInfo"
}
$version = $versionMatch.Matches[0].Groups[1].Value
$zipPath = Join-Path $dist "20min20s-windows-$version.zip"

dotnet restore $solution /p:Configuration=$Configuration /p:Platform='Any CPU'
dotnet build $solution -c $Configuration
dotnet restore $updater /p:Configuration=$Configuration /p:Platform='Any CPU'
dotnet build $updater -c $Configuration

New-Item -ItemType Directory -Force $dist | Out-Null
Copy-Item (Join-Path $appOutput '20min20s.exe') $distExe -Force

if (Test-Path (Join-Path $updaterOutput '20min20sUp.exe')) {
    Copy-Item (Join-Path $updaterOutput '20min20sUp.exe') (Join-Path $appOutput '20min20sUp.exe') -Force
}

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($appOutput, $zipPath)

Write-Output "Built $Configuration package:"
Write-Output "  EXE: $distExe"
Write-Output "  ZIP: $zipPath"
