[CmdletBinding()]
param(
    [ValidateSet('win-x64', 'win-arm64')]
    [string]$Runtime = 'win-x64',
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$artifacts = Join-Path $root 'artifacts'
$publish = Join-Path $artifacts 'publish'

if (-not $SkipTests) {
    dotnet test (Join-Path $root 'FolderColorizer.slnx') --configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw 'Tests failed.'
    }
}

dotnet publish (Join-Path $root 'src\FolderColorizer\FolderColorizer.csproj') `
    --configuration Release `
    --runtime $Runtime `
    --self-contained true `
    --output $publish `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:PublishTrimmed=false

if ($LASTEXITCODE -ne 0) {
    throw 'Publish failed.'
}

$compilerCandidates = @(
    (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
    (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe'),
    (Get-Command ISCC.exe -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source)
) | Where-Object { $_ -and (Test-Path -LiteralPath $_) }

$compiler = $compilerCandidates | Select-Object -First 1
if (-not $compiler) {
    throw 'Inno Setup 6 was not found. Install it from https://jrsoftware.org/isdl.php and run this script again.'
}

& $compiler (Join-Path $root 'installer\FolderColorizer.iss')
if ($LASTEXITCODE -ne 0) {
    throw 'Installer compilation failed.'
}

Write-Host "Installer created in $artifacts\installer" -ForegroundColor Green
