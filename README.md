# GK2 Mod Framework 0.1.4

GK2 Mod Framework is a shared foundation for **Graveyard Keeper 2** code mods. It runs on BepInEx 5 and adds an in-game Mods menu, reusable settings, mod metadata, dependency checks, lifecycle events, and game-build compatibility reporting.

The framework does not change gameplay by itself. It does not replace BepInEx, load DLL files independently, download mods, or modify the game's managed assemblies.

Nexus Mods: https://www.nexusmods.com/graveyardkeeper2/mods/42

## Requirements

- Graveyard Keeper 2 for Windows x64
- Verified full-release Steam builds: `25457344`, `25467846`
- Demo build `25344626` remains supported
- BepInEx `5.4.23.5` x64

Mods that require a known build may remain disabled after future game updates until that build is verified and added to the framework fingerprint list.

## Installation

1. Install BepInEx 5.4.23.5 x64 in the Graveyard Keeper 2 folder.
2. Start the game once, then close it.
3. Copy the archive contents into the game folder. The DLL must end up at `BepInEx/plugins/GK2.Framework.dll`.
4. Start the game. The main menu should contain a **Mods** button.

To verify loading, open `BepInEx/LogOutput.log` and look for `GK2_FRAMEWORK_READY`.

## Updating and uninstalling

To update, close the game and replace `BepInEx/plugins/GK2.Framework.dll`. Existing BepInEx configuration files are preserved.

To uninstall, close the game and remove that DLL. Mods that require the framework will no longer load. Their configuration files and save data are not removed automatically.

## Player features

- Native-style **Mods** window in the main menu
- Installed framework mod list and detailed metadata
- Compatibility and dependency status
- Runtime enable/disable for mods that explicitly support it
- Restart-required state for mods that cannot be toggled safely at runtime
- Optional integration mode for standalone mods that should not expose a framework-owned Enable/Disable control
- Automatically generated controls for toggles, integer and float sliders, dropdowns, keybinds, text fields, and read-only values
- Settings stored through BepInEx configuration files

## Creating a framework mod

The archive includes a buildable project in `Templates/GK2.Framework.ModTemplate`. Copy it into your development folder, assign a unique plugin ID, and update its metadata.

Register the mod from the BepInEx plugin's `Awake()` method:

```csharp
private void Awake()
{
    FrameworkApi.RegisterMod(new MyMod(), Config);
}
```

Use `Gk2ModContext.Settings` during `OnRegister` to create BepInEx-backed settings that appear automatically in the Mods menu. See `docs/NEW_MOD_GUIDE.md` for the normal hard-dependency workflow, `docs/OPTIONAL_INTEGRATION.md` for the tested standalone + optional bridge pattern, and `docs/PUBLIC_API.md` for the supported API.

## Compatibility and known limitations

- Verified on Graveyard Keeper 2 full-release Steam builds `25457344` and `25467846`, Unity `6000.3.9f1`, Mono x64.
- Backward compatibility was also rechecked on Demo build `25344626`.
- Mouse input is verified. Gamepad navigation is not verified.
- Runtime disabling is only safe when a mod completely reverses its own patches, subscriptions, and changes.
- The framework does not resolve or load BepInEx plugin DLLs. BepInEx remains responsible for plugin loading.
- There is no mod downloader, automatic updater, DLL hot reload, or file manager.
- English is the built-in fallback language. Mods are responsible for localizing their metadata and setting descriptions.

## Troubleshooting

**The Mods button is missing:** confirm that BepInEx loaded and that `GK2_FRAMEWORK_READY` appears in `BepInEx/LogOutput.log`.

**A mod does not appear:** check that its DLL is in `BepInEx/plugins`, it declares the framework as a hard BepInEx dependency, and its plugin ID is unique.

**A mod shows Unknown Build or Incompatible:** open its details in the Mods menu. Do not force-enable a mod that requires a known game build.

**A mod shows Faulted or a dependency error:** read the reason in the details panel and inspect `BepInEx/LogOutput.log`. Include the game build, framework version, and relevant log section when reporting a problem.

Do not install development outputs such as PDB files, `bin`/`obj` folders, diagnostic DLLs, or extra copies of BepInEx/Harmony libraries.

## License

GK2 Mod Framework is distributed under the MIT License. See `LICENSE`.
