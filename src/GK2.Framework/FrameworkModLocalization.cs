using System;

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
            if (mod.Instance is ImportedSettingsMod)
                return metadata.Name + " — " + FrameworkUi.L("import.label", "Imported settings");
            return IsFramework(mod)
                ? metadata.Name
                : FrameworkLocalization.Get(
                    metadata.Id,
                    "mod.name",
                    metadata.Name);
        }

        internal static string ModDescription(RegisteredMod mod)
        {
            if (mod.Instance is ImportedSettingsMod)
                return FrameworkUi.L("import.description", "Edits the original plugin configuration. When changes take effect depends on the plugin; restart if unsure. Framework does not manage this plugin or verify its gameplay compatibility.");
            Gk2ModMetadata metadata = mod.Metadata;
            return IsFramework(mod)
                ? FrameworkLocalization.Get(
                    "framework.description",
                    metadata.Description)
                : FrameworkLocalization.Get(
                    metadata.Id,
                    "mod.description",
                    metadata.Description);
        }

        internal static string SectionName(
            RegisteredMod mod,
            string section)
        {
            if (string.IsNullOrWhiteSpace(section))
                return FrameworkUi.L(
                    "settings.general",
                    "General");

            if (IsFramework(mod))
            {
                return string.Equals(
                    section,
                    "UI",
                    StringComparison.OrdinalIgnoreCase)
                        ? FrameworkUi.L("settings.ui", "UI")
                        : section;
            }

            return FrameworkLocalization.Get(
                mod.Metadata.Id,
                "sections." + section,
                section);
        }

        internal static string SettingName(
            RegisteredMod mod,
            IGk2Setting setting)
        {
            if (IsFramework(mod) && setting.UniqueKey == "Import.Enabled")
                return FrameworkUi.L("import.enabled", setting.DisplayName);
            if (IsFramework(mod) && setting.UniqueKey == "Import.ExcludedIds")
                return FrameworkUi.L("import.excluded", setting.DisplayName);
            if (IsFrameworkWindowScale(mod, setting))
            {
                return FrameworkUi.L(
                    "settings.window_scale",
                    setting.DisplayName);
            }

            if (IsFramework(mod))
                return setting.DisplayName;

            return FrameworkLocalization.Get(
                mod.Metadata.Id,
                "settings." + setting.UniqueKey + ".name",
                setting.DisplayName);
        }

        internal static string SettingDescription(
            RegisteredMod mod,
            IGk2Setting setting)
        {
            if (IsFramework(mod) && setting.UniqueKey == "Import.Enabled")
                return FrameworkUi.L("import.enabled_description", setting.Description);
            if (IsFramework(mod) && setting.UniqueKey == "Import.ExcludedIds")
                return FrameworkUi.L("import.excluded_description", setting.Description);
            if (setting is ImportedSetting && setting.IsReadOnly)
                return setting.Description + "\n" + FrameworkUi.L("import.readonly", "This setting type or editor is not supported; shown read-only.");
            if (mod.Instance is ImportedSettingsMod) return setting.Description;
            if (IsFrameworkWindowScale(mod, setting))
            {
                return FrameworkUi.L(
                    "settings.window_scale_description",
                    setting.Description ?? string.Empty);
            }

            if (IsFramework(mod))
                return setting.Description ?? string.Empty;

            return FrameworkLocalization.Get(
                mod.Metadata.Id,
                "settings." + setting.UniqueKey + ".description",
                setting.Description ?? string.Empty);
        }

        private static bool IsFramework(RegisteredMod mod) =>
            string.Equals(
                mod.Metadata.Id,
                FrameworkPlugin.PluginGuid,
                StringComparison.OrdinalIgnoreCase);

        private static bool IsFrameworkWindowScale(
            RegisteredMod mod,
            IGk2Setting setting) =>
            IsFramework(mod)
            && string.Equals(
                setting.UniqueKey,
                "UI.WindowScalePercent",
                StringComparison.OrdinalIgnoreCase);
    }
}
