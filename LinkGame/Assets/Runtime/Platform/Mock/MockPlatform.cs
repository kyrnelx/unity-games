using System;
using UnityEngine;

namespace Kyrnel.Platform
{
    public class MockPlatform : IPlatform
    {
        public void OpenGameClub(string openlink = null, Action<bool> callback = null)
        {
            Debug.Log("[MockPlatform] 模拟打开游戏圈。");
            callback?.Invoke(true);
        }

        public void PreloadRewardedVideo(string adUnitId)
        {
            Debug.Log($"[MockPlatform] 模拟预加载激励视频（adUnitId={adUnitId}）。");
        }

        public void ShowRewardedVideo(Action<bool> callback)
        {
            Debug.Log("[MockPlatform] 模拟播放激励视频，直接返回【完整观看】。");
            callback?.Invoke(true);
        }
    }
}
