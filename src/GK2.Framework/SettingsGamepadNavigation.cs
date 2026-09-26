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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null
                && eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            Activate(Input, eventData);
        }

        internal static void Activate(
            TMP_InputField input,
            BaseEventData eventData = null)
        {
            if (input == null
                || !input.isActiveAndEnabled
                || !input.interactable
                || input.readOnly)
            {
                return;
            }

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
