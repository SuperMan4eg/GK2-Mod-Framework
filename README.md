# GK2 Mod Framework 0.1.11

GK2 Mod Framework is a shared foundation for **Graveyard Keeper 2** code mods. It runs on BepInEx 5 and adds an in-game Mods menu, reusable settings, mod metadata, dependency checks, lifecycle events, and game-build compatibility reporting.

The framework does not change gameplay by itself. It does not replace BepInEx, load DLL files independently, download mods, or modify the game's managed assemblies.

Nexus Mods: https://www.nexusmods.com/graveyardkeeper2/mods/42

Source code: https://github.com/SuperMan4eg/GK2-Mod-Framework

The repository tracks current development. Use the Nexus Mods page for packaged release builds.

## Requirements

- Graveyard Keeper 2 for Windows x64
- Known full-release Steam fingerprints: `25457344`, `25467846`, `25506711`
- Structural unknown-build path verified on Steam build `25509347`
- Demo build `25344626` remains supported
- BepInEx `5.4.23.5` x64

Mods that require a known build remain fail-closed by default after future game updates. Starting with Framework 0.1.11, a mod that explicitly validates its own required game-side API contract can confirm that contract during registration and continue running on an otherwise unknown whole-assembly fingerprint.

## Installation

1. Install BepInEx 5.4.23.5 x64 in the Graveyard Keeper 2 folder.
2. Start the game once, then close it.
3. Copy the archive contents into the game folder. The DLL must end up at `BepInEx/plugins/GK2.Framework.dll`.
4. Start the game. The main menu should contain a **Mods** button.

To verify loading, open `BepInEx/LogOutput.log` and look for `GK2_FRAMEWORK_READY`.

## Updating and uninstalling

To update, close the game and extract the new archive into the game folder, allowing it to replace the existing Framework files. Starting with 0.1.8, the archive includes localization files under `BepInEx/plugins/GK2.Framework/Localization`, so updating only `GK2.Framework.dll` is not sufficient. Existing BepInEx configuration files are preserved.

To uninstall, close the game and remove `BepInEx/plugins/GK2.Framework.dll` and the `BepInEx/plugins/GK2.Framework` folder. Mods that require the framework will no longer load. Their configuration files and save data are not removed automatically.

## Player features

- Native-style **Mods** window in the main menu
- Responsive safe-area fitting for the Mods window and Framework-hosted mod Settings pages
- Dedicated **Framework Settings** gear button in the Mods menu
- Framework window scale from 50% to 100% available directly in the Framework Settings UI
- Installed framework mod list and detailed metadata
- Compatibility and dependency status
- Opt-in structural compatibility checks so validated mods can survive unrelated game updates without waiting for a new whole-assembly fingerprint
- Runtime enable/disable for mods that explicitly support it
- Restart-required state for mods that cannot be toggled safely at runtime
- Optional integration mode for standalone mods that should not expose a framework-owned Enable/Disable control
- Automatically generated controls for toggles, integer and float sliders, dropdowns, keybinds, text fields, and read-only values
- UTF-8 JSON localization files for Framework UI and opt-in localization in dependent mods; complete Framework catalogs are included for English, Russian, and Korean
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

- Whole-assembly fingerprints are verified for full-release Steam builds `25457344`, `25467846`, and `25506711`, Unity `6000.3.9f1`, Mono x64. Framework 0.1.11's targeted unknown-build path was additionally runtime-tested on Steam build `25509347`, whose Assembly-CSharp hash changed while its decompiled C# remained identical to `25506711`.
- Backward compatibility was also rechecked on Demo build `25344626`.
- Mouse input is verified. The gamepad-mode Mods menu open path and directional navigation are runtime-tested, including the scaled `1600x900` path. Physical controller hardware is not part of the automated test.
- Responsive fitting is runtime-tested at `1600x900`; fit calculations are also regression-tested for `1366x768`, `1280x720`, `1920x1080`, and `3840x2160`.
- Runtime disabling is only safe when a mod completely reverses its own patches, subscriptions, and changes.
- The framework does not resolve or load BepInEx plugin DLLs. BepInEx remains responsible for plugin loading.
- There is no mod downloader, automatic updater, DLL hot reload, or file manager.
- English is the built-in fallback language. Framework 0.1.9+ ships complete English, Russian, and Korean catalogs for Framework-owned UI and can load UTF-8 JSON language files for integrated mods that explicitly use the localization API. Framework 0.1.9+ also resolves the game's native Korean, Japanese, and Chinese TMP font assets for Framework-created UI so CJK translations render with the glyph atlases shipped by the game. Framework 0.1.11 rebuilds its persistent Mods UI after a game-language change so translated labels and native language fonts stay synchronized when switching languages without restarting the game. Individual mods remain responsible for choosing which of their own strings are localized and for rendering text in mod-owned UI.

## Troubleshooting

**The Mods button is missing:** confirm that BepInEx loaded and that `GK2_FRAMEWORK_READY` appears in `BepInEx/LogOutput.log`.

**A mod does not appear:** the Mods list only contains mods that register with GK2 Mod Framework. A standalone BepInEx mod can still run normally but will not appear unless you installed its Framework-compatible build or an optional Framework bridge. Check `BepInEx/LogOutput.log` for `Registered GK2 mod [<id>]`; if that line is missing, the loaded plugin is not integrated with the Framework.

**A mod shows Unknown Build or Incompatible:** open its details in the Mods menu. Mods that implement the Framework 0.1.11 structural compatibility contract may continue running on an unknown whole-assembly fingerprint after validating the exact game API they use. Mods without that opt-in check remain blocked; do not force-enable them.

**A mod shows Faulted or a dependency error:** read the reason in the details panel and inspect `BepInEx/LogOutput.log`. Include the game build, framework version, and relevant log section when reporting a problem.

**The Mods window is larger than you prefer:** automatic fitting requires no configuration. To make Framework-hosted windows even smaller, open **Mods -> Framework Settings** and lower **Window scale (%)**. The default value `100` still shrinks automatically when needed to keep the window inside the current safe area. The same value remains stored in `BepInEx/config/ru.superman4eg.gk2.framework.cfg`.

Do not install development outputs such as PDB files, `bin`/`obj` folders, diagnostic DLLs, or extra copies of BepInEx/Harmony libraries.

## License

GK2 Mod Framework is distributed under the MIT License. See `LICENSE`.
