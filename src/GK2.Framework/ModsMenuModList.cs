using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2.Framework
{
    internal sealed class ModsMenuModList
    {
        private readonly RectTransform content;
        private readonly ScrollRect scroll;
        private readonly Dictionary<string, GameObject> selectionMarks =
            new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Image> statusBadges =
            new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, GamepadNavigationItem> navigationItems =
            new Dictionary<string, GamepadNavigationItem>(StringComparer.OrdinalIgnoreCase);
        private readonly Action<RegisteredMod> onSelected;

        internal ModsMenuModList(Image frame, Action<RegisteredMod> onSelected)
        {
            content = FrameworkUi.CreateVerticalScrollContent(frame, out scroll);
            this.onSelected = onSelected ?? throw new ArgumentNullException(nameof(onSelected));
        }

        internal RegisteredMod Refresh(string selectedId)
        {
            selectionMarks.Clear();
            statusBadges.Clear();
            navigationItems.Clear();
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                GameObject oldItem = content.GetChild(i).gameObject;
                oldItem.SetActive(false);
                UnityEngine.Object.Destroy(oldItem);
            }

            List<RegisteredMod> mods = FrameworkApi.Mods
                .Where(m => !string.Equals(m.Metadata.Id, FrameworkPlugin.PluginGuid, StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => m.Metadata.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(m => m.Metadata.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();

            FrameworkLog.Source?.LogInfo(
                "GK2_MODS_REGISTRY_SNAPSHOT: total=" + FrameworkApi.Mods.Count
                + ";visible=" + mods.Count
                + ";ids=" + string.Join(",", FrameworkApi.Mods.Select(m => m.Metadata.Id)));

            if (mods.Count == 0)
            {
                string pluginIds = string.Join(",", Chainloader.PluginInfos.Keys
                    .Where(id => !string.Equals(id, FrameworkPlugin.PluginGuid, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(id => id, StringComparer.OrdinalIgnoreCase));
                Assembly[] frameworkAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => string.Equals(a.GetName().Name, "GK2.Framework", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                string assemblyInfo = string.Join(" | ", frameworkAssemblies.Select(a =>
                    (a.GetName().Version?.ToString() ?? "<no-version>")
                    + " @ " + SafeAssemblyLocation(a)
                    + " # " + a.ManifestModule.ModuleVersionId));

                FrameworkLog.Source?.LogInfo(
                    "GK2_MODS_EMPTY_DIAGNOSTIC: bepinexPlugins="
                    + Math.Max(0, Chainloader.PluginInfos.Count - 1)
                    + ";pluginIds=" + pluginIds
                    + ";frameworkAssemblies=" + frameworkAssemblies.Length
                    + ";assemblies=" + assemblyInfo);
            }

            FrameworkUi.SetContentHeight(content, 18f + mods.Count * 40f);
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;

            float y = -10f;
            foreach (RegisteredMod mod in mods)
            {
                RegisteredMod captured = mod;
                LazyButton item = FrameworkUi.CreateButton(
                    "Mod_" + mod.Metadata.Id,
                    content,
                    null,
                    mod.Metadata.Name + "  " + mod.Metadata.Version,
                    new Vector2(10f, y - 34f),
                    new Vector2(-10f, y),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f));

                TextMeshProUGUI itemLabel = item.GetComponentInChildren<TextMeshProUGUI>();
                if (itemLabel != null)
                {
                    itemLabel.alignment = TextAlignmentOptions.MidlineLeft;
                    itemLabel.fontSize = NativeUiSkin.IsReady ? 15f : itemLabel.fontSize;
                    FrameworkUi.ApplyLabelText(itemLabel);
                    itemLabel.rectTransform.offsetMin = new Vector2(10f, 0f);
                    itemLabel.rectTransform.offsetMax = new Vector2(-38f, 0f);
                }

                Image marker = FrameworkUi.CreateImage(
                    "Selected", item.transform,
                    NativeUiSkin.IsReady ? NativeUiSkin.ValueColor : new Color(1f, 0.65f, 0.2f, 1f));
                marker.raycastTarget = false;
                FrameworkUi.SetRect(
                    marker.rectTransform,
                    Vector2.zero,
                    new Vector2(5f, 0f),
                    Vector2.zero,
                    new Vector2(0f, 1f));
                marker.gameObject.SetActive(false);
                selectionMarks[mod.Metadata.Id] = marker.gameObject;

                Image badge = FrameworkUi.CreateImage("StatusIcon", item.transform, Color.white);
                badge.raycastTarget = false;
                FrameworkUi.SetRect(
                    badge.rectTransform,
                    new Vector2(-28f, -7f),
                    new Vector2(-14f, 7f),
                    new Vector2(1f, 0.5f),
                    new Vector2(1f, 0.5f));

                Image core = FrameworkUi.CreateImage("Core", badge.transform, Color.white);
                core.raycastTarget = false;
                FrameworkUi.SetRect(
                    core.rectTransform,
                    new Vector2(5f, 5f),
                    new Vector2(-5f, -5f),
                    Vector2.zero,
                    Vector2.one);
                statusBadges[mod.Metadata.Id] = badge;
                ApplyState(mod);

                item.onClick.AddListener(() => onSelected(captured));
                item.SetCallbacksIntoGamepadNavigationItem();
                GamepadNavigationItem navigation = item.GetComponent<GamepadNavigationItem>();
                if (navigation != null)
                {
                    navigationItems[mod.Metadata.Id] = navigation;
                    navigation.SetCallbacks(
                        () => { item.ForceOnEnter(); onSelected(captured); },
                        item.ForceOnExit,
                        item.ForceOnClick);
                }
                y -= 40f;
            }

            return selectedId == null
                ? mods.FirstOrDefault()
                : mods.FirstOrDefault(m =>
                    string.Equals(m.Metadata.Id, selectedId, StringComparison.OrdinalIgnoreCase))
                    ?? mods.FirstOrDefault();
        }

        internal void RefreshStates()
        {
            foreach (RegisteredMod mod in FrameworkApi.Mods)
                ApplyState(mod);
        }

        private void ApplyState(RegisteredMod mod)
        {
            if (mod == null) return;
            if (!statusBadges.TryGetValue(mod.Metadata.Id, out Image badge)) return;

            ModUiState state = ModUiStateResolver.Get(mod);
            Sprite nativeStatusSprite = NativeUiSkin.GetStatusSprite(state.Severity);
            Image core = badge.transform.Find("Core")?.GetComponent<Image>();
            if (nativeStatusSprite != null)
            {
                badge.sprite = nativeStatusSprite;
                badge.type = Image.Type.Simple;
                badge.preserveAspect = true;
                badge.color = state.Severity == ModUiSeverity.Off
                    ? new Color(0.52f, 0.52f, 0.52f, 0.58f)
                    : Color.white;
                if (core != null) core.gameObject.SetActive(false);
            }
            else
            {
                badge.sprite = NativeUiSkin.SmallButtonSprite;
                badge.type = NativeUiSkin.SmallButtonSprite != null
                    ? Image.Type.Sliced
                    : Image.Type.Simple;
                badge.preserveAspect = false;
                badge.color = state.Color;
                if (core != null)
                {
                    core.gameObject.SetActive(true);
                    core.color = Color.Lerp(state.Color, Color.white, 0.58f);
                }
            }
        }

        internal GamepadNavigationItem GetNavigationItem(string modId)
        {
            if (string.IsNullOrWhiteSpace(modId)) return null;
            navigationItems.TryGetValue(modId, out GamepadNavigationItem item);
            return item;
        }

        internal void SetSelected(string selectedId)
        {
            foreach (KeyValuePair<string, GameObject> pair in selectionMarks)
            {
                if (pair.Value != null)
                    pair.Value.SetActive(string.Equals(
                        pair.Key, selectedId, StringComparison.OrdinalIgnoreCase));
            }
        }

        private static string SafeAssemblyLocation(Assembly assembly)
        {
            try { return string.IsNullOrWhiteSpace(assembly?.Location) ? "<dynamic>" : assembly.Location; }
            catch { return "<unavailable>"; }
        }
    }
}
