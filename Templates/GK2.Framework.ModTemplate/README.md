# GK2 Framework Mod Template

A buildable starting point for a GK2 Mod Framework consumer mod.

## Requirements

- BepInEx 5 installed for Graveyard Keeper 2
- `GK2.Framework.dll` installed in `BepInEx/plugins`
- A compatible .NET SDK

## Before building

Copy this folder into your development workspace, then rename:

- The folder and project file
- `AssemblyName` and `RootNamespace`
- The namespace and plugin classes
- `PluginGuid`, `PluginName`, author, version, and description

The plugin ID must be unique and stable.

Build with:

```powershell
dotnet build .\YourMod.csproj -c Release
```

To use a framework DLL outside the game directory:

```powershell
dotnet build .\YourMod.csproj -c Release -p:FrameworkDll="D:\Path\GK2.Framework.dll"
```

See `docs/NEW_MOD_GUIDE.md` in the framework package for lifecycle, settings, dependency, packaging, and testing guidance.

The template intentionally contains no game patches. Each mod must select compatible patch points and completely reverse runtime changes in `OnDisable` before declaring `SupportsRuntimeToggle=true`.
