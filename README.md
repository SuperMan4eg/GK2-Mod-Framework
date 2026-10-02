# GK2 Mod Framework 0.1.17

GK2 Mod Framework is a shared foundation for **Graveyard Keeper 2** code mods. It runs on BepInEx 5 and adds an in-game Mods menu, reusable settings, mod metadata, dependency checks, lifecycle events, and game-build compatibility reporting.

The framework does not change gameplay by itself. It does not replace BepInEx, load DLL files independently, download mods, or modify the game's managed assemblies.

Nexus Mods: https://www.nexusmods.com/graveyardkeeper2/mods/42

Source code: https://github.com/SuperMan4eg/GK2-Mod-Framework

The repository tracks current development. Use the Nexus Mods page for packaged release builds.

The published release is **0.1.18**. Existing-entry settings adoption and version-suffix support listed under **Unreleased** are source changes for a future release; they are not included in the published 0.1.18 download.

## Requirements

- Graveyard Keeper 2 for Windows x64
- Known full-release Steam fingerprints: `25457344`, `25467846`, `25506711`
- Structural unknown-build path verified on Steam builds `25509347` and `25533739`
- Demo build `25344626` remains supported
- BepInEx `5.4.23.5` x64

Mods that require a known build remain fail-closed by default after future game updates. Starting with Framework 0.1.11, a mod that explicitly validates its own required game-side API contract can confirm that contract during registration and continue running on an otherwise unknown whole-assembly fingerprint.

## Installation

1. Install BepInEx 5.4.23.5 x64 in the Graveyard Keeper 2 folder.
2. Start the game once, then close it.
3. Copy the archive contents into the game folder. The DLL must end up at `BepInEx/plugins/GK2.Framework.dll`.
4. Start the game. The main menu should contain a **Mods** button. Starting with 0.1.12, the in-game ESC/pause menu also contains a **Mods** button.

To verify loading, open `BepInEx/LogOutput.log` and look for `GK2_FRAMEWORK_READY`.

## Updating and uninstalling

To update, close the game and extract the new archive into the game folder, allowing it to replace the existing Framework files. Starting with 0.1.8, the archive includes localization files under `BepInEx/plugins/GK2.Framework/Localization`, so updating only `GK2.Framework.dll` is not sufficient. Existing BepInEx configuration files are preserved.

To uninstall, close the game and remove `BepInEx/plugins/GK2.Framework.dll` and the `BepInEx/plugins/GK2.Framework` folder. Mods that require the framework will no longer load. Their configuration files and save data are not removed automatically.

## Player features

- Native-style **Mods** window available from both the main menu and the in-game ESC/pause menu
- Responsive safe-area fitting for the Mods window and Framework-hosted mod Settings pages
- Explicit gamepad navigation between the mod list and the selected mod's Enable/Disable and Settings controls, including full navigation inside mod Settings pages
- Dedicated **Framework Settings** gear button in the Mods menu
- Framework window scale from 50% to 100% available directly in the Framework Settings UI
- Installed framework mod list and detailed metadata
- Fixed search fields above the mod list and settings: filter while typing, clear with ×, and use the on-screen keyboard with a controller. Mod search includes names and IDs; settings search includes names, descriptions, categories and keys. Search ignores letter case and does not change saved settings. Returning from settings preserves the mod search; opening settings starts a fresh settings search.
- Compatibility and dependency status
- **Copy report** on a selected mod copies its version, current compatibility status, game and Unity versions, and game assembly fingerprint for support requests. Review the clipboard text before sharing it.
- Opt-in structural compatibility checks so validated mods can survive unrelated game updates without waiting for a new whole-assembly fingerprint
- Runtime enable/disable for mods that explicitly support it
- Restart-required state for mods that cannot be toggled safely at runtime
- Optional integration mode for standalone mods that should not expose a framework-owned Enable/Disable control
- Automatically generated controls for toggles, integer and float sliders, dropdowns, keybinds, editable text, non-persistent action buttons, and read-only values
- Integer and float sliders include a synchronized exact-number field for values that are difficult to hit precisely with the slider; typed values still obey each setting's min/max/step rules
- Controller text/numeric editing uses a Framework-owned on-screen keyboard with commit/cancel and focus restoration, without depending on Steam Overlay
- Conditional settings can hide irrelevant rows or keep them visible but disabled/grayed out while preserving stored values
- UTF-8 JSON localization files for Framework UI and dependent mods; Framework-owned Mods UI can resolve registered mod names/descriptions, section headings and setting names/descriptions from each mod's catalog at render time; complete Framework catalogs are included for English, Bulgarian, German, Spanish, French, Russian, Korean, and Simplified Chinese
- A confirmation-protected **Quit to apply** action for pending changes that require a restart, available from both the main menu and the in-game pause menu
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

- Framework 0.1.18 mod/settings search passed runtime checks on Steam build `25601286` and owner acceptance. The release startup was checked with published GK2 Tweak Pack 0.1.6 and GK2 Ultrawide Fix 0.1.6. This does not certify every mod or controller configuration.
- Whole-assembly fingerprints are verified for full-release Steam builds `25457344`, `25467846`, and `25506711`, Unity `6000.3.9f1`, Mono x64. Framework 0.1.11's targeted unknown-build path was runtime-tested on Steam build `25509347` and again on build `25533739` (Assembly-CSharp SHA-256 `7ACB243A08897D8CC7B67EF17AED50EEA17857494AEF4278F4F3B00D324823E5`): the global fingerprint remained `Unknown`, while current consumer mods that validate their structural contracts registered `Compatible`.
- Framework 0.1.16 menu startup and Copy report were tested on Steam build `25601286` (Assembly-CSharp SHA-256 `37A17C02B53F9BF62ABE76398CED3BCB847774EB5AC12353CF32D8B6044E1134`). The global build fingerprint remains `Unknown`; this focused check does not establish full compatibility for every mod on that build.
- Backward compatibility was also rechecked on Demo build `25344626`.
- Mouse/physical-keyboard input is verified, including visible caret/edit state for text and exact numeric fields. Gamepad-mode navigation is runtime-tested from the mod list through per-mod Settings controls, including toggles, sliders, exact numeric entry, dropdowns, keybinds, text entry, action buttons, per-row Reset paths, Reset/rebuild focus, conditional hidden/disabled rows, Settings-page return focus, and the scaled `1600x900` path. Text and numeric entry from a controller uses a Framework-owned on-screen keyboard with commit/cancel and focus restoration, so it does not depend on Steam Overlay. The controller keyboard currently provides a Latin text layout; entering text in other writing systems requires a physical keyboard. Automated probes use the game's real navigation controller/input mode, and the 0.1.13 controller keyboard/settings path also passed owner physical-controller testing; broader device/mapping coverage remains useful.
- Responsive fitting is runtime-tested at `1600x900`; fit calculations are also regression-tested for `1366x768`, `1280x720`, `1920x1080`, and `3840x2160`.
- Runtime disabling is only safe when a mod completely reverses its own patches, subscriptions, and changes.
- The framework does not resolve or load BepInEx plugin DLLs. BepInEx remains responsible for plugin loading.
- Starting with 0.1.12, release ZIP entries carry portable Unix metadata (`0755` directories, `0644` regular files) to avoid permission problems when extracting under Linux/Steam Proton. The archive metadata and normal-user extraction path are regression-tested; full gameplay under Proton still depends on the user's BepInEx/Proton setup.
- There is no mod downloader, automatic updater, DLL hot reload, or file manager.
- English is the built-in fallback language. Framework ships complete English, Russian, Korean, Bulgarian, French, Simplified Chinese, German (`de`), and Spanish (`es`) catalogs for Framework-owned UI. Registered mod metadata, section headings, and setting names/descriptions can also be localized when a mod supplies standard catalog keys. Framework can load UTF-8 JSON language files for integrated mods through the public localization API. Framework-created UI uses the game's native Korean, Japanese, and Chinese TMP font assets when those languages are active. Reopening Mods after a language change rebuilds the window with the current text and font. Individual mods remain responsible for localizing and refreshing their own UI.

## Troubleshooting

**The Mods button is missing:** confirm that BepInEx loaded and that `GK2_FRAMEWORK_READY` appears in `BepInEx/LogOutput.log`.

**A mod does not appear:** the Mods list only contains mods that register with GK2 Mod Framework. A standalone BepInEx mod can still run normally but will not appear unless you installed its Framework-compatible build or an optional Framework bridge. Check `BepInEx/LogOutput.log` for `Registered GK2 mod [<id>]`; if that line is missing, the loaded plugin is not integrated with the Framework.

**A mod shows Unknown Build or Incompatible:** open its details in the Mods menu. Mods that implement the Framework 0.1.11 structural compatibility contract may continue running on an unknown whole-assembly fingerprint after validating the exact game API they use. Mods without that opt-in check remain blocked; do not force-enable them.

**A mod shows Faulted or a dependency error:** read the reason in the details panel and inspect `BepInEx/LogOutput.log`. Include the game build, framework version, and relevant log section when reporting a problem.

**The Mods window is larger than you prefer:** automatic fitting requires no configuration. To make Framework-hosted windows even smaller, open **Mods -> Framework Settings** and lower **Window scale (%)**. The default value `100` still shrinks automatically when needed to keep the window inside the current safe area. The same value remains stored in `BepInEx/config/ru.superman4eg.gk2.framework.cfg`.

Do not install development outputs such as PDB files, `bin`/`obj` folders, diagnostic DLLs, or extra copies of BepInEx/Harmony libraries.

## License

GK2 Mod Framework is distributed under the MIT License. See `LICENSE`.

Spanish Framework UI translation was contributed by a community member. The contributor name was not supplied.
