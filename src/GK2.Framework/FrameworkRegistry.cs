using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace GK2.Framework
{
    public sealed class RegisteredMod
    {
        internal readonly IGk2Mod Instance;
        internal readonly ConfigEntry<bool> EnabledEntry;
        public Gk2ModMetadata Metadata => Instance.Metadata;
        public Gk2Settings Settings { get; }
        public ModCompatibilityStatus Status { get; internal set; }
        public string StatusDetail { get; internal set; }
        public bool IsEnabled { get; internal set; }
        public bool IsEnabledOnNextStart => !Metadata.FrameworkManagesEnabledState || EnabledEntry.Value;
        public bool HasPendingRestart => Metadata.FrameworkManagesEnabledState
            && !Metadata.SupportsRuntimeToggle
            && StartupEnabledPreference != EnabledEntry.Value;
        internal bool StartupEnabledPreference { get; }
        internal bool RegistrationFailed { get; set; }
        internal bool LifecycleFaulted { get; set; }
        internal bool CurrentBuildCompatibilityConfirmed { get; set; }
        internal string CurrentBuildCompatibilityDetail { get; set; }
        internal RegisteredMod(IGk2Mod instance, Gk2Settings settings, ConfigEntry<bool> enabled)
        {
            Instance = instance;
            Settings = settings;
            EnabledEntry = enabled;
            StartupEnabledPreference = enabled?.Value ?? true;
            StatusDetail = string.Empty;
        }
    }

    internal sealed class FrameworkRegistry
    {
        private readonly Dictionary<string, RegisteredMod> byId = new Dictionary<string, RegisteredMod>(StringComparer.OrdinalIgnoreCase);
        private readonly List<RegisteredMod> ordered = new List<RegisteredMod>();
        private readonly Dictionary<ConfigFile, string> configOwners = new Dictionary<ConfigFile, string>();
        private readonly ManualLogSource log;
        private readonly BuildFingerprint build;
        internal IReadOnlyList<RegisteredMod> Mods => ordered;

        internal FrameworkRegistry(ManualLogSource log, BuildFingerprint build) { this.log = log; this.build = build; }

        internal RegisteredMod Register(IGk2Mod mod, ConfigFile config)
        {
            if (mod == null) throw new ArgumentNullException(nameof(mod));
            Gk2ModMetadata metadata = mod.Metadata ?? throw new InvalidOperationException("Mod metadata is null.");
            if (byId.ContainsKey(metadata.Id)) throw new InvalidOperationException("Duplicate GK2 mod id: " + metadata.Id);
            ConfigEntry<bool> enabled = metadata.FrameworkManagesEnabledState
                ? BindEnabledEntry(config, metadata.Id)
                : null;
            var settings = new Gk2Settings(config);
            var record = new RegisteredMod(mod, settings, enabled);
            byId.Add(metadata.Id, record); ordered.Add(record);
            if (!Safe(record, "OnRegister", () => mod.OnRegister(new Gk2ModContext(
                settings,
                new Gk2ModLogger(log, metadata.Id),
                build,
                detail => ConfirmCurrentBuildCompatibility(record, detail)))))
                record.RegistrationFailed = true;
            ReevaluateAll();
            log.LogInfo($"Registered GK2 mod [{metadata.Id}] {metadata.Version} by {metadata.Author}; status={record.Status}");
            return record;
        }

        private ConfigEntry<bool> BindEnabledEntry(ConfigFile config, string modId)
        {
            if (!configOwners.ContainsKey(config))
            {
                configOwners.Add(config, modId);
                return config.Bind("Framework", "Enabled", true, "Enable this mod on game startup.");
            }

            string key = "Enabled." + modId;
            return config.Bind("Framework", key, true,
                "Enable framework mod '" + modId + "' on game startup.");
        }

        internal bool SetRuntimeEnabled(string id, bool enabled)
        {
            if (!byId.TryGetValue(id, out var record)
                || !record.Metadata.FrameworkManagesEnabledState
                || !record.Metadata.SupportsRuntimeToggle
                || record.EnabledEntry == null) return false;
            record.EnabledEntry.Value = enabled;
            ReevaluateAll();
            return record.IsEnabled == enabled;
        }

        internal bool SetEnabledOnNextStart(string id, bool enabled)
        {
            if (!byId.TryGetValue(id, out RegisteredMod record)
                || !record.Metadata.FrameworkManagesEnabledState
                || record.EnabledEntry == null) return false;
            record.EnabledEntry.Value = enabled;
            return record.EnabledEntry.Value == enabled;
        }

        internal void NotifyGameStarted()
        {
            foreach (RegisteredMod mod in BuildDependencyOrder(out _))
            {
                if (!mod.IsEnabled) continue;
                if (Safe(mod, "OnGameStarted", mod.Instance.OnGameStarted)) continue;

                mod.LifecycleFaulted = true;
                ReevaluateAll();
            }
        }

        internal void NotifyReturnedToMainMenu()
        {
            bool faulted = false;
            List<RegisteredMod> order = BuildDependencyOrder(out _);
            for (int i = order.Count - 1; i >= 0; i--)
            {
                RegisteredMod mod = order[i];
                if (!mod.IsEnabled) continue;
                if (Safe(mod, "OnReturnedToMainMenu", mod.Instance.OnReturnedToMainMenu)) continue;
                mod.LifecycleFaulted = true;
                faulted = true;
            }
            if (faulted) ReevaluateAll();
        }

        private void ReevaluateAll()
        {
            List<RegisteredMod> order = BuildDependencyOrder(out HashSet<RegisteredMod> cycleMembers);

            foreach (RegisteredMod record in order)
            {
                if (cycleMembers.Contains(record))
                {
                    record.Status = ModCompatibilityStatus.DependencyCycle;
                    record.StatusDetail = "Required dependency cycle detected.";
                    continue;
                }

                record.Status = ResolveBaseStatus(record, out string detail);
                record.StatusDetail = detail;
            }

            for (int i = order.Count - 1; i >= 0; i--)
            {
                RegisteredMod record = order[i];
                bool shouldRemainEnabled = record.Status == ModCompatibilityStatus.Compatible
                    && IsEnabledForCurrentSession(record)
                    && RequiredDependenciesConfigured(record);

                if (!shouldRemainEnabled && record.IsEnabled)
                    DisableRecord(record);
            }

            foreach (RegisteredMod record in order)
            {
                if (record.Status != ModCompatibilityStatus.Compatible || !IsEnabledForCurrentSession(record)) continue;

                if (!RequiredDependenciesEnabled(record, out string unavailable))
                {
                    if (record.IsEnabled) DisableRecord(record);
                    record.Status = ModCompatibilityStatus.DependencyUnavailable;
                    record.StatusDetail = unavailable;
                    continue;
                }

                if (record.IsEnabled) continue;
                if (Safe(record, "OnEnable", record.Instance.OnEnable))
                {
                    record.IsEnabled = true;
                    continue;
                }

                record.LifecycleFaulted = true;
                record.Status = ModCompatibilityStatus.Faulted;
                if (string.IsNullOrWhiteSpace(record.StatusDetail))
                    record.StatusDetail = "OnEnable failed.";
            }
        }

        private void ConfirmCurrentBuildCompatibility(RegisteredMod record, string detail)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            if (build.Status == BuildCompatibilityStatus.Incompatible)
                throw new InvalidOperationException(
                    "The current game build cannot be confirmed because the base build fingerprint is incompatible.");

            record.CurrentBuildCompatibilityConfirmed = true;
            record.CurrentBuildCompatibilityDetail = string.IsNullOrWhiteSpace(detail)
                ? "Mod compatibility contract passed for the current game build."
                : detail.Trim();

            log.LogInfo(
                $"[{record.Metadata.Id}] GK2_BUILD_CONTRACT_PASSED: build={build.AssemblyCSharpSha256}; "
                + record.CurrentBuildCompatibilityDetail);
        }

        private ModCompatibilityStatus ResolveBaseStatus(RegisteredMod record, out string detail)
        {
            if (record.RegistrationFailed)
            {
                detail = string.IsNullOrWhiteSpace(record.StatusDetail) ? "OnRegister failed." : record.StatusDetail;
                return ModCompatibilityStatus.Faulted;
            }

            if (record.LifecycleFaulted)
            {
                detail = string.IsNullOrWhiteSpace(record.StatusDetail) ? "Lifecycle callback failed." : record.StatusDetail;
                return ModCompatibilityStatus.Faulted;
            }

            if (record.Metadata.RequiresKnownBuild && build.Status != BuildCompatibilityStatus.Compatible)
            {
                bool contractAllowsUnknownBuild = build.Status == BuildCompatibilityStatus.Unknown
                    && record.CurrentBuildCompatibilityConfirmed;
                if (!contractAllowsUnknownBuild)
                {
                    detail = "Known compatible game build required; current build is " + build.Status;
                    return ModCompatibilityStatus.UnknownBuild;
                }
            }

            foreach (Gk2ModDependency dep in record.Instance.Dependencies ?? Array.Empty<Gk2ModDependency>())
            {
                if (!byId.TryGetValue(dep.Id, out RegisteredMod found))
                {
                    if (dep.Optional) continue;
                    detail = "Missing dependency: " + dep.Id;
                    return ModCompatibilityStatus.MissingDependency;
                }

                Version version = found.Metadata.Version;
                if ((dep.MinimumVersion != null && version < dep.MinimumVersion)
                    || (dep.MaximumVersionExclusive != null && version >= dep.MaximumVersionExclusive))
                {
                    detail = $"Dependency {dep.Id} version {version} is outside the supported range.";
                    return ModCompatibilityStatus.VersionMismatch;
                }

                if (dep.Optional) continue;
                if (found.Status != ModCompatibilityStatus.Compatible)
                {
                    detail = $"Dependency {dep.Id} is unavailable ({found.Status}).";
                    return ModCompatibilityStatus.DependencyUnavailable;
                }

                if (!IsEnabledForCurrentSession(found))
                {
                    detail = "Dependency " + dep.Id + " is disabled for the current session.";
                    return ModCompatibilityStatus.DependencyUnavailable;
                }
            }

            if (build.Status == BuildCompatibilityStatus.Compatible)
            {
                detail = string.Empty;
            }
            else if (record.CurrentBuildCompatibilityConfirmed)
            {
                detail = record.CurrentBuildCompatibilityDetail;
            }
            else
            {
                detail = "Running on an unverified game build";
            }
            return ModCompatibilityStatus.Compatible;
        }

        private bool RequiredDependenciesConfigured(RegisteredMod record)
        {
            foreach (Gk2ModDependency dep in record.Instance.Dependencies ?? Array.Empty<Gk2ModDependency>())
            {
                if (dep.Optional) continue;
                if (!byId.TryGetValue(dep.Id, out RegisteredMod found)) return false;
                if (found.Status != ModCompatibilityStatus.Compatible || !IsEnabledForCurrentSession(found)) return false;
            }
            return true;
        }

        private static bool IsEnabledForCurrentSession(RegisteredMod record)
        {
            if (!record.Metadata.FrameworkManagesEnabledState) return true;
            return record.Metadata.SupportsRuntimeToggle
                ? record.EnabledEntry.Value
                : record.StartupEnabledPreference;
        }

        private bool RequiredDependenciesEnabled(RegisteredMod record, out string detail)
        {
            foreach (Gk2ModDependency dep in record.Instance.Dependencies ?? Array.Empty<Gk2ModDependency>())
            {
                if (dep.Optional) continue;
                if (!byId.TryGetValue(dep.Id, out RegisteredMod found))
                {
                    detail = "Missing dependency: " + dep.Id;
                    return false;
                }
                if (!found.IsEnabled)
                {
                    detail = $"Dependency {dep.Id} is not enabled ({found.Status}).";
                    return false;
                }
            }
            detail = string.Empty;
            return true;
        }

        private void DisableRecord(RegisteredMod record)
        {
            if (!Safe(record, "OnDisable", record.Instance.OnDisable))
                record.LifecycleFaulted = true;
            record.IsEnabled = false;
        }

        private List<RegisteredMod> BuildDependencyOrder(out HashSet<RegisteredMod> cycleMembers)
        {
            var result = new List<RegisteredMod>(ordered.Count);
            var state = new Dictionary<RegisteredMod, int>();
            var stack = new List<RegisteredMod>();
            cycleMembers = new HashSet<RegisteredMod>();

            foreach (RegisteredMod record in ordered)
                Visit(record, state, stack, result, cycleMembers);

            return result;
        }

        private void Visit(RegisteredMod record, Dictionary<RegisteredMod, int> state, List<RegisteredMod> stack,
            List<RegisteredMod> result, HashSet<RegisteredMod> cycleMembers)
        {
            if (state.TryGetValue(record, out int current) && current == 2) return;
            if (current == 1) return;

            state[record] = 1;
            stack.Add(record);

            foreach (Gk2ModDependency dep in record.Instance.Dependencies ?? Array.Empty<Gk2ModDependency>())
            {
                if (dep.Optional || !byId.TryGetValue(dep.Id, out RegisteredMod dependency)) continue;

                if (!state.TryGetValue(dependency, out int dependencyState))
                {
                    Visit(dependency, state, stack, result, cycleMembers);
                    continue;
                }

                if (dependencyState != 1) continue;
                int cycleStart = stack.IndexOf(dependency);
                for (int i = Math.Max(0, cycleStart); i < stack.Count; i++)
                    cycleMembers.Add(stack[i]);
            }

            stack.RemoveAt(stack.Count - 1);
            state[record] = 2;
            if (!result.Contains(record)) result.Add(record);
        }

        private bool Safe(RegisteredMod record, string callback, Action action)
        {
            try { action(); return true; }
            catch (Exception ex)
            {
                record.Status = ModCompatibilityStatus.Faulted;
                record.StatusDetail = callback + " failed: " + ex.GetType().Name;
                log.LogError($"[{record.Metadata.Id}] {callback} failed: {ex}");
                return false;
            }
        }
    }
}
