using BepInEx;
using GK2.Framework;
using GK2.OptionalIntegrationTemplate.Main;

namespace GK2.OptionalIntegrationTemplate.FrameworkBridge
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(MainPlugin.PluginGuid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(FrameworkPlugin.PluginGuid, BepInDependency.DependencyFlags.HardDependency)]
    public sealed class FrameworkBridgePlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.yourname.gk2.mymod.framework";
        public const string PluginName = "My Mod - GK2 Framework Integration";
        public const string PluginVersion = "1.0.0";

        private void Awake()
        {
            MainPlugin main = MainPlugin.Instance;
            if (main == null)
            {
                Logger.LogError("Main mod instance is unavailable.");
                return;
            }

            FrameworkApi.RegisterMod(new FrameworkBridge(main), main.Config);
        }

        private sealed class FrameworkBridge : Gk2ModBase
        {
            private readonly MainPlugin main;
            private readonly Gk2ModMetadata metadata = new Gk2ModMetadata(
                MainPlugin.PluginGuid,
                MainPlugin.PluginName,
                "YourName",
                MainPlugin.PluginVersion,
                "Optional GK2 Mod Framework integration.",
                supportsRuntimeToggle: false,
                requiresKnownBuild: false,
                frameworkManagesEnabledState: false);

            internal FrameworkBridge(MainPlugin main) { this.main = main; }
            public override Gk2ModMetadata Metadata => metadata;

            public override void OnRegister(Gk2ModContext context)
            {
                context.Settings.AddToggle(
                    "General",
                    "FeatureEnabled",
                    true,
                    "Feature enabled",
                    "Enable the feature.");

                context.Settings.AddReadOnly(
                    "Status",
                    "Integration",
                    "Framework integration",
                    "Shows whether the optional bridge is active.",
                    () => main != null ? "Active" : "Unavailable");
            }
        }
    }
}
