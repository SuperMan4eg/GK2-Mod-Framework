using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using UnityEngine;

namespace GK2.Framework
{
    // These registrations expose configuration only; they never own the plugin lifecycle.
    internal sealed class ImportedSettingsMod : Gk2ModBase, IDisposable
    {
        internal readonly string SourceId;
        internal readonly ConfigFile Config;
        internal readonly ConfigEntryBase[] Entries;
        private readonly List<ImportedSetting> adapters = new List<ImportedSetting>();
        private readonly Gk2ModMetadata metadata;
        public override Gk2ModMetadata Metadata => metadata;
        internal ImportedSettingsMod(string id, string name, string version, ConfigFile config, ConfigEntryBase[] entries)
        {
            SourceId = id; Config = config; Entries = entries;
            metadata = new Gk2ModMetadata("ru.superman4eg.gk2.imported." + id, name, "Unknown", version,
                string.Empty, false, false, false);
        }
        public override void OnRegister(Gk2ModContext context)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in Entries)
            {
                if (ImportedSetting.Hidden(entry)) continue;
                if (!keys.Add(entry.Definition.Section + "." + entry.Definition.Key))
                {
                    FrameworkLog.Source?.LogWarning("GK2_CONFIG_IMPORT_KEY_COLLISION: " + SourceId);
                    continue;
                }
                var adapter = new ImportedSetting(entry);
                adapters.Add(adapter);
                context.Settings.AddImported(adapter);
            }
        }
        public void Dispose() { foreach (var adapter in adapters) adapter.Dispose(); adapters.Clear(); }
    }

    internal static class ImportedSettings
    {
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<string> ExcludedIds;
        private static bool syncing;
        internal static bool SameConfig(ConfigFile a, ConfigFile b) => ReferenceEquals(a, b)
            || (a != null && b != null && string.Equals(a.ConfigFilePath, b.ConfigFilePath, StringComparison.OrdinalIgnoreCase));
        internal static void Refresh()
        {
            if (syncing || FrameworkApi.Registry == null) return;
            syncing = true;
            try
            {
                var registry = FrameworkApi.Registry;
                var excluded = new HashSet<string>((ExcludedIds?.Value ?? string.Empty)
                    .Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()), StringComparer.OrdinalIgnoreCase);
                var desired = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (Enabled?.Value == true)
                foreach (var info in Chainloader.PluginInfos.Values.ToArray())
                {
                    if (info.Instance == null || info.Metadata.GUID == FrameworkPlugin.PluginGuid || excluded.Contains(info.Metadata.GUID)) continue;
                    try
                    {
                        var config = info.Instance.Config;
                        if (registry.Mods.Any(m => !(m.Instance is ImportedSettingsMod)
                            && (string.Equals(m.Metadata.Id, info.Metadata.GUID, StringComparison.OrdinalIgnoreCase) || SameConfig(m.Settings.Config, config)))) continue;
                        var entries = config.Select(p => p.Value).OrderBy(e => e.Definition.Section, StringComparer.Ordinal)
                            .ThenBy(e => e.Definition.Key, StringComparer.Ordinal).ToArray();
                        if (!entries.Any(e => !ImportedSetting.Hidden(e))) continue;
                        var existing = registry.Mods.FirstOrDefault(m => m.Instance is ImportedSettingsMod old && SameConfig(old.Config, config));
                        if (existing?.Instance is ImportedSettingsMod previous && previous.Entries.SequenceEqual(entries))
                        { desired.Add(existing.Metadata.Id); continue; }
                        if (existing != null) registry.RemoveImported(existing);
                        var imported = registry.Register(new ImportedSettingsMod(info.Metadata.GUID, info.Metadata.Name,
                            info.Metadata.Version.ToString(), config, entries), config);
                        desired.Add(imported.Metadata.Id);
                    }
                    catch (Exception ex) { FrameworkLog.Source?.LogWarning("GK2_CONFIG_IMPORT_SKIPPED: " + info.Metadata.GUID + "; " + ex.GetType().Name); }
                }
                foreach (var record in registry.Mods.Where(m => m.Instance is ImportedSettingsMod && !desired.Contains(m.Metadata.Id)).ToArray())
                    registry.RemoveImported(record);
            }
            finally { syncing = false; }
        }
    }

    internal sealed class ImportedSetting : IGk2Setting, IDisposable
    {
        internal readonly ConfigEntryBase Entry;
        private readonly List<object> choices = new List<object>();
        public string UniqueKey => Section + "." + Key;
        public string Section => Entry.Definition.Section;
        public string Key => Entry.Definition.Key;
        public string DisplayName => Key;
        public string Description => Entry.Description.Description ?? string.Empty;
        public SettingKind Kind { get; }
        public Type ValueType => Entry.SettingType;
        public object DefaultValue => Entry.DefaultValue;
        public object Minimum { get; }
        public object Maximum { get; }
        public double Step => Kind == SettingKind.IntegerSlider ? 1 : .01;
        public IReadOnlyList<object> Choices => choices;
        public bool IsReadOnly => Kind == SettingKind.ReadOnly;
        public int Order => 0;
        public event Action<IGk2Setting> ValueChanged;
        public object Value
        {
            get { try { return IsReadOnly ? (object)Convert.ToString(Entry.BoxedValue, CultureInfo.CurrentCulture) : Entry.BoxedValue; } catch { return string.Empty; } }
            set
            {
                if (IsReadOnly) return;
                object parsed = value;
                if (value is string text && ValueType == typeof(int))
                {
                    if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number)) return;
                    parsed = number;
                }
                else if (value is string floatText && ValueType == typeof(float))
                {
                    if (!float.TryParse(floatText, NumberStyles.Float, CultureInfo.CurrentCulture, out float number)
                        && !float.TryParse(floatText, NumberStyles.Float, CultureInfo.InvariantCulture, out number)) return;
                    if (float.IsNaN(number) || float.IsInfinity(number)) return;
                    parsed = number;
                }
                if (parsed == null || !ValueType.IsInstanceOfType(parsed)) return;
                if (choices.Count > 0 && !choices.Contains(parsed)) return;
                Entry.BoxedValue = parsed; // Native validation, events and SaveOnConfigSet remain authoritative.
            }
        }
        internal ImportedSetting(ConfigEntryBase entry)
        {
            Entry = entry;
            Kind = SettingKind.ReadOnly;
            Type type = entry.SettingType;
            bool nativeType = type == typeof(bool) || type == typeof(int) || type == typeof(float)
                || type == typeof(string) || type == typeof(KeyboardShortcut) || (type.IsEnum && !type.IsDefined(typeof(FlagsAttribute), false));
            if (nativeType && !Flag(entry, "ReadOnly", true) && !Flag(entry, "IsReadOnly", true))
            {
                var acceptable = entry.Description.AcceptableValues;
                var acceptableType = acceptable?.GetType();
                if (acceptableType?.IsGenericType == true && acceptableType.GetGenericTypeDefinition() == typeof(AcceptableValueList<>))
                {
                    foreach (var item in (System.Collections.IEnumerable)acceptableType.GetProperty("AcceptableValues").GetValue(acceptable)) choices.Add(item);
                    if (choices.Count > 0 && choices.All(type.IsInstanceOfType)) Kind = SettingKind.Dropdown;
                }
                else if (acceptable is AcceptableValueRange<int> integers && integers.MinValue < integers.MaxValue)
                { Kind = SettingKind.IntegerSlider; Minimum = integers.MinValue; Maximum = integers.MaxValue; }
                else if (acceptable is AcceptableValueRange<float> floats && floats.MinValue < floats.MaxValue
                    && !float.IsInfinity(floats.MinValue) && !float.IsInfinity(floats.MaxValue)
                    && !float.IsNaN(floats.MinValue) && !float.IsNaN(floats.MaxValue))
                { Kind = SettingKind.FloatSlider; Minimum = floats.MinValue; Maximum = floats.MaxValue; }
                else if (acceptable == null)
                {
                    if (type == typeof(bool)) Kind = SettingKind.Toggle;
                    else if (type == typeof(KeyboardShortcut)) Kind = SettingKind.Keybind;
                    else if (type.IsEnum) { Kind = SettingKind.Dropdown; foreach (var item in Enum.GetValues(type)) choices.Add(item); }
                    else Kind = SettingKind.Text;
                }
            }
            Entry.ConfigFile.SettingChanged += OnChanged;
        }
        public void ResetToDefault() { if (!IsReadOnly) Entry.BoxedValue = Entry.DefaultValue; }
        private void OnChanged(object sender, SettingChangedEventArgs args)
        { if (ReferenceEquals(args.ChangedSetting, Entry)) ValueChanged?.Invoke(this); }
        public void Dispose() => Entry.ConfigFile.SettingChanged -= OnChanged;
        internal static bool Hidden(ConfigEntryBase entry) => Flag(entry, "Browsable", false)
            || Flag(entry, "IsPassword", true) || Flag(entry, "Password", true);
        private static bool Flag(ConfigEntryBase entry, string name, bool expected)
        {
            foreach (object tag in entry.Description.Tags ?? Array.Empty<object>())
            {
                if (tag == null) continue;
                var type = tag.GetType();
                object value = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(tag)
                    ?? type.GetField(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(tag);
                if (value is bool flag && flag == expected) return true;
            }
            return false;
        }
    }
}
