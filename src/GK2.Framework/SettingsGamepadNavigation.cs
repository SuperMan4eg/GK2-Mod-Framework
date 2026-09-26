using System;
using System.Collections;
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
        internal Action<TMP_InputField> GamepadFallback;

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
            BaseEventData eventData = null)
        {
            if (!CanEdit(input))
                return;

            ActivateDesktopInput(input, eventData);

            if (!LazyInput.IsGamepadActive)
                return;

            RuntimeInputFieldActivator activator =
                input.GetComponent<RuntimeInputFieldActivator>();

            // Do not rely on Steam's overlay keyboard here. On desktop Steam
            // configurations both gamepad text-input APIs can report a result
            // without presenting a usable editing surface. The Framework
            // keyboard is deterministic and uses the game's existing
            // GamepadNavigationController, so it also works when Steam Input
            // or an overlay is unavailable.
            if (activator?.GamepadFallback != null)
            {
                FrameworkLog.Source?.LogInfo(
                    "GK2_SETTINGS_GAMEPAD_FALLBACK_KEYBOARD_OPEN: "
                    + input.name);
                activator.GamepadFallback(input);
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

            int end = (input.text ?? string.Empty).Length;
            input.caretPosition = end;
            input.selectionAnchorPosition = end;
            input.selectionFocusPosition = end;

            RuntimeInputFieldActivator activator =
                input.GetComponent<RuntimeInputFieldActivator>();
            if (activator != null)
                activator.StartCoroutine(activator.LogEditStateNextFrame());
        }

        private IEnumerator LogEditStateNextFrame()
        {
            yield return null;
            yield return null;

            TMP_InputField input = Input;
            if (input == null)
                yield break;

            bool selected = EventSystem.current != null
                && EventSystem.current.currentSelectedGameObject
                    == input.gameObject;

            Graphic caret = null;
            Graphic[] graphics = input.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic != null
                    && graphic.name.IndexOf(
                        "caret",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    caret = graphic;
                    break;
                }
            }

            string caretState = caret == null
                ? "missing"
                : "present"
                    + "; active=" + caret.gameObject.activeInHierarchy
                    + "; enabled=" + caret.enabled
                    + "; alpha=" + caret.color.a.ToString("0.###")
                    + "; width="
                    + caret.rectTransform.rect.width.ToString("0.###");

            FrameworkLog.Source?.LogInfo(
                "GK2_SETTINGS_INPUT_EDIT_STATE: "
                + input.name
                + "; focused=" + input.isFocused
                + "; selected=" + selected
                + "; caret=" + caretState);
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
