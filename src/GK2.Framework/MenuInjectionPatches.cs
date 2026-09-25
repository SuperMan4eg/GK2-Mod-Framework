using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GK2.Framework
{
    internal static class MenuInjectionPatches
    {
        private static UIMainMenuWindow mainMenuOwner;
        private static UIGamePauseWindow pauseMenuOwner;
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LazyButton), nameof(LazyButton.OnPointerClick))]
        private static void PointerClickPrefix(LazyButton __instance)
        {
            if (__instance != null && __instance.name == "GK2ModsButton")
                FrameworkLog.Source?.LogInfo("GK2_MODS_POINTER_CLICK");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GamepadNavigationItem), nameof(GamepadNavigationItem.Focus))]
        private static void NavigationFocusPrefix(GamepadNavigationItem __instance)
        {
            if (__instance != null && __instance.name == "GK2ModsButton")
                FrameworkLog.Source?.LogInfo("GK2_MODS_NAV_FOCUS");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GamepadNavigationItem), nameof(GamepadNavigationItem.Select))]
        private static void NavigationSelectPrefix(GamepadNavigationItem __instance)
        {
            if (__instance != null && __instance.name == "GK2ModsButton")
                FrameworkLog.Source?.LogInfo("GK2_MODS_NAV_SUBMIT");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIMainMenuWindow), nameof(UIMainMenuWindow.Init))]
        private static void MainMenuInitPostfix(UIMainMenuWindow __instance)
        {
            mainMenuOwner = __instance;
            InjectRuntimeButton(__instance, "gameSettingsButton");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIGamePauseWindow), nameof(UIGamePauseWindow.Init))]
        private static void PauseMenuInitPostfix(UIGamePauseWindow __instance)
        {
            pauseMenuOwner = __instance;
            InjectPauseMenuButton(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIGamePauseWindow), nameof(UIGamePauseWindow.Open))]
        private static void PauseMenuOpenPostfix(UIGamePauseWindow __instance)
        {
            pauseMenuOwner = __instance;
            GamepadNavigationController navigation = __instance.GetComponent<GamepadNavigationController>();
            if (navigation != null && LazyInput.IsGamepadActive)
                navigation.ReinitItems(focusOnFirstActive: true);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIMainMenuWindow), nameof(UIMainMenuWindow.Open))]
        private static void MainMenuOpenPostfix(UIMainMenuWindow __instance)
        {
            LazyButton template = AccessTools.Field(typeof(UIMainMenuWindow), "gameSettingsButton")?.GetValue(__instance) as LazyButton;
            LazyButton clone = __instance.transform.Find("Bg/Vertical Group/GK2ModsButton")?.GetComponent<LazyButton>();
            if (template != null) LogButtonState("TEMPLATE_AFTER_OPEN", template);
            if (clone != null)
            {
                SynchronizeFinalVisuals(template, clone);
                LogButtonState("CLONE_AFTER_OPEN", clone);
                TextMeshProUGUI label = clone.GetComponentInChildren<TextMeshProUGUI>(true);
                FrameworkLog.Source?.LogInfo("GK2_MODS_LABEL_AFTER_OPEN: language=" + LLBase.CurrentLang
                    + "; text=" + (label == null ? "<missing>" : label.text));
            }
            __instance.StartCoroutine(LogAtEndOfFrame(__instance, template, clone));
        }

        private static void SynchronizeFinalVisuals(LazyButton template, LazyButton button)
        {
            if (template == null || button == null) return;
            RectTransform sourceRect = (RectTransform)template.transform;
            RectTransform targetRect = (RectTransform)button.transform;
            targetRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, sourceRect.rect.width);
            targetRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sourceRect.rect.height);
            LayoutElement layout = button.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.minWidth = sourceRect.rect.width;
                layout.preferredWidth = sourceRect.rect.width;
                layout.minHeight = sourceRect.rect.height;
                layout.preferredHeight = sourceRect.rect.height;
                layout.flexibleWidth = 0f;
                layout.flexibleHeight = 0f;
            }

            TextMeshProUGUI source = template.GetComponentInChildren<TextMeshProUGUI>(true);
            TextMeshProUGUI target = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (source != null && target != null)
            {
                RectTransform sourceGraphic = template.targetGraphic.rectTransform;
                Vector2 insetMin = source.rectTransform.offsetMin - sourceGraphic.offsetMin;
                Vector2 insetMax = source.rectTransform.offsetMax - sourceGraphic.offsetMax;
                target.rectTransform.anchorMin = Vector2.zero;
                target.rectTransform.anchorMax = Vector2.one;
                target.rectTransform.offsetMin = insetMin;
                target.rectTransform.offsetMax = insetMax;
                target.font = source.font;
                target.fontSharedMaterial = source.fontSharedMaterial;
                target.color = source.color;
                target.SetVerticesDirty();
                target.SetLayoutDirty();
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIExtensions), nameof(UIExtensions.RefreshContentFitterAndDisable))]
        private static void RefreshContentFitterPrefix(RectTransform transform)
        { LogCloneDuringLayout("LAYOUT_DISABLE_PREFIX", transform); }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIExtensions), nameof(UIExtensions.RefreshContentFitterAndDisable))]
        private static void RefreshContentFitterPostfix(RectTransform transform)
        { LogCloneDuringLayout("LAYOUT_DISABLE_POSTFIX", transform); }

        private static void InjectPauseMenuButton(UIGamePauseWindow window)
        {
            try
            {
                LazyButton template = AccessTools.Field(typeof(UIGamePauseWindow), "settingsBtn")?.GetValue(window)
                    as LazyButton;
                if (template == null)
                {
                    FrameworkLog.Error("Pause-menu Mods button template was not found: settingsBtn");
                    return;
                }

                Transform existing = template.transform.parent.Find("GK2PauseModsButton");
                if (existing != null) UnityEngine.Object.Destroy(existing.gameObject);

                LazyButton button = CreateRuntimeButton(template, "GK2PauseModsButton");
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(() =>
                {
                    FrameworkLog.Source?.LogInfo("GK2_PAUSE_MODS_BUTTON_CLICKED");
                    ModsMenuWindow.OpenFromPauseMenu(pauseMenuOwner);
                });
                button.LazyUIElementId = "gk2_framework_pause_mods";

                ModsButtonLocalization localization = button.gameObject.AddComponent<ModsButtonLocalization>();
                localization.Initialize(
                    button.GetComponentsInChildren<TextMeshProUGUI>(true),
                    template.GetComponentInChildren<TextMeshProUGUI>(true));

                button.SetCallbacksIntoGamepadNavigationItem();
                GamepadNavigationController navigation = window.GetComponent<GamepadNavigationController>();
                if (navigation != null)
                    navigation.ReinitItems(focusOnFirstActive: false);
                if (button.transform.parent is RectTransform parent)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(parent);

                FrameworkLog.Source?.LogInfo("GK2_PAUSE_MODS_BUTTON_INJECTED: UIGamePauseWindow");
            }
            catch (Exception ex)
            {
                FrameworkLog.Error("Pause-menu Mods button injection failed: " + ex);
            }
        }

        private static void InjectRuntimeButton(UIMainMenuWindow window, string templateField)
        {
            try
            {
                LazyButton template = AccessTools.Field(window.GetType(), templateField)?.GetValue(window) as LazyButton;
                if (template == null)
                {
                    FrameworkLog.Error("Mods button template was not found: " + templateField);
                    return;
                }

                Transform existing = template.transform.parent.Find("GK2ModsButton");
                if (existing != null) UnityEngine.Object.Destroy(existing.gameObject);

                LogButtonState("TEMPLATE_AFTER_INIT", template);
                LazyButton button = CreateRuntimeButton(template, "GK2ModsButton");
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(OnModsButtonClicked);
                button.LazyUIElementId = "gk2_framework_mods";

                ModsButtonLocalization localization = button.gameObject.AddComponent<ModsButtonLocalization>();
                localization.Initialize(button.GetComponentsInChildren<TextMeshProUGUI>(true),
                    template.GetComponentInChildren<TextMeshProUGUI>(true));

                button.SetCallbacksIntoGamepadNavigationItem();

                if (button.transform.parent is RectTransform parent)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(parent);

                LogButtonState("CLONE_CONFIGURED", button);
                window.StartCoroutine(LogAfterLifecycle(window, template, button));
                FrameworkLog.Source?.LogInfo("GK2_MODS_BUTTON_INJECTED: UIMainMenuWindow");
            }
            catch (Exception ex) { FrameworkLog.Error("Mods button injection failed: " + ex); }
        }

        private static LazyButton CreateRuntimeButton(LazyButton template, string objectName)
        {
            RectTransform templateRect = (RectTransform)template.transform;
            Image templateImage = template.targetGraphic as Image;
            TextMeshProUGUI templateLabel = template.GetComponentInChildren<TextMeshProUGUI>(true);

            GameObject root = new GameObject(objectName, typeof(RectTransform));
            RectTransform rect = (RectTransform)root.transform;
            rect.anchorMin = templateRect.anchorMin;
            rect.anchorMax = templateRect.anchorMax;
            rect.pivot = templateRect.pivot;
            rect.sizeDelta = templateRect.rect.size;
            rect.localScale = templateRect.localScale;

            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.preferredWidth = templateRect.rect.width;
            layout.preferredHeight = templateRect.rect.height;
            layout.minWidth = templateRect.rect.width;
            layout.minHeight = templateRect.rect.height;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;

            Image image = root.AddComponent<Image>();
            if (templateImage != null)
            {
                image.sprite = templateImage.sprite;
                image.type = templateImage.type;
                image.preserveAspect = templateImage.preserveAspect;
                image.fillCenter = templateImage.fillCenter;
                image.fillMethod = templateImage.fillMethod;
                image.fillAmount = templateImage.fillAmount;
                image.fillClockwise = templateImage.fillClockwise;
                image.fillOrigin = templateImage.fillOrigin;
                image.pixelsPerUnitMultiplier = templateImage.pixelsPerUnitMultiplier;
                image.color = templateImage.color;
                image.material = templateImage.material;
            }
            image.raycastTarget = true;

            LazyButton button = root.AddComponent<LazyButton>();
            button.targetGraphic = image;
            button.transition = template.transition;
            button.colors = template.colors;
            button.spriteState = template.spriteState;
            button.animationTriggers = template.animationTriggers;
            button.navigation = template.navigation;

            GamepadNavigationItem templateNavigation = template.GetComponent<GamepadNavigationItem>();
            GamepadNavigationItem navigation = root.AddComponent<GamepadNavigationItem>();
            if (templateNavigation != null)
            {
                navigation.group = templateNavigation.group;
                navigation.ignoreScroll = templateNavigation.ignoreScroll;
            }

            GameObject labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(root.transform, false);
            RectTransform labelRect = (RectTransform)labelObject.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            if (templateLabel != null)
            {
                label.font = templateLabel.font;
                label.fontSharedMaterial = templateLabel.fontSharedMaterial;
                label.fontSize = templateLabel.fontSize;
                label.fontStyle = templateLabel.fontStyle;
                label.color = templateLabel.color;
                label.alignment = templateLabel.alignment;
                label.enableAutoSizing = templateLabel.enableAutoSizing;
                label.fontSizeMin = templateLabel.fontSizeMin;
                label.fontSizeMax = templateLabel.fontSizeMax;
                label.characterSpacing = templateLabel.characterSpacing;
                label.wordSpacing = templateLabel.wordSpacing;
                label.lineSpacing = templateLabel.lineSpacing;
                label.paragraphSpacing = templateLabel.paragraphSpacing;
                label.margin = templateLabel.margin;
                label.overflowMode = templateLabel.overflowMode;
                label.richText = templateLabel.richText;
            }
            label.raycastTarget = false;

            // Let Unity run Awake/OnEnable for LazyButton while the new object is in an active hierarchy.
            // Parenting it under the currently hidden LazyWindow before this point defers Awake and causes
            // LazyButton.SanitizeEmptyEvents to replace callbacks when the menu finally opens.
            root.transform.SetParent(template.transform.parent, false);
            root.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
            return button;
        }

        private static IEnumerator LogAfterLifecycle(UIMainMenuWindow window, LazyButton template, LazyButton button)
        {
            yield return null;
            if (template != null) LogButtonState("TEMPLATE_AFTER_LIFECYCLE", template);
            if (button != null)
            {
                LogButtonState("CLONE_AFTER_LIFECYCLE", button);
                TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
                FrameworkLog.Source?.LogInfo("GK2_MODS_LABEL_FINAL: language=" + LLBase.CurrentLang
                    + "; text=" + (label == null ? "<missing>" : label.text));
            }
        }

        private static IEnumerator LogAtEndOfFrame(UIMainMenuWindow window, LazyButton template, LazyButton clone)
        {
            yield return new WaitForEndOfFrame();
            if (template != null) LogButtonState("TEMPLATE_END_OF_FRAME", template);
            if (clone != null)
            {
                LogButtonState("CLONE_END_OF_FRAME", clone);
                LogRaycast(clone);
                window.StartCoroutine(LogAfterIntro(clone));
            }
        }

        private static IEnumerator LogAfterIntro(LazyButton button)
        {
            yield return new WaitForSecondsRealtime(3f);
            if (button == null) yield break;
            LogButtonState("CLONE_AFTER_INTRO", button);
            LogRaycast(button);
        }

        private static void LogCloneDuringLayout(string marker, RectTransform transform)
        {
            if (transform == null || transform.GetComponent<UIMainMenuWindow>() == null) return;
            LazyButton clone = transform.Find("Bg/Vertical Group/GK2ModsButton")?.GetComponent<LazyButton>();
            if (clone != null) LogButtonState(marker, clone);
        }

        private static void OnModsButtonClicked()
        {
            FrameworkLog.Source?.LogInfo("GK2_MODS_BUTTON_CLICKED");
            ModsMenuWindow.OpenFromMainMenu(mainMenuOwner);
        }

        private static void LogButtonState(string marker, LazyButton button)
        {
            var state = new StringBuilder();
            GamepadNavigationItem navigation = button.GetComponent<GamepadNavigationItem>();
            state.Append("GK2_MODS_BUTTON_STATE ").Append(marker)
                .Append(": path=").Append(GetPath(button.transform))
                .Append("; activeSelf=").Append(button.gameObject.activeSelf)
                .Append("; activeInHierarchy=").Append(button.gameObject.activeInHierarchy)
                .Append("; LazyButton.enabled=").Append(button.enabled)
                .Append("; Selectable.interactable=").Append(button.interactable)
                .Append("; IsInteractable=").Append(button.IsInteractable())
                .Append("; keepPressed=").Append(ReadField(button, typeof(LazyButton), "keepPressed"))
                .Append("; selectionState=").Append(ReadProperty(button, typeof(Selectable), "currentSelectionState"))
                .Append("; groupsAllowInteraction=").Append(ReadField(button, typeof(Selectable), "m_GroupsAllowInteraction"))
                .Append("; transition=").Append(button.transition)
                .Append("; navigation.mode=").Append(button.navigation.mode)
                .Append("; targetGraphic=").Append(button.targetGraphic == null ? "<null>" : GetPath(button.targetGraphic.transform))
                .Append("; onClick.persistent=").Append(button.onClick.GetPersistentEventCount())
                .Append("; onClick.runtime=").Append(CountRuntimeListeners(button.onClick))
                .Append("; siblingIndex=").Append(button.transform.GetSiblingIndex())
                .Append("; navigation.present=").Append(navigation != null);
            if (navigation != null)
            {
                Button configuredButton = AccessTools.Field(typeof(GamepadNavigationItem), "configuredForButton")?.GetValue(navigation) as Button;
                state.Append("; navigation.enabled=").Append(navigation.enabled)
                    .Append("; navigation.Active=").Append(navigation.Active)
                    .Append("; navigation.focused=").Append(navigation.IsFocused)
                    .Append("; navigation.configuredForSelf=").Append(configuredButton == button);
            }
            FrameworkLog.Source?.LogInfo(state.ToString());

            Graphic graphic = button.targetGraphic;
            if (graphic != null)
            {
                Image image = graphic as Image;
                FrameworkLog.Source?.LogInfo("GK2_MODS_GRAPHIC " + marker + ": path=" + GetPath(graphic.transform)
                    + "; enabled=" + graphic.enabled + "; raycastTarget=" + graphic.raycastTarget
                    + "; color=" + graphic.color + "; canvasRenderer.cull=" + graphic.canvasRenderer.cull
                    + "; image.type=" + (image == null ? "n/a" : image.type.ToString()));
            }

            RectTransform rect = button.transform as RectTransform;
            if (rect != null)
            {
                Vector3[] corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                FrameworkLog.Source?.LogInfo("GK2_MODS_RECT " + marker + ": rect=" + rect.rect
                    + "; worldCorners=" + string.Join(" | ", Array.ConvertAll(corners, c => c.ToString())));
            }

            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
            {
                Vector3[] textCorners = new Vector3[4];
                text.rectTransform.GetWorldCorners(textCorners);
                FrameworkLog.Source?.LogInfo("GK2_MODS_TEXT " + marker + ": value=" + text.text
                    + "; enabled=" + text.enabled + "; active=" + text.gameObject.activeInHierarchy
                    + "; color=" + text.color + "; alpha=" + text.alpha
                    + "; font=" + (text.font == null ? "<null>" : text.font.name)
                    + "; material=" + (text.fontSharedMaterial == null ? "<null>" : text.fontSharedMaterial.name)
                    + "; fontSize=" + text.fontSize + "; autoSize=" + text.enableAutoSizing
                    + "; alignment=" + text.alignment + "; overflow=" + text.overflowMode
                    + "; raycastTarget=" + text.raycastTarget + "; cull=" + text.canvasRenderer.cull
                    + "; rect=" + text.rectTransform.rect + "; worldCorners="
                    + string.Join(" | ", Array.ConvertAll(textCorners, c => c.ToString())));
            }

            CanvasGroup[] groups = button.GetComponentsInParent<CanvasGroup>(true);
            for (int i = 0; i < groups.Length; i++)
            {
                CanvasGroup group = groups[i];
                FrameworkLog.Source?.LogInfo("GK2_MODS_CANVAS_GROUP " + marker + "[" + i + "]: path="
                    + GetPath(group.transform) + "; enabled=" + group.enabled + "; interactable="
                    + group.interactable + "; blocksRaycasts=" + group.blocksRaycasts
                    + "; ignoreParentGroups=" + group.ignoreParentGroups + "; alpha=" + group.alpha);
            }

            Component[] components = button.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component != null)
                    FrameworkLog.Source?.LogInfo("GK2_MODS_COMPONENT " + marker + ": path=" + GetPath(component.transform)
                        + "; type=" + component.GetType().FullName + "; enabled=" + GetEnabled(component));
            }

            LogRuntimeFields(marker + ".LazyButton", button, typeof(LazyButton));
            LogRuntimeFields(marker + ".Selectable", button, typeof(Selectable));
            if (navigation != null) LogRuntimeFields(marker + ".GamepadNavigationItem", navigation, typeof(GamepadNavigationItem));
        }

        private static string GetEnabled(Component component)
        {
            Behaviour behaviour = component as Behaviour;
            return behaviour == null ? "n/a" : behaviour.enabled.ToString();
        }

        private static object ReadField(object instance, Type declaringType, string name)
        {
            var field = AccessTools.Field(declaringType, name);
            return field == null ? "<field-missing>" : field.GetValue(instance);
        }

        private static object ReadProperty(object instance, Type declaringType, string name)
        {
            PropertyInfo property = AccessTools.Property(declaringType, name);
            return property == null ? "<property-missing>" : SafeValue(() => property.GetValue(instance, null));
        }

        private static void LogRuntimeFields(string marker, object instance, Type type)
        {
            var values = new StringBuilder("GK2_MODS_FIELDS ").Append(marker).Append(": ");
            for (Type current = type; current != null && current != typeof(MonoBehaviour); current = current.BaseType)
            {
                FieldInfo[] fields = current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                foreach (FieldInfo field in fields)
                {
                    string lower = field.Name.ToLowerInvariant();
                    if (!(lower.Contains("active") || lower.Contains("enable") || lower.Contains("input")
                        || lower.Contains("state") || lower.Contains("select") || lower.Contains("disable")
                        || lower.Contains("navigation") || lower.Contains("press") || lower.Contains("interact")
                        || lower.Contains("focus") || lower.Contains("group") || lower.Contains("target"))) continue;
                    values.Append(current.Name).Append('.').Append(field.Name).Append('=')
                        .Append(SafeValue(() => field.GetValue(instance))).Append("; ");
                }
            }
            FrameworkLog.Source?.LogInfo(values.ToString());
        }

        private static object SafeValue(Func<object> getter)
        {
            try { return getter() ?? "<null>"; }
            catch (Exception ex) { return "<" + ex.GetType().Name + ">"; }
        }

        private static int CountRuntimeListeners(UnityEventBase evt)
        {
            object calls = AccessTools.Field(typeof(UnityEventBase), "m_Calls")?.GetValue(evt);
            object runtimeCalls = calls == null ? null : AccessTools.Field(calls.GetType(), "m_RuntimeCalls")?.GetValue(calls);
            ICollection collection = runtimeCalls as ICollection;
            return collection == null ? -1 : collection.Count;
        }

        private static void LogRaycast(LazyButton button)
        {
            if (EventSystem.current == null)
            {
                FrameworkLog.Source?.LogWarning("GK2_MODS_RAYCAST: EventSystem.current=<null>");
                return;
            }
            RectTransform rect = button.transform as RectTransform;
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
            Canvas canvas = button.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.rootCanvas.worldCamera : null;
            Vector2 screenCenter = RectTransformUtility.WorldToScreenPoint(camera, worldCenter);
            var eventData = new PointerEventData(EventSystem.current) { position = screenCenter };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            FrameworkLog.Source?.LogInfo("GK2_MODS_RAYCAST: center=" + screenCenter + "; count=" + results.Count);
            for (int i = 0; i < results.Count; i++)
            {
                RaycastResult result = results[i];
                FrameworkLog.Source?.LogInfo("GK2_MODS_RAYCAST[" + i + "]: object=" + GetPath(result.gameObject.transform)
                    + "; module=" + result.module?.GetType().FullName + "; sortingLayer=" + result.sortingLayer
                    + "; sortingOrder=" + result.sortingOrder + "; depth=" + result.depth + "; distance=" + result.distance);
            }
        }

        private static string GetPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }
    }

    internal sealed class ModsButtonLocalization : MonoBehaviour
    {
        private TextMeshProUGUI[] labels;
        private TextMeshProUGUI styleSource;

        internal void Initialize(TextMeshProUGUI[] targetLabels, TextMeshProUGUI sourceLabel)
        {
            labels = targetLabels;
            styleSource = sourceLabel;
            GameSettings.OnLanguageChanged -= Apply;
            GameSettings.OnLanguageChanged += Apply;
            Apply();
        }

        private void OnDestroy() { GameSettings.OnLanguageChanged -= Apply; }

        private void Apply()
        {
            NativeUiSkin.TryCapture();
            string text = FrameworkLocalization.Get("mods.title", "Mods");
            if (labels != null)
                foreach (TextMeshProUGUI label in labels)
                    if (label != null)
                    {
                        if (NativeUiSkin.IsReady)
                        {
                            FrameworkUi.ApplyBoldFont(label);
                            if (styleSource != null) label.color = styleSource.color;
                        }
                        else if (styleSource != null)
                        {
                            label.font = styleSource.font;
                            label.fontSharedMaterial = styleSource.fontSharedMaterial;
                            label.color = styleSource.color;
                        }
                        label.text = text;
                        label.SetVerticesDirty();
                        label.SetLayoutDirty();
                    }
            FrameworkLog.Source?.LogInfo("GK2_MODS_LABEL_APPLIED: language=" + LLBase.CurrentLang + "; text=" + text);
        }
    }
}
