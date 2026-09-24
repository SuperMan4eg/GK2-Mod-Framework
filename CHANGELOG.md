# Changelog

## 0.1.9 — 2026-09-24

- Fixed Framework-created UI text rendering as blank spaces for Korean translations.
- Framework UI now resolves its regular and bold TMP font assets through the game's native `LazyFontData` language mapping instead of forcing Latin-only `small_font` / `small_font_bold` assets.
- Korean now uses the game's built-in `korean` font atlas; the same path also selects the game's native Japanese and Chinese font assets when those languages are active.
- Kept the existing Framework UI colors, layout and native skin behavior unchanged for non-CJK languages.

## 0.1.8 — 2026-09-24

- Added UTF-8 JSON localization files under `BepInEx/plugins/GK2.Framework/Localization/<mod-id>/<language>.json`.
- Added `FrameworkLocalization.Get(modId, key, englishFallback)` so Framework-dependent mods can opt into the same localization system.
- Added current-language, neutral-language, English, and caller-fallback lookup, including early use of the game's persisted selected language and normalization of Korean `kr`/`kor` aliases to `ko`.
- Added `GetLocalizationDirectory`, per-mod/all-cache `Reload` helpers, and retained the existing global `Resolver` behavior for 0.1.x compatibility.
- Routed the main-menu Mods label through the localization system and included the Framework English key file plus the existing Russian Mods-label translation as language files.
- Localization remains opt-in: the Framework does not scan, resize, rewrite, or translate arbitrary third-party UI automatically.

## 0.1.7 — 2026-09-23

- Added a compact gear-shaped **Framework Settings** button to the Mods menu without exposing the framework as a normal mod-list entry or drawing a rectangular button background.
- The Framework Settings page now exposes `Window scale (%)` through the same native-style settings UI used by framework mods.
- Window scale changes apply immediately while the settings page is open and remain stored in the existing BepInEx framework config.
- Runtime-tested the new entry point at `1600x900`, including the standalone `i_bronze_gear` target graphic, live 70% scaling, Back navigation, and gamepad-navigation registration.
- Hidden the disabled per-mod Settings button when no Framework-integrated mod is selected.
- Clarified the empty state: standalone BepInEx mods only appear in the list when they register directly with the Framework or include a Framework bridge.
- Added registry and loaded-assembly diagnostics when the Mods list is unexpectedly empty, including registered IDs, detected BepInEx plugin IDs, and duplicate Framework assembly locations.

## 0.1.6 — 2026-09-23

- Added responsive safe-area fitting for the Framework Mods window and all Framework-hosted mod Settings pages.
- Fixed the fixed-size `760x500` Framework panel overflowing lower resolutions such as `1600x900` after the game's UI scale factor was applied.
- Added a configurable `WindowScalePercent` setting from 50% to 100% in 10% steps, stored in the Framework BepInEx config.
- Automatic fitting always caps the selected scale so the Framework window remains inside the current safe area.
- Added live refitting when resolution, safe area, game UI scale, or the user scale setting changes.
- Runtime-tested automatic fitting at `1600x900` and gamepad directional navigation while scaled; synthetic fit checks also cover `1366x768`, `1280x720`, `1920x1080`, and `3840x2160`.

## 0.1.5 — 2026-09-23

- Fixed a controller-mode crash when opening the runtime-created Mods menu.
- Guarded controller button-tip rendering when the runtime window has no serialized `LazyButtonTipsStr`.
- Reinitialized gamepad navigation after dynamic mod/settings controls are created or page visibility changes.
- Initialized the runtime gamepad navigation group list so directional navigation cannot dereference an unassigned serialized list.

## 0.1.4 — 2026-09-23

- Added an official optional-integration mode for standalone mods through `Gk2ModMetadata(..., frameworkManagesEnabledState: false)`.
- Optional integration registrations no longer create a framework-owned Enabled config entry or show a misleading Enable/Disable control.
- Added a runtime-tested standalone + framework bridge guide and buildable template.
- Kept the original `Gk2ModMetadata` constructor for binary compatibility with existing 0.1.x consumers.

## 0.1.3 — 2026-09-23

- Added compatibility for Graveyard Keeper 2 Steam build `25467846`.
- Verified that the framework UI/lifecycle target classes used by the current release are unchanged from build `25457344`.
- Changed hard-blocking `UnknownBuild` and `DependencyUnavailable` states from warning severity to error severity so the UI no longer suggests that a blocked mod can run.

## 0.1.2 — 2026-09-22

- Added compatibility for the Graveyard Keeper 2 full release, Steam build `25457344`.
- Added the full-release build fingerprint while retaining Demo build `25344626` support.
- Retargeted developer build defaults to the full-release game directory.
- Kept the public API inside the existing `0.1.x` compatibility range.

## 0.1.1 — 2026-09-22

- Fixed keybind capture so Left/Right Shift, Control, and Alt can be assigned as standalone keys.
- Preserved modifier combinations such as Shift+K, with the regular key stored as the main key.
- Displayed key combinations in modifier-first order.
- Prevented Windows AltGr input from adding a synthetic Left Control to Right Alt combinations.

## 0.1.0 — 2026-09-21

Initial public preview for Graveyard Keeper 2 Demo.

- Added framework mod registration, metadata, dependency ranges, and compatibility states.
- Added guarded lifecycle callbacks and shared game events.
- Added game-build fingerprinting for compatibility checks.
- Added a native-style Mods button and Mods menu.
- Added BepInEx-backed settings with toggle, integer slider, float slider, dropdown, enum, keybind, editable text, and read-only controls.
- Added safe runtime toggling for mods that explicitly support it and restart-required state for other mods.
- Added English public documentation and a buildable consumer template.

Known limitations are listed in `README.md`.
