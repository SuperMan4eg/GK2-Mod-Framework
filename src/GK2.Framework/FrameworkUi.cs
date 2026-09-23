using BepInEx.Configuration;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2.Framework
{
    internal static class FrameworkUi
    {
        internal static TextMeshProUGUI StyleSource { get; set; }
        internal static ConfigEntry<int> WindowScalePercent { get; set; }

        internal static string L(string key, string fallback) => FrameworkLocalization.Get(key, fallback);

        internal static float GetWindowScaleLimit()
        {
            int percent = WindowScalePercent?.Value ?? 100;
            return Mathf.Clamp(percent / 100f, 0.5f, 1f);
        }

        internal static float CalculateResponsiveWindowScale(Vector2 baseSize, float gameScaleFactor, Rect safeArea)
        {
            if (baseSize.x <= 0f || baseSize.y <= 0f) return GetWindowScaleLimit();

            float scaleFactor = gameScaleFactor > 0.001f ? gameScaleFactor : 1f;
            float safeWidth = Mathf.Max(1f, safeArea.width / scaleFactor);
            float safeHeight = Mathf.Max(1f, safeArea.height / scaleFactor);
            const float margin = 16f;

            float fitWidth = Mathf.Max(0.01f, safeWidth - margin * 2f) / baseSize.x;
            float fitHeight = Mathf.Max(0.01f, safeHeight - margin * 2f) / baseSize.y;
            float fit = Mathf.Min(1f, fitWidth, fitHeight);
            return Mathf.Clamp(Mathf.Min(fit, GetWindowScaleLimit()), 0.5f, 1f);
        }

        internal static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        internal static TextMeshProUGUI CreateText(string name, Transform parent, float size,
            TextAlignmentOptions alignment, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            if (NativeUiSkin.IsReady && NativeUiSkin.RegularFont != null)
            {
                text.font = NativeUiSkin.RegularFont;
                if (NativeUiSkin.RegularMaterial != null)
                    text.fontSharedMaterial = NativeUiSkin.RegularMaterial;
            }
            else if (StyleSource != null)
            {
                text.font = StyleSource.font;
                text.fontSharedMaterial = StyleSource.fontSharedMaterial;
            }
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        internal static LazyButton CreateButton(string name, Transform parent, LazyButton template, string label,
            Vector2 offsetMin, Vector2 offsetMax, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            SetRect(rect, offsetMin, offsetMax, anchorMin, anchorMax);

            Image image = go.AddComponent<Image>();
            image.color = new Color(0.28f, 0.12f, 0.07f, 1f);
            if (template?.targetGraphic is Image source)
            {
                image.sprite = source.sprite;
                image.type = source.type;
                image.material = source.material;
            }
            else if (NativeUiSkin.IsReady && NativeUiSkin.CellSprite != null)
            {
                image.sprite = NativeUiSkin.CellSprite;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }

            LazyButton button = go.AddComponent<LazyButton>();
            button.targetGraphic = image;
            if (template != null)
            {
                button.transition = template.transition;
                button.colors = template.colors;
                button.spriteState = template.spriteState;
            }
            else if (NativeUiSkin.IsReady)
            {
                button.transition = Selectable.Transition.ColorTint;
                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 0.92f, 0.72f, 1f);
                colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
                colors.selectedColor = colors.highlightedColor;
                colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.5f);
                colors.colorMultiplier = 1f;
                colors.fadeDuration = 0.1f;
                button.colors = colors;
            }

            go.AddComponent<GamepadNavigationItem>();
            TextMeshProUGUI text = CreateText("Label", go.transform, 17f, TextAlignmentOptions.Center, Color.white);
            Stretch(text.rectTransform);
            text.text = label;
            return button;
        }

        internal static void ApplyHeaderText(TextMeshProUGUI text)
        {
            if (text == null || !NativeUiSkin.IsReady) return;
            if (NativeUiSkin.BoldFont != null) text.font = NativeUiSkin.BoldFont;
            if (NativeUiSkin.BoldMaterial != null)
                text.fontSharedMaterial = NativeUiSkin.BoldMaterial;
            text.color = Color.white;
        }

        internal static void ApplyLabelText(TextMeshProUGUI text)
        {
            if (text == null || !NativeUiSkin.IsReady) return;
            if (NativeUiSkin.RegularFont != null) text.font = NativeUiSkin.RegularFont;
            if (NativeUiSkin.RegularMaterial != null)
                text.fontSharedMaterial = NativeUiSkin.RegularMaterial;
            text.color = NativeUiSkin.LabelColor;
        }

        internal static void ApplyValueText(TextMeshProUGUI text)
        {
            if (text == null || !NativeUiSkin.IsReady) return;
            if (NativeUiSkin.RegularFont != null) text.font = NativeUiSkin.RegularFont;
            if (NativeUiSkin.RegularMaterial != null)
                text.fontSharedMaterial = NativeUiSkin.RegularMaterial;
            text.color = NativeUiSkin.ValueColor;
        }

        internal static void ApplyCell(Image image)
        {
            if (image == null || !NativeUiSkin.IsReady || NativeUiSkin.CellSprite == null) return;
            image.sprite = NativeUiSkin.CellSprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        internal static void ApplyFrame(Image image)
        {
            if (image == null || !NativeUiSkin.IsReady || NativeUiSkin.FrameSprite == null) return;
            image.sprite = NativeUiSkin.FrameSprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        internal static void ApplyDialogButton(LazyButton button)
        {
            if (button == null || !NativeUiSkin.IsReady) return;
            if (button.targetGraphic is Image image && NativeUiSkin.DialogButtonSprite != null)
            {
                image.sprite = NativeUiSkin.DialogButtonSprite;
                image.type = Image.Type.Tiled;
                image.color = Color.white;
            }

            button.transition = Selectable.Transition.SpriteSwap;
            button.colors = NativeUiSkin.DialogButtonColors;
            button.spriteState = NativeUiSkin.DialogButtonSpriteState;

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                if (NativeUiSkin.BoldFont != null) label.font = NativeUiSkin.BoldFont;
                if (NativeUiSkin.BoldMaterial != null)
                    label.fontSharedMaterial = NativeUiSkin.BoldMaterial;
                label.fontSize = 16f;
                label.color = NativeUiSkin.ButtonTextColor;
            }
        }

        internal static void ApplyGearButton(LazyButton button)
        {
            if (button == null) return;

            if (button.targetGraphic is Image image)
            {
                image.sprite = NativeUiSkin.SettingsGearSprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.color = Color.white;
                image.material = null;
            }

            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.68f, 0.68f, 0.68f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.45f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.spriteState = default;

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null) label.gameObject.SetActive(false);
        }

        internal static RectTransform CreateVerticalScrollContent(Image frame, out ScrollRect scroll)
        {
            scroll = frame.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32f;

            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportObject.transform.SetParent(frame.transform, false);
            RectTransform viewport = (RectTransform)viewportObject.transform;
            Stretch(viewport);

            RectTransform content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            scroll.viewport = viewport;
            scroll.content = content;
            return content;
        }

        internal static void SetContentHeight(RectTransform content, float height)
        {
            Vector2 size = content.sizeDelta;
            size.y = Mathf.Max(0f, height);
            content.sizeDelta = size;
            content.anchoredPosition = Vector2.zero;
        }

        internal static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        internal static void SetRect(RectTransform rect, Vector2 min, Vector2 max,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = min;
            rect.offsetMax = max;
        }
    }
}
