using System;
using System.Text;
using UnityEngine;

namespace GK2.Framework
{
    internal static class CompatibilityReport
    {
        internal static string Build(RegisteredMod mod)
        {
            if (mod == null) throw new ArgumentNullException(nameof(mod));

            BuildFingerprint build = FrameworkApi.CurrentBuild;
            var report = new StringBuilder();
            report.AppendLine("GK2 Framework compatibility report");
            report.AppendLine("Framework: " + FrameworkPlugin.PluginVersion);
            report.AppendLine("Game: " + OneLine(Application.version));
            report.AppendLine("Unity: " + OneLine(build?.UnityVersion));
            report.AppendLine("Assembly-CSharp SHA-256: " + OneLine(build?.AssemblyCSharpSha256));
            report.AppendLine("Framework build status: " + (build?.Status.ToString() ?? "Unknown"));
            report.AppendLine("Selected mod: " + OneLine(mod.Metadata.Id));
            report.AppendLine("Mod version: " + OneLine(mod.Metadata.Version.ToString()));
            report.AppendLine("Mod status: " + mod.Status);
            if (mod.Instance is ImportedSettingsMod imported)
            {
                report.AppendLine("Registration scope: imported settings only; gameplay compatibility not verified.");
                report.AppendLine("Source plugin: " + OneLine(imported.SourceId));
            }
            report.AppendLine("Status detail: " + OneLine(mod.StatusDetail));
            report.Append("Log: BepInEx/LogOutput.log");
            return report.ToString();
        }

        private static string OneLine(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "n/a";
            var result = new StringBuilder(Math.Min(value.Length, 200));
            foreach (char character in value)
            {
                if (result.Length >= 200) break;
                result.Append(char.IsControl(character) ? ' ' : character);
            }
            return result.ToString().Trim();
        }
    }
}
