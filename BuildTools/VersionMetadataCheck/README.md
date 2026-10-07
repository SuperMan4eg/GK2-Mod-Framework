# Framework version metadata gate
Built by build-release.ps1 using .NET SDK10. Reads PE metadata without executing Framework or resolving game dependencies. Arguments: DLL-or-release-ZIP, expected-version. Exit0 PASS; exit1 FAIL.

Checks BepInPlugin identity/version, FrameworkPlugin.PluginVersion constant, assembly/file/product versions. Exact ZIP checked after packaging. Runtime self-registration checked separately by internal exact-artifact probe.

Negative regression: run built VersionMetadataCheck.dll against frozen0.1.21 ZIP with expected0.1.21; it must fail because BepInEx reports0.1.20.
