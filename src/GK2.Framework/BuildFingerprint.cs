using System;
using System.IO;
using System.Security.Cryptography;
using BepInEx;
using UnityEngine;

namespace GK2.Framework
{
    public enum BuildCompatibilityStatus { Compatible, Unknown, Incompatible }

    public sealed class BuildFingerprint
    {
        public const string KnownDemoAssemblySha256 = "03E02DCDB1CE95B52B698250F99AC59F886521800DB68790F4AC6388389670BF";
        public const string KnownReleaseAssemblySha256 = "185A742AB88B13E0CA9AB26EE820DDEFCBA05FAFD76A6027939F889DE65CCD50";
        public const string KnownReleaseUpdate1AssemblySha256 = "3180591FFB3BB8B076D1BB9B1EB5C27383C8EC6B32385C4EAB57F31DB00DA0FB";
        public string UnityVersion { get; }
        public string AssemblyCSharpPath { get; }
        public string AssemblyCSharpSha256 { get; }
        public BuildCompatibilityStatus Status { get; }

        private BuildFingerprint(string unityVersion, string path, string sha256, BuildCompatibilityStatus status)
        {
            UnityVersion = unityVersion;
            AssemblyCSharpPath = path;
            AssemblyCSharpSha256 = sha256;
            Status = status;
        }

        internal static BuildFingerprint Capture()
        {
            string path = Path.Combine(Paths.ManagedPath, "Assembly-CSharp.dll");
            if (!File.Exists(path)) return new BuildFingerprint(Application.unityVersion, path, string.Empty, BuildCompatibilityStatus.Incompatible);
            string hash;
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            bool knownDemo = string.Equals(hash, KnownDemoAssemblySha256, StringComparison.OrdinalIgnoreCase);
            bool knownRelease = string.Equals(hash, KnownReleaseAssemblySha256, StringComparison.OrdinalIgnoreCase);
            bool knownReleaseUpdate1 = string.Equals(hash, KnownReleaseUpdate1AssemblySha256, StringComparison.OrdinalIgnoreCase);
            BuildCompatibilityStatus status = knownDemo || knownRelease || knownReleaseUpdate1
                ? BuildCompatibilityStatus.Compatible : BuildCompatibilityStatus.Unknown;
            return new BuildFingerprint(Application.unityVersion, path, hash, status);
        }

        public override string ToString() => $"Unity {UnityVersion}; Assembly-CSharp {AssemblyCSharpSha256}; {Status}";
    }
}
