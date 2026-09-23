using BepInEx.Logging;

namespace GK2.Framework
{
    public sealed class Gk2ModLogger
    {
        private readonly ManualLogSource source;
        private readonly string prefix;
        internal Gk2ModLogger(ManualLogSource source, string modId) { this.source = source; prefix = "[" + modId + "] "; }
        public void Info(object message) => source.LogInfo(prefix + message);
        public void Warning(object message) => source.LogWarning(prefix + message);
        public void Error(object message) => source.LogError(prefix + message);
        public void Debug(object message) => source.LogDebug(prefix + message);
    }
}
