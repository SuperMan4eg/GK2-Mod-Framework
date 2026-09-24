using System;
using System.Collections.Generic;
using System.Linq;
using LazyBearTechnology;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2.Framework
{
    internal sealed class ModsMenuDetails
    {
        private readonly TextMeshProUGUI metadataText;
        private readonly TextMeshProUGUI enabledText;
        private readonly LazyButton enabledButton;
        private readonly LazyButton settingsButton;

        internal ModsMenuDetails(Image panel, LazyButton template, Action toggleSelected, Action openSettings)
        {
            metadataText = FrameworkUi.CreateText(
                "Metadata", panel.rectTransform, NativeUiSkin.IsReady ? 16f : 18f,
                TextAlignmentOptions.TopLeft, Color.white);
            FrameworkUi.ApplyLabelText(metadataText);
            FrameworkUi.SetRect(
                metadataText.rectTransform,
                new Vector2(18f, 86f),
                new Vector2(-18f, -16f),
                Vector2.zero,
                Vector2.one);

            enabledButton = FrameworkUi.CreateButton(
                "Enabled",
                panel.rectTransform,
                NativeUiSkin.IsReady ? null : template,
                FrameworkUi.L("mods.enabled", "Enabled"),
                new Vector2(18f, 42f),
                new Vector2(170f, 72f),
                Vector2.zero,
                Vector2.zero);
            enabledText = enabledButton.GetComponentInChildren<TextMeshProUGUI>();
            FrameworkUi.ApplyDialogButton(enabledButton);
            enabledButton.onClick.AddListener(() => toggleSelected());
            enabledButton.SetCallbacksIntoGamepadNavigationItem();

            settingsButton = FrameworkUi.CreateButton(
                "Settings",
                panel.rectTransform,
                NativeUiSkin.IsReady ? null : template,
                FrameworkUi.L("mods.settings", "Settings"),
                new Vector2(180f, 42f),
                new Vector2(332f, 72f),
                Vector2.zero,
                Vector2.zero);
            FrameworkUi.ApplyDialogButton(settingsButton);
            settingsButton.interactable = false;
            settingsButton.onClick.AddListener(() => openSettings());
            settingsButton.SetCallbacksIntoGamepadNavigationItem();
        }

        internal void Show(RegisteredMod mod)
        {
            if (mod == null)
            {
                metadataText.text = FrameworkUi.L(
                    "mods.none",
                    "No Framework-integrated mods are registered.\n\n"
                    + "Standalone BepInEx mods can still run, but they appear here only when they register directly with the Framework or include a Framework bridge.");
                enabledButton.gameObject.SetActive(false);
                enabledButton.interactable = false;
                settingsButton.gameObject.SetActive(false);
                settingsButton.interactable = false;
                return;
            }

            settingsButton.gameObject.SetActive(true);

            Gk2ModMetadata meta = mod.Metadata;
            ModUiState state = ModUiStateResolver.Get(mod);
            string issue = string.IsNullOrWhiteSpace(state.Issue)
                ? string.Empty
                : "\n" + FrameworkUi.L("mods.issue", "Issue") + ": " + state.Issue;

            string enabledInfo = string.Empty;
            if (meta.FrameworkManagesEnabledState)
            {
                enabledInfo =
                    "\n" + FrameworkUi.L("mods.enabled", "Enabled") + ": "
                    + (mod.IsEnabled ? FrameworkUi.L("common.yes", "Yes") : FrameworkUi.L("common.no", "No"))
                    + (meta.SupportsRuntimeToggle
                        ? string.Empty
                        : "\n" + FrameworkUi.L("mods.next_start", "Next start") + ": "
                        + (mod.IsEnabledOnNextStart ? FrameworkUi.L("common.yes", "Yes") : FrameworkUi.L("common.no", "No")))
                    + "\n" + FrameworkUi.L("mods.runtime", "Runtime") + ": " + ModUiStateResolver.RuntimeLabel(mod);
            }

            metadataText.text =
                meta.Name
                + "\n\n" + FrameworkUi.L("mods.version", "Version") + ": " + meta.Version
                + "\n" + FrameworkUi.L("mods.author", "Author") + ": " + meta.Author
                + "\n" + FrameworkUi.L("mods.compatibility", "Compatibility") + ": " + FormatCompatibilityStatus(mod.Status)
                + issue
                + "\n" + FrameworkUi.L("mods.dependencies", "Dependencies") + ": " + FormatDependencies(mod)
                + enabledInfo
                + "\n\n" + meta.Description;

            enabledButton.gameObject.SetActive(meta.FrameworkManagesEnabledState);
            FrameworkUi.SetRect(
                (RectTransform)settingsButton.transform,
                meta.FrameworkManagesEnabledState ? new Vector2(180f, 42f) : new Vector2(18f, 42f),
                meta.FrameworkManagesEnabledState ? new Vector2(332f, 72f) : new Vector2(170f, 72f),
                Vector2.zero,
                Vector2.zero);

            if (meta.FrameworkManagesEnabledState)
            {
                if (meta.SupportsRuntimeToggle)
                {
                    enabledText.text = mod.IsEnabled
                        ? FrameworkUi.L("common.disable", "Disable")
                        : FrameworkUi.L("common.enable", "Enable");
                }
                else
                {
                    enabledText.text = mod.IsEnabledOnNextStart
                        ? FrameworkUi.L("mods.disable_after_restart", "Disable after restart")
                        : FrameworkUi.L("mods.enable_after_restart", "Enable after restart");
                }
                enabledButton.interactable = true;
            }
            else
            {
                enabledButton.interactable = false;
            }

            settingsButton.interactable = mod.Settings.Items.Count > 0;
        }

        private static string FormatCompatibilityStatus(ModCompatibilityStatus status)
        {
            switch (status)
            {
                case ModCompatibilityStatus.Compatible:
                    return FrameworkUi.L("mods.compatibility.compatible", "Compatible");
                case ModCompatibilityStatus.UnknownBuild:
                    return FrameworkUi.L("mods.compatibility.unknown_build", "Unknown build");
                case ModCompatibilityStatus.MissingDependency:
                    return FrameworkUi.L("mods.compatibility.missing_dependency", "Missing dependency");
                case ModCompatibilityStatus.VersionMismatch:
                    return FrameworkUi.L("mods.compatibility.version_mismatch", "Version mismatch");
                case ModCompatibilityStatus.Disabled:
                    return FrameworkUi.L("mods.compatibility.disabled", "Disabled");
                case ModCompatibilityStatus.Faulted:
                    return FrameworkUi.L("mods.compatibility.faulted", "Faulted");
                case ModCompatibilityStatus.DependencyUnavailable:
                    return FrameworkUi.L("mods.compatibility.dependency_unavailable", "Dependency unavailable");
                case ModCompatibilityStatus.DependencyCycle:
                    return FrameworkUi.L("mods.compatibility.dependency_cycle", "Dependency cycle");
                default:
                    return status.ToString();
            }
        }

        private static string FormatDependencies(RegisteredMod mod)
        {
            IReadOnlyList<Gk2ModDependency> dependencies =
                mod.Instance.Dependencies ?? Array.Empty<Gk2ModDependency>();
            if (dependencies.Count == 0)
                return FrameworkUi.L("mods.dependencies_none", "None");

            return string.Join(", ", dependencies.Select(dep =>
            {
                string minimum = dep.MinimumVersion?.ToString();
                string maximum = dep.MaximumVersionExclusive?.ToString();
                string range = minimum == null && maximum == null
                    ? string.Empty
                    : " [" + (minimum ?? "*") + ", " + (maximum ?? "*") + ")";
                string optional = dep.Optional
                    ? " (" + FrameworkUi.L("mods.optional", "optional") + ")"
                    : string.Empty;
                return dep.Id + range + optional;
            }));
        }
    }
}
