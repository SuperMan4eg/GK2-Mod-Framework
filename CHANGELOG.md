# Changelog

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
