using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using LazyBearTechnology;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GK2.Framework
{
    public static class FrameworkLocalization
    {
        private const string FrameworkModId = "ru.superman4eg.gk2.framework";
        private static readonly object Sync = new object();
        private static readonly Dictionary<string, Dictionary<string, string>> Cache =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        // Legacy host override kept for 0.1.x compatibility. Returning null/empty falls back to language files.
        public static Func<string, string> Resolver { get; set; }

        public static string CurrentLanguage => ResolveCurrentLanguage();

        public static string RootDirectory =>
            Path.Combine(Paths.PluginPath, "GK2.Framework", "Localization");

        public static string Get(string key, string englishFallback)
        {
            string translated = Resolver?.Invoke(key);
            if (!string.IsNullOrEmpty(translated)) return translated;
            return Get(FrameworkModId, key, englishFallback);
        }

        public static string Get(string modId, string key, string englishFallback)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Localization key is required.", nameof(key));

            string normalizedModId = NormalizeModId(modId);
            foreach (string language in EnumerateLanguageCandidates(CurrentLanguage))
            {
                Dictionary<string, string> translations = GetTranslations(normalizedModId, language);
                if (translations.TryGetValue(key, out string value) && !string.IsNullOrEmpty(value))
                    return value;
            }

            return englishFallback ?? string.Empty;
        }

        public static string GetLocalizationDirectory(string modId)
        {
            string normalizedModId = NormalizeModId(modId);
            return Path.Combine(RootDirectory, normalizedModId);
        }

        public static void Reload()
        {
            lock (Sync) Cache.Clear();
        }

        public static void Reload(string modId)
        {
            string normalizedModId = NormalizeModId(modId);
            string prefix = normalizedModId + "|";
            lock (Sync)
            {
                var keys = new List<string>(Cache.Keys);
                foreach (string cacheKey in keys)
                    if (cacheKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        Cache.Remove(cacheKey);
            }
        }

        private static Dictionary<string, string> GetTranslations(string modId, string language)
        {
            string cacheKey = modId + "|" + language;
            lock (Sync)
            {
                if (Cache.TryGetValue(cacheKey, out Dictionary<string, string> cached))
                    return cached;

                Dictionary<string, string> loaded = LoadTranslations(modId, language);
                Cache[cacheKey] = loaded;
                return loaded;
            }
        }

        private static Dictionary<string, string> LoadTranslations(string modId, string language)
        {
            var empty = new Dictionary<string, string>(StringComparer.Ordinal);
            string path = Path.Combine(GetLocalizationDirectory(modId), language + ".json");
            if (!File.Exists(path)) return empty;

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                var parsed = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                if (parsed == null) return empty;

                var result = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (KeyValuePair<string, string> pair in parsed)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key)) continue;
                    result[pair.Key.Trim()] = pair.Value ?? string.Empty;
                }

                FrameworkLog.Source?.LogInfo(
                    $"GK2_LOCALIZATION_LOADED: mod={modId}; language={language}; entries={result.Count}; path={path}");
                return result;
            }
            catch (Exception ex)
            {
                FrameworkLog.Source?.LogWarning(
                    $"GK2_LOCALIZATION_LOAD_FAILED: mod={modId}; language={language}; path={path}; error={ex.Message}");
                return empty;
            }
        }

        private static IEnumerable<string> EnumerateLanguageCandidates(string language)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (seen.Add(language)) yield return language;

            int separator = language.IndexOf('_');
            if (separator > 0)
            {
                string neutral = language.Substring(0, separator);
                if (seen.Add(neutral)) yield return neutral;
            }

            if (seen.Add("en")) yield return "en";
        }

        private static string ResolveCurrentLanguage()
        {
            string persisted = GetSavedLanguage();
            if (!string.IsNullOrWhiteSpace(persisted))
                return NormalizeLanguage(persisted);

            return NormalizeLanguage(LLBase.CurrentLang);
        }

        private static string GetSavedLanguage()
        {
            try
            {
                string json = PlayerPrefs.GetString("settings", string.Empty);
                if (string.IsNullOrWhiteSpace(json)) return null;
                JObject settings = JObject.Parse(json);
                return settings["language"]?.ToString();
            }
            catch (Exception ex)
            {
                FrameworkLog.Source?.LogWarning("GK2_LOCALIZATION_LANGUAGE_DETECT_FAILED: " + ex.Message);
                return null;
            }
        }

        private static string NormalizeModId(string modId)
        {
            if (string.IsNullOrWhiteSpace(modId))
                throw new ArgumentException("Mod id is required.", nameof(modId));

            string normalized = modId.Trim().ToLowerInvariant();
            if (normalized.Contains("..") || normalized.IndexOfAny(new[] { '/', '\\', ':' }) >= 0)
                throw new ArgumentException("Mod id contains invalid path characters.", nameof(modId));
            return normalized;
        }

        private static string NormalizeLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language)) return "en";

            string normalized = language.Trim().ToLowerInvariant().Replace('-', '_');
            if (normalized == "kr" || normalized == "kor") return "ko";
            return normalized;
        }
    }
}
