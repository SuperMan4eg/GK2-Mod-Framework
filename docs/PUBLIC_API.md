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

`Gk2ModContext` provides `Settings`, a mod-prefixed `Log`, and the current `Build` fingerprint.

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

`BuildFingerprint` contains the Unity version, the location and SHA-256 hash of `Assembly-CSharp.dll`, and a `BuildCompatibilityStatus`. A mod with `RequiresKnownBuild=true` is blocked on an unknown build.

`RegisteredMod` exposes metadata, settings, compatibility status and detail, current enabled state, next-start state, and pending-restart state. Runtime state setters are not public. For registrations with `FrameworkManagesEnabledState=false`, framework enable-state setters return `false`, no framework Enabled entry is created, and the registration remains active whenever its compatibility/dependency state permits it.

## Localization

`FrameworkLocalization.Resolver` is an optional global function for resolving framework localization keys. An empty result uses the English fallback. Each mod is responsible for localizing its own metadata and setting labels.

## Not part of the public contract

UI classes, UI object names, patches, `FrameworkRegistry`, `NativeUiSkin`, `FrameworkUi`, and internal descriptor implementations may change in any `0.1.x` release. Consumer mods should not patch or access them through reflection without their own compatibility layer.
