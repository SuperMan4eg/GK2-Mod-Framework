using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace GK2.Framework
{
    public static class FrameworkApi
    {
        internal static FrameworkRegistry Registry;
        public static bool IsReady => Registry != null;
        public static BuildFingerprint CurrentBuild { get; internal set; }
        public static IReadOnlyList<RegisteredMod> Mods => Registry?.Mods ?? Array.Empty<RegisteredMod>();

        public static RegisteredMod RegisterMod(IGk2Mod mod, ConfigFile config)
        {
            if (Registry == null) throw new InvalidOperationException("GK2 Framework has not initialized.");
            if (config == null) throw new ArgumentNullException(nameof(config));
            return Registry.Register(mod, config);
        }

        public static bool SetRuntimeEnabled(string id, bool enabled) => Registry != null && Registry.SetRuntimeEnabled(id, enabled);
        public static bool SetEnabledOnNextStart(string id, bool enabled) =>
            Registry != null && Registry.SetEnabledOnNextStart(id, enabled);
        public static void ToggleModsMenu() => ModsMenuWindow.Toggle();
    }
}
