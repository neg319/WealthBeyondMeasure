$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$libsDir = Join-Path $repoRoot 'Libs'
New-Item -ItemType Directory -Force -Path $libsDir | Out-Null

$requiredFiles = @(
    'Assembly-CSharp.dll',
    'UnityEngine.CoreModule.dll',
    'UnityEngine.IMGUIModule.dll',
    'UnityEngine.TextRenderingModule.dll',
    'UnityEngine.AssetBundleModule.dll'
)

function Test-AllReferencesPresent {
    foreach ($file in $requiredFiles) {
        if (-not (Test-Path (Join-Path $libsDir $file))) {
            return $false
        }
    }
    return $true
}

if (Test-AllReferencesPresent) {
    Write-Host 'RimWorld references already present in Libs/.'
    exit 0
}

if ($env:RW_MANAGED_ZIP_B64) {
    $zipPath = Join-Path $env:RUNNER_TEMP 'rimworld-managed.zip'
    [IO.File]::WriteAllBytes($zipPath, [Convert]::FromBase64String($env:RW_MANAGED_ZIP_B64))
    Expand-Archive -Path $zipPath -DestinationPath $libsDir -Force
}
else {
    $mapping = @{
        'RW_ASSEMBLY_CSHARP_B64' = 'Assembly-CSharp.dll'
        'RW_UNITYENGINE_COREMODULE_B64' = 'UnityEngine.CoreModule.dll'
        'RW_UNITYENGINE_IMGUIMODULE_B64' = 'UnityEngine.IMGUIModule.dll'
        'RW_UNITYENGINE_TEXTRENDERINGMODULE_B64' = 'UnityEngine.TextRenderingModule.dll'
        'RW_UNITYENGINE_ASSETBUNDLEMODULE_B64' = 'UnityEngine.AssetBundleModule.dll'
    }

    foreach ($secretName in $mapping.Keys) {
        $value = [Environment]::GetEnvironmentVariable($secretName)
        if ($value) {
            $outPath = Join-Path $libsDir $mapping[$secretName]
            [IO.File]::WriteAllBytes($outPath, [Convert]::FromBase64String($value))
        }
    }
}

if (-not (Test-AllReferencesPresent)) {
    throw @'
Missing RimWorld reference DLLs.

Provide them in one of these ways:
- put the required DLLs in the repo root Libs/ folder for a private repository build
- add a GitHub secret named RW_MANAGED_ZIP_B64 with a base64 encoded zip containing the five DLLs
- add the individual DLL secrets documented in README.md

Do not publish proprietary RimWorld DLLs in a public repository.
'@
}

Write-Host 'RimWorld references restored successfully.'
