using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Bootstrap;
using UnityEngine;

namespace GK2.Framework
{
    internal static class LoadedModsReport
    {
        internal static string Build()
        {
            var result = new StringBuilder("GK2 loaded mods\n");
            result.AppendLine("Framework: " + FrameworkPlugin.PluginVersion);
            result.AppendLine("Game: " + OneLine(Application.version) + "; Unity: " + OneLine(Application.unityVersion));
            result.AppendLine("Game assembly SHA-256: " + OneLine(FrameworkApi.CurrentBuild?.AssemblyCSharpSha256));
            var mods = FrameworkApi.Mods.ToArray();
            var matched = new HashSet<RegisteredMod>();
            var plugins = Chainloader.PluginInfos.Values.Where(plugin => plugin.Instance != null)
                .OrderBy(plugin => plugin.Metadata.GUID, StringComparer.OrdinalIgnoreCase).ToArray();
            result.AppendLine("Loaded BepInEx plugins: " + plugins.Length);
            foreach (var plugin in plugins)
            {
                var registrations = mods.Where(mod => string.Equals(mod.Metadata.Id, plugin.Metadata.GUID, StringComparison.OrdinalIgnoreCase)
                    || (mod.Instance is ImportedSettingsMod imported && string.Equals(imported.SourceId, plugin.Metadata.GUID, StringComparison.OrdinalIgnoreCase))).ToArray();
                foreach (var mod in registrations) matched.Add(mod);
                result.Append("- " + OneLine(plugin.Metadata.Name) + " | " + plugin.Metadata.Version + " | " + OneLine(plugin.Metadata.GUID));
                result.AppendLine(registrations.Length == 0 ? " | loaded; no Framework registration"
                    : " | " + string.Join("; ", registrations.Select(State)));
            }
            var additional = mods.Where(mod => !matched.Contains(mod)).OrderBy(mod => mod.Metadata.Id, StringComparer.OrdinalIgnoreCase).ToArray();
            if (additional.Length > 0)
            {
                result.AppendLine("Other Framework registrations (not matched to a loaded plugin ID):");
                foreach (var mod in additional) result.AppendLine("- " + OneLine(mod.Metadata.Name) + " | " + mod.Metadata.Version + " | " + OneLine(mod.Metadata.Id) + " | " + State(mod));
            }
            return result.ToString().TrimEnd();
        }
        private static string State(RegisteredMod mod) => (mod.Instance is ImportedSettingsMod ? "imported settings only" : mod.IsEnabled ? "enabled" : "disabled")
            + "; " + mod.Status + (mod.HasPendingRestart ? "; restart pending" : string.Empty);
        internal static string OneLine(string value) => string.IsNullOrWhiteSpace(value) ? "n/a"
            : new string(value.Take(160).Select(character => char.IsControl(character) ? ' ' : character).ToArray()).Replace("<", "[").Replace(">", "]");
    }
}
