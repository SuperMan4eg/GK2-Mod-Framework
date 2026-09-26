namespace GK2.Framework
{
    /// <summary>
    /// Resolves optional per-mod UI translations at render time so a rebuilt Mods window
    /// reflects the game's current language without re-registering mods or rebinding config.
    /// </summary>
    internal static class FrameworkModLocalization
    {
        internal static string ModName(RegisteredMod mod)
        {
            Gk2ModMetadata metadata = mod.Metadata;
            return metadata.Id == FrameworkPlugin.PluginGuid
                ? metadata.Name
                : FrameworkLocalization.Get(metadata.Id, "mod.name", metadata.Name);
        }

        internal static string ModDescription(RegisteredMod mod)
        {
            Gk2ModMetadata metadata = mod.Metadata;
            return metadata.Id == FrameworkPlugin.PluginGuid
                ? FrameworkLocalization.Get("framework.description", metadata.Description)
                : FrameworkLocalization.Get(metadata.Id, "mod.description", metadata.Description);
        }

        internal static string SectionName(RegisteredMod mod, string section)
        {
            if (string.IsNullOrWhiteSpace(section))
                return FrameworkUi.L("settings.general", "General");

            if (mod.Metadata.Id == FrameworkPlugin.PluginGuid)
            {
                return string.Equals(section, "UI", System.StringComparison.OrdinalIgnoreCase)
                    ? FrameworkUi.L("settings.ui", "UI")
                    : section;
            }

            return FrameworkLocalization.Get(mod.Metadata.Id, "sections." + section, section);
        }

        internal static string SettingName(RegisteredMod mod, IGk2Setting setting)
        {
            if (IsFrameworkWindowScale(mod, setting))
                return FrameworkUi.L("settings.window_scale", setting.DisplayName);

            if (mod.Metadata.Id == FrameworkPlugin.PluginGuid)
                return setting.DisplayName;

            return FrameworkLocalization.Get(
                mod.Metadata.Id,
                "settings." + setting.UniqueKey + ".name",
                setting.DisplayName);
        }

        internal static string SettingDescription(RegisteredMod mod, IGk2Setting setting)
        {
            if (IsFrameworkWindowScale(mod, setting))
                return FrameworkUi.L("settings.window_scale_description", setting.Description ?? string.Empty);

            if (mod.Metadata.Id == FrameworkPlugin.PluginGuid)
                return setting.Description ?? string.Empty;

            return FrameworkLocalization.Get(
                mod.Metadata.Id,
                "settings." + setting.UniqueKey + ".description",
                setting.Description ?? string.Empty);
        }

        private static bool IsFrameworkWindowScale(RegisteredMod mod, IGk2Setting setting) =>
            mod.Metadata.Id == FrameworkPlugin.PluginGuid
            && string.Equals(setting.UniqueKey, "UI.WindowScalePercent", System.StringComparison.OrdinalIgnoreCase);
    }
}
