using System;

namespace GK2.Framework
{
    public static class FrameworkLocalization
    {
        // Optional host-provided resolver. Returning null/empty uses the English fallback.
        public static Func<string, string> Resolver { get; set; }

        public static string Get(string key, string englishFallback)
        {
            string translated = Resolver?.Invoke(key);
            return string.IsNullOrEmpty(translated) ? englishFallback : translated;
        }
    }
}
