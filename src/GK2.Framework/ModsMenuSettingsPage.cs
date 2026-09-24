using System;
using System.Collections.Generic;
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

        private RegisteredMod selected;
        private IGk2Setting capturingKeybind;
        private TextMeshProUGUI captureLabel;
        private KeyCode pendingMainModifier = KeyCode.None;

        internal bool IsOpen => page.activeSelf;
        internal bool IsCapturingKeybind => capturingKeybind != null;

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

            LazyButton back = FrameworkUi.CreateButton(
                "Back",
                settings.rectTransform,
                NativeUiSkin.IsReady ? null : template,
                FrameworkUi.L("common.back", "Back"),
                new Vector2(-206f, -42f),
                new Vector2(-112f, -14f),
                Vector2.one,
                Vector2.one);
            FrameworkUi.ApplyDialogButton(back);
            back.onClick.AddListener(() => this.requestClose());
            back.SetCallbacksIntoGamepadNavigationItem();

            LazyButton resetAll = FrameworkUi.CreateButton(
                "ResetAll",
                settings.rectTransform,
                NativeUiSkin.IsReady ? null : template,
                FrameworkUi.L("settings.reset_all", "Reset all"),
                new Vector2(-104f, -42f),
                new Vector2(-12f, -14f),
                Vector2.one,
                Vector2.one);
            FrameworkUi.ApplyDialogButton(resetAll);
            resetAll.onClick.AddListener(ResetAllSettings);
            resetAll.SetCallbacksIntoGamepadNavigationItem();

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
            selected = mod;
            page.SetActive(true);
            BuildControls();
            FrameworkLog.Source?.LogInfo("GK2_MOD_SETTINGS_OPENED: " + selected.Metadata.Id);
        }

        internal void Close()
        {
            CancelKeybindCapture(false);
            page.SetActive(false);
            FrameworkLog.Source?.LogInfo("GK2_MOD_SETTINGS_CLOSED: " + selected?.Metadata.Id);
        }

        internal void HideWithoutLog()
        {
            CancelKeybindCapture(false);
            page.SetActive(false);
        }

        internal bool HandleUpdate()
        {
            if (capturingKeybind == null) return false;
            CaptureKeybind();
            return true;
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

        private void BuildControls()
        {
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                content.GetChild(i).gameObject.SetActive(false);
                UnityEngine.Object.Destroy(content.GetChild(i).gameObject);
            }

            title.text = selected.Metadata.Name + " — " + FrameworkUi.L("mods.settings", "Settings");
            List<IGk2Setting> settings = selected.Settings.Items
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

            switch (setting.Kind)
            {
                case SettingKind.Toggle:
                    CreateToggle(setting, control);
                    break;
                case SettingKind.IntegerSlider:
                case SettingKind.FloatSlider:
                    CreateSlider(setting, control);
                    break;
                case SettingKind.Dropdown:
                    CreateChoice(setting, control);
                    break;
                case SettingKind.Keybind:
                    CreateKeybind(setting, control);
                    break;
                case SettingKind.Text:
                    CreateTextInput(setting, control);
                    break;
                default:
                    CreateValueText(setting, control);
                    break;
            }

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
            }

            return rowHeight;
        }

        private void CreateToggle(IGk2Setting setting, RectTransform parent)
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
        }

        private void CreateSlider(IGk2Setting setting, RectTransform parent)
        {
            GameObject go = new GameObject("Slider", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            FrameworkUi.Stretch((RectTransform)go.transform);

            Slider slider = go.AddComponent<Slider>();
            Image bg = FrameworkUi.CreateImage(
                "Background", go.transform, new Color(0.2f, 0.1f, 0.07f, 1f));
            FrameworkUi.Stretch(bg.rectTransform);
            if (NativeUiSkin.IsReady && NativeUiSkin.ProgressBackgroundSprite != null)
            {
                bg.sprite = NativeUiSkin.ProgressBackgroundSprite;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }

            Image fill = FrameworkUi.CreateImage(
                "Fill", go.transform, new Color(0.75f, 0.32f, 0.12f, 1f));
            FrameworkUi.Stretch(fill.rectTransform);
            if (NativeUiSkin.IsReady && NativeUiSkin.ProgressFillSprite != null)
            {
                fill.sprite = NativeUiSkin.ProgressFillSprite;
                fill.type = Image.Type.Sliced;
                fill.color = Color.white;
            }
            slider.fillRect = fill.rectTransform;

            Image handle = FrameworkUi.CreateImage(
                "Handle", go.transform, new Color(1f, 0.75f, 0.35f, 1f));
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
            slider.value = Convert.ToSingle(setting.Value);

            TextMeshProUGUI valueText = FrameworkUi.CreateText(
                "Value", go.transform, 14f, TextAlignmentOptions.Center, Color.white);
            FrameworkUi.ApplyValueText(valueText);
            FrameworkUi.Stretch(valueText.rectTransform);

            Action<float> refresh = value =>
                valueText.text = setting.Kind == SettingKind.IntegerSlider
                    ? ((int)value).ToString()
                    : value.ToString("0.######");

            refresh(slider.value);
            slider.onValueChanged.AddListener(value =>
            {
                double step = setting.Step;
                double snapped = step > 0 ? Math.Round(value / step) * step : value;
                if (setting.Kind == SettingKind.IntegerSlider)
                    setting.Value = Convert.ToInt32(snapped);
                else
                    setting.Value = Convert.ToSingle(snapped);

                refresh(Convert.ToSingle(setting.Value));
                LogChanged(setting);
            });
        }

        private void CreateChoice(IGk2Setting setting, RectTransform parent)
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
        }

        private void CreateKeybind(IGk2Setting setting, RectTransform parent)
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

        private void CreateTextInput(IGk2Setting setting, RectTransform parent)
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
            input.textViewport = textViewport;
            input.textComponent = text;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.text = Convert.ToString(setting.Value);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Masking;
            input.onEndEdit.AddListener(value =>
            {
                setting.Value = value;
                LogChanged(setting);
            });
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
            BuildControls();
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
            BuildControls();
        }

        private void LogChanged(IGk2Setting setting)
        {
            FrameworkLog.Source?.LogInfo(
                "GK2_MOD_SETTING_CHANGED: "
                + selected.Metadata.Id + "/" + setting.UniqueKey + "=" + setting.Value);
        }
    }
}
