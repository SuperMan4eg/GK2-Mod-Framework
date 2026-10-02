Historical brainstorming only (archived context). This file is not the task queue. Before selecting any idea, validate relevance and duplicates under Workflows/TASK_TRACKING.md; actionable work belongs to GitHub Modding-Hub issues / Project #2.

# GK2 Mod Framework — варианты следующего обновления

Дата: 2026-09-29. Историческая запись идей. Владелец выбрал объединение пунктов 1 и 3: они реализованы в тестовом кандидате 0.1.16 и проходят проверку перед релизом. Опубликованная версия остаётся 0.1.15. Пункты 2 и 4 остаются предложениями.

## 1. Small stability update: inactive-menu coroutine and game 1.007 check

Player/developer benefit: remove a confusing Unity error reported during otherwise successful Mods-button injection, and clearly establish what 0.1.15/next Framework version actually supports on the updated game.

Selected scope: stop starting the optional `LogAfterLifecycle` diagnostic coroutine from `UIMainMenuWindow.Init` while its GameObject can be inactive. Keep button creation and normal `Open` behavior intact. Exact-artifact startup checked on Steam build `25601286`; broader settings/keyboard/restart regressions and owner visual review remain pending. Unknown-build fail-closed behavior remains.

Evidence: `Evidence/MAIN_MENU_COROUTINE_REPORT_2026-09-29.md` and `Evidence/GK2-Framework-0.1.16-Test-2026-09-29.md`. The source timing issue is verified; controlled startup did not reproduce the old error, but the unsafe diagnostic start is removed.

## 2. Controller keyboard: EN/RU/symbol layouts

Player benefit: type Russian text and common punctuation into Framework-hosted settings without a physical keyboard. Current on-screen keyboard is Latin-only; owner previously confirmed controller text input works for the existing layout.

Proposed scope: choose keyboard layout from the active language where supported, add a visible layout switch and a symbols page, preserve caret, commit/cancel, gamepad focus and existing numeric entry. Keep CJK/IME input explicitly out of this release until designed and tested separately.

Evidence: `STATUS.md` backlog and `README.md` known limitations. Status remains a proposal; no Cyrillic keyboard implementation or owner test exists. Effort/risk: medium, mainly focus/navigation and font handling. Test with XInput and Steam Input, RU/EN switching, empty/long text, cancel, and restoring the previous value.

## 3. Copyable compatibility report in the Mods menu

Player/developer benefit: make bug reports actionable without asking users to find multiple log entries manually, especially after game updates.

Selected scope: a button in mod details copies a short report containing game/Unity version, game assembly fingerprint, Framework version, selected mod status/detail, plus the relative log path. It is opt-in and excludes Framework-owned absolute paths, full config, saves and logs. Mod-supplied status text should be reviewed before sharing. The global build status remains `Unknown` on the test game build.

Evidence: `Evidence/GK2-Framework-0.1.16-Test-2026-09-29.md`. The test probe clicked the button and verified clipboard fields; owner visual and controller checks remain pending.

## 4. Opt-in safe-area layout helper for mod-owned windows

Developer benefit: third-party mods could fit their own windows to different resolutions using the same safe-area behavior that Framework-owned Mods and Settings windows already use.

Proposed scope: a small opt-in public API with explicit ownership/lifecycle rules; never silently resize arbitrary game or mod UI. Document an example consumer and compatibility guarantees for existing 0.1.x mods.

Evidence: `STATUS.md` backlog and existing Framework responsive UI. Status: idea only. Effort/risk: high because it extends the public API and must be tested against at least one independent mod window, multiple resolutions/aspect ratios and controller navigation. Better as a separate feature release after the stability update.

## Suggested sequencing

1. Finish the 0.1.16 owner check and release gate for combined ideas 1 and 3.
2. Consider idea 2 as a separate user-facing feature.
3. Consider idea 4 only after a concrete consumer demonstrates the contract.

Do not mark 0.1.16 published until its exact release artifact is prepared and the owner confirms publication under `Workflows/MOD_LIFECYCLE.md`.
