using BepInEx;
using BepInEx.Configuration;

namespace GK2.OptionalIntegrationTemplate.Main
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class MainPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.yourname.gk2.mymod";
        public const string PluginName = "My Mod";
        public const string PluginVersion = "1.0.0";

        public static MainPlugin Instance { get; private set; }
        public ConfigEntry<bool> FeatureEnabled { get; private set; }

        private void Awake()
        {
            Instance = this;
            FeatureEnabled = Config.Bind("General", "FeatureEnabled", true, "Enable the feature.");

            // Standalone gameplay initialization goes here.
        }
    }
}
