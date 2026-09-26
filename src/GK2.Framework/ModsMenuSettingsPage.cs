using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BepInEx.Configuration;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2.Framework
{
    internal sealed class ModsMenuSettingsPage
    {
        private readonly GameObject page;
        private readonly RectTransform content;
        private readonly ScrollRect scroll;
        private readonly TextMeshProUGUI title;
        private readonly Action requestClose;
        private readonly LazyButton backButton;
        private readonly LazyButton resetAllButton;
        private readonly List<NavigationRow> navigationRows = new List<NavigationRow>();

        private RegisteredMod selected;
        private IGk2Setting capturingKeybind;
        private TextMeshProUGUI captureLabel;
        private KeyCode pendingMainModifier = KeyCode.None;
        private bool presentationRefreshPending;

        internal bool IsOpen => page.activeSelf;
        internal bool IsCapturingKeybind => capturingKeybind != null;

        private sealed class NavigationRow
        {
            internal GamepadNavigationItem Primary { get; }
            internal GamepadNavigationItem Secondary { get; }
            internal GamepadNavigationItem Reset { get; }

            internal NavigationRow(
                GamepadNavigationItem primary,
                GamepadNavigationItem secondary,
                GamepadNavigationItem reset)
            {
                Primary = primary;
                Secondary = secondary;
                Reset = reset;
            }
        }

        internal ModsMenuSettingsPage(RectTransform parent, LazyButton template, Action requestClose)
        {
            this.requestClose = requestClose ?? throw new ArgumentNullException(nameof(requestClose));

            Image settings = FrameworkUi.CreateImage(
                "SettingsPage", parent,
                NativeUiSkin.IsReady ? Color.white : new Color(0.065f, 0.038f, 0.03f, 0.99f));
            FrameworkUi.ApplyCell(settings);
            FrameworkUi.SetRect(
                settings.rectTransform,
                new Vector2(16f, 16f),
                new Vector2(-16f, -48f),
                Vector2.zero,
                Vector2.one);
            page = settings.gameObject;

            title = FrameworkUi.CreateText(
                "SettingsTitle",
                settings.rectTransform,
                NativeUiSkin.IsReady ? 16f : 22f,
                TextAlignmentOptions.TopLeft,
                NativeUiSkin.IsReady ? Color.white : new Color(1f, 0.75f, 0.35f));
            FrameworkUi.ApplyHeaderText(title);
            FrameworkUi.SetRect(
                title.rectTransform,
                new Vector2(16f, -10f),
                new Vector2(-250f, -38f),
                new Vector2(0f, 1f),
                Vector2.one);

            backButton = FrameworkUi.CreateButton(
                "Back",
                settings.rectTransform,
                NativeUiSkin.IsReady ? null : template,
                FrameworkUi.L("common.back", "Back"),
                new Vector2(-206f, -42f),
                new Vector2(-112f, -14f),
                Vector2.one,
                Vector2.one);
            FrameworkUi.ApplyDialogButton(backButton);
            backButton.onClick.AddListener(() => this.requestClose());
            backButton.SetCallbacksIntoGamepadNavigationItem();

            resetAllButton = FrameworkUi.CreateButton(
                "ResetAll",
                settings.rectTransform,
                NativeUiSkin.IsReady ? null : template,
                FrameworkUi.L("settings.reset_all", "Reset all"),
                new Vector2(-104f, -42f),
                new Vector2(-12f, -14f),
                Vector2.one,
                Vector2.one);
            FrameworkUi.ApplyDialogButton(resetAllButton);
            resetAllButton.onClick.AddListener(ResetAllSettings);
            resetAllButton.SetCallbacksIntoGamepadNavigationItem();

            Image contentFrame = FrameworkUi.CreateImage(
                "SettingsContent", settings.rectTransform,
                NativeUiSkin.IsReady ? Color.white : new Color(0.04f, 0.025f, 0.02f, 0.92f));
            FrameworkUi.ApplyCell(contentFrame);
            FrameworkUi.SetRect(
                contentFrame.rectTransform,
                new Vector2(12f, 12f),
                new Vector2(-12f, -50f),
                Vector2.zero,
                Vector2.one);
            content = FrameworkUi.CreateVerticalScrollContent(contentFrame, out scroll);

            page.SetActive(false);
        }

        internal void Open(RegisteredMod mod)
        {
            if (mod == null || mod.Settings.Items.Count == 0) return;

            DetachPresentationEvents();
            selected = mod;
            selected.Settings.PresentationChanged += OnPresentationChanged;
            selected.Settings.RefreshConditions();

            page.SetActive(true);
            BuildControls();
            FrameworkLog.Source?.LogInfo("GK2_MOD_SETTINGS_OPENED: " + selected.Metadata.Id);
        }

        internal void Close()
        {
            CancelKeybindCapture(false);
            TryCancelTextInputEditing();
            DetachPresentationEvents();
            page.SetActive(false);
            FrameworkLog.Source?.LogInfo("GK2_MOD_SETTINGS_CLOSED: " + selected?.Metadata.Id);
        }

        internal void HideWithoutLog()
        {
            CancelKeybindCapture(false);
            TryCancelTextInputEditing();
            DetachPresentationEvents();
            page.SetActive(false);
        }

        internal bool HandleUpdate()
        {
            if (presentationRefreshPending
                && capturingKeybind == null)
            {
                presentationRefreshPending = false;
                RebuildKeepingFocus();
                return true;
            }

            if (capturingKeybind == null) return false;
            CaptureKeybind();
            return true;
        }

        private void OnPresentationChanged()
        {
            if (page.activeSelf)
                presentationRefreshPending = true;
        }

        private void DetachPresentationEvents()
        {
            if (selected != null)
                selected.Settings.PresentationChanged -= OnPresentationChanged;

            presentationRefreshPending = false;
        }

        internal void CancelKeybindCapture(bool userCancelled)
        {
            if (capturingKeybind != null && captureLabel != null)
                captureLabel.text = FormatKeybind(capturingKeybind.Value);

            if (capturingKeybind != null && userCancelled)
            {
                FrameworkLog.Source?.LogInfo(
                    "GK2_MOD_KEYBIND_CAPTURE_CANCELLED: "
                    + selected?.Metadata.Id + "/" + capturingKeybind.UniqueKey);
            }

            capturingKeybind = null;
            captureLabel = null;
            pendingMainModifier = KeyCode.None;
        }

        internal bool TryCancelTextInputEditing()
        {
            TMP_InputField[] inputs =
                page.GetComponentsInChildren<TMP_InputField>(false);
            for (int i = 0; i < inputs.Length; i++)
            {
                TMP_InputField input = inputs[i];
                if (input == null || !input.isFocused)
                    continue;

                input.DeactivateInputField();
                return true;
            }

            return false;
        }

        private void BuildControls()
        {
            presentationRefreshPending = false;
            navigationRows.Clear();
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                content.GetChild(i).gameObject.SetActive(false);
                UnityEngine.Object.Destroy(content.GetChild(i).gameObject);
            }

            title.text = selected.Metadata.Name + " — " + FrameworkUi.L("mods.settings", "Settings");
            List<IGk2Setting> settings = selected.Settings.Items
                .Where(s => selected.Settings.GetPresentation(s).Visible)
                .OrderBy(s => s.Section)
                .ThenBy(s => s.Order)
                .ThenBy(s => s.DisplayName)
                .ToList();

            float y = -6f;
            string currentSection = null;
            int sectionIndex = 0;
            foreach (IGk2Setting setting in settings)
            {
                string section = string.IsNullOrWhiteSpace(setting.Section)
                    ? FrameworkUi.L("settings.general", "General")
                    : selected.Metadata.Id == FrameworkPlugin.PluginGuid
                        && string.Equals(setting.Section, "UI", StringComparison.OrdinalIgnoreCase)
                            ? FrameworkUi.L("settings.ui", "UI")
                            : setting.Section;
                if (!string.Equals(currentSection, section, StringComparison.OrdinalIgnoreCase))
                {
                    CreateSectionHeader(section, sectionIndex++, y);
                    currentSection = section;
                    y -= 27f;
                }

                float rowHeight = CreateSettingRow(setting, y);
                y -= rowHeight + 3f;
            }

            FrameworkUi.SetContentHeight(content, Mathf.Max(0f, -y + 10f));
            ConfigureGamepadNavigation();
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
        }

        private string GetDisplayName(IGk2Setting setting)
        {
            if (selected?.Metadata.Id == FrameworkPlugin.PluginGuid
                && string.Equals(setting.UniqueKey, "UI.WindowScalePercent", StringComparison.OrdinalIgnoreCase))
            {
                return FrameworkUi.L("settings.window_scale", setting.DisplayName);
            }

            return setting.DisplayName;
        }

        private string GetDescription(IGk2Setting setting)
        {
            if (selected?.Metadata.Id == FrameworkPlugin.PluginGuid
                && string.Equals(setting.UniqueKey, "UI.WindowScalePercent", StringComparison.OrdinalIgnoreCase))
            {
                return FrameworkUi.L("settings.window_scale_description", setting.Description ?? string.Empty);
            }

            return setting.Description ?? string.Empty;
        }

        private void CreateSectionHeader(string section, int index, float y)
        {
            Image header = FrameworkUi.CreateImage(
                "Section_" + index,
                content,
                NativeUiSkin.IsReady ? Color.white : new Color(0.13f, 0.075f, 0.05f, 0.96f));
            FrameworkUi.ApplyCell(header);
            header.raycastTarget = false;
            FrameworkUi.SetRect(
                header.rectTransform,
                new Vector2(5f, y - 23f),
                new Vector2(-5f, y),
                new Vector2(0f, 1f),
                Vector2.one);

            TextMeshProUGUI text = FrameworkUi.CreateText(
                "Label", header.transform, 15f, TextAlignmentOptions.Left,
                new Color(1f, 0.75f, 0.35f));
            FrameworkUi.SetRect(
                text.rectTransform,
                new Vector2(9f, 0f),
                new Vector2(-6f, 0f),
                Vector2.zero,
                Vector2.one);
            text.fontStyle = FontStyles.Bold;
            if (NativeUiSkin.IsReady)
            {
                FrameworkUi.ApplyHeaderText(text);
                text.color = NativeUiSkin.ValueColor;
            }
            text.text = section;
        }

        private float CreateSettingRow(IGk2Setting setting, float y)
        {
            Image row = FrameworkUi.CreateImage(
                "Setting_" + setting.UniqueKey,
                content,
                NativeUiSkin.IsReady
                    ? new Color(0f, 0f, 0f, 0f)
                    : new Color(0.1f, 0.06f, 0.045f, 0.95f));

            TextMeshProUGUI name = FrameworkUi.CreateText(
                "Name", row.rectTransform, 15f, TextAlignmentOptions.Left, Color.white);
            name.text = GetDisplayName(setting);
            if (NativeUiSkin.IsReady)
            {
                name.fontSize = 16f;
                FrameworkUi.ApplyLabelText(name);
            }
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.textWrappingMode = TextWrappingModes.NoWrap;

            TextMeshProUGUI description = FrameworkUi.CreateText(
                "Description",
                row.rectTransform,
                13f,
                TextAlignmentOptions.TopLeft,
                new Color(0.82f, 0.78f, 0.73f));
            description.text = GetDescription(setting);
            if (NativeUiSkin.IsReady)
            {
                FrameworkUi.ApplyLabelText(description);
                description.color = Color.Lerp(NativeUiSkin.HintColor, Color.white, 0.22f);
            }
            description.textWrappingMode = TextWrappingModes.Normal;
            description.overflowMode = TextOverflowModes.Truncate;

            const float textWidth = 250f;
            float descriptionHeight = string.IsNullOrWhiteSpace(description.text)
                ? 0f
                : Mathf.Max(14f, Mathf.Ceil(
                    description.GetPreferredValues(description.text, textWidth, 1000f).y));
            float rowHeight = Mathf.Max(
                36f,
                string.IsNullOrWhiteSpace(description.text)
                    ? 36f
                    : 24f + descriptionHeight + 2f);

            FrameworkUi.SetRect(
                row.rectTransform,
                new Vector2(5f, y - rowHeight),
                new Vector2(-5f, y),
                new Vector2(0f, 1f),
                Vector2.one);

            FrameworkUi.SetRect(
                name.rectTransform,
                new Vector2(8f, -22f),
                new Vector2(258f, -4f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f));

            if (!string.IsNullOrWhiteSpace(description.text))
            {
                FrameworkUi.SetRect(
                    description.rectTransform,
                    new Vector2(8f, -rowHeight + 3f),
                    new Vector2(258f, -22f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f));
            }
            else
            {
                description.gameObject.SetActive(false);
            }

            RectTransform control =
                new GameObject("Control", typeof(RectTransform)).GetComponent<RectTransform>();
            control.SetParent(row.transform, false);
            FrameworkUi.SetRect(
                control,
                new Vector2(-326f, -13f),
                new Vector2(setting.IsReadOnly ? -6f : -62f, 13f),
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f));

            GamepadNavigationItem primaryNavigation = null;
            GamepadNavigationItem secondaryNavigation = null;
            switch (setting.Kind)
            {
                case SettingKind.Toggle:
                    primaryNavigation = CreateToggle(setting, control);
                    break;
                case SettingKind.IntegerSlider:
                case SettingKind.FloatSlider:
                    primaryNavigation = CreateSlider(
                        setting,
                        control,
                        out secondaryNavigation);
                    break;
                case SettingKind.Dropdown:
                    primaryNavigation = CreateChoice(setting, control);
                    break;
                case SettingKind.Keybind:
                    primaryNavigation = CreateKeybind(setting, control);
                    break;
                case SettingKind.Text:
                    primaryNavigation = CreateTextInput(setting, control);
                    break;
                case SettingKind.Button:
                    primaryNavigation = CreateActionButton(setting, control);
                    break;
                default:
                    CreateValueText(setting, control);
                    break;
            }

            GamepadNavigationItem resetNavigation = null;
            if (!setting.IsReadOnly)
            {
                LazyButton reset = FrameworkUi.CreateButton(
                    "Reset",
                    row.rectTransform,
                    null,
                    FrameworkUi.L("settings.reset", "Reset"),
                    new Vector2(-56f, -13f),
                    new Vector2(-6f, 13f),
                    new Vector2(1f, 0.5f),
                    new Vector2(1f, 0.5f));
                reset.onClick.AddListener(() => ResetSetting(setting));
                reset.SetCallbacksIntoGamepadNavigationItem();
                resetNavigation = reset.GetComponent<GamepadNavigationItem>();
            }

            SettingPresentationState presentation =
                selected.Settings.GetPresentation(setting);
            if (!presentation.Enabled)
            {
                ApplyDisabledPresentation(
                    row.gameObject,
                    primaryNavigation,
                    secondaryNavigation,
                    resetNavigation);
            }

            navigationRows.Add(new NavigationRow(
                primaryNavigation,
                secondaryNavigation,
                resetNavigation));
            return rowHeight;
        }

        private static void ApplyDisabledPresentation(
            GameObject row,
            GamepadNavigationItem primaryNavigation,
            GamepadNavigationItem secondaryNavigation,
            GamepadNavigationItem resetNavigation)
        {
            if (row == null)
                return;

            CanvasGroup group =
                row.GetComponent<CanvasGroup>()
                ?? row.AddComponent<CanvasGroup>();
            group.alpha = 0.5f;
            group.interactable = false;
            group.blocksRaycasts = false;

            Selectable[] selectables =
                row.GetComponentsInChildren<Selectable>(
                    includeInactive: true);
            for (int i = 0; i < selectables.Length; i++)
            {
                if (selectables[i] != null)
                    selectables[i].interactable = false;
            }

            if (primaryNavigation != null)
                primaryNavigation.Active = false;
            if (secondaryNavigation != null)
                secondaryNavigation.Active = false;
            if (resetNavigation != null)
                resetNavigation.Active = false;
        }

        private GamepadNavigationItem CreateToggle(IGk2Setting setting, RectTransform parent)
        {
            GameObject go = new GameObject("Toggle", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            FrameworkUi.Stretch((RectTransform)go.transform);

            Image bg = go.AddComponent<Image>();
            bg.color = new Color(0.25f, 0.12f, 0.07f, 1f);
            FrameworkUi.ApplyCell(bg);
            Toggle toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = bg;

            TextMeshProUGUI text = FrameworkUi.CreateText(
                "Value", go.transform, 16f, TextAlignmentOptions.Center, Color.white);
            FrameworkUi.ApplyValueText(text);
            FrameworkUi.Stretch(text.rectTransform);

            void Refresh(bool value)
            {
                text.text = value
                    ? FrameworkUi.L("common.on", "On")
                    : FrameworkUi.L("common.off", "Off");
            }

            bool current = (bool)setting.Value;
            toggle.isOn = current;
            Refresh(current);
            toggle.onValueChanged.AddListener(value =>
            {
                setting.Value = value;
                Refresh(value);
                LogChanged(setting);
            });

            return CreateControlNavigation(
                go,
                () => toggle.isOn = !toggle.isOn);
        }

        private GamepadNavigationItem CreateSlider(
            IGk2Setting setting,
            RectTransform parent,
            out GamepadNavigationItem numericNavigation)
        {
            numericNavigation = null;
            GameObject go = new GameObject("Slider", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            FrameworkUi.Stretch((RectTransform)go.transform);

            RectTransform track =
                new GameObject("Track", typeof(RectTransform)).GetComponent<RectTransform>();
            track.SetParent(go.transform, false);
            FrameworkUi.SetRect(
                track,
                Vector2.zero,
                new Vector2(-78f, 0f),
                Vector2.zero,
                Vector2.one);

            Slider slider = track.gameObject.AddComponent<Slider>();
            Image bg = FrameworkUi.CreateImage(
                "Background", track, new Color(0.2f, 0.1f, 0.07f, 1f));
            FrameworkUi.Stretch(bg.rectTransform);
            if (NativeUiSkin.IsReady && NativeUiSkin.ProgressBackgroundSprite != null)
            {
                bg.sprite = NativeUiSkin.ProgressBackgroundSprite;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }

            Image fill = FrameworkUi.CreateImage(
                "Fill", track, new Color(0.75f, 0.32f, 0.12f, 1f));
            FrameworkUi.Stretch(fill.rectTransform);
            if (NativeUiSkin.IsReady && NativeUiSkin.ProgressFillSprite != null)
            {
                fill.sprite = NativeUiSkin.ProgressFillSprite;
                fill.type = Image.Type.Sliced;
                fill.color = Color.white;
            }
            slider.fillRect = fill.rectTransform;

            Image handle = FrameworkUi.CreateImage(
                "Handle", track, new Color(1f, 0.75f, 0.35f, 1f));
            handle.rectTransform.sizeDelta = new Vector2(12f, NativeUiSkin.IsReady ? 18f : 34f);
            if (NativeUiSkin.IsReady && NativeUiSkin.SliderHandleSprite != null)
            {
                handle.sprite = NativeUiSkin.SliderHandleSprite;
                handle.type = Image.Type.Sliced;
                handle.color = Color.white;
                slider.transition = Selectable.Transition.SpriteSwap;
                slider.colors = NativeUiSkin.SliderColors;
                slider.spriteState = NativeUiSkin.SliderSpriteState;
            }
            slider.targetGraphic = handle;
            slider.handleRect = handle.rectTransform;
            slider.minValue = Convert.ToSingle(setting.Minimum);
            slider.maxValue = Convert.ToSingle(setting.Maximum);
            slider.wholeNumbers = setting.Kind == SettingKind.IntegerSlider;

            Image inputBackground = FrameworkUi.CreateImage(
                "NumericInput", go.transform, new Color(0.18f, 0.09f, 0.06f, 1f));
            FrameworkUi.ApplyCell(inputBackground);
            FrameworkUi.SetRect(
                inputBackground.rectTransform,
                new Vector2(-70f, 1f),
                new Vector2(0f, -1f),
                new Vector2(1f, 0f),
                Vector2.one);

            TMP_InputField input = inputBackground.gameObject.AddComponent<TMP_InputField>();
            RectTransform textViewport =
                new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D))
                    .GetComponent<RectTransform>();
            textViewport.SetParent(inputBackground.transform, false);
            FrameworkUi.SetRect(
                textViewport,
                new Vector2(6f, 2f),
                new Vector2(-6f, -2f),
                Vector2.zero,
                Vector2.one);

            TextMeshProUGUI inputText = FrameworkUi.CreateText(
                "Text", textViewport, NativeUiSkin.IsReady ? 15f : 14f,
                TextAlignmentOptions.Center, Color.white);
            FrameworkUi.ApplyValueText(inputText);
            FrameworkUi.Stretch(inputText.rectTransform);
            inputText.textWrappingMode = TextWrappingModes.NoWrap;
            inputText.overflowMode = TextOverflowModes.Masking;
            ConfigureRuntimeInputField(
                input,
                inputBackground,
                textViewport,
                inputText,
                TouchScreenKeyboardType.NumbersAndPunctuation,
                setting.DisplayName,
                64);

            void RefreshFromSetting()
            {
                float current = Convert.ToSingle(setting.Value);
                slider.SetValueWithoutNotify(current);
                input.SetTextWithoutNotify(FormatNumericValue(setting, setting.Value));
            }

            void ApplyNumericValue(double rawValue, bool logChange)
            {
                double normalized = NormalizeNumericValue(setting, rawValue);
                if (setting.Kind == SettingKind.IntegerSlider)
                    setting.Value = Convert.ToInt32(normalized);
                else
                    setting.Value = Convert.ToSingle(normalized);

                RefreshFromSetting();
                if (logChange) LogChanged(setting);
            }

            RefreshFromSetting();
            slider.onValueChanged.AddListener(value => ApplyNumericValue(value, true));
            input.onEndEdit.AddListener(value =>
            {
                if (TryParseNumericValue(value, out double parsed))
                    ApplyNumericValue(parsed, true);
                else
                    RefreshFromSetting();
            });

            numericNavigation = CreateControlNavigation(
                inputBackground.gameObject,
                () => RuntimeInputFieldActivator.Activate(
                    input,
                    null,
                    setting.DisplayName,
                    64));
            GamepadNavigationItem numericTarget = numericNavigation;

            GamepadNavigationItem navigation =
                CreateControlNavigation(
                    go,
                    () =>
                    {
                        GamepadNavigationController controller =
                            go.GetComponentInParent<GamepadNavigationController>();
                        if (controller != null
                            && numericTarget != null
                            && numericTarget.Active)
                        {
                            controller.SetFocusedItem(numericTarget);
                        }
                        else
                        {
                            RuntimeInputFieldActivator.Activate(
                                input,
                                null,
                                setting.DisplayName,
                                64);
                        }
                    });
            SettingsNavigationControl navigationControl =
                go.AddComponent<SettingsNavigationControl>();
            navigationControl.Slider = slider;

            double step = Math.Max(setting.Step, 0.000001d);
            double range = Convert.ToDouble(setting.Maximum)
                - Convert.ToDouble(setting.Minimum);
            navigationControl.SliderAmount = (float)(
                Math.Ceiling(range / 100d / step) * step);
            if (navigationControl.SliderAmount <= 0f)
                navigationControl.SliderAmount = (float)step;

            return navigation;
        }

        private static string FormatNumericValue(IGk2Setting setting, object value)
        {
            if (setting.Kind == SettingKind.IntegerSlider)
                return Convert.ToInt32(value).ToString(CultureInfo.InvariantCulture);

            return Convert.ToSingle(value).ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static bool TryParseNumericValue(string text, out double value)
        {
            if (double.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.CurrentCulture,
                out value))
            {
                return true;
            }

            if (double.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value))
            {
                return true;
            }

            string normalized = (text ?? string.Empty).Trim().Replace(',', '.');
            return double.TryParse(
                normalized,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value);
        }

        private static double NormalizeNumericValue(IGk2Setting setting, double value)
        {
            double minimum = Convert.ToDouble(setting.Minimum, CultureInfo.InvariantCulture);
            double maximum = Convert.ToDouble(setting.Maximum, CultureInfo.InvariantCulture);
            double clamped = Math.Max(minimum, Math.Min(maximum, value));
            double step = setting.Step;
            double snapped = step > 0d ? Math.Round(clamped / step) * step : clamped;
            snapped = Math.Max(minimum, Math.Min(maximum, snapped));

            if (setting.Kind == SettingKind.IntegerSlider)
                return Convert.ToInt32(snapped);

            return Convert.ToSingle(snapped);
        }

        private GamepadNavigationItem CreateChoice(IGk2Setting setting, RectTransform parent)
        {
            Image background = FrameworkUi.CreateImage(
                "Dropdown", parent, new Color(0.18f, 0.09f, 0.06f, 1f));
            FrameworkUi.ApplyCell(background);
            FrameworkUi.Stretch(background.rectTransform);

            TMP_Dropdown dropdown = background.gameObject.AddComponent<TMP_Dropdown>();
            TextMeshProUGUI caption = FrameworkUi.CreateText(
                "Caption", background.transform, NativeUiSkin.IsReady ? 16f : 15f,
                TextAlignmentOptions.Center, Color.white);
            FrameworkUi.ApplyValueText(caption);
            FrameworkUi.SetRect(
                caption.rectTransform,
                new Vector2(8f, 2f),
                new Vector2(-8f, -2f),
                Vector2.zero,
                Vector2.one);
            caption.textWrappingMode = TextWrappingModes.NoWrap;
            caption.overflowMode = TextOverflowModes.Ellipsis;
            dropdown.captionText = caption;

            GameObject template = new GameObject("Template", typeof(RectTransform));
            template.transform.SetParent(background.transform, false);
            RectTransform templateRect = (RectTransform)template.transform;
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.anchoredPosition = Vector2.zero;
            templateRect.sizeDelta = new Vector2(0f, Math.Max(34f, setting.Choices.Count * 30f));

            Image templateImage = template.AddComponent<Image>();
            templateImage.color = new Color(0.12f, 0.065f, 0.045f, 1f);
            FrameworkUi.ApplyCell(templateImage);
            template.AddComponent<CanvasGroup>();
            ScrollRect dropdownScroll = template.AddComponent<ScrollRect>();
            dropdownScroll.horizontal = false;
            dropdownScroll.vertical = true;
            dropdownScroll.movementType = ScrollRect.MovementType.Clamped;
            dropdownScroll.scrollSensitivity = 30f;

            Image viewport = FrameworkUi.CreateImage("Viewport", template.transform, Color.white);
            FrameworkUi.Stretch(viewport.rectTransform);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            RectTransform dropdownContent =
                new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            dropdownContent.SetParent(viewport.transform, false);
            dropdownContent.anchorMin = new Vector2(0f, 1f);
            dropdownContent.anchorMax = new Vector2(1f, 1f);
            dropdownContent.pivot = new Vector2(0.5f, 1f);
            dropdownContent.anchoredPosition = Vector2.zero;
            dropdownContent.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = dropdownContent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = dropdownContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject item = new GameObject("Item", typeof(RectTransform));
            item.transform.SetParent(dropdownContent, false);
            RectTransform itemRect = (RectTransform)item.transform;
            itemRect.anchorMin = new Vector2(0f, 1f);
            itemRect.anchorMax = new Vector2(1f, 1f);
            itemRect.pivot = new Vector2(0.5f, 1f);
            itemRect.sizeDelta = new Vector2(0f, 30f);
            LayoutElement itemLayout = item.AddComponent<LayoutElement>();
            itemLayout.minHeight = 30f;
            itemLayout.preferredHeight = 30f;
            itemLayout.flexibleWidth = 1f;
            Image itemBg = item.AddComponent<Image>();
            itemBg.color = new Color(0.2f, 0.1f, 0.07f, 1f);
            FrameworkUi.ApplyCell(itemBg);
            Toggle toggle = item.AddComponent<Toggle>();
            toggle.targetGraphic = itemBg;

            TextMeshProUGUI itemLabel = FrameworkUi.CreateText(
                "Item Label", item.transform, 14f, TextAlignmentOptions.Left, Color.white);
            FrameworkUi.ApplyLabelText(itemLabel);
            FrameworkUi.SetRect(
                itemLabel.rectTransform,
                new Vector2(10f, 2f),
                new Vector2(-8f, -2f),
                Vector2.zero,
                Vector2.one);
            itemLabel.textWrappingMode = TextWrappingModes.NoWrap;
            itemLabel.overflowMode = TextOverflowModes.Ellipsis;
            itemLabel.enableAutoSizing = true;
            itemLabel.fontSizeMin = 11f;
            itemLabel.fontSizeMax = 14f;

            dropdownScroll.viewport = viewport.rectTransform;
            dropdownScroll.content = dropdownContent;
            dropdownScroll.horizontal = false;
            dropdown.template = templateRect;
            dropdown.itemText = itemLabel;
            dropdown.options.Clear();

            foreach (object choice in setting.Choices)
                dropdown.options.Add(new TMP_Dropdown.OptionData(Convert.ToString(choice)));

            int selectedIndex = 0;
            for (int i = 0; i < setting.Choices.Count; i++)
            {
                if (Equals(setting.Choices[i], setting.Value))
                {
                    selectedIndex = i;
                    break;
                }
            }

            dropdown.SetValueWithoutNotify(selectedIndex);
            dropdown.RefreshShownValue();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(dropdownContent);
            dropdown.onValueChanged.AddListener(index =>
            {
                if (index < 0 || index >= setting.Choices.Count) return;
                setting.Value = setting.Choices[index];
                LogChanged(setting);
            });
            template.SetActive(false);

            GamepadNavigationItem navigation =
                CreateControlNavigation(
                    background.gameObject,
                    () => CycleDropdown(dropdown, 1));
            SettingsNavigationControl navigationControl =
                background.gameObject.AddComponent<SettingsNavigationControl>();
            navigationControl.Dropdown = dropdown;
            return navigation;
        }

        private GamepadNavigationItem CreateKeybind(IGk2Setting setting, RectTransform parent)
        {
            LazyButton button = FrameworkUi.CreateButton(
                "Keybind",
                parent,
                null,
                FormatKeybind(setting.Value),
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                Vector2.one);
            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
            FrameworkUi.ApplyValueText(text);
            button.onClick.AddListener(() =>
            {
                capturingKeybind = setting;
                captureLabel = text;
                pendingMainModifier = KeyCode.None;
                text.text = FrameworkUi.L("settings.press_key", "Press a key (Esc cancels)");
            });
            button.SetCallbacksIntoGamepadNavigationItem();
            return button.GetComponent<GamepadNavigationItem>();
        }

        private void CaptureKeybind()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelKeybindCapture(true);
                return;
            }

            KeyCode main = FindPressedKey(modifier: false);
            if (main != KeyCode.None)
            {
                CommitKeybind(main);
                return;
            }

            // A modifier cannot be committed on KeyDown: doing so would turn
            // the first half of Shift+K into a standalone Shift binding. Wait
            // for release, while still allowing a regular key to complete a
            // modifier combination on a later frame.
            if (pendingMainModifier != KeyCode.None)
            {
                if (!Input.GetKey(pendingMainModifier))
                    CommitKeybind(pendingMainModifier);
                return;
            }

            pendingMainModifier = FindPressedKey(modifier: true);
        }

        private void CommitKeybind(KeyCode main)
        {
            if (capturingKeybind == null) return;

            var modifiers = new List<KeyCode>();
            bool rightAltActive = main == KeyCode.RightAlt || Input.GetKey(KeyCode.RightAlt);
            foreach (KeyCode key in new[]
            {
                KeyCode.LeftControl,
                KeyCode.RightControl,
                KeyCode.LeftShift,
                KeyCode.RightShift,
                KeyCode.LeftAlt,
                KeyCode.RightAlt
            })
            {
                // Windows reports Right Alt (AltGr) as RightAlt + a synthetic
                // LeftControl. Store the key the user physically selected.
                if (rightAltActive && key == KeyCode.LeftControl) continue;
                if (key != main && Input.GetKey(key)) modifiers.Add(key);
            }

            capturingKeybind.Value = new KeyboardShortcut(main, modifiers.ToArray());
            captureLabel.text = FormatKeybind(capturingKeybind.Value);
            LogChanged(capturingKeybind);
            capturingKeybind = null;
            captureLabel = null;
            pendingMainModifier = KeyCode.None;
        }

        private static KeyCode FindPressedKey(bool modifier)
        {
            // AltGr emits LeftControl and RightAlt together on Windows. Prefer
            // the physical Right Alt key when selecting a modifier main key.
            if (modifier && Input.GetKeyDown(KeyCode.RightAlt))
                return KeyCode.RightAlt;

            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if (key != KeyCode.None && IsModifier(key) == modifier && Input.GetKeyDown(key))
                    return key;
            }

            return KeyCode.None;
        }

        private static string FormatKeybind(object value)
        {
            if (!(value is KeyboardShortcut shortcut))
                return Convert.ToString(value);

            var keys = new List<string>();
            if (shortcut.Modifiers != null)
            {
                foreach (KeyCode modifier in shortcut.Modifiers)
                    keys.Add(modifier.ToString());
            }

            if (shortcut.MainKey != KeyCode.None)
                keys.Add(shortcut.MainKey.ToString());

            return string.Join(" + ", keys);
        }

        private static bool IsModifier(KeyCode key) =>
            key == KeyCode.LeftControl
            || key == KeyCode.RightControl
            || key == KeyCode.LeftShift
            || key == KeyCode.RightShift
            || key == KeyCode.LeftAlt
            || key == KeyCode.RightAlt;

        private GamepadNavigationItem CreateTextInput(IGk2Setting setting, RectTransform parent)
        {
            Image bg = FrameworkUi.CreateImage(
                "TextInput", parent, new Color(0.18f, 0.09f, 0.06f, 1f));
            FrameworkUi.ApplyCell(bg);
            FrameworkUi.Stretch(bg.rectTransform);

            TMP_InputField input = bg.gameObject.AddComponent<TMP_InputField>();

            RectTransform textViewport =
                new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D))
                    .GetComponent<RectTransform>();
            textViewport.SetParent(bg.transform, false);
            FrameworkUi.SetRect(
                textViewport,
                new Vector2(8f, 2f),
                new Vector2(-8f, -2f),
                Vector2.zero,
                Vector2.one);

            TextMeshProUGUI text = FrameworkUi.CreateText(
                "Text", textViewport, NativeUiSkin.IsReady ? 16f : 15f,
                TextAlignmentOptions.Left, Color.white);
            FrameworkUi.ApplyValueText(text);
            FrameworkUi.Stretch(text.rectTransform);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Masking;

            ConfigureRuntimeInputField(
                input,
                bg,
                textViewport,
                text,
                TouchScreenKeyboardType.Default,
                setting.DisplayName,
                256);
            input.SetTextWithoutNotify(Convert.ToString(setting.Value));

            input.onEndEdit.AddListener(value =>
            {
                setting.Value = value;
                input.SetTextWithoutNotify(Convert.ToString(setting.Value));
                LogChanged(setting);
            });

            return CreateControlNavigation(
                bg.gameObject,
                () => RuntimeInputFieldActivator.Activate(
                    input,
                    null,
                    setting.DisplayName,
                    256));
        }

        private static void ConfigureRuntimeInputField(
            TMP_InputField input,
            Image background,
            RectTransform textViewport,
            TextMeshProUGUI text,
            TouchScreenKeyboardType keyboardType,
            string gamepadHeaderText,
            int gamepadMaxLength)
        {
            if (input == null
                || background == null
                || textViewport == null
                || text == null)
            {
                return;
            }

            background.raycastTarget = true;
            input.targetGraphic = background;
            input.textViewport = textViewport;
            input.textComponent = text;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.contentType = TMP_InputField.ContentType.Custom;
            input.characterValidation = TMP_InputField.CharacterValidation.None;
            input.keyboardType = keyboardType;
            input.interactable = true;
            input.readOnly = false;
            input.navigation = new Navigation
            {
                mode = Navigation.Mode.None
            };

            RuntimeInputFieldActivator activator =
                input.gameObject.GetComponent<RuntimeInputFieldActivator>()
                ?? input.gameObject.AddComponent<RuntimeInputFieldActivator>();
            activator.Input = input;
            activator.GamepadHeaderText = gamepadHeaderText;
            activator.GamepadMaxLength = gamepadMaxLength;
        }

        private static GamepadNavigationItem CreateControlNavigation(
            GameObject target,
            UnityEngine.Events.UnityAction onSelect)
        {
            if (target == null)
                return null;

            GamepadNavigationItem navigation =
                target.GetComponent<GamepadNavigationItem>()
                ?? target.AddComponent<GamepadNavigationItem>();

            // Runtime settings should use the game's single global
            // GamepadDynamicSelector. A local focusFrame would be rendered
            // in addition to that selector and produces the duplicate
            // highlight reported by users.
            navigation.focusFrame = null;
            navigation.FocusRectTransform =
                target.transform as RectTransform;

            navigation.SetCallbacks(
                null,
                null,
                onSelect);
            return navigation;
        }

        private static void CycleDropdown(
            TMP_Dropdown dropdown,
            int delta)
        {
            if (dropdown == null
                || !dropdown.IsInteractable()
                || dropdown.options == null
                || dropdown.options.Count == 0)
            {
                return;
            }

            int next = Mathf.Clamp(
                dropdown.value + delta,
                0,
                dropdown.options.Count - 1);
            if (next != dropdown.value)
                dropdown.value = next;
        }

        private void ConfigureGamepadNavigation()
        {
            GamepadNavigationItem backNavigation =
                backButton?.GetComponent<GamepadNavigationItem>();
            GamepadNavigationItem resetAllNavigation =
                resetAllButton?.GetComponent<GamepadNavigationItem>();

            backNavigation?.ResetCustomDirections();
            resetAllNavigation?.ResetCustomDirections();

            var primary = new List<GamepadNavigationItem>();
            var resets = new List<GamepadNavigationItem>();

            for (int i = 0; i < navigationRows.Count; i++)
            {
                NavigationRow row = navigationRows[i];
                row.Primary?.ResetCustomDirections();
                row.Secondary?.ResetCustomDirections();
                row.Reset?.ResetCustomDirections();

                bool primaryActive =
                    row.Primary != null && row.Primary.Active;
                bool secondaryActive =
                    row.Secondary != null && row.Secondary.Active;
                bool resetActive =
                    row.Reset != null && row.Reset.Active;

                if (primaryActive)
                    primary.Add(row.Primary);
                if (resetActive)
                    resets.Add(row.Reset);

                if (secondaryActive && primaryActive)
                {
                    row.Secondary.SetCustomDirectionItem(
                        GUIDirection.Left,
                        row.Primary);
                }

                if (secondaryActive && resetActive)
                {
                    row.Secondary.SetCustomDirectionItem(
                        GUIDirection.Right,
                        row.Reset);
                    row.Reset.SetCustomDirectionItem(
                        GUIDirection.Left,
                        row.Secondary);
                }
                else if (primaryActive && resetActive)
                {
                    row.Reset.SetCustomDirectionItem(
                        GUIDirection.Left,
                        row.Primary);

                    SettingsNavigationControl control =
                        row.Primary.GetComponent<SettingsNavigationControl>();
                    if (control == null
                        || !control.UsesHorizontalAdjustment)
                    {
                        row.Primary.SetCustomDirectionItem(
                            GUIDirection.Right,
                            row.Reset);
                    }
                }
            }

            if (backNavigation != null && resetAllNavigation != null)
            {
                backNavigation.SetCustomDirectionItem(
                    GUIDirection.Right,
                    resetAllNavigation,
                    setAlsoBackwardsCustomDirection: true);
            }

            LinkVerticalColumn(
                backNavigation,
                primary);
            LinkVerticalColumn(
                resetAllNavigation,
                resets);
            LinkSecondaryRows(backNavigation);
        }

        private void LinkSecondaryRows(
            GamepadNavigationItem backNavigation)
        {
            for (int i = 0; i < navigationRows.Count; i++)
            {
                GamepadNavigationItem secondary =
                    navigationRows[i].Secondary;
                if (secondary == null || !secondary.Active)
                    continue;

                GamepadNavigationItem up = backNavigation;
                for (int previous = i - 1; previous >= 0; previous--)
                {
                    GamepadNavigationItem candidate =
                        navigationRows[previous].Primary;
                    if (candidate != null && candidate.Active)
                    {
                        up = candidate;
                        break;
                    }
                }

                if (up != null)
                    secondary.SetCustomDirectionItem(
                        GUIDirection.Up,
                        up);

                for (int next = i + 1; next < navigationRows.Count; next++)
                {
                    GamepadNavigationItem candidate =
                        navigationRows[next].Primary;
                    if (candidate == null || !candidate.Active)
                        continue;

                    secondary.SetCustomDirectionItem(
                        GUIDirection.Down,
                        candidate);
                    break;
                }
            }
        }

        private static void LinkVerticalColumn(
            GamepadNavigationItem header,
            List<GamepadNavigationItem> items)
        {
            if (items == null || items.Count == 0)
                return;

            if (header != null)
            {
                header.SetCustomDirectionItem(
                    GUIDirection.Down,
                    items[0]);
                items[0].SetCustomDirectionItem(
                    GUIDirection.Up,
                    header);
            }

            for (int i = 1; i < items.Count; i++)
            {
                items[i - 1].SetCustomDirectionItem(
                    GUIDirection.Down,
                    items[i]);
                items[i].SetCustomDirectionItem(
                    GUIDirection.Up,
                    items[i - 1]);
            }
        }

        private void RebuildKeepingFocus()
        {
            GamepadNavigationController controller =
                content.GetComponentInParent<GamepadNavigationController>();

            bool restoreGamepadFocus =
                controller != null && LazyInput.IsGamepadActive;
            int focusedIndex = -1;

            if (restoreGamepadFocus
                && controller.FocusedItem != null)
            {
                GamepadNavigationItem[] oldItems =
                    content.GetComponentsInChildren<GamepadNavigationItem>(
                        includeInactive: false)
                    .Where(item => item != null && item.Active)
                    .ToArray();
                focusedIndex = Array.IndexOf(
                    oldItems,
                    controller.FocusedItem);
            }

            float scrollY = content.anchoredPosition.y;
            BuildControls();
            content.anchoredPosition = new Vector2(
                content.anchoredPosition.x,
                scrollY);

            if (!restoreGamepadFocus)
                return;

            controller.ReinitItems(
                focusOnFirstActive: false);

            GamepadNavigationItem[] newItems =
                content.GetComponentsInChildren<GamepadNavigationItem>(
                    includeInactive: false)
                    .Where(item => item != null && item.Active)
                    .ToArray();

            if (focusedIndex >= 0 && newItems.Length > 0)
            {
                int targetIndex = Mathf.Clamp(
                    focusedIndex,
                    0,
                    newItems.Length - 1);
                controller.SetFocusedItem(
                    newItems[targetIndex]);
            }
            else if (!controller.FocusOnFirstActive())
            {
                FrameworkLog.Source?.LogWarning(
                    "GK2_SETTINGS_GAMEPAD_FOCUS_RESTORE_FAILED");
            }
        }

        private GamepadNavigationItem CreateActionButton(
            IGk2Setting setting,
            RectTransform parent)
        {
            LazyButton button = FrameworkUi.CreateButton(
                "Action",
                parent,
                null,
                Convert.ToString(setting.Value),
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                Vector2.one);

            TextMeshProUGUI text =
                button.GetComponentInChildren<TextMeshProUGUI>();
            FrameworkUi.ApplyValueText(text);

            button.onClick.AddListener(() =>
            {
                try
                {
                    ((ButtonSetting)setting).Click?.Invoke();
                }
                catch (Exception ex)
                {
                    FrameworkLog.Error(
                        "GK2_MOD_SETTING_BUTTON_FAILED: "
                        + selected?.Metadata.Id + "/"
                        + setting.UniqueKey + ": " + ex);
                }

                if (text != null)
                    text.text = Convert.ToString(setting.Value);

                selected?.Settings.RefreshConditions();
            });

            button.SetCallbacksIntoGamepadNavigationItem();
            return button.GetComponent<GamepadNavigationItem>();
        }

        private void CreateValueText(IGk2Setting setting, RectTransform parent)
        {
            TextMeshProUGUI text = FrameworkUi.CreateText(
                "ReadOnly", parent, NativeUiSkin.IsReady ? 16f : 15f,
                TextAlignmentOptions.Left, Color.white);
            FrameworkUi.ApplyValueText(text);
            FrameworkUi.SetRect(
                text.rectTransform,
                new Vector2(8f, 2f),
                new Vector2(-8f, -2f),
                Vector2.zero,
                Vector2.one);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.text = Convert.ToString(setting.Value);
        }

        private void ResetSetting(IGk2Setting setting)
        {
            setting.ResetToDefault();
            FrameworkLog.Source?.LogInfo(
                "GK2_MOD_SETTING_RESET: " + selected.Metadata.Id + "/" + setting.UniqueKey);
            RebuildKeepingFocus();
        }

        private void ResetAllSettings()
        {
            if (selected == null) return;
            foreach (IGk2Setting setting in selected.Settings.Items)
            {
                if (!setting.IsReadOnly) setting.ResetToDefault();
            }

            FrameworkLog.Source?.LogInfo(
                "GK2_MOD_SETTING_RESET: " + selected.Metadata.Id + "/*");
            RebuildKeepingFocus();
        }

        private void LogChanged(IGk2Setting setting)
        {
            FrameworkLog.Source?.LogInfo(
                "GK2_MOD_SETTING_CHANGED: "
                + selected.Metadata.Id + "/" + setting.UniqueKey + "=" + setting.Value);
        }
    }
}
