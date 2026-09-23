using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using GK2.Framework;

namespace GK2.Framework.ModTemplate
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(FrameworkPlugin.PluginGuid, BepInDependency.DependencyFlags.HardDependency)]
    public sealed class ModTemplatePlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.yourname.gk2.modtemplate";
        public const string PluginName = "GK2 Mod Template";
        public const string PluginVersion = "0.1.0";

        private void Awake()
        {
            FrameworkApi.RegisterMod(new ModTemplate(), Config);
        }
    }

    internal sealed class ModTemplate : Gk2ModBase
    {
        private readonly Gk2ModMetadata metadata = new Gk2ModMetadata(
            ModTemplatePlugin.PluginGuid,
            ModTemplatePlugin.PluginName,
            "YourName",
            ModTemplatePlugin.PluginVersion,
            "Template consumer for GK2 Mod Framework.",
            supportsRuntimeToggle: true,
            requiresKnownBuild: false);

        private readonly IReadOnlyList<Gk2ModDependency> dependencies = new[]
        {
            new Gk2ModDependency(FrameworkPlugin.PluginGuid, "0.1.0", "0.2.0")
        };

        private Gk2ModLogger log;
        private ConfigEntry<bool> featureEnabled;
        private ConfigEntry<int> strength;

        public override Gk2ModMetadata Metadata => metadata;
        public override IReadOnlyList<Gk2ModDependency> Dependencies => dependencies;

        public override void OnRegister(Gk2ModContext context)
        {
            log = context.Log;

            featureEnabled = context.Settings.AddToggle(
                "General",
                "FeatureEnabled",
                true,
                "Feature enabled",
                "Enable the example feature.",
                order: 0);

            strength = context.Settings.AddIntSlider(
                "Gameplay",
                "Strength",
                5,
                0,
                10,
                "Strength",
                "Example gameplay value.",
                step: 1,
                order: 0);

            context.Settings.AddReadOnly(
                "Status",
                "Summary",
                "Current state",
                "Live read-only value.",
                () => $"enabled={featureEnabled.Value}; strength={strength.Value}",
                order: 0);

            log.Info("MOD_TEMPLATE_REGISTERED");
        }

        public override void OnEnable()
        {
            // Apply Harmony patches / subscribe to game events here.
            log.Info("MOD_TEMPLATE_ENABLED");
        }

        public override void OnDisable()
        {
            // Undo every runtime change made in OnEnable.
            log.Info("MOD_TEMPLATE_DISABLED");
        }

        public override void OnGameStarted()
        {
            // Save/gameplay objects are available here.
        }

        public override void OnReturnedToMainMenu()
        {
            // Clear per-session state here.
        }
    }
}
