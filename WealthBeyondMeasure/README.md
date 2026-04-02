# Wealth Beyond Measure

A vibe coded RimWorld 1.6 colony economy mod by Johnny Berry / Vyberware.

## v1.14.6

- updated the currency art with the new player supplied textures for Silver, Dollars, Gold, Caps, Septims, Drakes, and Cats
- updated the Credits texture with the corrected image
- GitHub Actions build and release pipeline added.
- Automatic artifact packaging added.
- Release tags now publish a ready to use mod zip.
- Build setup cleaned up for private references and public safe automation.

## What the mod does

Wealth Beyond Measure gives each colonist a personal place in your colony's economy.

Colonists earn pay based on the actual in game hours they spend doing work. They pick up that pay from a single Bank Vault on payday, keep their funds on them, and spend their own money on personal food and medicine. Poor colonists get stronger Community Assistance discounts, wealthy colonists pay closer to full price, and anyone can go into debt if they use more than they can afford.

Money does more than sit in an inventory stack. A colonist's wealth changes what they want to wear, what food they are happy eating, what weapons they prefer to carry, what kind of bed feels right, how they feel about payday, and how other colonists see them. Rich colonists tend to respect other rich colonists. Poor colonists are more comfortable around other poor colonists. Matching status creates stronger social bonds, while wide wealth gaps create friction.

The mod also treats families as households. Married colonists share wealth. Children inherit their parents' status until adulthood. High Maintenance and Frugal traits can push a pawn away from the expectations of their current rank. A visiting banker can be called to the settlement through the Bank Vault and will buy nearly anything with effectively unlimited funds. He arrives with a heavily armored guard carrying a marksman weapon and tied to the same visitor group so the escort stays friendly unless the banker or guard is attacked.

## Feature summary

- Personal wages tracked by active work type in decimal in game hours
- Per work type pay rates in mod settings
- Weekly payday with rounded payout collection
- Positive earnings and negative expenses tracked separately
- Community Assistance pricing from 10% to 100% based on wealth level
- Personal food and medicine costs with debt allowed when funds run low
- Exemptions for hauling, delivery, tending, cooking, and other colony task item use
- Shared household wealth for spouses
- Parent based wealth for children until adulthood
- Ten wealth ranks from Poor to Extremely Filthy Rich
- Wealth based apparel, food, weapon, bed, and comfort preferences
- Mood changes from payday results, meals, and lifestyle fit
- Wealth based social opinion bonuses, similarity bonuses, and class gap penalties
- High Maintenance and Frugal traits in pawn generation
- Bank Vault treasury building that is free to place, movable, rotatable, fireproof, and limited to one per map
- Bank Vault only stores colony currency at Critical priority
- Projected payday payout tracking on the Bank Vault
- Request Banker command for a trader style banker visit with a heavily armored marksman guard
- Currency display options for Silver, Dollars, Gold, Credits, Caps, Septims, Drakes, and Cats
- Matching currency appearance swaps for each currency option

## Bank Vault notes

The Bank Vault is the heart of the system.

It is a buildable treasury safe that only accepts colony currency. It defaults to Critical priority, cannot be used as a general storage container, is free to build, can be uninstalled and moved, is limited to one per map, and is set up to be fireproof and effectively indestructible along with the currency stored inside it.

## GitHub build and release

This repository is set up to build on GitHub Actions and package a clean RimWorld mod zip automatically.

### What the workflow does

- builds the DLL on pushes, pull requests, and manual runs
- packages a release ready mod zip that excludes source files and build only files
- uploads the finished zip as a workflow artifact
- creates a GitHub release automatically when you push a tag like `v1.14.6`

### RimWorld references

RimWorld and Unity reference DLLs are still required to compile the mod. Because those files are proprietary, the workflow is set up to use either a private `Libs/` folder or GitHub Secrets rather than asking you to publish them in a public repository.

Accepted setup options:

#### Private repo option
Place these files in the repo root `Libs/` folder:
- `Assembly-CSharp.dll`
- `UnityEngine.CoreModule.dll`
- `UnityEngine.IMGUIModule.dll`
- `UnityEngine.TextRenderingModule.dll`
- `UnityEngine.AssetBundleModule.dll`

#### GitHub Secrets option
Either add one secret named `RW_MANAGED_ZIP_B64` containing a base64 encoded zip with those files, or add these individual base64 secrets:
- `RW_ASSEMBLY_CSHARP_B64`
- `RW_UNITYENGINE_COREMODULE_B64`
- `RW_UNITYENGINE_IMGUIMODULE_B64`
- `RW_UNITYENGINE_TEXTRENDERINGMODULE_B64`
- `RW_UNITYENGINE_ASSETBUNDLEMODULE_B64`

### Workflow outputs

The workflow packages a zip named like this:

`WealthBeyondMeasure-v1.14.6.zip`

That zip includes the playable mod folder only:
- `About/`
- `Assemblies/`
- `Defs/`
- `Languages/`
- `Textures/`
- `README.md`
