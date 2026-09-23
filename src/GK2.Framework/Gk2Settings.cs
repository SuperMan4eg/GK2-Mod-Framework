using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace GK2.Framework
{
    public enum SettingKind { Toggle, IntegerSlider, FloatSlider, Dropdown, Keybind, Text, ReadOnly }

    public interface IGk2Setting
    {
        string UniqueKey { get; }
        string Section { get; }
        string Key { get; }
        string DisplayName { get; }
        string Description { get; }
        SettingKind Kind { get; }
        Type ValueType { get; }
        object Value { get; set; }
        object DefaultValue { get; }
        object Minimum { get; }
        object Maximum { get; }
        double Step { get; }
        IReadOnlyList<object> Choices { get; }
        bool IsReadOnly { get; }
        int Order { get; }
        event Action<IGk2Setting> ValueChanged;
        void ResetToDefault();
    }

    internal sealed class ConfigSetting<T> : IGk2Setting
    {
        private readonly ConfigEntry<T> entry;
        private readonly List<object> choices = new List<object>();
        public string UniqueKey => Section + "." + Key;
        public string Section { get; }
        public string Key { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public SettingKind Kind { get; }
        public Type ValueType => typeof(T);
        public object DefaultValue => entry.DefaultValue;
        public object Minimum { get; }
        public object Maximum { get; }
        public double Step { get; }
        public IReadOnlyList<object> Choices => choices;
        public bool IsReadOnly => false;
        public int Order { get; }
        public event Action<IGk2Setting> ValueChanged;
        public object Value
        {
            get => entry.Value;
            set
            {
                if (value is T typed) entry.Value = typed;
                else entry.Value = (T)Convert.ChangeType(value, typeof(T));
            }
        }

        internal ConfigSetting(ConfigEntry<T> entry, string displayName, string description, SettingKind kind,
            object min = null, object max = null, double step = 1d, IEnumerable<T> values = null, int order = 0)
        {
            this.entry = entry;
            Section = entry.Definition.Section;
            Key = entry.Definition.Key;
            DisplayName = displayName;
            Description = description ?? string.Empty;
            Kind = kind;
            Minimum = min;
            Maximum = max;
            Step = step;
            Order = order;
            if (values != null) foreach (T value in values) choices.Add(value);
            entry.SettingChanged += (_, __) => ValueChanged?.Invoke(this);
        }

        public void ResetToDefault() => entry.Value = (T)entry.DefaultValue;
    }

    internal sealed class ReadOnlySetting : IGk2Setting
    {
        private readonly Func<string> getter;
        public string UniqueKey => Section + "." + Key;
        public string Section { get; }
        public string Key { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public SettingKind Kind => SettingKind.ReadOnly;
        public Type ValueType => typeof(string);
        public object Value { get => getter(); set { } }
        public object DefaultValue => getter();
        public object Minimum => null;
        public object Maximum => null;
        public double Step => 0d;
        public IReadOnlyList<object> Choices => Array.Empty<object>();
        public bool IsReadOnly => true;
        public int Order { get; }
        public event Action<IGk2Setting> ValueChanged { add { } remove { } }
        internal ReadOnlySetting(string section, string key, string displayName, string description, Func<string> getter, int order)
        { Section = section; Key = key; DisplayName = displayName; Description = description ?? string.Empty; this.getter = getter; Order = order; }
        public void ResetToDefault() { }
    }

    public sealed class Gk2Settings
    {
        private readonly ConfigFile config;
        private readonly List<IGk2Setting> settings = new List<IGk2Setting>();
        public IReadOnlyList<IGk2Setting> Items => settings;
        internal Gk2Settings(ConfigFile config) { this.config = config; }

        public ConfigEntry<bool> AddToggle(string section, string key, bool value, string name, string description, int order = 0)
        { var e = config.Bind(section, key, value, description); settings.Add(new ConfigSetting<bool>(e, name, description, SettingKind.Toggle, order: order)); return e; }

        public ConfigEntry<int> AddIntSlider(string section, string key, int value, int min, int max, string name, string description, int step = 1, int order = 0)
        { var e = config.Bind(section, key, value, new ConfigDescription(description, new AcceptableValueRange<int>(min, max))); settings.Add(new ConfigSetting<int>(e, name, description, SettingKind.IntegerSlider, min, max, Math.Max(1, step), order: order)); return e; }

        public ConfigEntry<float> AddFloatSlider(string section, string key, float value, float min, float max, string name, string description, float step = 0.01f, int order = 0)
        { var e = config.Bind(section, key, value, new ConfigDescription(description, new AcceptableValueRange<float>(min, max))); settings.Add(new ConfigSetting<float>(e, name, description, SettingKind.FloatSlider, min, max, Math.Max(0.000001f, step), order: order)); return e; }

        public ConfigEntry<string> AddDropdown(string section, string key, string value, string[] choices, string name, string description, int order = 0)
        { var e = config.Bind(section, key, value, new ConfigDescription(description, new AcceptableValueList<string>(choices))); settings.Add(new ConfigSetting<string>(e, name, description, SettingKind.Dropdown, values: choices, order: order)); return e; }

        public ConfigEntry<T> AddDropdown<T>(string section, string key, T value, T[] choices, string name, string description, int order = 0)
        { var e = config.Bind(section, key, value, description); settings.Add(new ConfigSetting<T>(e, name, description, SettingKind.Dropdown, values: choices, order: order)); return e; }

        public ConfigEntry<T> AddEnum<T>(string section, string key, T value, string name, string description, int order = 0) where T : struct, Enum
        { T[] choices = (T[])Enum.GetValues(typeof(T)); return AddDropdown(section, key, value, choices, name, description, order); }

        public ConfigEntry<KeyboardShortcut> AddKeybind(string section, string key, KeyboardShortcut value, string name, string description, int order = 0)
        { var e = config.Bind(section, key, value, description); settings.Add(new ConfigSetting<KeyboardShortcut>(e, name, description, SettingKind.Keybind, order: order)); return e; }

        public ConfigEntry<string> AddText(string section, string key, string value, string name, string description, int order = 0)
        { var e = config.Bind(section, key, value, description); settings.Add(new ConfigSetting<string>(e, name, description, SettingKind.Text, order: order)); return e; }

        public void AddReadOnly(string section, string key, string name, string description, Func<string> getter, int order = 0)
        { settings.Add(new ReadOnlySetting(section, key, name, description, getter, order)); }
    }
}
