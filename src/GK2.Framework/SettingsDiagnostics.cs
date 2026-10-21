using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace GK2.Framework
{
    internal static class SettingsDiagnostics
    {
        internal static bool SameShortcut(KeyboardShortcut left, KeyboardShortcut right) => left.MainKey == right.MainKey
            && new HashSet<KeyCode>(left.Modifiers ?? Array.Empty<KeyCode>()).SetEquals(right.Modifiers ?? Array.Empty<KeyCode>());
        internal static bool IsChanged(IGk2Setting setting)
        {
            if (setting == null || setting.IsReadOnly || setting.Kind == SettingKind.ReadOnly || setting.Kind == SettingKind.Button) return false;
            object current = setting.Value, original = setting.DefaultValue;
            return current is KeyboardShortcut a && original is KeyboardShortcut b ? !SameShortcut(a, b) : !Equals(current, original);
        }
        internal static IReadOnlyList<RegisteredMod> Conflicts(RegisteredMod owner, IGk2Setting setting, IEnumerable<RegisteredMod> mods)
        {
            if (setting?.Kind != SettingKind.Keybind || !(setting.Value is KeyboardShortcut shortcut) || shortcut.MainKey == KeyCode.None)
                return Array.Empty<RegisteredMod>();
            return mods.Where(mod => !ReferenceEquals(mod, owner) && !string.Equals(mod.Metadata.Id, owner.Metadata.Id, StringComparison.OrdinalIgnoreCase)
                && mod.Settings.Items.Any(other => other.Kind == SettingKind.Keybind && other.Value is KeyboardShortcut value
                    && value.MainKey != KeyCode.None && SameShortcut(shortcut, value)))
                .OrderBy(mod => mod.Metadata.Id, StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }
}
