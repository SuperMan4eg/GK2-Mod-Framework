using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace GK2.Framework
{
    internal enum AppearanceRole { Background, Text, Accent }

    // Tracks only graphics created by Framework; game UI and semantic warning colors are untouched.
    internal sealed class AppearanceTarget : MonoBehaviour
    {
        internal Graphic Graphic;
        internal AppearanceRole Role;
        internal Color NativeColor;
        private void OnDestroy() => FrameworkAppearance.Remove(this);
    }

    internal static class FrameworkAppearance
    {
        private static readonly List<AppearanceTarget> targets = new List<AppearanceTarget>();
        internal static ConfigEntry<string> Preset, Background, Text, Accent;
        internal static string Validation { get; private set; } = string.Empty;
        private static bool resetting;
        private static string L(string key, string fallback) => FrameworkUi.L("appearance." + key, fallback);

        internal static void Register(Gk2Settings settings)
        {
            Preset = settings.AddDropdown("Appearance", "Preset", "Game",
                new[] { "Game", "Slate", "Parchment", "High contrast", "Custom" },
                L("preset", "Color theme"), L("preset_description", "Game restores the original style. Custom uses the three color fields below. Changes apply immediately."), -90);
            Background = settings.AddText("Appearance", "Background", "#20232D", L("background", "Custom background"),
                L("color_description", "Custom theme only. Enter an opaque color as #RRGGBB, for example #20232D. Invalid or unreadable combinations keep the game colors."), -89);
            Text = settings.AddText("Appearance", "Text", "#DDD6CF", L("text", "Custom text"), L("color_description", "Custom theme only. Enter an opaque color as #RRGGBB, for example #20232D. Invalid or unreadable combinations keep the game colors."), -88);
            Accent = settings.AddText("Appearance", "Accent", "#FFBD00", L("accent", "Custom accent"), L("color_description", "Custom theme only. Enter an opaque color as #RRGGBB, for example #20232D. Invalid or unreadable combinations keep the game colors."), -87);
            foreach (var entry in new[] { Preset, Background, Text, Accent }) entry.SettingChanged += Changed;
            settings.AddReadOnly("Appearance", "Validation", L("status", "Theme status"), L("status_description", "Warnings and errors keep their own colors."),
                () => string.IsNullOrEmpty(Validation) ? L("valid", "Ready") : Validation, -86);
            settings.AddButton("Appearance", "Reset", L("reset", "Restore game colors"), L("reset_description", "Reset the theme and all custom color fields to their defaults."),
                () => L("reset", "Restore game colors"), Reset, -85);
            Refresh();
        }

        internal static void Reset()
        {
            resetting = true;
            try { Preset.Value = "Game"; Background.Value = "#20232D"; Text.Value = "#DDD6CF"; Accent.Value = "#FFBD00"; }
            finally { resetting = false; Refresh(); }
        }
        private static void Changed(object sender, EventArgs args) { if (!resetting) Refresh(); }
        internal static bool IsPanel(string name) => name == "NativeInnerBackground" || name == "Panel" || name == "ModList" || name == "Details" || name == "SettingsPage" || name == "SettingsContent";
        internal static void Bind(Graphic graphic, AppearanceRole role)
        {
            if (graphic == null) return;
            AppearanceTarget target = graphic.GetComponent<AppearanceTarget>();
            if (target == null) { target = graphic.gameObject.AddComponent<AppearanceTarget>(); targets.Add(target); }
            target.Graphic = graphic; target.Role = role; target.NativeColor = graphic.color;
            Apply(target);
        }
        internal static void Remove(AppearanceTarget target) => targets.Remove(target);

        internal static void Unbind(Graphic graphic)
        {
            var target = graphic?.GetComponent<AppearanceTarget>();
            if (target == null) return;
            targets.Remove(target); UnityEngine.Object.Destroy(target);
        }
        internal static void Refresh()
        {
            GetPalette(out _, out _, out _);
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                if (targets[i] == null || targets[i].Graphic == null) targets.RemoveAt(i);
                else Apply(targets[i]);
            }
        }
        private static void Apply(AppearanceTarget target)
        {
            if (!GetPalette(out Color bg, out Color text, out Color accent)) { target.Graphic.color = target.NativeColor; return; }
            Color color = target.Role == AppearanceRole.Background ? bg : target.Role == AppearanceRole.Accent ? accent : text;
            color.a = target.NativeColor.a;
            target.Graphic.color = color;
        }
        private static bool GetPalette(out Color bg, out Color text, out Color accent)
        {
            bg = text = accent = Color.white; Validation = string.Empty;
            switch (Preset?.Value ?? "Game")
            {
                case "Game": return false;
                case "Slate": bg = new Color(.10f,.14f,.19f); text = new Color(.87f,.92f,.96f); accent = new Color(.42f,.85f,1f); return true;
                case "Parchment": bg = new Color(.23f,.19f,.13f); text = new Color(1f,.94f,.78f); accent = new Color(1f,.77f,.35f); return true;
                case "High contrast": bg = Color.black; text = Color.white; accent = Color.yellow; return true;
                case "Custom":
                    if (!Parse(Background.Value, out bg) || !Parse(Text.Value, out text) || !Parse(Accent.Value, out accent))
                    { Validation = L("invalid", "Use #RRGGBB for all three colors. Game colors are active."); return false; }
                    if (Contrast(bg, text) < 4.5f || Contrast(bg, accent) < 3f)
                    { Validation = L("contrast", "Increase text/accent contrast against the background. Game colors are active."); return false; }
                    return true;
                default: Validation = L("invalid", "Use #RRGGBB for all three colors. Game colors are active."); return false;
            }
        }
        private static bool Parse(string value, out Color color)
        {
            color = Color.white;
            return value != null && value.Trim().Length == 7 && value.Trim()[0] == '#' && ColorUtility.TryParseHtmlString(value.Trim(), out color);
        }
        private static float Luminance(Color c) => .2126f * Linear(c.r) + .7152f * Linear(c.g) + .0722f * Linear(c.b);
        private static float Linear(float c) => c <= .04045f ? c / 12.92f : Mathf.Pow((c + .055f) / 1.055f, 2.4f);
        private static float Contrast(Color a, Color b) { float x = Luminance(a), y = Luminance(b); return (Mathf.Max(x,y)+.05f)/(Mathf.Min(x,y)+.05f); }
    }
}
