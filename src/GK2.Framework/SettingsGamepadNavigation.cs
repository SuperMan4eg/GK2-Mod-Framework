using System.Collections.Generic;
using HarmonyLib;
using LazyBearTechnology;
using UnityEngine;
using UnityEngine.UI;

namespace GK2.Framework
{
    // Put on a slider row's navigation item: how far left/right moves the slider with a controller.
    internal sealed class SliderNudge : MonoBehaviour
    {
        internal Slider Slider;
        internal float Amount;
    }

    // Controller support for the settings page: left/right changes a focused slider, and the list
    // scrolls to keep the focused row visible.
    internal static class SettingsGamepadNavigation
    {
        private const string WindowName = "GK2ModsMenuWindow";

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GamepadNavigationController), nameof(GamepadNavigationController.Navigate))]
        private static bool NavigatePrefix(GamepadNavigationController __instance, GUIDirection direction)
        {
            if (__instance.name != WindowName) return true;
            if (direction != GUIDirection.Left && direction != GUIDirection.Right) return true;

            GamepadNavigationItem focused = __instance.FocusedItem;
            SliderNudge nudge = focused == null ? null : focused.GetComponent<SliderNudge>();
            if (nudge == null || nudge.Slider == null || !nudge.Slider.IsInteractable()) return true;

            nudge.Slider.value += direction == GUIDirection.Right ? nudge.Amount : -nudge.Amount;
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GamepadNavigationController), nameof(GamepadNavigationController.Navigate))]
        private static void NavigatePostfix(GamepadNavigationController __instance)
        {
            if (__instance.name != WindowName) return;
            GamepadNavigationItem focused = __instance.FocusedItem;
            ScrollRect scroll = focused == null ? null : focused.GetComponentInParent<ScrollRect>();
            if (scroll == null || scroll.viewport == null || scroll.content == null) return;

            // Keep the whole row (including its description) in view, not just the control.
            Transform row = focused.transform;
            while (row.parent != null && row.parent != scroll.content) row = row.parent;
            if (row.parent != scroll.content) return;

            Vector3[] item = new Vector3[4];
            Vector3[] view = new Vector3[4];
            ((RectTransform)row).GetWorldCorners(item);
            scroll.viewport.GetWorldCorners(view);

            float move = 0f;
            if (item[0].y < view[0].y) move = view[0].y - item[0].y;
            else if (item[1].y > view[1].y) move = view[1].y - item[1].y;
            if (move != 0f) scroll.content.position += new Vector3(0f, move, 0f);
        }
    }
}
