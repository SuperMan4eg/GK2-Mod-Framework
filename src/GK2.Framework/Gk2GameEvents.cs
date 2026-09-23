using System;
using BepInEx.Logging;

namespace GK2.Framework
{
    public static class Gk2GameEvents
    {
        public static event Action GameStarted;
        public static event Action ReturnedToMainMenu;
        public static event Action GamePaused;
        public static event Action GameUnpaused;
        private static ManualLogSource log;

        internal static void Bind(ManualLogSource logger)
        {
            log = logger;
            MainGame.OnGameStarted += OnGameStarted;
            MainGame.OnGoToMainMenu += OnReturnedToMainMenu;
            MainGame.OnGamePaused += () => InvokeSafely(GamePaused, "GamePaused");
            MainGame.OnGameUnpaused += () => InvokeSafely(GameUnpaused, "GameUnpaused");
        }

        private static void OnGameStarted()
        { FrameworkApi.Registry?.NotifyGameStarted(); InvokeSafely(GameStarted, "GameStarted"); }
        private static void OnReturnedToMainMenu()
        { FrameworkApi.Registry?.NotifyReturnedToMainMenu(); InvokeSafely(ReturnedToMainMenu, "ReturnedToMainMenu"); }

        private static void InvokeSafely(Action handlers, string name)
        {
            if (handlers == null) return;
            foreach (Delegate handler in handlers.GetInvocationList())
                try { ((Action)handler)(); } catch (Exception ex) { log?.LogError($"Game event {name} handler failed: {ex}"); }
        }
    }
}
