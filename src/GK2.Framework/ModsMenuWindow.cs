using HarmonyLib;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2.Framework
{
    internal sealed class ModsMenuWindow : LazyWindow<LazyWidgetDataBase>
    {
        private static ModsMenuWindow instance;
        private static UIMainMenuWindow returnWindow;

        private GameObject mainPage;
        private ModsMenuModList modList;
        private ModsMenuDetails details;
        private ModsMenuSettingsPage settingsPage;
        private RegisteredMod selected;

        internal static void OpenFromMainMenu(UIMainMenuWindow mainMenu)
        {
            if (mainMenu == null) return;
            returnWindow = mainMenu;
            if (instance == null) instance = CreateInstance(mainMenu);
            if (instance.IsShown) return;

            mainMenu.Close();
            instance.Open(null);
            FrameworkLog.Source?.LogInfo("GK2_MODS_MENU_OPENED");
        }

        internal static void Toggle()
        {
            if (instance != null && instance.IsShown)
                instance.Close();
            else if (returnWindow != null)
                OpenFromMainMenu(returnWindow);
        }

        private static ModsMenuWindow CreateInstance(UIMainMenuWindow mainMenu)
        {
            LazyButton template =
                AccessTools.Field(typeof(UIMainMenuWindow), "gameSettingsButton")?.GetValue(mainMenu)
                as LazyButton;
            NativeUiSkin.TryCapture();
            FrameworkUi.StyleSource = NativeUiSkin.IsReady
                ? null
                : template?.GetComponentInChildren<TextMeshProUGUI>(true);

            GameObject root = new GameObject("GK2ModsMenuWindow", typeof(RectTransform));
            root.transform.SetParent(FindUiRoot(mainMenu), false);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 450;

            root.AddComponent<GraphicRaycaster>();
            root.AddComponent<GamepadNavigationController>();

            ModsMenuWindow window = root.AddComponent<ModsMenuWindow>();
            window.BuildUi(template);
            window.Init();
            return window;
        }

        private static Transform FindUiRoot(UIMainMenuWindow mainMenu)
        {
            GUIElements gui = GUIElements.Instance;
            UIFitter fitter = gui == null
                ? null
                : AccessTools.Field(typeof(GUIElements), "uiFitter")?.GetValue(gui) as UIFitter;

            if (fitter != null) return fitter.transform;
            if (gui != null && gui.Root != null) return gui.Root;
            return mainMenu.transform.parent;
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
            RectTransform panelRect = panel.rectTransform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(760f, 500f);
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

            modList = new ModsMenuModList(listPanel, Select);
            details = new ModsMenuDetails(detailPanel, template, ToggleSelected, OpenSettingsPage);
            settingsPage = new ModsMenuSettingsPage(panelRect, template, CloseSettingsPage);
        }

        public override void Open(LazyWidgetDataBase data)
        {
            settingsPage.HideWithoutLog();
            mainPage.SetActive(true);
            if (closeButton != null) closeButton.gameObject.SetActive(true);

            // Important: dynamic LazyButtons are created only after the window hierarchy
            // has been activated by base.Open(), otherwise LazyButton.Awake can reset listeners.
            base.Open(data);
            RefreshMods();
        }

        protected override void Update()
        {
            if (settingsPage.HandleUpdate()) return;
            base.Update();
        }

        protected override bool OnPressedBack()
        {
            if (settingsPage.IsCapturingKeybind)
            {
                settingsPage.CancelKeybindCapture(true);
                return true;
            }

            if (settingsPage.IsOpen)
            {
                CloseSettingsPage();
                return true;
            }

            return base.OnPressedBack();
        }

        public override void Close()
        {
            base.Close();
            FrameworkLog.Source?.LogInfo("GK2_MODS_MENU_CLOSED");

            UIMainMenuWindow target = returnWindow;
            returnWindow = null;
            if (target != null) target.Open(null);
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
            FrameworkLog.Source?.LogInfo("GK2_MOD_SELECTED: " + (selectedId ?? "<none>"));
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
        }

        private void OpenSettingsPage()
        {
            if (selected == null || selected.Settings.Items.Count == 0) return;
            mainPage.SetActive(false);
            if (closeButton != null) closeButton.gameObject.SetActive(false);
            settingsPage.Open(selected);
        }

        private void CloseSettingsPage()
        {
            settingsPage.Close();
            mainPage.SetActive(true);
            if (closeButton != null) closeButton.gameObject.SetActive(true);
        }

        protected override void TestDraw() { }
    }
}
