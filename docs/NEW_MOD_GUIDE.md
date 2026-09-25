# Creating a mod with GK2 Mod Framework

This guide covers the minimum public workflow for a Graveyard Keeper 2 code mod.

## 1. Start from the template

Install BepInEx 5 and place the public `GK2.Framework.dll` in `BepInEx/plugins`. Copy `Templates/GK2.Framework.ModTemplate` into your development workspace.

Rename the project, assembly, root namespace, plugin class, `PluginGuid`, and `PluginName`. Use a unique, stable plugin ID; it identifies the BepInEx plugin, framework registration, and configuration file.

The minimum entry point is:

```csharp
private void Awake()
{
    FrameworkApi.RegisterMod(new MyMod(), Config);
}
```

Keep the hard BepInEx dependency on `FrameworkPlugin.PluginGuid` when the mod is built directly around framework types.

If the mod must remain fully standalone without GK2 Mod Framework, use the tested split main + optional bridge pattern in `OPTIONAL_INTEGRATION.md` instead of directly referencing framework types from the main assembly.

## 2. Define metadata

Every framework mod provides `Gk2ModMetadata`:

```csharp
new Gk2ModMetadata(
    id: "com.yourname.gk2.mymod",
    name: "My Mod",
    author: "YourName",
    version: "0.1.0",
    description: "A short player-facing description.",
    supportsRuntimeToggle: false,
    requiresKnownBuild: true);
```

Set `supportsRuntimeToggle` to `true` only when `OnDisable` completely reverses every change made by `OnEnable`. Otherwise, the Mods menu saves the new state for the next game start.

Set `requiresKnownBuild` to `true` when the mod depends on specific game methods or fields and an unknown build could be unsafe.

Starting with Framework 0.1.11, a mod can stay fail-closed by default while still surviving harmless game updates. In `OnRegister()`, validate the exact game-side contract your mod needs. If every required type/member/signature is still present, call `context.ConfirmCurrentBuildCompatibility(...)`. This lets only that mod run on an otherwise unknown whole-assembly fingerprint.

Example:

```csharp
public override void OnRegister(Gk2ModContext context)
{
    MethodInfo target = AccessTools.Method(typeof(SomeGameType), "TargetMethod");
    MethodInfo dependency = AccessTools.Method(typeof(OtherGameType), "RequiredCall");
    if (target == null || target.ReturnType != typeof(bool) || dependency == null)
        throw new MissingMethodException("Required game API contract changed.");
    if (!Gk2CompatibilityInspector.Calls(target, dependency))
        throw new MissingMethodException("Expected game call path changed.");

    context.ConfirmCurrentBuildCompatibility(
        "Target method/signature/call-path contract is intact.");

    // Register settings after or before the check as appropriate.
}
```

Do not confirm an unknown build just because `OnRegister()` completed. The confirmation is only for mods that perform a real structural compatibility check first.

## 3. Use the lifecycle

- `OnRegister` — add settings and perform compatibility checks.
- `OnEnable` — apply patches, subscriptions, or runtime behavior.
- `OnDisable` — reverse every action performed by `OnEnable`.
- `OnGameStarted` — access objects that require active gameplay or a loaded save.
- `OnReturnedToMainMenu` — clear per-session references.

An exception in a lifecycle callback is isolated and marks that mod as faulted for the current process. Required dependents are prevented from starting.

## 4. Register settings

Settings create BepInEx `ConfigEntry<T>` values and appear automatically in the Mods menu:

```csharp
ConfigEntry<bool> enabled = context.Settings.AddToggle(
    "General", "FeatureEnabled", true,
    "Feature enabled", "Enable this feature.");

ConfigEntry<float> multiplier = context.Settings.AddFloatSlider(
    "Gameplay", "Multiplier", 1f, 0.25f, 3f,
    "Multiplier", "Controls the effect strength.",
    step: 0.05f);

ConfigEntry<string> mode = context.Settings.AddDropdown(
    "Visual", "Mode", "Normal",
    new[] { "Low", "Normal", "High" },
    "Mode", "Select a visual preset.");

context.Settings.AddReadOnly(
    "Status", "Runtime", "Current state",
    "Live diagnostic value shown to the player.",
    () => "Ready");
```

Supported controls are toggle, integer slider, float slider, dropdown, enum, `KeyboardShortcut`, editable text, and read-only text. In Framework 0.1.12+, integer/float slider rows also expose a synchronized exact numeric input. The input does not bypass your declared range or step: Framework clamps to min/max and normalizes to the same step used by the slider.

The first argument is the section. Settings are grouped by section and sorted by `order`, then display name. Useful section names include `General`, `Gameplay`, `Visual`, `Controls`, `Advanced`, and `Status`.

Do not store dropdown display text as the only value. Do not create a separate keybind format; use BepInEx `KeyboardShortcut`.

## 5. Declare framework dependencies

The template declares compatibility with framework `0.1.x`:

```csharp
new Gk2ModDependency(
    FrameworkPlugin.PluginGuid,
    "0.1.0",
    "0.2.0")
```

The maximum version is exclusive. Required dependencies are enabled first and disabled after their dependents. To describe an optional integration, pass `optional: true`.

This dependency model does not load DLLs. Continue using BepInEx attributes for plugin loading and ordering.

For optional framework integration, do not merely replace a hard dependency with `SoftDependency` while keeping direct framework references in the main assembly. Keep the main assembly framework-free and place framework-specific code in a separate bridge plugin. See `OPTIONAL_INTEGRATION.md`.

## 6. Add localization files (optional)

Framework 0.1.8 can load UTF-8 JSON translations from `BepInEx/plugins/GK2.Framework/Localization/<mod-id>/<language>.json`.

For example, Korean strings for `com.yourname.gk2.mymod` go in `BepInEx/plugins/GK2.Framework/Localization/com.yourname.gk2.mymod/ko.json`:

```json
{
  "ui.title": "My translated title",
  "settings.feature_enabled": "My translated setting"
}
```

Resolve text through the public API and always provide an English fallback:

```csharp
string title = FrameworkLocalization.Get(
    PluginGuid,
    "ui.title",
    "My Mod");
```

Lookup falls back from the current game language to its neutral language when applicable, then `en`, then the supplied English fallback. Mods opt in explicitly; the Framework does not rewrite arbitrary third-party UI. If a mod keeps UI open while the game language changes, that mod is responsible for refreshing its existing labels.

## 7. Build and package

Build from the mod project folder:

```powershell
dotnet build .\MyMod.csproj -c Release
```

To select a different game or framework installation:

```powershell
dotnet build .\MyMod.csproj -c Release -p:GameDir="D:\Games\Graveyard Keeper 2"
dotnet build .\MyMod.csproj -c Release -p:FrameworkDll="D:\SDK\GK2.Framework.dll"
```

Ship only the mod DLL and any genuine runtime dependencies. Do not include PDB files, `bin`/`obj` folders, diagnostic assemblies, game assemblies, or copies of BepInEx/Harmony libraries.

List GK2 Mod Framework and BepInEx 5 as requirements on the mod page.

## 8. Test before release

Verify all of the following:

1. The project builds with no warnings or errors.
2. The mod appears once in the Mods menu with correct metadata.
3. Settings show under clear sections and survive closing and reopening the menu.
4. Configuration values survive a game restart.
5. Runtime-toggle mods pass Enable → Disable → Enable without leaving patches, subscriptions, or objects behind.
6. Restart-only mods show the current and next-start state correctly.
7. Loading a save and returning to the main menu leaves no stale session references.
8. The BepInEx log contains no new exceptions.
9. A mod with `RequiresKnownBuild=true` is tested against the exact supported game fingerprint, or its structural compatibility contract is deliberately tested on an unknown build before using `ConfirmCurrentBuildCompatibility`.

For exact contracts and stability boundaries, see `PUBLIC_API.md`.
