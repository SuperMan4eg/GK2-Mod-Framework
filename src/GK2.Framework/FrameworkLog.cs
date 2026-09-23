using BepInEx.Logging;

namespace GK2.Framework
{
    internal static class FrameworkLog
    {
        internal static ManualLogSource Source;
        internal static void Error(object value) => Source?.LogError(value);
    }
}
