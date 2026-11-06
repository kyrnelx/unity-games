#if WECHAT

using System;
using WeChatWASM;

namespace Kyrnel.Platform
{
    public partial class WeChatPlatform : IPlatform
    {
        private const string AdUnitId = "adunit-de82036eaa70f864";

        private WXRewardedVideoAd _rewardedVideoAd;
        private Action<bool> _onCloseAction;
        private bool _isShowing;
        
        public void PreloadRewardedVideo(string adUnitId = AdUnitId)
        {
            if (_rewardedVideoAd != null) return;

            _rewardedVideoAd = WXBase.CreateRewardedVideoAd(new WXCreateRewardedVideoAdParam
            {
                adUnitId = adUnitId,
                multiton = false
            });

            if (_rewardedVideoAd == null) return;
            
            _rewardedVideoAd.OnClose(res =>
            {
                FinishShowAd(res.isEnded);
            });
            
            _rewardedVideoAd.OnError(res =>
            {
                this.Error($"激励视频广告错误: {res.errMsg}");
                FinishShowAd(false);
            });
        }
        
        public void ShowRewardedVideo(Action<bool> callback)
        {
            if (_isShowing) return;

            if (_rewardedVideoAd == null)
                PreloadRewardedVideo();

            if (_rewardedVideoAd == null)
            {
                callback?.Invoke(false);
                return;
            }
            
            _isShowing = true;
            _onCloseAction = callback;

            _rewardedVideoAd.Show(null, _ =>
            {
                _rewardedVideoAd.Load(_ => _rewardedVideoAd.Show(null, _ => FinishShowAd(false)),
                    _ => FinishShowAd(false));
            });
        }
        
        private void FinishShowAd(bool isSuccess)
        {
            if (!_isShowing) return;
            _isShowing = false;

            var cb = _onCloseAction;
            _onCloseAction = null;
            cb?.Invoke(isSuccess);
        }
    }
}

#endif