using HarmonyLib;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GK2.Framework
{
    internal sealed class SettingsNavigationControl : MonoBehaviour
    {
        internal Slider Slider;
        internal float SliderAmount;
        internal TMP_Dropdown Dropdown;

        internal bool UsesHorizontalAdjustment =>
            Slider != null || Dropdown != null;
    }

    internal sealed class RuntimeInputFieldActivator
        : MonoBehaviour, IPointerClickHandler
    {
        internal TMP_InputField Input;
        internal string GamepadHeaderText;
        internal int GamepadMaxLength = 256;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null
                && eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            ActivateDesktopInput(Input, eventData);
        }

        internal static void Activate(
            TMP_InputField input,
            BaseEventData eventData = null,
            string gamepadHeaderText = null,
            int gamepadMaxLength = 256)
        {
            if (!CanEdit(input))
                return;

            // Keep the normal TMP path active as a fallback for keyboard/mouse.
            // On a physical gamepad, TMP_InputField alone does not present an
            // editing surface, so use the game's platform keyboard abstraction.
            ActivateDesktopInput(input, eventData);

            if (!LazyInput.IsGamepadActive)
                return;

            try
            {
                string previous = input.text ?? string.Empty;
                int maxLength = gamepadMaxLength > 0
                    ? gamepadMaxLength
                    : 256;
                string header = string.IsNullOrWhiteSpace(gamepadHeaderText)
                    ? input.name
                    : gamepadHeaderText;

                FrameworkLog.Source?.LogInfo(
                    "GK2_SETTINGS_GAMEPAD_KEYBOARD_OPEN: "
                    + input.name);

                LazyAPI.Platform.ShowKeyboard(
                    value =>
                    {
                        // LazyPlatformDefault reports an empty string both when
                        // Steam's keyboard is cancelled/failed and when no text
                        // is returned. Treat that as cancellation when replacing
                        // a non-empty value so a Back press cannot erase config.
                        if (string.IsNullOrEmpty(value)
                            && !string.IsNullOrEmpty(previous))
                        {
                            input.SetTextWithoutNotify(previous);
                            FrameworkLog.Source?.LogInfo(
                                "GK2_SETTINGS_GAMEPAD_KEYBOARD_CANCEL: "
                                + input.name);
                            return;
                        }

                        input.SetTextWithoutNotify(value ?? string.Empty);
                        input.onEndEdit.Invoke(input.text);
                        FrameworkLog.Source?.LogInfo(
                            "GK2_SETTINGS_GAMEPAD_KEYBOARD_COMMIT: "
                            + input.name);
                    },
                    maxLength,
                    header);
            }
            catch (System.Exception ex)
            {
                FrameworkLog.Error(
                    "GK2_SETTINGS_GAMEPAD_KEYBOARD_FAILED: "
                    + input.name + ": " + ex);
            }
        }

        private static bool CanEdit(TMP_InputField input)
        {
            return input != null
                && input.isActiveAndEnabled
                && input.interactable
                && !input.readOnly;
        }

        private static void ActivateDesktopInput(
            TMP_InputField input,
            BaseEventData eventData)
        {
            if (!CanEdit(input))
                return;

            EventSystem current = EventSystem.current;
            if (current != null)
                current.SetSelectedGameObject(input.gameObject, eventData);

            input.Select();
            input.ActivateInputField();
        }
    }

    [HarmonyPatch]
    internal static class SettingsGamepadNavigation
    {
        private const string WindowName = "GK2ModsMenuWindow";

        [HarmonyPrefix]
        [HarmonyPatch(
            typeof(GamepadNavigationController),
            nameof(GamepadNavigationController.Navigate))]
        private static bool NavigatePrefix(
            GamepadNavigationController __instance,
            GUIDirection direction)
        {
            if (__instance == null
                || __instance.name != WindowName
                || (direction != GUIDirection.Left
                    && direction != GUIDirection.Right))
            {
                return true;
            }

            GamepadNavigationItem focused =
                __instance.FocusedItem;
            SettingsNavigationControl control =
                focused == null
                    ? null
                    : focused.GetComponent<SettingsNavigationControl>();

            if (control == null)
                return true;

            int delta = direction == GUIDirection.Right ? 1 : -1;

            if (control.Slider != null
                && control.Slider.IsInteractable())
            {
                control.Slider.value +=
                    delta * control.SliderAmount;
                return false;
            }

            if (control.Dropdown != null
                && control.Dropdown.IsInteractable()
                && control.Dropdown.options != null
                && control.Dropdown.options.Count > 0)
            {
                int count = control.Dropdown.options.Count;
                int next = Mathf.Clamp(
                    control.Dropdown.value + delta,
                    0,
                    count - 1);
                if (next != control.Dropdown.value)
                    control.Dropdown.value = next;
                return false;
            }

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(
            typeof(GamepadNavigationController),
            nameof(GamepadNavigationController.Navigate))]
        private static void NavigatePostfix(
            GamepadNavigationController __instance)
        {
            if (__instance == null
                || __instance.name != WindowName)
            {
                return;
            }

            GamepadNavigationItem focused =
                __instance.FocusedItem;
            ScrollRect scroll =
                focused == null
                    ? null
                    : focused.GetComponentInParent<ScrollRect>();

            if (scroll == null
                || scroll.viewport == null
                || scroll.content == null)
            {
                return;
            }

            Transform row = focused.transform;
            while (row.parent != null
                && row.parent != scroll.content)
            {
                row = row.parent;
            }

            if (row.parent != scroll.content
                || !(row is RectTransform rowRect))
            {
                return;
            }

            Vector3[] itemCorners = new Vector3[4];
            Vector3[] viewportCorners = new Vector3[4];
            rowRect.GetWorldCorners(itemCorners);
            scroll.viewport.GetWorldCorners(viewportCorners);

            float move = 0f;
            if (itemCorners[0].y < viewportCorners[0].y)
                move = viewportCorners[0].y - itemCorners[0].y;
            else if (itemCorners[1].y > viewportCorners[1].y)
                move = viewportCorners[1].y - itemCorners[1].y;

            if (!Mathf.Approximately(move, 0f))
            {
                scroll.content.position +=
                    new Vector3(0f, move, 0f);
            }
        }
    }
}
