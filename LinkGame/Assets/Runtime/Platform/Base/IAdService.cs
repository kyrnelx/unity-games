using System;

namespace Kyrnel.Platform
{
    public interface IAdService
    {
        void PreloadRewardedVideo(string adUnitId);
        
        // onshow onload onerror onclose res.isend
        void ShowRewardedVideo(Action<bool> callback);
    }
}