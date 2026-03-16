$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root '20min20s\20min20s.csproj'

dotnet restore $project /p:Configuration=Debug /p:Platform=AnyCPU
dotnet msbuild $project /t:Build /p:Configuration=Debug /p:Platform=AnyCPU /v:minimal
