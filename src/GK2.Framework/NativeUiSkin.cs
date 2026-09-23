using System;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2.Framework
{
    internal static class NativeUiSkin
    {
        internal static bool IsReady { get; private set; }

        internal static TMP_FontAsset RegularFont { get; private set; }
        internal static TMP_FontAsset BoldFont { get; private set; }
        internal static Material RegularMaterial { get; private set; }
        internal static Material BoldMaterial { get; private set; }

        internal static Color LabelColor { get; private set; }
        internal static Color ValueColor { get; private set; }
        internal static Color ButtonTextColor { get; private set; }
        internal static Color HintColor { get; private set; }

        internal static Sprite FrameSprite { get; private set; }
        internal static Sprite SideSprite { get; private set; }
        internal static Sprite SideTopSprite { get; private set; }
        internal static Sprite SideBottomSprite { get; private set; }
        internal static Sprite HeaderSprite { get; private set; }
        internal static Sprite HeaderDecorSprite { get; private set; }
        internal static Sprite CellSprite { get; private set; }

        internal static Sprite DialogButtonSprite { get; private set; }
        internal static ColorBlock DialogButtonColors { get; private set; }
        internal static SpriteState DialogButtonSpriteState { get; private set; }

        internal static Sprite SmallButtonSprite { get; private set; }
        internal static ColorBlock SmallButtonColors { get; private set; }
        internal static SpriteState SmallButtonSpriteState { get; private set; }

        internal static Sprite ProgressBackgroundSprite { get; private set; }
        internal static Sprite ProgressFillSprite { get; private set; }
        internal static Sprite SliderHandleSprite { get; private set; }
        internal static ColorBlock SliderColors { get; private set; }
        internal static SpriteState SliderSpriteState { get; private set; }

        internal static Sprite StatusOkSprite { get; private set; }
        internal static Sprite StatusOffSprite { get; private set; }
        internal static Sprite StatusWarningSprite { get; private set; }
        internal static Sprite StatusErrorSprite { get; private set; }

        internal static bool TryCapture()
        {
            if (IsReady) return true;

            try
            {
                UIGameSettingsWindow window = LazyUI.GetWindow<UIGameSettingsWindow>();
                if (window == null) return false;

                Transform root = window.transform;
                Transform layout = root.Find("GenericWIndowLayout");
                Transform content = layout?.Find("Content");
                Transform frame = layout?.Find("Frame");
                Transform header = frame?.Find("HeaderGroup");

                FrameSprite = GetImage(frame)?.sprite;
                SideSprite = GetImage(frame?.Find("BackMask/SideL"))?.sprite;
                SideBottomSprite = GetImage(frame?.Find("BackMask/SideL/DecorBot"))?.sprite;
                SideTopSprite = GetImage(frame?.Find("BackMask/SideL/DecorTop"))?.sprite;
                HeaderSprite = GetImage(header?.Find("Background"))?.sprite;
                HeaderDecorSprite = GetImage(header?.Find("DecorCommonLeft"))?.sprite;

                TextMeshProUGUI headerText = header?.Find("Header")?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI labelText =
                    content?.Find("ResolutionSwitchBtn/LeftName")?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI valueText =
                    content?.Find("ResolutionSwitchBtn/Back/Value")?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI hintText =
                    layout?.Find("ButtonTipsStr")?.GetComponent<TextMeshProUGUI>();

                RegularFont = labelText?.font;
                RegularMaterial = labelText?.fontSharedMaterial;
                BoldFont = headerText?.font;
                BoldMaterial = headerText?.fontSharedMaterial;
                LabelColor = labelText != null ? labelText.color : new Color(0.59f, 0.55f, 0.53f, 1f);
                ValueColor = valueText != null ? valueText.color : new Color(1f, 0.74f, 0f, 1f);
                HintColor = hintText != null ? hintText.color : LabelColor;

                Image cell = GetImage(content?.Find("ResolutionSwitchBtn/Back"));
                CellSprite = cell?.sprite;

                LazyButton dialogButton =
                    content?.Find("DialogueButtonPrefab")?.GetComponent<LazyButton>();
                if (dialogButton?.targetGraphic is Image dialogImage)
                {
                    DialogButtonSprite = dialogImage.sprite;
                    DialogButtonColors = dialogButton.colors;
                    DialogButtonSpriteState = dialogButton.spriteState;
                }

                TextMeshProUGUI dialogText =
                    content?.Find("DialogueButtonPrefab/Content/Back/Label")
                        ?.GetComponent<TextMeshProUGUI>();
                ButtonTextColor = dialogText != null ? dialogText.color : ValueColor;

                LazyButton smallButton =
                    content?.Find("ResolutionSwitchBtn/ToLeft")?.GetComponent<LazyButton>();
                if (smallButton?.targetGraphic is Image smallImage)
                {
                    SmallButtonSprite = smallImage.sprite;
                    SmallButtonColors = smallButton.colors;
                    SmallButtonSpriteState = smallButton.spriteState;
                }

                Slider nativeSlider =
                    content?.Find("MasterVolume/Slider")?.GetComponent<Slider>();
                if (nativeSlider != null)
                {
                    ProgressBackgroundSprite =
                        GetImage(content.Find("MasterVolume/Slider/Background"))?.sprite;
                    ProgressFillSprite =
                        GetImage(content.Find("MasterVolume/Slider/Fill Area/Fill"))?.sprite;
                    SliderHandleSprite =
                        GetImage(content.Find("MasterVolume/Slider/Handle Slide Area/Handle"))?.sprite;
                    SliderColors = nativeSlider.colors;
                    SliderSpriteState = nativeSlider.spriteState;
                }

                Sprite[] loadedSprites = Resources.FindObjectsOfTypeAll<Sprite>();
                StatusOkSprite = FindSprite(loadedSprites, "btn_i-check", "icon_checkmark");
                StatusOffSprite = FindSprite(
                    loadedSprites, "btn_i-check_inactive", "icon-item_checkbox");
                StatusWarningSprite = FindSprite(
                    loadedSprites, "fishing_attention_mark_at_0");
                StatusErrorSprite = FindSprite(
                    loadedSprites, "btn_i-close_cross-s", "cross");

                IsReady =
                    RegularFont != null
                    && BoldFont != null
                    && FrameSprite != null
                    && HeaderSprite != null
                    && CellSprite != null
                    && DialogButtonSprite != null;

                FrameworkLog.Source?.LogInfo(
                    "GK2_NATIVE_UI_SKIN_CAPTURE: "
                    + "regularFont=" + (RegularFont?.name ?? "<null>")
                    + ";boldFont=" + (BoldFont?.name ?? "<null>")
                    + ";frame=" + (FrameSprite?.name ?? "<null>")
                    + ";header=" + (HeaderSprite?.name ?? "<null>")
                    + ";cell=" + (CellSprite?.name ?? "<null>")
                    + ";dialog=" + (DialogButtonSprite?.name ?? "<null>")
                    + ";sliderBg=" + (ProgressBackgroundSprite?.name ?? "<null>")
                    + ";sliderFill=" + (ProgressFillSprite?.name ?? "<null>")
                    + ";sliderHandle=" + (SliderHandleSprite?.name ?? "<null>"));

                FrameworkLog.Source?.LogInfo(
                    IsReady
                        ? "GK2_NATIVE_UI_SKIN_READY"
                        : "GK2_NATIVE_UI_SKIN_PARTIAL");

                FrameworkLog.Source?.LogInfo(
                    "GK2_NATIVE_STATUS_ICONS: ok=" + SpriteName(StatusOkSprite)
                    + ";off=" + SpriteName(StatusOffSprite)
                    + ";warning=" + SpriteName(StatusWarningSprite)
                    + ";error=" + SpriteName(StatusErrorSprite));
                return IsReady;
            }
            catch (Exception ex)
            {
                FrameworkLog.Source?.LogWarning(
                    "GK2 native UI skin capture failed; using framework fallback: " + ex.Message);
                return false;
            }
        }

        private static Image GetImage(Transform transform) =>
            transform == null ? null : transform.GetComponent<Image>();

        internal static Sprite GetStatusSprite(ModUiSeverity severity)
        {
            switch (severity)
            {
                case ModUiSeverity.Ok: return StatusOkSprite;
                case ModUiSeverity.Off: return StatusOffSprite;
                case ModUiSeverity.Warning: return StatusWarningSprite;
                case ModUiSeverity.Error: return StatusErrorSprite;
                default: return null;
            }
        }

        private static Sprite FindSprite(Sprite[] sprites, params string[] preferredNames)
        {
            if (sprites == null || preferredNames == null) return null;
            foreach (string preferredName in preferredNames)
            {
                foreach (Sprite sprite in sprites)
                {
                    if (sprite != null && string.Equals(
                        sprite.name, preferredName, StringComparison.OrdinalIgnoreCase))
                        return sprite;
                }
            }
            return null;
        }

        private static string SpriteName(Sprite sprite) => sprite?.name ?? "<fallback>";
    }
}
