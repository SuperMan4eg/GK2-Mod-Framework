using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace GK2.Framework
{
    public enum SettingKind { Toggle, IntegerSlider, FloatSlider, Dropdown, Keybind, Text, ReadOnly, Button }

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

    internal sealed class ButtonSetting : IGk2Setting
    {
        private readonly Func<string> label;
        internal Action Click { get; }

        public string UniqueKey => Section + "." + Key;
        public string Section { get; }
        public string Key { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public SettingKind Kind => SettingKind.Button;
        public Type ValueType => typeof(string);
        public object Value
        {
            get
            {
                try
                {
                    return label?.Invoke() ?? string.Empty;
                }
                catch (Exception ex)
                {
                    FrameworkLog.Source?.LogWarning(
                        "GK2_SETTING_BUTTON_LABEL_FAILED: "
                        + UniqueKey + "; " + ex);
                    return string.Empty;
                }
            }
            set { }
        }
        public object DefaultValue => Value;
        public object Minimum => null;
        public object Maximum => null;
        public double Step => 0d;
        public IReadOnlyList<object> Choices => Array.Empty<object>();
        public bool IsReadOnly => true;
        public int Order { get; }
        public event Action<IGk2Setting> ValueChanged { add { } remove { } }

        internal ButtonSetting(
            string section,
            string key,
            string displayName,
            string description,
            Func<string> label,
            Action click,
            int order)
        {
            Section = section;
            Key = key;
            DisplayName = displayName;
            Description = description ?? string.Empty;
            this.label = label;
            Click = click;
            Order = order;
        }

        public void ResetToDefault() { }
    }

    internal readonly struct SettingPresentationState
    {
        internal bool Visible { get; }
        internal bool Enabled { get; }

        internal SettingPresentationState(bool visible, bool enabled)
        {
            Visible = visible;
            Enabled = enabled;
        }

        public override bool Equals(object obj) =>
            obj is SettingPresentationState other
            && Visible == other.Visible
            && Enabled == other.Enabled;

        public override int GetHashCode() =>
            (Visible ? 1 : 0) | (Enabled ? 2 : 0);
    }

    internal sealed class SettingPresentationCondition
    {
        internal Func<bool> Visibility;
        internal Func<bool> Enabled;
    }

    public sealed class Gk2Settings
    {
        private readonly ConfigFile config;
        private readonly List<IGk2Setting> settings = new List<IGk2Setting>();
        private readonly Dictionary<string, SettingPresentationCondition> conditions =
            new Dictionary<string, SettingPresentationCondition>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, SettingPresentationState> presentation =
            new Dictionary<string, SettingPresentationState>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> conditionWarnings =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<IGk2Setting> Items => settings;

        internal event Action PresentationChanged;

        internal Gk2Settings(ConfigFile config) { this.config = config; }

        public void SetVisibilityCondition(
            string section,
            string key,
            Func<bool> condition)
        {
            SetCondition(section, key, condition, visibility: true);
        }

        public void SetEnabledCondition(
            string section,
            string key,
            Func<bool> condition)
        {
            SetCondition(section, key, condition, visibility: false);
        }

        public void RefreshConditions()
        {
            RefreshPresentation(raiseEvent: true);
        }

        internal SettingPresentationState GetPresentation(
            IGk2Setting setting)
        {
            if (setting == null)
                return new SettingPresentationState(true, true);

            if (presentation.TryGetValue(
                setting.UniqueKey,
                out SettingPresentationState state))
            {
                return state;
            }

            state = EvaluatePresentation(setting);
            presentation[setting.UniqueKey] = state;
            return state;
        }

        public ConfigEntry<bool> AddToggle(string section, string key, bool value, string name, string description, int order = 0)
        { var e = config.Bind(section, key, value, description); RegisterSetting(new ConfigSetting<bool>(e, name, description, SettingKind.Toggle, order: order)); return e; }

        public ConfigEntry<int> AddIntSlider(string section, string key, int value, int min, int max, string name, string description, int step = 1, int order = 0)
        { var e = config.Bind(section, key, value, new ConfigDescription(description, new AcceptableValueRange<int>(min, max))); RegisterSetting(new ConfigSetting<int>(e, name, description, SettingKind.IntegerSlider, min, max, Math.Max(1, step), order: order)); return e; }

        public ConfigEntry<float> AddFloatSlider(string section, string key, float value, float min, float max, string name, string description, float step = 0.01f, int order = 0)
        { var e = config.Bind(section, key, value, new ConfigDescription(description, new AcceptableValueRange<float>(min, max))); RegisterSetting(new ConfigSetting<float>(e, name, description, SettingKind.FloatSlider, min, max, Math.Max(0.000001f, step), order: order)); return e; }

        public ConfigEntry<string> AddDropdown(string section, string key, string value, string[] choices, string name, string description, int order = 0)
        { var e = config.Bind(section, key, value, new ConfigDescription(description, new AcceptableValueList<string>(choices))); RegisterSetting(new ConfigSetting<string>(e, name, description, SettingKind.Dropdown, values: choices, order: order)); return e; }

        public ConfigEntry<T> AddDropdown<T>(string section, string key, T value, T[] choices, string name, string description, int order = 0)
        { var e = config.Bind(section, key, value, description); RegisterSetting(new ConfigSetting<T>(e, name, description, SettingKind.Dropdown, values: choices, order: order)); return e; }

        public ConfigEntry<T> AddEnum<T>(string section, string key, T value, string name, string description, int order = 0) where T : struct, Enum
        { T[] choices = (T[])Enum.GetValues(typeof(T)); return AddDropdown(section, key, value, choices, name, description, order); }

        public ConfigEntry<KeyboardShortcut> AddKeybind(string section, string key, KeyboardShortcut value, string name, string description, int order = 0)
        { var e = config.Bind(section, key, value, description); RegisterSetting(new ConfigSetting<KeyboardShortcut>(e, name, description, SettingKind.Keybind, order: order)); return e; }

        public ConfigEntry<string> AddText(string section, string key, string value, string name, string description, int order = 0)
        { var e = config.Bind(section, key, value, description); RegisterSetting(new ConfigSetting<string>(e, name, description, SettingKind.Text, order: order)); return e; }

        public void AddButton(
            string section,
            string key,
            string name,
            string description,
            Func<string> label,
            Action onClick,
            int order = 0)
        {
            if (label == null)
                throw new ArgumentNullException(nameof(label));
            if (onClick == null)
                throw new ArgumentNullException(nameof(onClick));

            RegisterSetting(
                new ButtonSetting(
                    section,
                    key,
                    name,
                    description,
                    label,
                    onClick,
                    order));
        }

        public void AddReadOnly(string section, string key, string name, string description, Func<string> getter, int order = 0)
        { RegisterSetting(new ReadOnlySetting(section, key, name, description, getter, order)); }

        private void RegisterSetting(IGk2Setting setting)
        {
            settings.Add(setting);
            presentation[setting.UniqueKey] =
                new SettingPresentationState(true, true);
            setting.ValueChanged += OnSettingValueChanged;
        }

        private void OnSettingValueChanged(IGk2Setting _)
        {
            RefreshPresentation(raiseEvent: true);
        }

        private void SetCondition(
            string section,
            string key,
            Func<bool> condition,
            bool visibility)
        {
            string uniqueKey = BuildUniqueKey(section, key);
            if (!ContainsSetting(uniqueKey))
            {
                throw new ArgumentException(
                    "Setting is not registered: " + uniqueKey);
            }

            if (!conditions.TryGetValue(
                uniqueKey,
                out SettingPresentationCondition state))
            {
                state = new SettingPresentationCondition();
                conditions.Add(uniqueKey, state);
            }

            if (visibility)
                state.Visibility = condition;
            else
                state.Enabled = condition;

            if (state.Visibility == null && state.Enabled == null)
                conditions.Remove(uniqueKey);

            conditionWarnings.Remove(
                uniqueKey + (visibility ? "|visible" : "|enabled"));
            RefreshPresentation(raiseEvent: true);
        }

        private bool ContainsSetting(string uniqueKey)
        {
            for (int i = 0; i < settings.Count; i++)
            {
                if (string.Equals(
                    settings[i].UniqueKey,
                    uniqueKey,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void RefreshPresentation(bool raiseEvent)
        {
            bool changed = false;

            for (int i = 0; i < settings.Count; i++)
            {
                IGk2Setting setting = settings[i];
                SettingPresentationState next =
                    EvaluatePresentation(setting);

                if (!presentation.TryGetValue(
                    setting.UniqueKey,
                    out SettingPresentationState previous)
                    || !previous.Equals(next))
                {
                    presentation[setting.UniqueKey] = next;
                    changed = true;
                }
            }

            if (changed && raiseEvent)
                PresentationChanged?.Invoke();
        }

        private SettingPresentationState EvaluatePresentation(
            IGk2Setting setting)
        {
            if (setting == null
                || !conditions.TryGetValue(
                    setting.UniqueKey,
                    out SettingPresentationCondition condition))
            {
                return new SettingPresentationState(true, true);
            }

            bool visible = EvaluatePredicate(
                setting.UniqueKey,
                "visible",
                condition.Visibility);
            bool enabled = EvaluatePredicate(
                setting.UniqueKey,
                "enabled",
                condition.Enabled);

            return new SettingPresentationState(
                visible,
                enabled);
        }

        private bool EvaluatePredicate(
            string uniqueKey,
            string kind,
            Func<bool> predicate)
        {
            if (predicate == null)
                return true;

            try
            {
                return predicate();
            }
            catch (Exception ex)
            {
                string warningKey = uniqueKey + "|" + kind;
                if (conditionWarnings.Add(warningKey))
                {
                    FrameworkLog.Source?.LogWarning(
                        "GK2_SETTING_CONDITION_FAILED: "
                        + uniqueKey + "; kind=" + kind
                        + "; fail_open=true; " + ex);
                }

                return true;
            }
        }

        private static string BuildUniqueKey(
            string section,
            string key) =>
            (section ?? string.Empty) + "." + (key ?? string.Empty);
    }
}
