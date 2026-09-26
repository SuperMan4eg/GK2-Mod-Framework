using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace GK2.Framework.Example
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(FrameworkPlugin.PluginGuid, BepInDependency.DependencyFlags.HardDependency)]
    public sealed class ExamplePlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "ru.superman4eg.gk2.framework.example";
        public const string PluginName = "GK2 Framework Example";
        public const string PluginVersion = "0.1.0";

        private void Awake() { FrameworkApi.RegisterMod(new ExampleMod(), Config); }
    }

    internal sealed class ExampleMod : Gk2ModBase
    {
        private readonly Gk2ModMetadata metadata = new Gk2ModMetadata(
            ExamplePlugin.PluginGuid, ExamplePlugin.PluginName, "SuperMan4eg", ExamplePlugin.PluginVersion,
            "Smoke-test settings for GK2 Mod Framework. It does not modify game balance.",
            supportsRuntimeToggle: true, requiresKnownBuild: false);
        private readonly IReadOnlyList<Gk2ModDependency> dependencies = new[]
        {
            new Gk2ModDependency(FrameworkPlugin.PluginGuid, "0.1.0", "0.2.0")
        };
        private Gk2ModLogger log;
        private ConfigEntry<bool> showGreeting;
        private ConfigEntry<int> sampleCount;
        private ConfigEntry<float> sampleScale;
        private ConfigEntry<string> sampleMode;
        private ConfigEntry<KeyboardShortcut> menuShortcut;
        private ConfigEntry<string> note;
        private ConfigEntry<bool> conditionalPartEnabled;
        private ConfigEntry<int> conditionalHiddenOffset;
        private ConfigEntry<int> conditionalDisabledOffset;
        private int actionClickCount;

        public override Gk2ModMetadata Metadata => metadata;
        public override IReadOnlyList<Gk2ModDependency> Dependencies => dependencies;

        public override void OnRegister(Gk2ModContext context)
        {
            log = context.Log;
            showGreeting = context.Settings.AddToggle("Example", "ShowGreeting", true, "Test toggle", "A harmless boolean example.");
            sampleCount = context.Settings.AddIntSlider("Example", "SampleCount", 3, 0, 10, "Integer slider", "A harmless integer example.");
            sampleScale = context.Settings.AddFloatSlider("Example", "SampleScale", 1f, 0.25f, 2f, "Float slider", "A harmless float example.");
            sampleMode = context.Settings.AddDropdown("Example", "SampleMode", "Normal", new[] { "Quiet", "Normal", "Verbose" }, "Dropdown", "A harmless choice example.");
            menuShortcut = context.Settings.AddKeybind("Example", "SampleKeybind", new KeyboardShortcut(KeyCode.F9), "Keybind", "Captured and stored, but performs no gameplay action.");
            note = context.Settings.AddText("Example", "Note", "Framework smoke test", "Text", "An editable text setting.");
            context.Settings.AddButton(
                "Example",
                "Action",
                "Action button",
                "A non-persistent action row with a dynamic label.",
                () => "Run (" + actionClickCount + ")",
                () => actionClickCount++);

            conditionalPartEnabled = context.Settings.AddToggle(
                "Conditional", "PartEnabled", true,
                "Enable conditional part",
                "Controls the visibility/enabled examples below.",
                order: 0);
            conditionalHiddenOffset = context.Settings.AddIntSlider(
                "Conditional", "HiddenOffset", 25, 0, 100,
                "Hidden when disabled",
                "This row disappears while the parent toggle is off.",
                step: 5,
                order: 1);
            conditionalDisabledOffset = context.Settings.AddIntSlider(
                "Conditional", "DisabledOffset", 50, 0, 100,
                "Disabled when parent is off",
                "This row stays visible but becomes unavailable while the parent toggle is off.",
                step: 5,
                order: 2);

            context.Settings.SetVisibilityCondition(
                "Conditional",
                "HiddenOffset",
                () => conditionalPartEnabled.Value);
            context.Settings.SetEnabledCondition(
                "Conditional",
                "DisabledOffset",
                () => conditionalPartEnabled.Value);

            context.Settings.AddReadOnly("Status", "Summary", "Read-only value", "Live value assembled from test settings.",
                () => $"toggle={showGreeting.Value}, count={sampleCount.Value}, scale={sampleScale.Value:0.00}, mode={sampleMode.Value}, key={menuShortcut.Value}, note={note.Value}, conditional={conditionalPartEnabled.Value}, hiddenOffset={conditionalHiddenOffset.Value}, disabledOffset={conditionalDisabledOffset.Value}");
            log.Info($"GK2_EXAMPLE_SETTINGS_LOADED: toggle={showGreeting.Value}; count={sampleCount.Value}; scale={sampleScale.Value:R}; mode={sampleMode.Value}; key={menuShortcut.Value}; note={note.Value}; conditional={conditionalPartEnabled.Value}; hiddenOffset={conditionalHiddenOffset.Value}; disabledOffset={conditionalDisabledOffset.Value}");
            log.Info("GK2_EXAMPLE_REGISTERED");
        }

        public override void OnEnable() => log.Info("GK2_EXAMPLE_ENABLED");
        public override void OnDisable() => log.Info("GK2_EXAMPLE_DISABLED");
        public override void OnGameStarted() => log.Info("Game started event received; no gameplay state changed.");
        public override void OnReturnedToMainMenu() => log.Info("Returned-to-main-menu event received.");
    }
}
