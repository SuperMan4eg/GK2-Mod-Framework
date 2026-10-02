using System;
using System.Collections.Generic;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2.Framework
{
    // The search input belongs to the frame, never to its scrolling content.
    internal sealed class ModsMenuSearch
    {
        internal TMP_InputField Input { get; }
        internal GamepadNavigationItem Navigation { get; }
        private readonly RectTransform overlayRoot;
        private readonly string label;
        private readonly List<GameObject> hidden = new List<GameObject>();
        private SettingsVirtualKeyboard keyboard;
        private GamepadNavigationController controller;

        internal ModsMenuSearch(RectTransform parent, RectTransform overlayRoot,
            string label, float top, Action<string> changed)
        {
            this.overlayRoot = overlayRoot;
            this.label = label;
            Image background = FrameworkUi.CreateImage("Search", parent, new Color(0.18f, 0.09f, 0.06f, 1f));
            FrameworkUi.ApplyCell(background);
            FrameworkUi.SetRect(background.rectTransform, new Vector2(10f, top - 30f),
                new Vector2(-46f, top), new Vector2(0f, 1f), Vector2.one);
            Input = background.gameObject.AddComponent<TMP_InputField>();
            RectTransform viewport = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D))
                .GetComponent<RectTransform>();
            viewport.SetParent(background.transform, false);
            FrameworkUi.SetRect(viewport, new Vector2(8f, 2f), new Vector2(-8f, -2f), Vector2.zero, Vector2.one);
            TextMeshProUGUI text = FrameworkUi.CreateText("Text", viewport, 15f, TextAlignmentOptions.MidlineLeft, Color.white);
            FrameworkUi.ApplyValueText(text);
            FrameworkUi.Stretch(text.rectTransform);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Masking;
            TextMeshProUGUI placeholder = FrameworkUi.CreateText("Placeholder", viewport, 15f, TextAlignmentOptions.MidlineLeft, Color.white);
            FrameworkUi.ApplyLabelText(placeholder);
            FrameworkUi.Stretch(placeholder.rectTransform);
            placeholder.text = label;
            placeholder.textWrappingMode = TextWrappingModes.NoWrap;
            placeholder.overflowMode = TextOverflowModes.Ellipsis;
            placeholder.color = new Color(1f, 1f, 1f, 0.55f);
            Input.targetGraphic = background;
            Input.textViewport = viewport;
            Input.textComponent = text;
            Input.placeholder = placeholder;
            Input.lineType = TMP_InputField.LineType.SingleLine;
            Input.characterLimit = 256;
            Input.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            Input.customCaretColor = true;
            Input.caretColor = Color.white;
            Input.caretWidth = 2;
            Input.enabled = false;
            Input.enabled = true;
            RuntimeInputFieldActivator activator = background.gameObject.AddComponent<RuntimeInputFieldActivator>();
            activator.Input = Input;
            activator.GamepadFallback = field => OpenKeyboard();
            Navigation = background.gameObject.AddComponent<GamepadNavigationItem>();
            Navigation.focusFrame = null;
            Navigation.FocusRectTransform = background.rectTransform;
            Navigation.SetCallbacks(null, null, () => RuntimeInputFieldActivator.Activate(Input));
            LazyButton clear = FrameworkUi.CreateButton("ClearSearch", parent, null, "×",
                new Vector2(-40f, top - 30f), new Vector2(-10f, top), Vector2.one, Vector2.one);
            clear.onClick.AddListener(() => Input.text = string.Empty);
            clear.SetCallbacksIntoGamepadNavigationItem();
            GamepadNavigationItem clearNavigation = clear.GetComponent<GamepadNavigationItem>();
            Navigation.SetCustomDirectionItem(GUIDirection.Right, clearNavigation, setAlsoBackwardsCustomDirection: true);
            Input.onValueChanged.AddListener(value => changed(value));
        }

        internal static bool Matches(string query, params string[] values)
        {
            string term = (query ?? string.Empty).Trim();
            if (term.Length == 0) return true;
            foreach (string value in values)
                if (value != null && value.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private void OpenKeyboard()
        {
            if (keyboard != null) return;
            controller = overlayRoot.GetComponentInParent<GamepadNavigationController>();
            if (controller == null) return;
            Input.DeactivateInputField();
            hidden.Clear();
            foreach (Transform child in overlayRoot)
                if (child.gameObject.activeSelf) { hidden.Add(child.gameObject); child.gameObject.SetActive(false); }
            keyboard = new SettingsVirtualKeyboard(overlayRoot, null, controller, label,
                Input.text, false, false, 256, value => CloseKeyboard(value), () => CloseKeyboard(null));
        }

        private void CloseKeyboard(string value)
        {
            if (keyboard == null) return;
            keyboard.Dispose();
            keyboard = null;
            foreach (GameObject child in hidden) if (child != null) child.SetActive(true);
            hidden.Clear();
            if (value != null) Input.text = value;
            controller.ReinitItems(focusOnFirstActive: false);
            if (Navigation.gameObject.activeInHierarchy) controller.SetFocusedItem(Navigation);
        }

        internal bool HandleUpdate() => keyboard != null && keyboard.HandleUpdate();
        internal bool CancelEditing()
        {
            if (keyboard != null) { CloseKeyboard(null); return true; }
            if (!Input.isFocused) return false;
            Input.DeactivateInputField();
            return true;
        }
    }
}
