using System;
using System.Collections.Generic;

namespace GK2.Framework
{
    public sealed class Gk2ModMetadata
    {
        public string Id { get; }
        public string Name { get; }
        public string Author { get; }
        public Version Version { get; }
        public string Description { get; }
        public bool SupportsRuntimeToggle { get; }
        public bool RequiresKnownBuild { get; }
        public bool FrameworkManagesEnabledState { get; }

        public Gk2ModMetadata(string id, string name, string author, string version, string description,
            bool supportsRuntimeToggle = false, bool requiresKnownBuild = false)
            : this(id, name, author, version, description, supportsRuntimeToggle, requiresKnownBuild, true)
        {
        }

        public Gk2ModMetadata(string id, string name, string author, string version, string description,
            bool supportsRuntimeToggle, bool requiresKnownBuild, bool frameworkManagesEnabledState)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Mod id is required.", nameof(id));
            Id = id.Trim().ToLowerInvariant();
            Name = string.IsNullOrWhiteSpace(name) ? Id : name.Trim();
            Author = string.IsNullOrWhiteSpace(author) ? "Unknown" : author.Trim();
            Version = Version.Parse(version);
            Description = description ?? string.Empty;
            SupportsRuntimeToggle = supportsRuntimeToggle;
            RequiresKnownBuild = requiresKnownBuild;
            FrameworkManagesEnabledState = frameworkManagesEnabledState;
        }
    }

    public sealed class Gk2ModDependency
    {
        public string Id { get; }
        public Version MinimumVersion { get; }
        public Version MaximumVersionExclusive { get; }
        public bool Optional { get; }

        public Gk2ModDependency(string id, string minimumVersion = null, string maximumVersionExclusive = null, bool optional = false)
        {
            Id = (id ?? throw new ArgumentNullException(nameof(id))).Trim().ToLowerInvariant();
            MinimumVersion = string.IsNullOrWhiteSpace(minimumVersion) ? null : Version.Parse(minimumVersion);
            MaximumVersionExclusive = string.IsNullOrWhiteSpace(maximumVersionExclusive) ? null : Version.Parse(maximumVersionExclusive);
            Optional = optional;
        }
    }

    public interface IGk2Mod
    {
        Gk2ModMetadata Metadata { get; }
        IReadOnlyList<Gk2ModDependency> Dependencies { get; }
        void OnRegister(Gk2ModContext context);
        void OnEnable();
        void OnDisable();
        void OnGameStarted();
        void OnReturnedToMainMenu();
    }

    public abstract class Gk2ModBase : IGk2Mod
    {
        private static readonly IReadOnlyList<Gk2ModDependency> NoDependencies = Array.Empty<Gk2ModDependency>();
        public abstract Gk2ModMetadata Metadata { get; }
        public virtual IReadOnlyList<Gk2ModDependency> Dependencies => NoDependencies;
        public virtual void OnRegister(Gk2ModContext context) { }
        public virtual void OnEnable() { }
        public virtual void OnDisable() { }
        public virtual void OnGameStarted() { }
        public virtual void OnReturnedToMainMenu() { }
    }

    public sealed class Gk2ModContext
    {
        private readonly Action<string> confirmCurrentBuildCompatibility;

        public Gk2Settings Settings { get; }
        public Gk2ModLogger Log { get; }
        public BuildFingerprint Build { get; }

        internal Gk2ModContext(
            Gk2Settings settings,
            Gk2ModLogger log,
            BuildFingerprint build,
            Action<string> confirmCurrentBuildCompatibility = null)
        {
            Settings = settings;
            Log = log;
            Build = build;
            this.confirmCurrentBuildCompatibility = confirmCurrentBuildCompatibility;
        }

        public void ConfirmCurrentBuildCompatibility(string detail = null)
        {
            if (confirmCurrentBuildCompatibility == null)
                throw new InvalidOperationException(
                    "Build compatibility confirmation is unavailable in this Framework context.");

            confirmCurrentBuildCompatibility(detail);
        }
    }

    public enum ModCompatibilityStatus
    {
        Compatible,
        UnknownBuild,
        MissingDependency,
        VersionMismatch,
        Disabled, // Legacy value kept for API compatibility; framework no longer emits it.
        Faulted,
        DependencyUnavailable,
        DependencyCycle
    }
}
