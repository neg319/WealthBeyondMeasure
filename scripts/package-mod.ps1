$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$modRoot = Join-Path $repoRoot 'WealthBeyondMeasure'
$manifestPath = Join-Path $modRoot 'About/Manifest.xml'
$dllPath = Join-Path $modRoot 'Assemblies/WealthBeyondMeasure.dll'
$outDir = Join-Path $repoRoot 'out'
$stageRoot = Join-Path $outDir 'stage'
$stageModRoot = Join-Path $stageRoot 'WealthBeyondMeasure'

if (-not (Test-Path $manifestPath)) {
    throw "Manifest not found at $manifestPath"
}

if (-not (Test-Path $dllPath)) {
    throw "Compiled mod assembly not found at $dllPath"
}

[xml]$manifest = Get-Content -Path $manifestPath
$version = $manifest.Manifest.version
if (-not $version) {
    throw 'Unable to read version from About/Manifest.xml'
}

Remove-Item -Recurse -Force $stageRoot -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $stageModRoot | Out-Null
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$copyItems = @('About', 'Assemblies', 'Defs', 'Languages', 'Textures', 'README.md')
foreach ($item in $copyItems) {
    Copy-Item -Path (Join-Path $modRoot $item) -Destination $stageModRoot -Recurse -Force
}

Get-ChildItem -Path (Join-Path $stageModRoot 'Assemblies') -Filter '*.pdb' -ErrorAction SilentlyContinue | Remove-Item -Force
Get-ChildItem -Path (Join-Path $stageModRoot 'Assemblies') -Filter '.gitkeep' -ErrorAction SilentlyContinue | Remove-Item -Force

$zipPath = Join-Path $outDir ("WealthBeyondMeasure-v{0}.zip" -f $version)
Remove-Item -Force $zipPath -ErrorAction SilentlyContinue
Compress-Archive -Path $stageModRoot -DestinationPath $zipPath -CompressionLevel Optimal

$hash = (Get-FileHash -Algorithm SHA256 -Path $zipPath).Hash
Set-Content -Path ($zipPath + '.sha256.txt') -Value $hash

Write-Host "Packaged mod zip: $zipPath"
