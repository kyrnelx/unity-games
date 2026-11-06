using System;

namespace Kyrnel.Platform
{
    public interface IGameClubeService
    {
        void OpenGameClub(string openlink = null, Action<bool> callback = null);
    }
}