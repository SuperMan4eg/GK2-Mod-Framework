using BepInEx;
using HarmonyLib;

namespace GK2.Framework
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class FrameworkPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "ru.superman4eg.gk2.framework";
        public const string PluginName = "GK2 Mod Framework";
        public const string PluginVersion = FrameworkBuildVersion.Value;
        private Harmony harmony;

        private void Awake()
        {
            FrameworkLog.Source = Logger;
            FrameworkApi.CurrentBuild = BuildFingerprint.Capture();
            FrameworkApi.Registry = new FrameworkRegistry(Logger, FrameworkApi.CurrentBuild);
            FrameworkApi.Registry.Register(new FrameworkSelfMod(), Config);
            Gk2GameEvents.Bind(Logger);
            harmony = new Harmony(PluginGuid);
            harmony.PatchAll(typeof(MenuInjectionPatches));
            harmony.PatchAll(typeof(SettingsGamepadNavigation));
            Logger.LogInfo("GK2_FRAMEWORK_READY: " + FrameworkApi.CurrentBuild);
        }

        private void OnDestroy() { harmony?.UnpatchSelf(); }
        private void Start() => ImportedSettings.Refresh();

        private sealed class FrameworkSelfMod : Gk2ModBase
        {
            private readonly Gk2ModMetadata metadata = new Gk2ModMetadata(
                PluginGuid, PluginName, "SuperMan4eg", PluginVersion,
                FrameworkLocalization.Get(
                    "framework.description",
                    "Shared lifecycle, compatibility, settings and Mods menu API for GK2 BepInEx mods."));
            public override Gk2ModMetadata Metadata => metadata;

            public override void OnRegister(Gk2ModContext context)
            {
                ImportedSettings.Enabled = context.Settings.AddToggle("Import", "Enabled", true,
                    FrameworkLocalization.Get("import.enabled", "Import standalone mod settings"),
                    FrameworkLocalization.Get("import.enabled_description", "Show supported settings from loaded BepInEx plugins. Reopen the Mods list after changing this option."));
                ImportedSettings.ExcludedIds = context.Settings.AddText("Import", "ExcludedIds", string.Empty,
                    FrameworkLocalization.Get("import.excluded", "Excluded plugin IDs"),
                    FrameworkLocalization.Get("import.excluded_description", "Comma-separated plugin IDs to omit from settings import. Reopen the Mods list after changing this value."));
                FrameworkUi.WindowScalePercent = context.Settings.AddIntSlider(
                    "UI",
                    "WindowScalePercent",
                    100,
                    50,
                    100,
                    FrameworkLocalization.Get("settings.window_scale", "Window scale (%)"),
                    FrameworkLocalization.Get(
                        "settings.window_scale_description",
                        "Maximum size of Framework-hosted windows. Windows still shrink automatically to fit the current safe area."),
                    10,
                    -100);
                FrameworkUi.WindowScalePercent.SettingChanged += (_, __) => ModsMenuWindow.RefreshResponsiveScale();
            }
        }
    }
}
