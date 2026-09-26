# Changelog

## 0.1.14 — 2026-09-26

- Fixed the in-game pause-menu Mods button losing the native text outline/material styling. The runtime button now reapplies the exact font, shared material and text color from the native Settings button after localization and on every pause-menu open, matching the already-correct main-menu behavior across languages.
- Added a complete Simplified Chinese (`zh_cn`) Framework UI catalog.

## 0.1.13 — 2026-09-26

- Added render-time localization for registered mods in Framework-owned UI: mod names/descriptions, section headings, and setting names/descriptions can now come from each mod's localization catalog without changing config keys or requiring re-registration.
- Added a complete Bulgarian Framework UI catalog.
- Fixed gamepad navigation inside mod Settings pages. Controller focus can now enter setting rows from Back/Reset All and traverse toggles, sliders, dropdowns, keybinds, text fields, action buttons and per-setting Reset controls; focused rows are kept visible while scrolling.
- Added controller behavior for setting controls: Submit toggles toggles, Left/Right adjusts sliders and dropdowns, Submit starts keybind capture, and text fields can be activated without trapping focus.
- Made the exact numeric field beside integer/float sliders a real controller focus target. Submit on a slider enters exact-value focus; from there the row Reset control is reachable without sacrificing Left/Right slider adjustment.
- Fixed actual text/numeric editing: runtime text fields now enter a proper TMP edit state with a visible caret for mouse/physical-keyboard input. Controller Submit opens a Framework-owned on-screen keyboard that does not depend on Steam Overlay; OK commits, Back/Cancel preserves the old value, and focus returns to the originating field.
- Preserved the originating main-page focus when leaving a mod Settings page, so returning no longer jumps to the first mod in the list.
- Fixed `AddText` mouse editing by giving runtime TMP input fields an explicit pointer/gamepad activation path; click-to-focus, editing, commit and Back cancellation are regression-tested.
- Removed the duplicate setting highlight by relying on the game's single global `GamepadDynamicSelector` instead of creating a second local focus frame for runtime setting controls.
- Added conditional settings through `SetVisibilityCondition`, `SetEnabledCondition` and `RefreshConditions`. Hidden settings leave the layout/navigation while keeping their stored value; disabled settings remain visible but are grayed out and non-interactable.
- Added `AddButton` for non-persistent mod actions with a dynamic label and normal mouse/gamepad activation. Action rows intentionally have no Reset button.
- Conditional UI rebuilds preserve scroll/focus where possible, skip hidden/disabled rows in gamepad navigation, and fail open if a consumer condition throws.
- Runtime-tested the 0.1.13 candidate on Steam build `25533739` / Assembly-CSharp SHA-256 `7ACB243A08897D8CC7B67EF17AED50EEA17857494AEF4278F4F3B00D324823E5`. The global fingerprint remains `Unknown`; current structurally validating consumer mods still register `Compatible`.
- Existing 0.1.x setting registration signatures remain unchanged; the new APIs are additive.
- Thanks to `a-solanas` for PR #1 and physical XInput/Steam Input testing that helped shape the controller navigation work.
- Thanks to `AcTePuKc` / Shteryan Nikolaev for PR #2 (Bulgarian localization) and PR #3 (registered-mod UI localization).

## 0.1.12 — 2026-09-25

- Reworked release ZIP creation for portable Unix permission metadata: archive entries are marked as Unix, directories are normalized to `0755`, regular files to `0644`, and the release script validates those modes before accepting the package. This targets the Linux/Steam Proton extraction issue where `Localization` could become inaccessible to a normal user.
- Added explicit gamepad navigation between the left mod list and the right-side mod controls. Focusing a mod row now selects that mod; Right moves into Enable/Disable and Settings when available, and Left returns predictably to the selected row.
- Added a localized native-style `Mods` button to the in-game ESC/pause menu.
- Integer and float slider settings now include an exact numeric input field beside the slider. Manual input stays synchronized with the slider and still respects the setting's min/max/step contract; invalid text is rejected without corrupting the config, and float input accepts both dot and comma decimal separators.
- Reused the same Mods window from both main menu and pause menu; Back returns to the source window instead of maintaining a second UI implementation.
- Pause-menu transitions preserve the modal pause stack so opening/closing Mods from ESC does not intentionally drop `MainGame.IsGamePaused` between windows.
- Existing public 0.1.x API remains binary-compatible.

## 0.1.11 — 2026-09-24

- Added opt-in structural compatibility confirmation for mods that validate their own game-side API contract on an otherwise unknown game build.
- Added `Gk2ModContext.ConfirmCurrentBuildCompatibility(detail)`; a `RequiresKnownBuild=true` mod may call it from `OnRegister()` only after its required methods, fields, properties and signatures have been validated successfully.
- Added public `Gk2CompatibilityInspector.Calls(source, target)` for targeted IL call-path validation without exposing Harmony internals to consumer mods.
- Unknown builds remain fail-closed for mods that do not explicitly confirm a validated contract, and an incompatible base fingerprint cannot be overridden.
- Existing 0.1.x mods remain binary-compatible; no existing metadata constructor or lifecycle signature changed.
- Fixed Mods-list status badges so informational `StatusDetail` text on a `Compatible` mod no longer turns the mod into a warning. Compatible enabled mods show `OK`; pending restart remains `WARN`; actual compatibility/dependency failures keep their error state.
- Fixed live language switching for Framework UI: reopening Mods after a game-language change now rebuilds the persistent Framework window with the current translations and current native TMP font, preventing mixed RU/KO/EN text and blank Hangul labels.
- Framework-owned setting names/descriptions are resolved at render time instead of staying frozen in the startup language, and the injected main-menu Mods button now resolves its font directly from the current game language.
- Runtime-tested the new opt-in path on Steam build `25509347`: the Framework correctly kept the global build status `Unknown`, validated consumer mods could confirm their own contracts and run, while an older strict mod without confirmation remained `UnknownBuild`.

## 0.1.10 — 2026-09-24

- Added compatibility with Graveyard Keeper 2 Steam build `25506711`.
- Reverified Framework UI, lifecycle, localization/font, resolution, and menu integration targets against the updated game assemblies.
- Kept the public 0.1.x API and existing Framework behavior unchanged; this release only extends the verified game-build fingerprint set.

## 0.1.9 — 2026-09-24

- Fixed Framework-created UI text rendering as blank spaces for Korean translations.
- Framework UI now resolves its regular and bold TMP font assets through the game's native `LazyFontData` language mapping instead of forcing Latin-only `small_font` / `small_font_bold` assets.
- Korean now uses the game's built-in `korean` font atlas; the same path also selects the game's native Japanese and Chinese font assets when those languages are active.
- Added complete built-in Russian and Korean Framework localization catalogs alongside the complete English catalog (48/48 Framework-owned UI keys in each language).
- Fixed localized Settings UI layout: the Russian per-setting Reset label is now shorter, button labels do not wrap, text inputs/read-only values use safe inner padding, and dropdown option labels keep full horizontal width instead of collapsing into vertical text.
- Hardened the runtime TMP dropdown template with stretch anchors, explicit item sizing, no-wrap/ellipsis text, a standard `CanvasGroup`, and a layout rebuild after option population.
- Kept the existing Framework UI colors, layout and native skin behavior unchanged outside these fixes.

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
