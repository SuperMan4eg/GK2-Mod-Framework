# Optional integration with GK2 Mod Framework

Use this pattern when your mod must remain fully functional without GK2 Mod Framework, but you want to add Mods-menu metadata/settings integration when the framework is installed.

## Architecture

Split the mod into two assemblies:

- **Main mod** — normal BepInEx plugin, owns gameplay logic and config, and does not reference `GK2.Framework.dll`.
- **Framework bridge** — small optional plugin that references both the main mod and `GK2.Framework.dll`.

The bridge declares hard BepInEx dependencies on the main mod and the framework. If the framework is absent, BepInEx skips only the bridge; the main mod still loads normally.

This is different from changing a direct framework dependency from hard to soft. If the main assembly directly references framework types, the CLR can still require `GK2.Framework.dll` when loading that assembly.

## Main plugin

The standalone plugin keeps its normal config and gameplay behavior:

```csharp
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class MainPlugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.yourname.gk2.mymod";
    public const string PluginName = "My Mod";
    public const string PluginVersion = "1.0.0";

    public static MainPlugin Instance { get; private set; }
    public ConfigEntry<bool> FeatureEnabled { get; private set; }

    private void Awake()
    {
        Instance = this;
        FeatureEnabled = Config.Bind(
            "General", "FeatureEnabled", true,
            "Enable the feature.");

        // Normal standalone mod initialization.
    }
}
```

Do not add a BepInEx dependency on GK2 Mod Framework to this project.

## Framework bridge

The optional bridge depends on both plugins:

```csharp
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(MainPlugin.PluginGuid, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(FrameworkPlugin.PluginGuid, BepInDependency.DependencyFlags.HardDependency)]
public sealed class FrameworkBridgePlugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.yourname.gk2.mymod.framework";
    public const string PluginName = "My Mod - GK2 Framework Integration";
    public const string PluginVersion = "1.0.0";

    private void Awake()
    {
        MainPlugin main = MainPlugin.Instance;
        if (main == null)
        {
            Logger.LogError("Main mod instance is unavailable.");
            return;
        }

        FrameworkApi.RegisterMod(new FrameworkBridge(main), main.Config);
    }
}
```

Using the main mod's `ConfigFile` is intentional. If the bridge registers the same Section/Key/type through `Gk2Settings`, BepInEx returns the existing `ConfigEntry<T>`, so the Mods menu edits the same value used by the standalone mod.

Example:

```csharp
public override void OnRegister(Gk2ModContext context)
{
    context.Settings.AddToggle(
        "General",
        "FeatureEnabled",
        true,
        "Feature enabled",
        "Enable the feature.");
}
```

## Packaging choices

Two supported distribution models:

1. Ship the main mod as the normal download and provide the bridge as an optional file for users who also install GK2 Mod Framework.
2. Ship both DLLs together. When GK2 Mod Framework is absent, BepInEx skips the bridge and still loads the standalone main mod.

The second model was runtime-tested with BepInEx 5.4.23.5.

## Enabled-state behavior

For a pure integration bridge, construct `Gk2ModMetadata` with `frameworkManagesEnabledState: false`. The framework will still register and run the bridge lifecycle, but it will not create its own `Framework.Enabled` config entry and will not show an Enable/Disable control for that mod in the Mods menu.

```csharp
new Gk2ModMetadata(
    MainPlugin.PluginGuid,
    MainPlugin.PluginName,
    "YourName",
    MainPlugin.PluginVersion,
    "Optional GK2 Mod Framework integration.",
    supportsRuntimeToggle: false,
    requiresKnownBuild: false,
    frameworkManagesEnabledState: false);
```

This keeps the standalone mod's enabled state under the standalone mod's control and avoids presenting the bridge as if it were the gameplay module.

If you want the Framework Enable/Disable control to manage the standalone gameplay module, keep framework-managed enabled state and explicitly connect bridge lifecycle callbacks to an enable/disable or restart-state API exposed by your main mod.

## When to use hard dependency instead

Use the normal hard-dependency template when your mod is built directly around framework types/lifecycle/settings and has no reason to run without GK2 Mod Framework.

Use optional integration when the standalone mod is the primary product and framework integration is an enhancement.
