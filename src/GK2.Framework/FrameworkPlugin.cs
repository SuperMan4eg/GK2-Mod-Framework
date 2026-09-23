using BepInEx;
using HarmonyLib;

namespace GK2.Framework
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class FrameworkPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "ru.superman4eg.gk2.framework";
        public const string PluginName = "GK2 Mod Framework";
        public const string PluginVersion = "0.1.4";
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
            Logger.LogInfo("GK2_FRAMEWORK_READY: " + FrameworkApi.CurrentBuild);
        }

        private void OnDestroy() { harmony?.UnpatchSelf(); }

        private sealed class FrameworkSelfMod : Gk2ModBase
        {
            private readonly Gk2ModMetadata metadata = new Gk2ModMetadata(
                PluginGuid, PluginName, "SuperMan4eg", PluginVersion,
                "Shared lifecycle, compatibility, settings and Mods menu API for GK2 BepInEx mods.");
            public override Gk2ModMetadata Metadata => metadata;
        }
    }
}
