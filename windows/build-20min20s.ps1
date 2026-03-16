$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$solution = Join-Path $root '20min20s.sln'
$updater = Join-Path $root '20min20sUp\20min20sUp.csproj'

dotnet restore $solution /p:Configuration=Debug /p:Platform="Any CPU"
dotnet msbuild $solution /t:Build /p:Configuration=Debug /p:Platform="Any CPU" /v:minimal
dotnet restore $updater /p:Configuration=Debug /p:Platform="Any CPU"
dotnet msbuild $updater /t:Build /p:Configuration=Debug /p:Platform="Any CPU" /v:minimal
