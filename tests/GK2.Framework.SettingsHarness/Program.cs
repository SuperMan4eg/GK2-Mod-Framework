using System.Reflection;
using BepInEx.Configuration;
using GK2.Framework;
using UnityEngine;

internal enum SampleMode { Off, Gentle, Fast }

internal static class Program
{
    private static int Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "gk2-framework-settings-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            Run(Path.Combine(root, "bridge.cfg"));
            Console.WriteLine("Settings harness passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static void Run(string configPath)
    {
        var config = new ConfigFile(configPath, true);
        Gk2Settings settings = CreateSettings(config);

        ConfigEntry<bool> enabled = config.Bind("General", "Enabled", true, "Enable feature.");
        ConfigEntry<int> count = config.Bind("General", "Count", 5,
            new ConfigDescription("Count.", new AcceptableValueRange<int>(1, 10)));
        ConfigEntry<float> scale = config.Bind("General", "Scale", 1.5f,
            new ConfigDescription("Scale.", new AcceptableValueRange<float>(0.5f, 2f)));
        ConfigEntry<string> mode = config.Bind("General", "Mode", "Safe",
            new ConfigDescription("Mode.", new AcceptableValueList<string>("Safe", "Fast")));
        ConfigEntry<SampleMode> enumMode = config.Bind("General", "EnumMode", SampleMode.Gentle, "Mode enum.");
        ConfigEntry<KeyboardShortcut> shortcut = config.Bind("General", "Shortcut", new KeyboardShortcut(KeyCode.T), "Shortcut.");
        ConfigEntry<string> note = config.Bind("General", "Note", "Hello", "Note.");

        Assert(ReferenceEquals(enabled, settings.AddToggle(enabled)), "Toggle must retain the original entry.");
        Assert(ReferenceEquals(count, settings.AddIntSlider(count, 1, 10)), "Int slider must retain the original entry.");
        Assert(ReferenceEquals(scale, settings.AddFloatSlider(scale, 0.5f, 2f)), "Float slider must retain the original entry.");
        Assert(ReferenceEquals(mode, settings.AddDropdown(mode, new[] { "Safe", "Fast" })), "Dropdown must retain the original entry.");
        Assert(ReferenceEquals(enumMode, settings.AddEnum(enumMode)), "Enum must retain the original entry.");
        Assert(ReferenceEquals(shortcut, settings.AddKeybind(shortcut)), "Keybind must retain the original entry.");
        Assert(ReferenceEquals(note, settings.AddText(note)), "Text must retain the original entry.");
        Assert(settings.Items.Count == 7, "All adopted entries must be registered.");

        IGk2Setting enabledSetting = settings.Items[0];
        int changed = 0;
        enabledSetting.ValueChanged += _ => changed++;
        enabledSetting.Value = false;
        Assert(!enabled.Value, "Framework edits must update the original entry.");
        enabled.Value = true;
        Assert(changed >= 2, "Original entry changes must propagate back to Framework.");
        enabled.Value = false;
        enabledSetting.ResetToDefault();
        Assert(enabled.Value, "Reset must use the original entry default.");

        settings.Items[1].Value = 100;
        Assert(count.Value == 10, "Adoption must preserve acceptable-value clamping.");
        Assert(settings.Items[0].DisplayName == "Enabled" && settings.Items[0].Description == "Enable feature.",
            "Entry key and description must supply default display metadata.");
        note.Value = "Persisted through original file";
        config.Save();
        var reloaded = new ConfigFile(configPath, false);
        Assert(reloaded.Bind("General", "Count", 5).Value == 10, "Original file must persist adopted values.");
        Assert(reloaded.Bind("General", "Note", "Hello").Value == note.Value, "Original text must survive reload.");
        AssertThrows<ArgumentNullException>(() => settings.AddToggle((ConfigEntry<bool>)null!), "Null adoption must fail.");

        AssertThrows<InvalidOperationException>(() => settings.AddToggle(enabled), "Duplicate adoption must fail.");
        var otherConfig = new ConfigFile(Path.Combine(Path.GetDirectoryName(configPath)!, "other.cfg"), true);
        ConfigEntry<bool> foreign = otherConfig.Bind("General", "Foreign", true, "Foreign.");
        AssertThrows<ArgumentException>(() => settings.AddToggle(foreign), "Foreign ConfigFile entries must fail.");

        Assert(new Gk2ModMetadata("example.a", "A", "Author", "1.2.3-beta.4", "").Version == new Version(1, 2, 3), "Prerelease plugin versions must parse.");
        Assert(new Gk2ModMetadata("example.b", "B", "Author", "1.2.3+build.7", "").Version == new Version(1, 2, 3), "Build metadata versions must parse.");
        Assert(new Gk2ModDependency("example.a", "1.2.3-preview.1").MinimumVersion == new Version(1, 2, 3), "Prerelease dependency bounds must parse.");
        AssertThrows<ArgumentException>(() => new Gk2ModMetadata("example.c", "C", "Author", "beta", ""), "Invalid versions must still fail.");
        foreach (string invalid in new[] { "1.2.3-", "1.2.3+", "1.2.3-beta..4", "1.2.3-beta 4", "1.2.3+build+other" })
            AssertThrows<ArgumentException>(() => new Gk2ModMetadata("example.c", "C", "Author", invalid, ""),
                "Malformed version suffix must fail: " + invalid);
        Assert(new Gk2ModMetadata("example.d", "D", "Author", "1.2.3-beta.4+build.7", "").Version == new Version(1, 2, 3),
            "Combined valid suffixes must parse.");
    }

    private static Gk2Settings CreateSettings(ConfigFile config)
    {
        ConstructorInfo? constructor = typeof(Gk2Settings).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            new[] { typeof(ConfigFile) },
            modifiers: null);
        if (constructor == null) throw new InvalidOperationException("Gk2Settings internal constructor was not found.");
        return (Gk2Settings)constructor.Invoke(new object[] { config });
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void AssertThrows<T>(Action action, string message) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException(message);
    }
}
