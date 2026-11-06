using System;
using UnityEngine;

namespace Kyrnel.LinkGame
{
    public class PlatformManager : MonoBehaviour
    {
        private readonly Kyrnel.Platform.PlatformManager _platformManager = new Kyrnel.Platform.PlatformManager();

        private void Awake()
        {
            _platformManager.Initialize();
        }

        public void OpenGameClub()
        {
            _platformManager.Platform.OpenGameClub();
        }
        
        public void OpenAd(Action<bool> callback)
        {
            _platformManager.Platform.ShowRewardedVideo(callback);
        }
    }
}
