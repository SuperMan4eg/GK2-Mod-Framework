# GK2 Mod Framework 0.1 public API

API status: preview. Existing public signatures are intended to remain compatible within `0.1.x`, while new members may be added. Before `1.0`, breaking changes may occur with a changelog entry and migration note.

Call framework APIs on the Unity main thread. Thread safety is not part of the current contract.

## Entry point

For a mod built directly on the framework, register one framework mod from `BaseUnityPlugin.Awake()` after declaring a hard BepInEx dependency on `ru.superman4eg.gk2.framework`:

```csharp
RegisteredMod registration = FrameworkApi.RegisterMod(mod, Config);
```

`FrameworkApi` also exposes:

- `IsReady` — whether the registry is available.
- `CurrentBuild` — the detected `BuildFingerprint`.
- `Mods` — a read-only collection of registered mods.
- `SetRuntimeEnabled(id, enabled)` — changes the current state only for mods with `SupportsRuntimeToggle=true`.
- `SetEnabledOnNextStart(id, enabled)` — saves the state to apply after restart.
- `ToggleModsMenu()` — opens or closes the menu when the main-menu UI is available.

## Metadata and dependencies

`Gk2ModMetadata` contains the stable mod ID, name, author, semantic version, description, runtime-toggle support, known-build requirement, and whether the framework manages that registration's enabled state. IDs are normalized to lowercase and must not change after publication.

The original constructor signature remains supported for binary compatibility and defaults `FrameworkManagesEnabledState` to `true`. Optional integration bridges can use the extended constructor with `frameworkManagesEnabledState: false`; those registrations do not get a framework-owned Enabled config entry or Enable/Disable control.

`Gk2ModDependency` uses a half-open version range: `[minimumVersion, maximumVersionExclusive)`. Required dependencies affect availability and lifecycle order. Optional dependencies may be absent; when present, their version range is still checked.

The framework does not load DLLs and does not replace BepInEx dependency resolution.

A standalone mod can integrate optionally by moving all framework references into a separate bridge plugin. The main plugin remains framework-free, while the bridge hard-depends on both the main plugin and GK2 Mod Framework. This pattern has been runtime-verified both with and without the framework installed. See `OPTIONAL_INTEGRATION.md`.

## Lifecycle

- `OnRegister(context)` — register settings and perform compatibility checks. Called once.
- `OnEnable()` — apply runtime behavior.
- `OnDisable()` — completely reverse everything applied by `OnEnable()`.
- `OnGameStarted()` — gameplay and a save are active.
- `OnReturnedToMainMenu()` — release per-session references.

An exception in a lifecycle callback is isolated and logged. The mod becomes `Faulted` for the current process. The framework cannot automatically remove another mod's patches, so a mod must only advertise runtime toggling when it provides complete rollback.

## Context

`Gk2ModContext` provides `Settings`, a mod-prefixed `Log`, the current `Build` fingerprint, and `ConfirmCurrentBuildCompatibility(detail)`.

`ConfirmCurrentBuildCompatibility` is an opt-in escape hatch for a mod with `RequiresKnownBuild=true` when the Framework does not recognize the current whole-assembly fingerprint. Call it only from `OnRegister()` and only after the mod has verified every game-side method, field, property, type/signature, or other runtime contract it depends on. A successful confirmation allows that mod to run on a `BuildCompatibilityStatus.Unknown` build while keeping the global build fingerprint unknown. It does not override `BuildCompatibilityStatus.Incompatible`.

Do not call this method merely because registration did not throw. A mod that does not perform a real structural compatibility check should remain fail-closed on unknown builds.

`Gk2CompatibilityInspector.Calls(source, target)` is a public helper for stronger contract checks. It reads the original IL body of `source` and returns whether it contains a direct `call`/`callvirt` to `target`, resolving metadata tokens through reflection. This is useful when the mod depends not only on a member existing, but on a specific game pipeline still calling that member. Combine it with normal reflection checks for parameter/return types, field/property types, constants, and other invariants your patch assumes.

## Settings

`Gk2Settings` creates a real `ConfigEntry<T>` and a descriptor used by the Mods menu. Supported methods are:

- `AddToggle`
- `AddIntSlider`
- `AddFloatSlider`
- `AddDropdown` and generic `AddDropdown<T>`
- `AddEnum<T>`
- `AddKeybind`
- `AddText`
- `AddReadOnly`

Usage rules:

- `Section.Key` must be unique within a `ConfigFile`.
- Numeric defaults must be inside the inclusive min/max range, and the step must be positive.
- Dropdown choices must not be empty, and the default must be one of the underlying values.
- Keybinds use BepInEx `KeyboardShortcut`; do not maintain a parallel format.
- Display text is not a stable stored value.
- Value changes are reported through `ConfigEntry.SettingChanged`.

## Events

`Gk2GameEvents` exposes `GameStarted`, `ReturnedToMainMenu`, `GamePaused`, and `GameUnpaused`. An exception in one subscriber does not stop other subscribers. A mod that subscribes during `OnEnable` must unsubscribe during `OnDisable`.

## Compatibility and runtime state

`BuildFingerprint` contains the Unity version, the location and SHA-256 hash of `Assembly-CSharp.dll`, and a `BuildCompatibilityStatus`. By default, a mod with `RequiresKnownBuild=true` is blocked on an unknown build. Starting with Framework 0.1.11, that specific mod may remain compatible on an unknown whole-assembly fingerprint when its `OnRegister()` performs a real structural contract check and then calls `context.ConfirmCurrentBuildCompatibility(...)`. Mods that do not opt in remain blocked.

`RegisteredMod` exposes metadata, settings, compatibility status and detail, current enabled state, next-start state, and pending-restart state. Runtime state setters are not public. For registrations with `FrameworkManagesEnabledState=false`, framework enable-state setters return `false`, no framework Enabled entry is created, and the registration remains active whenever its compatibility/dependency state permits it.

## Localization

Framework 0.1.8 adds UTF-8 JSON language files and keeps `FrameworkLocalization.Resolver` as a legacy host override for 0.1.x compatibility.

Language files live under `BepInEx/plugins/GK2.Framework/Localization/<mod-id>/<language>.json`. Files are flat JSON objects whose keys and values are strings. The selected language is read from the game's persisted `settings.language` value when available, which makes localization usable during early mod registration; `LLBase.CurrentLang` is the fallback. Language codes are normalized to lowercase, `-` becomes `_`, and Korean aliases `kr`/`kor` normalize to `ko`.

Use `FrameworkLocalization.Get(modId, key, englishFallback)` from dependent mods. Lookup order is the current language, its neutral language when applicable (for example `pt_br -> pt`), `en`, then the supplied fallback. `GetLocalizationDirectory(modId)` returns the shared directory for packaging or diagnostics. `Reload(modId)` and `Reload()` clear cached language files after edits.

The existing two-argument `FrameworkLocalization.Get(key, englishFallback)` is reserved for Framework-owned strings. `FrameworkLocalization.Resolver`, when set, still gets first chance to resolve those Framework keys; returning null or an empty string continues into the file/fallback path.

Localization is opt-in for dependent mods. The Framework does not scan or rewrite arbitrary third-party UI text. Mods decide where to call the localization API and remain responsible for refreshing any already-created UI when the game language changes. Starting with 0.1.9, Framework-owned runtime UI resolves its TMP font through the game's native `LazyFontData` mapping, so the game-provided Korean, Japanese, and Chinese font assets are used when those languages are active. This font behavior applies to Framework-owned UI only; a mod that draws its own UI remains responsible for selecting a font that supports its translated text.

## Not part of the public contract

UI classes, UI object names, patches, `FrameworkRegistry`, `NativeUiSkin`, `FrameworkUi`, and internal descriptor implementations may change in any `0.1.x` release. Consumer mods should not patch or access them through reflection without their own compatibility layer.
