using UnityEngine;

namespace GK2.Framework
{
    internal enum ModUiSeverity
    {
        Ok,
        Off,
        Warning,
        Error
    }

    internal readonly struct ModUiState
    {
        internal ModUiSeverity Severity { get; }
        internal string Badge { get; }
        internal string Issue { get; }
        internal Color Color { get; }

        internal ModUiState(ModUiSeverity severity, string badge, string issue, Color color)
        {
            Severity = severity;
            Badge = badge;
            Issue = issue ?? string.Empty;
            Color = color;
        }
    }

    internal static class ModUiStateResolver
    {
        internal static ModUiState Get(RegisteredMod mod)
        {
            if (mod == null)
                return new ModUiState(
                    ModUiSeverity.Off,
                    FrameworkUi.L("mods.state.off", "OFF"),
                    string.Empty,
                    new Color(0.28f, 0.28f, 0.28f, 1f));

            switch (mod.Status)
            {
                case ModCompatibilityStatus.Faulted:
                case ModCompatibilityStatus.MissingDependency:
                case ModCompatibilityStatus.VersionMismatch:
                case ModCompatibilityStatus.DependencyCycle:
                    return Error(mod.StatusDetail);

                case ModCompatibilityStatus.UnknownBuild:
                case ModCompatibilityStatus.DependencyUnavailable:
                    return Error(mod.StatusDetail);

                case ModCompatibilityStatus.Disabled:
                    return Off();

                case ModCompatibilityStatus.Compatible:
                    if (mod.HasPendingRestart)
                        return Warning(FrameworkUi.L(
                            "mods.issue.pending_restart",
                            "Restart required to apply the scheduled enabled state."));
                    return mod.IsEnabled ? Ok() : Off();

                default:
                    return Warning(mod.StatusDetail);
            }
        }

        internal static string RuntimeLabel(RegisteredMod mod)
        {
            if (mod == null) return string.Empty;
            if (!mod.Metadata.FrameworkManagesEnabledState)
                return FrameworkUi.L("mods.runtime.external", "Managed by mod");
            return mod.Metadata.SupportsRuntimeToggle
                ? FrameworkUi.L("mods.runtime.live", "Live toggle")
                : FrameworkUi.L("mods.runtime.restart", "Restart required");
        }

        private static ModUiState Ok() =>
            new ModUiState(
                ModUiSeverity.Ok,
                FrameworkUi.L("mods.state.ok", "OK"),
                string.Empty,
                new Color(0.18f, 0.46f, 0.22f, 1f));

        private static ModUiState Off() =>
            new ModUiState(
                ModUiSeverity.Off,
                FrameworkUi.L("mods.state.off", "OFF"),
                string.Empty,
                new Color(0.28f, 0.28f, 0.28f, 1f));

        private static ModUiState Warning(string issue) =>
            new ModUiState(
                ModUiSeverity.Warning,
                FrameworkUi.L("mods.state.warning", "WARN"),
                string.IsNullOrWhiteSpace(issue)
                    ? FrameworkUi.L("mods.issue.warning", "Attention required.")
                    : issue,
                new Color(0.68f, 0.43f, 0.08f, 1f));

        private static ModUiState Error(string issue) =>
            new ModUiState(
                ModUiSeverity.Error,
                FrameworkUi.L("mods.state.error", "ERROR"),
                string.IsNullOrWhiteSpace(issue)
                    ? FrameworkUi.L("mods.issue.error", "Mod cannot run.")
                    : issue,
                new Color(0.62f, 0.16f, 0.13f, 1f));
    }
}
