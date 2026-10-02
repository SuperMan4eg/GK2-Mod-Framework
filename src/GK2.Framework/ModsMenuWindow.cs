using System;
using HarmonyLib;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2.Framework
{
    internal sealed class ModsMenuWindow : LazyWindow<LazyWidgetDataBase>
    {
        private static readonly Vector2 BasePanelSize = new Vector2(760f, 500f);
        private static ModsMenuWindow instance;
        private static LazyWindow<LazyWidgetDataBase> returnWindow;

        private RectTransform panelRect;
        private Vector2 lastSafeAreaSize = new Vector2(-1f, -1f);
        private Vector2 lastSafeAreaPosition = new Vector2(-1f, -1f);
        private float lastGameScale = -1f;
        private int lastUserScalePercent = -1;
        private GameObject mainPage;
        private ModsMenuModList modList;
        private ModsMenuDetails details;
        private ModsMenuSettingsPage settingsPage;
        private LazyButton frameworkSettingsButton;
        private LazyButton quitToApplyButton;
        private GameObject restartConfirmation;
        private TextMeshProUGUI restartConfirmationMessage;
        private GamepadNavigationItem restartReturnFocus;
        private RegisteredMod selected;
        private GamepadNavigationItem settingsReturnFocus;
        private string builtLanguage;

        internal static void OpenFromMainMenu(UIMainMenuWindow mainMenu)
        {
            OpenFromWindow(mainMenu);
        }

        internal static void OpenFromPauseMenu(UIGamePauseWindow pauseMenu)
        {
            OpenFromWindow(pauseMenu);
        }

        private static void OpenFromWindow(LazyWindow<LazyWidgetDataBase> sourceWindow)
        {
            if (sourceWindow == null) return;
            returnWindow = sourceWindow;
            string currentLanguage = FrameworkLocalization.CurrentLanguage;
            if (instance != null
                && !string.Equals(instance.builtLanguage, currentLanguage, StringComparison.OrdinalIgnoreCase))
            {
                FrameworkLog.Source?.LogInfo(
                    "GK2_MODS_UI_LANGUAGE_REBUILD: from=" + instance.builtLanguage
                    + ";to=" + currentLanguage);
                GameObject stale = instance.gameObject;
                instance = null;
                if (stale != null)
                {
                    stale.SetActive(false);
                    UnityEngine.Object.Destroy(stale);
                }
            }

            if (instance == null) instance = CreateInstance(sourceWindow);
            if (instance.IsShown) return;

            bool preservePause = sourceWindow is UIGamePauseWindow && MainGame.IsGamePaused;
            if (preservePause)
            {
                instance.Open(null);
                sourceWindow.Close();
            }
            else
            {
                sourceWindow.Close();
                instance.Open(null);
            }
            FrameworkLog.Source?.LogInfo("GK2_MODS_MENU_OPENED");
        }

        internal static void Toggle()
        {
            if (instance != null && instance.IsShown)
                instance.Close();
            else if (returnWindow != null)
                OpenFromWindow(returnWindow);
        }

        internal static void RefreshResponsiveScale()
        {
            instance?.ApplyResponsiveScale(force: true);
        }

        private static ModsMenuWindow CreateInstance(LazyWindow<LazyWidgetDataBase> sourceWindow)
        {
            LazyButton template = null;
            if (sourceWindow is UIMainMenuWindow mainMenu)
            {
                template = AccessTools.Field(typeof(UIMainMenuWindow), "gameSettingsButton")?.GetValue(mainMenu)
                    as LazyButton;
            }
            else if (sourceWindow is UIGamePauseWindow pauseMenu)
            {
                template = AccessTools.Field(typeof(UIGamePauseWindow), "settingsBtn")?.GetValue(pauseMenu)
                    as LazyButton;
            }
            NativeUiSkin.TryCapture();
            FrameworkUi.StyleSource = NativeUiSkin.IsReady
                ? null
                : template?.GetComponentInChildren<TextMeshProUGUI>(true);

            GameObject root = new GameObject("GK2ModsMenuWindow", typeof(RectTransform));
            root.transform.SetParent(FindUiRoot(sourceWindow), false);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 450;

            root.AddComponent<GraphicRaycaster>();
            GamepadNavigationController navigation = root.AddComponent<GamepadNavigationController>();
            navigation.navigationGroupSources = new System.Collections.Generic.List<GamepadNavigationController.NavigationGroupSource>();
            navigation.useGridSkippedIfListEmpty = true;

            ModsMenuWindow window = root.AddComponent<ModsMenuWindow>();
            window.builtLanguage = FrameworkLocalization.CurrentLanguage;
            window.BuildUi(template);
            window.Init();
            return window;
        }

        private static Transform FindUiRoot(Component sourceWindow)
        {
            GUIElements gui = GUIElements.Instance;
            UIFitter fitter = gui == null
                ? null
                : AccessTools.Field(typeof(GUIElements), "uiFitter")?.GetValue(gui) as UIFitter;

            if (fitter != null) return fitter.transform;
            if (gui != null && gui.Root != null) return gui.Root;
            return sourceWindow.transform.parent;
        }

        private void BuildUi(LazyButton template)
        {
            RectTransform root = (RectTransform)transform;
            FrameworkUi.Stretch(root);

            Image shade = FrameworkUi.CreateImage(
                "Shade", root, new Color(0f, 0f, 0f, NativeUiSkin.IsReady ? 0.4f : 0.72f));
            FrameworkUi.Stretch(shade.rectTransform);

            Image panel = FrameworkUi.CreateImage(
                "Panel", root, NativeUiSkin.IsReady
                    ? Color.clear
                    : new Color(0.055f, 0.035f, 0.03f, 0.99f));
            panelRect = panel.rectTransform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = BasePanelSize;
            panelRect.anchoredPosition = Vector2.zero;

            if (NativeUiSkin.IsReady)
            {
                Image innerBackground = FrameworkUi.CreateImage(
                    "NativeInnerBackground",
                    panelRect,
                    new Color(0.105f, 0.112f, 0.14f, 1f));
                FrameworkUi.SetRect(
                    innerBackground.rectTransform,
                    new Vector2(13f, 13f),
                    new Vector2(-13f, -38f),
                    Vector2.zero,
                    Vector2.one);
                innerBackground.raycastTarget = false;

                Image frame = FrameworkUi.CreateImage("NativeFrame", panelRect, Color.white);
                FrameworkUi.Stretch(frame.rectTransform);
                FrameworkUi.ApplyFrame(frame);
                frame.raycastTarget = false;
                frame.transform.SetAsLastSibling();
            }

            Image header = FrameworkUi.CreateImage(
                "Header", panelRect, NativeUiSkin.IsReady ? Color.white : new Color(0.12f, 0.07f, 0.055f, 1f));
            FrameworkUi.SetRect(
                header.rectTransform,
                new Vector2(12f, -40f),
                new Vector2(-12f, -12f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f));
            if (NativeUiSkin.IsReady && NativeUiSkin.HeaderSprite != null)
            {
                header.sprite = NativeUiSkin.HeaderSprite;
                header.type = Image.Type.Sliced;
                header.color = Color.white;

                if (NativeUiSkin.HeaderDecorSprite != null)
                {
                    Image decorLeft = FrameworkUi.CreateImage("DecorLeft", header.rectTransform, Color.white);
                    decorLeft.sprite = NativeUiSkin.HeaderDecorSprite;
                    decorLeft.type = Image.Type.Simple;
                    FrameworkUi.SetRect(
                        decorLeft.rectTransform,
                        Vector2.zero,
                        new Vector2(28f, 0f),
                        Vector2.zero,
                        new Vector2(0f, 1f));
                    decorLeft.raycastTarget = false;

                    Image decorRight = FrameworkUi.CreateImage("DecorRight", header.rectTransform, Color.white);
                    decorRight.sprite = NativeUiSkin.HeaderDecorSprite;
                    decorRight.type = Image.Type.Simple;
                    FrameworkUi.SetRect(
                        decorRight.rectTransform,
                        new Vector2(-28f, 0f),
                        Vector2.zero,
                        new Vector2(1f, 0f),
                        Vector2.one);
                    decorRight.raycastTarget = false;
                }
            }

            TextMeshProUGUI title = FrameworkUi.CreateText(
                "Title",
                header.rectTransform,
                NativeUiSkin.IsReady ? 16f : 26f,
                TextAlignmentOptions.Center,
                Color.white);
            FrameworkUi.Stretch(title.rectTransform);
            FrameworkUi.ApplyHeaderText(title);
            title.text = FrameworkUi.L("mods.title", "Mods");

            closeButton = FrameworkUi.CreateButton(
                "Close",
                panelRect,
                null,
                FrameworkUi.L("common.back", "Back"),
                new Vector2(-48f, 12f),
                new Vector2(48f, 38f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f));
            FrameworkUi.ApplyDialogButton(closeButton);

            quitToApplyButton = FrameworkUi.CreateButton(
                "QuitToApply", panelRect, template,
                FrameworkUi.L("mods.restart.quit_to_apply", "Quit to apply"),
                new Vector2(-218f, 12f), new Vector2(-58f, 38f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            FrameworkUi.ApplyDialogButton(quitToApplyButton);
            quitToApplyButton.onClick.AddListener(ShowRestartConfirmation);
            quitToApplyButton.SetCallbacksIntoGamepadNavigationItem();

            mainPage = new GameObject("MainPage");

            Image listPanel = FrameworkUi.CreateImage(
                "ModList", panelRect, new Color(0.07f, 0.045f, 0.04f, 0.98f));
            FrameworkUi.SetRect(
                listPanel.rectTransform,
                new Vector2(16f, 48f),
                new Vector2(270f, -48f),
                Vector2.zero,
                new Vector2(0f, 1f));
            FrameworkUi.ApplyCell(listPanel);

            Image detailPanel = FrameworkUi.CreateImage(
                "Details", panelRect, new Color(0.07f, 0.045f, 0.04f, 0.98f));
            FrameworkUi.SetRect(
                detailPanel.rectTransform,
                new Vector2(280f, 48f),
                new Vector2(-16f, -48f),
                Vector2.zero,
                Vector2.one);
            FrameworkUi.ApplyCell(detailPanel);

            listPanel.transform.SetParent(mainPage.transform, true);
            detailPanel.transform.SetParent(mainPage.transform, true);
            mainPage.transform.SetParent(panelRect, true);

            frameworkSettingsButton = FrameworkUi.CreateButton(
                "FrameworkSettings",
                panelRect,
                null,
                string.Empty,
                new Vector2(-50f, 8f),
                new Vector2(-14f, 44f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f));
            FrameworkUi.ApplyGearButton(frameworkSettingsButton);
            TextMeshProUGUI frameworkSettingsLabel = frameworkSettingsButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (frameworkSettingsLabel != null) frameworkSettingsLabel.gameObject.SetActive(false);
            frameworkSettingsButton.onClick.AddListener(OpenFrameworkSettingsPage);
            frameworkSettingsButton.SetCallbacksIntoGamepadNavigationItem();
            frameworkSettingsButton.transform.SetParent(mainPage.transform, true);
            quitToApplyButton.transform.SetParent(mainPage.transform, true);

            BuildRestartConfirmation(template, root);

            modList = new ModsMenuModList(listPanel, Select);
            details = new ModsMenuDetails(detailPanel, template, ToggleSelected, OpenSettingsPage, CopyReport);
            settingsPage = new ModsMenuSettingsPage(panelRect, template, CloseSettingsPage);
        }

        public override void Open(LazyWidgetDataBase data)
        {
            settingsPage.HideWithoutLog();
            if (restartConfirmation != null) restartConfirmation.SetActive(false);
            restartReturnFocus = null;
            mainPage.SetActive(true);
            if (closeButton != null) closeButton.gameObject.SetActive(true);

            // LazyWindow initializes its gamepad controller before activating the window.
            // Runtime-created windows therefore need to be active first so navigation items
            // are discoverable when the game is already in gamepad mode.
            if (!gameObject.activeSelf) gameObject.SetActive(true);

            ApplyResponsiveScale(force: true);
            base.Open(data);
            RefreshMods();
            RefreshRestartAction();
            RefreshRestartConfirmationMessage();
            RefreshGamepadNavigation();
        }

        protected override void PrintTips()
        {
            if (lazyButtonTips != null) base.PrintTips();
        }

        protected override void PrintTips(GamepadNavigationItem gamepadNavigationItem)
        {
            if (lazyButtonTips != null) base.PrintTips(gamepadNavigationItem);
        }

        protected override void Update()
        {
            ApplyResponsiveScale(force: false);
            if (modList.HandleSearchUpdate()) return;
            if (settingsPage.HandleUpdate()) return;
            base.Update();
        }

        protected override bool OnPressedBack()
        {
            if (restartConfirmation != null && restartConfirmation.activeSelf)
            {
                HideRestartConfirmation();
                return true;
            }

            if (settingsPage.IsCapturingKeybind)
            {
                settingsPage.CancelKeybindCapture(true);
                return true;
            }

            if (settingsPage.TryCancelTextInputEditing())
                return true;

            if (modList.CancelSearchEditing()) return true;

            if (settingsPage.IsOpen)
            {
                CloseSettingsPage();
                return true;
            }

            return base.OnPressedBack();
        }

        public override void Close()
        {
            modList.CancelSearchEditing();
            settingsPage.TryCancelTextInputEditing();
            if (restartConfirmation != null) restartConfirmation.SetActive(false);
            restartReturnFocus = null;
            LazyWindow<LazyWidgetDataBase> target = returnWindow;
            returnWindow = null;
            bool preservePause = target is UIGamePauseWindow && MainGame.IsGamePaused;

            if (preservePause && target != null)
            {
                target.Open(null);
                base.Close();
            }
            else
            {
                base.Close();
                if (target != null) target.Open(null);
            }

            FrameworkLog.Source?.LogInfo("GK2_MODS_MENU_CLOSED");
        }

        private void RefreshMods()
        {
            string selectedId = selected?.Metadata.Id;
            Select(modList.Refresh(selectedId));
        }

        private void Select(RegisteredMod mod)
        {
            selected = mod;
            string selectedId = mod?.Metadata.Id;
            modList.SetSelected(selectedId);
            details.Show(mod);
            details.ConfigureGamepadNavigation(modList.GetNavigationItem(selectedId));
            FrameworkLog.Source?.LogInfo("GK2_MOD_SELECTED: " + (selectedId ?? "<none>"));
        }

        private void CopyReport()
        {
            if (selected == null) return;
            try
            {
                GUIUtility.systemCopyBuffer = CompatibilityReport.Build(selected);
                details.ReportCopied();
                FrameworkLog.Source?.LogInfo("GK2_COMPAT_REPORT_COPIED: mod=" + selected.Metadata.Id);
            }
            catch (Exception ex)
            {
                FrameworkLog.Source?.LogWarning("GK2_COMPAT_REPORT_COPY_FAILED: " + ex.GetType().Name);
            }
        }

        private void ToggleSelected()
        {
            if (selected == null || !selected.Metadata.FrameworkManagesEnabledState) return;

            if (selected.Metadata.SupportsRuntimeToggle)
                FrameworkApi.SetRuntimeEnabled(selected.Metadata.Id, !selected.IsEnabled);
            else
                FrameworkApi.SetEnabledOnNextStart(
                    selected.Metadata.Id, !selected.IsEnabledOnNextStart);

            modList.RefreshStates();
            Select(selected);
            RefreshRestartAction();
        }

        private void RefreshRestartAction()
        {
            if (quitToApplyButton == null) return;
            bool pendingRestart = false;
            foreach (RegisteredMod mod in FrameworkApi.Mods)
            {
                if (mod.HasPendingRestart)
                {
                    pendingRestart = true;
                    break;
                }
            }

            quitToApplyButton.gameObject.SetActive(pendingRestart);
        }

        private void RefreshRestartConfirmationMessage()
        {
            if (restartConfirmationMessage == null) return;
            bool inGame = returnWindow is UIGamePauseWindow;
            restartConfirmationMessage.text = inGame
                ? FrameworkUi.L(
                    "mods.restart.confirm_body_in_game",
                    "The game will close without saving. Any unsaved progress may be lost. Pending mod changes apply next time you launch. Relaunch from your usual platform after cloud sync finishes.")
                : FrameworkUi.L(
                    "mods.restart.confirm_body_main_menu",
                    "The game will close. Pending mod changes apply next time you launch. Relaunch from your usual platform after cloud sync finishes.");
        }

        private void BuildRestartConfirmation(LazyButton template, Transform root)
        {
            restartConfirmation = new GameObject("RestartConfirmation", typeof(RectTransform));
            restartConfirmation.transform.SetParent(root, false);
            RectTransform overlayRect = (RectTransform)restartConfirmation.transform;
            FrameworkUi.Stretch(overlayRect);
            Image overlay = restartConfirmation.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.72f);

            Image dialog = FrameworkUi.CreateImage(
                "Dialog", overlayRect,
                NativeUiSkin.IsReady ? new Color(0.105f, 0.112f, 0.14f, 1f) : new Color(0.12f, 0.07f, 0.055f, 1f));
            RectTransform dialogRect = dialog.rectTransform;
            dialogRect.anchorMin = dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.sizeDelta = new Vector2(520f, 190f);
            if (NativeUiSkin.IsReady) FrameworkUi.ApplyFrame(dialog);

            TextMeshProUGUI title = FrameworkUi.CreateText(
                "Title", dialogRect, 20f, TextAlignmentOptions.Center, Color.white);
            FrameworkUi.ApplyHeaderText(title);
            title.text = FrameworkUi.L("mods.restart.confirm_title", "Quit game?");
            FrameworkUi.SetRect(title.rectTransform, new Vector2(16f, -48f), new Vector2(-16f, -14f), new Vector2(0f, 1f), Vector2.one);

            restartConfirmationMessage = FrameworkUi.CreateText(
                "Message", dialogRect, 16f, TextAlignmentOptions.Center, Color.white);
            FrameworkUi.ApplyLabelText(restartConfirmationMessage);
            restartConfirmationMessage.textWrappingMode = TextWrappingModes.Normal;
            FrameworkUi.SetRect(restartConfirmationMessage.rectTransform, new Vector2(22f, 64f), new Vector2(-22f, -58f), Vector2.zero, Vector2.one);
            RefreshRestartConfirmationMessage();

            LazyButton cancel = FrameworkUi.CreateButton(
                "Cancel", dialogRect, template, FrameworkUi.L("common.cancel", "Cancel"),
                new Vector2(-175f, 14f), new Vector2(-10f, 46f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            FrameworkUi.ApplyDialogButton(cancel);
            cancel.onClick.AddListener(HideRestartConfirmation);
            cancel.SetCallbacksIntoGamepadNavigationItem();

            LazyButton quit = FrameworkUi.CreateButton(
                "Quit", dialogRect, template, FrameworkUi.L("mods.restart.quit", "Quit game"),
                new Vector2(10f, 14f), new Vector2(175f, 46f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            FrameworkUi.ApplyDialogButton(quit);
            quit.onClick.AddListener(QuitToApply);
            quit.SetCallbacksIntoGamepadNavigationItem();
            GamepadNavigationItem cancelNavigation = cancel.GetComponent<GamepadNavigationItem>();
            GamepadNavigationItem quitNavigation = quit.GetComponent<GamepadNavigationItem>();
            cancelNavigation?.SetCustomDirectionItem(GUIDirection.Right, quitNavigation);
            quitNavigation?.SetCustomDirectionItem(GUIDirection.Left, cancelNavigation);
            restartConfirmation.SetActive(false);
        }

        private void ShowRestartConfirmation()
        {
            RefreshRestartAction();
            if (!quitToApplyButton.gameObject.activeSelf) return;
            RefreshRestartConfirmationMessage();
            if (LazyInput.IsGamepadActive)
                restartReturnFocus = GetComponent<GamepadNavigationController>()?.FocusedItem;
            mainPage.SetActive(false);
            if (closeButton != null) closeButton.gameObject.SetActive(false);
            restartConfirmation.SetActive(true);
            RefreshGamepadNavigation();
        }

        private void HideRestartConfirmation()
        {
            restartConfirmation.SetActive(false);
            mainPage.SetActive(true);
            if (closeButton != null) closeButton.gameObject.SetActive(!LazyInput.IsGamepadActive);
            GamepadNavigationItem focus = restartReturnFocus;
            restartReturnFocus = null;
            RefreshGamepadNavigation(focus);
        }

        private void QuitToApply()
        {
            RefreshRestartAction();
            if (!quitToApplyButton.gameObject.activeSelf) return;
            FrameworkLog.Source?.LogInfo("GK2_QUIT_TO_APPLY_REQUESTED");
            Application.Quit();
        }

        private void OpenSettingsPage()
        {
            OpenSettingsPage(selected);
        }

        private void OpenFrameworkSettingsPage()
        {
            RegisteredMod framework = null;
            foreach (RegisteredMod mod in FrameworkApi.Mods)
            {
                if (mod.Metadata.Id == FrameworkPlugin.PluginGuid)
                {
                    framework = mod;
                    break;
                }
            }

            OpenSettingsPage(framework);
        }

        private void OpenSettingsPage(RegisteredMod mod)
        {
            if (mod == null || mod.Settings.Items.Count == 0) return;

            if (LazyInput.IsGamepadActive)
            {
                GamepadNavigationController navigation =
                    GetComponent<GamepadNavigationController>();
                settingsReturnFocus = navigation?.FocusedItem;
            }
            else
            {
                settingsReturnFocus = null;
            }

            mainPage.SetActive(false);
            if (closeButton != null) closeButton.gameObject.SetActive(false);
            settingsPage.Open(mod);
            RefreshGamepadNavigation();
        }

        private void CloseSettingsPage()
        {
            settingsPage.Close();
            mainPage.SetActive(true);
            if (closeButton != null) closeButton.gameObject.SetActive(!LazyInput.IsGamepadActive);

            GamepadNavigationItem preferredFocus = settingsReturnFocus;
            settingsReturnFocus = null;
            RefreshGamepadNavigation(preferredFocus);
        }

        private void RefreshGamepadNavigation(
            GamepadNavigationItem preferredFocus = null)
        {
            if (!LazyInput.IsGamepadActive) return;

            GamepadNavigationController navigation =
                GetComponent<GamepadNavigationController>();
            if (navigation == null) return;

            if (preferredFocus == null)
            {
                navigation.ReinitItems(focusOnFirstActive: true);
                return;
            }

            navigation.ReinitItems(focusOnFirstActive: false);
            if (preferredFocus.Active
                && preferredFocus.gameObject.activeInHierarchy)
            {
                navigation.SetFocusedItem(preferredFocus);
                return;
            }

            GamepadNavigationItem selectedRow =
                modList?.GetNavigationItem(selected?.Metadata.Id);
            if (selectedRow != null
                && selectedRow.Active
                && selectedRow.gameObject.activeInHierarchy)
            {
                navigation.SetFocusedItem(selectedRow);
                return;
            }

            navigation.FocusOnFirstActive();
        }

        private void ApplyResponsiveScale(bool force)
        {
            if (panelRect == null) return;

            Rect safeArea = Screen.safeArea;
            float gameScale = LazyUI.ScaleFactor > 0.001f
                ? LazyUI.ScaleFactor
                : ResolutionConfig.GetUiScaleFactor();
            int userPercent = FrameworkUi.WindowScalePercent?.Value ?? 100;

            if (!force
                && Mathf.Approximately(lastSafeAreaSize.x, safeArea.width)
                && Mathf.Approximately(lastSafeAreaSize.y, safeArea.height)
                && Mathf.Approximately(lastSafeAreaPosition.x, safeArea.x)
                && Mathf.Approximately(lastSafeAreaPosition.y, safeArea.y)
                && Mathf.Approximately(lastGameScale, gameScale)
                && lastUserScalePercent == userPercent)
                return;

            float applied = FrameworkUi.CalculateResponsiveWindowScale(BasePanelSize, gameScale, safeArea);
            panelRect.localScale = new Vector3(applied, applied, 1f);

            lastSafeAreaSize = safeArea.size;
            lastSafeAreaPosition = safeArea.position;
            lastGameScale = gameScale;
            lastUserScalePercent = userPercent;

            FrameworkLog.Source?.LogInfo(
                $"GK2_UI_WINDOW_SCALE: screen={Screen.width}x{Screen.height};"
                + $"safe={safeArea.width:0}x{safeArea.height:0}@{safeArea.x:0},{safeArea.y:0};"
                + $"gameScale={gameScale:0.###};user={userPercent}%;applied={applied:0.###}");
        }

        protected override void TestDraw() { }
    }
}
