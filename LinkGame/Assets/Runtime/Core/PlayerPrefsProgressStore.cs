using UnityEngine;

namespace Kyrnel.LinkGame
{
    /// <summary>
    /// 基于 PlayerPrefs 的进度持久化实现。
    /// </summary>
    public sealed class PlayerPrefsProgressStore : IProgressStore
    {
        public int GetInt(string key, int defaultValue)
        {
            return PlayerPrefs.GetInt(key, defaultValue);
        }

        public void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }
    }
}