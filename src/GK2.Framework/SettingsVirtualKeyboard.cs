using System;
using System.Collections.Generic;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2.Framework
{
    internal sealed class SettingsVirtualKeyboard
    {
        private readonly GameObject root;
        private readonly TextMeshProUGUI preview;
        private readonly GamepadNavigationController navigation;
        private readonly Action<string> submit;
        private readonly Action cancel;
        private readonly bool numeric;
        private readonly bool integerOnly;
        private readonly int maxLength;
        private readonly List<List<GamepadNavigationItem>> rows =
            new List<List<GamepadNavigationItem>>();
        private readonly List<LetterKey> letterKeys =
            new List<LetterKey>();

        private string value;
        private bool upperCase;
        private bool closed;

        private sealed class LetterKey
        {
            internal string Lower { get; }
            internal TextMeshProUGUI Label { get; }
            internal LetterKey(string lower, TextMeshProUGUI label)
            {
                Lower = lower;
                Label = label;
            }
        }

        internal bool IsOpen =>
            !closed && root != null && root.activeSelf;

        internal SettingsVirtualKeyboard(
            RectTransform parent,
            LazyButton template,
            GamepadNavigationController navigation,
            string titleText,
            string initialValue,
            bool numeric,
            bool integerOnly,
            int maxLength,
            Action<string> submit,
            Action cancel)
        {
            this.navigation = navigation
                ?? throw new ArgumentNullException(nameof(navigation));
            this.submit = submit
                ?? throw new ArgumentNullException(nameof(submit));
            this.cancel = cancel
                ?? throw new ArgumentNullException(nameof(cancel));
            this.numeric = numeric;
            this.integerOnly = integerOnly;
            this.maxLength = Mathf.Max(1, maxLength);
            value = initialValue ?? string.Empty;
            Image overlay = FrameworkUi.CreateImage(
                "VirtualKeyboard",
                parent,
                new Color(0f, 0f, 0f, 0.94f));
            FrameworkUi.Stretch(overlay.rectTransform);
            overlay.raycastTarget = true;
            root = overlay.gameObject;
            root.transform.SetAsLastSibling();

            Image panel = FrameworkUi.CreateImage(
                "Panel",
                overlay.rectTransform,
                NativeUiSkin.IsReady
                    ? Color.white
                    : new Color(0.08f, 0.045f, 0.03f, 1f));
            FrameworkUi.ApplyCell(panel);
            FrameworkUi.SetRect(
                panel.rectTransform,
                new Vector2(36f, 32f),
                new Vector2(-36f, -32f),
                Vector2.zero,
                Vector2.one);

            TextMeshProUGUI title = FrameworkUi.CreateText(
                "Title",
                panel.rectTransform,
                19f,
                TextAlignmentOptions.Center,
                Color.white);
            FrameworkUi.ApplyHeaderText(title);
            FrameworkUi.SetRect(
                title.rectTransform,
                new Vector2(20f, -16f),
                new Vector2(-20f, -48f),
                new Vector2(0f, 1f),
                Vector2.one);
            title.text = titleText ?? string.Empty;

            Image previewBackground = FrameworkUi.CreateImage(
                "Preview",
                panel.rectTransform,
                new Color(0.12f, 0.07f, 0.05f, 1f));
            FrameworkUi.ApplyCell(previewBackground);
            FrameworkUi.SetRect(
                previewBackground.rectTransform,
                new Vector2(32f, -92f),
                new Vector2(-32f, -58f),
                new Vector2(0f, 1f),
                Vector2.one);

            preview = FrameworkUi.CreateText(
                "Value",
                previewBackground.rectTransform,
                18f,
                TextAlignmentOptions.MidlineLeft,
                Color.white);
            FrameworkUi.ApplyValueText(preview);
            FrameworkUi.SetRect(
                preview.rectTransform,
                new Vector2(10f, 2f),
                new Vector2(-10f, -2f),
                Vector2.zero,
                Vector2.one);
            preview.textWrappingMode = TextWrappingModes.NoWrap;
            preview.overflowMode = TextOverflowModes.Ellipsis;
            GameObject gridObject = new GameObject(
                "Keys",
                typeof(RectTransform),
                typeof(GridLayoutGroup));
            gridObject.transform.SetParent(panel.transform, false);
            RectTransform gridRect =
                (RectTransform)gridObject.transform;
            FrameworkUi.SetRect(
                gridRect,
                new Vector2(24f, 22f),
                new Vector2(-24f, -112f),
                Vector2.zero,
                Vector2.one);

            GridLayoutGroup grid =
                gridObject.GetComponent<GridLayoutGroup>();
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.spacing = new Vector2(6f, 6f);
            grid.constraint =
                GridLayoutGroup.Constraint.FixedColumnCount;

            if (numeric)
            {
                grid.constraintCount = 4;
                grid.cellSize = new Vector2(112f, 46f);
                BuildNumericKeys(gridRect, template);
            }
            else
            {
                grid.constraintCount = 10;
                grid.cellSize = new Vector2(64f, 42f);
                BuildTextKeys(gridRect, template);
            }

            RefreshPreview();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);
            LinkNavigation();

            List<GamepadNavigationItem> items =
                new List<GamepadNavigationItem>();
            for (int row = 0; row < rows.Count; row++)
                items.AddRange(rows[row]);

            navigation.ReinitItems(
                focusOnFirstActive: false,
                customItems: items);
            if (items.Count > 0)
                navigation.SetFocusedItem(items[0]);
        }

        internal bool HandleUpdate()
        {
            if (!IsOpen)
                return false;

            HandlePhysicalKeyboardInput();
            if (!IsOpen)
                return true;

            if (Consume(GameKey.Back))
            {
                LazyInput.WaitForRelease(GameKey.Back);
                Cancel();
                return true;
            }

            if (Consume(GameKey.Select))
            {
                LazyInput.WaitForRelease(GameKey.Select);
                navigation.SelectFocusedItem();
                return true;
            }
            if (Consume(GameKey.Left)
                || Consume(GameKey.DpadLeft))
            {
                navigation.Navigate(GUIDirection.Left);
                return true;
            }

            if (Consume(GameKey.Right)
                || Consume(GameKey.DpadRight))
            {
                navigation.Navigate(GUIDirection.Right);
                return true;
            }

            if (Consume(GameKey.Up)
                || Consume(GameKey.DpadUp))
            {
                navigation.Navigate(GUIDirection.Up);
                return true;
            }

            if (Consume(GameKey.Down)
                || Consume(GameKey.DpadDown))
            {
                navigation.Navigate(GUIDirection.Down);
                return true;
            }

            return true;
        }

        internal void Dispose()
        {
            if (closed)
                return;

            closed = true;
            if (root != null)
            {
                root.SetActive(false);
                UnityEngine.Object.Destroy(root);
            }
        }

        private void BuildNumericKeys(
            RectTransform parent,
            LazyButton template)
        {
            string[][] layout =
            {
                new[] { "7", "8", "9", "Back" },
                new[] { "4", "5", "6", "-" },
                new[] { "1", "2", "3", "Clear" },
                new[] { "0", integerOnly ? "00" : ".", "OK", "Cancel" }
            };

            for (int row = 0; row < layout.Length; row++)
            {
                var navigationRow =
                    new List<GamepadNavigationItem>();
                for (int column = 0;
                    column < layout[row].Length;
                    column++)
                {
                    string key = layout[row][column];
                    navigationRow.Add(
                        CreateKey(
                            parent,
                            template,
                            key,
                            () => HandleNumericKey(key)));
                }
                rows.Add(navigationRow);
            }
        }

        private void BuildTextKeys(
            RectTransform parent,
            LazyButton template)
        {
            string[][] layout =
            {
                new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" },
                new[] { "q", "w", "e", "r", "t", "y", "u", "i", "o", "p" },
                new[] { "a", "s", "d", "f", "g", "h", "j", "k", "l", "Back" },
                new[] { "Shift", "z", "x", "c", "v", "b", "n", "m", ".", "," },
                new[] { "-", "_", "/", ":", "@", "+", "Space", "Clear", "OK", "Cancel" }
            };

            for (int row = 0; row < layout.Length; row++)
            {
                var navigationRow =
                    new List<GamepadNavigationItem>();
                for (int column = 0;
                    column < layout[row].Length;
                    column++)
                {
                    string key = layout[row][column];
                    navigationRow.Add(
                        CreateKey(
                            parent,
                            template,
                            key,
                            () => HandleTextKey(key)));
                }
                rows.Add(navigationRow);
            }
        }

        private GamepadNavigationItem CreateKey(
            RectTransform parent,
            LazyButton template,
            string label,
            Action onClick)
        {
            LazyButton button = FrameworkUi.CreateButton(
                "Key_" + label,
                parent,
                NativeUiSkin.IsReady ? null : template,
                DisplayLabel(label),
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                Vector2.one);
            FrameworkUi.ApplyDialogButton(button);
            button.onClick.AddListener(() => onClick());
            button.SetCallbacksIntoGamepadNavigationItem();

            TextMeshProUGUI text =
                button.GetComponentInChildren<TextMeshProUGUI>();
            FrameworkUi.ApplyValueText(text);

            if (!numeric
                && label.Length == 1
                && char.IsLetter(label[0]))
            {
                letterKeys.Add(new LetterKey(label, text));
            }

            return button.GetComponent<GamepadNavigationItem>();
        }

        private static string DisplayLabel(string key)
        {
            if (key == "Back")
                return FrameworkUi.L(
                    "settings.keyboard.back",
                    "Back");
            if (key == "Space")
                return FrameworkUi.L(
                    "settings.keyboard.space",
                    "Space");
            if (key == "Clear")
                return FrameworkUi.L(
                    "settings.keyboard.clear",
                    "Clear");
            if (key == "Cancel")
                return FrameworkUi.L(
                    "settings.keyboard.cancel",
                    "Cancel");
            if (key == "Shift") return "Aa";
            return key;
        }
        private void HandleNumericKey(string key)
        {
            switch (key)
            {
                case "Back":
                    Backspace();
                    break;
                case "Clear":
                    value = string.Empty;
                    RefreshPreview();
                    break;
                case "OK":
                    Submit();
                    break;
                case "Cancel":
                    Cancel();
                    break;
                case "-":
                    ToggleSign();
                    break;
                default:
                    AddText(key);
                    break;
            }
        }

        private void HandleTextKey(string key)
        {
            switch (key)
            {
                case "Back":
                    Backspace();
                    return;
                case "Clear":
                    value = string.Empty;
                    RefreshPreview();
                    return;
                case "OK":
                    Submit();
                    return;
                case "Cancel":
                    Cancel();
                    return;
                case "Space":
                    AddText(" ");
                    return;
                case "Shift":
                    ToggleCase();
                    return;
            }

            if (key.Length == 1
                && char.IsLetter(key[0]))
            {
                AddText(
                    upperCase
                        ? key.ToUpperInvariant()
                        : key.ToLowerInvariant());
                return;
            }

            AddText(key);
        }

        private void AddText(string text)
        {
            if (string.IsNullOrEmpty(text)
                || value.Length >= maxLength)
            {
                return;
            }

            if (numeric)
            {
                if (text == ".")
                {
                    if (integerOnly
                        || value.Contains(".")
                        || value.Contains(","))
                        return;
                }
                else if (text != "00"
                    && (text.Length != 1
                        || !char.IsDigit(text[0])))
                {
                    return;
                }
            }

            int remaining = maxLength - value.Length;
            value += text.Length <= remaining
                ? text
                : text.Substring(0, remaining);
            RefreshPreview();
        }

        private void Backspace()
        {
            if (string.IsNullOrEmpty(value))
                return;

            value = value.Substring(0, value.Length - 1);
            RefreshPreview();
        }

        private void ToggleSign()
        {
            if (value.StartsWith("-", StringComparison.Ordinal))
                value = value.Substring(1);
            else if (value.Length < maxLength)
                value = "-" + value;

            RefreshPreview();
        }

        private void ToggleCase()
        {
            upperCase = !upperCase;
            for (int i = 0; i < letterKeys.Count; i++)
            {
                LetterKey key = letterKeys[i];
                key.Label.text = upperCase
                    ? key.Lower.ToUpperInvariant()
                    : key.Lower;
            }
        }

        private void Submit()
        {
            if (closed)
                return;

            submit(value);
        }

        private void Cancel()
        {
            if (closed)
                return;

            cancel();
        }

        private void RefreshPreview()
        {
            if (preview != null)
                preview.text = value + "▌";
        }

        private void HandlePhysicalKeyboardInput()
        {
            string typed = Input.inputString;
            if (string.IsNullOrEmpty(typed))
                return;

            for (int i = 0; i < typed.Length; i++)
            {
                char ch = typed[i];
                if (ch == '\b')
                {
                    Backspace();
                }
                else if (ch == '\n' || ch == '\r')
                {
                    Submit();
                    return;
                }
                else if (numeric && ch == '-')
                {
                    ToggleSign();
                }
                else if (numeric && (ch == '.' || ch == ','))
                {
                    if (!integerOnly)
                        AddText(".");
                }
                else if (!char.IsControl(ch))
                {
                    AddText(ch.ToString());
                }
            }
        }
        private static bool Consume(GameKey key)
        {
            if (!LazyInput.GetKeyDown(key))
                return false;

            LazyInput.ClearKeyDown(key);
            return true;
        }

        private void LinkNavigation()
        {
            for (int row = 0; row < rows.Count; row++)
            {
                List<GamepadNavigationItem> current = rows[row];
                for (int column = 0;
                    column < current.Count;
                    column++)
                {
                    GamepadNavigationItem item = current[column];
                    if (column > 0)
                    {
                        item.SetCustomDirectionItem(
                            GUIDirection.Left,
                            current[column - 1]);
                    }

                    if (column + 1 < current.Count)
                    {
                        item.SetCustomDirectionItem(
                            GUIDirection.Right,
                            current[column + 1]);
                    }

                    if (row > 0)
                    {
                        List<GamepadNavigationItem> previous =
                            rows[row - 1];
                        item.SetCustomDirectionItem(
                            GUIDirection.Up,
                            previous[Mathf.Min(
                                column,
                                previous.Count - 1)]);
                    }
                    if (row + 1 < rows.Count)
                    {
                        List<GamepadNavigationItem> next =
                            rows[row + 1];
                        item.SetCustomDirectionItem(
                            GUIDirection.Down,
                            next[Mathf.Min(
                                column,
                                next.Count - 1)]);
                    }
                }
            }
        }
    }
}
