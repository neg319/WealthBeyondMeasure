# Wealth Beyond Measure

GitHub ready source repository for the RimWorld 1.6 mod **Wealth Beyond Measure** by Johnny Berry / Vyberware.

This repo is set up to:
- build the DLL with GitHub Actions
- package a clean mod zip automatically
- upload the packaged mod as a workflow artifact
- create a GitHub release zip automatically when you push a tag like `v1.14.8`

The playable mod content lives in `WealthBeyondMeasure/`.

## One time setup

RimWorld reference DLLs are required to compile the mod, but they should not be redistributed in a public repo.

You have two workable options:

### Option A: private repo
Place these files in the repo root `Libs/` folder:
- `Assembly-CSharp.dll`
- `UnityEngine.CoreModule.dll`
- `UnityEngine.IMGUIModule.dll`
- `UnityEngine.TextRenderingModule.dll`
- `UnityEngine.AssetBundleModule.dll`

### Option B: GitHub Secrets
Add either:
- one secret named `RW_MANAGED_ZIP_B64` containing a base64 encoded zip with the five DLLs above

or these individual secrets:
- `RW_ASSEMBLY_CSHARP_B64`
- `RW_UNITYENGINE_COREMODULE_B64`
- `RW_UNITYENGINE_IMGUIMODULE_B64`
- `RW_UNITYENGINE_TEXTRENDERINGMODULE_B64`
- `RW_UNITYENGINE_ASSETBUNDLEMODULE_B64`

## GitHub Actions behavior

- Pushes to `main` or `master` build and package the mod.
- Pull requests build and package the mod.
- Manual runs are available through **Run workflow**.
- Tags matching `v*` build, package, and publish a release zip.

The finished release zip is placed in `out/` during the workflow and uploaded as an artifact.
