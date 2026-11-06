using UnityEngine;

namespace Kyrnel
{
    public static class Debugger
    {
        public static void Log(this object logger, string message)
        {
            Debug.Log($"{logger.GetType().Name}:{message}");
        }

        public static void Warning(this object logger, string message)
        {
            Debug.LogWarning($"{logger.GetType().Name}:{message}");
        }

        public static void Error(this object logger, string message)
        {
            Debug.LogError($"{logger.GetType().Name}:{message}");
        }
    }
}