#if WECHAT

using System;
using UnityEngine;
using WeChatWASM;

namespace Kyrnel.Platform
{
    public partial class WeChatPlatform : IPlatform
    {
        private const string _openlink = "-SSEykJvFV3pORt5kTNpS7hEMVMuymST8UG3amThLTQhwjBXzkhGonUP_ODkD9Kn_7GWqAoGvk7nE7rjPyKtufOwe_hR09C6VxrsSkdXBIbJd_1A6MaYChWNW53POSRXj3xILeowWzmtgVYjDfL9hZ3oSNA8mP5cRaZkiGAbUjoUAw8dnkqFRo9-oDy3MufSQFgUlVRtyDbHvnjMqRIQiNvw69E_cb7EbK8bHWcBpUF95i_OKY8DKvav1hlLqPRKZ3qUeY2fE1N4IXbKzoMIoYC4sGkT2SooTjwWdr7ktwAkKMzdUyTwRZhDInmrqw1fYGt6ZWJHZhPZ4f08rxAG9A";
        
        public void OpenGameClub(string openlink = null, Action<bool> callback = null)
        {
            if (string.IsNullOrEmpty(openlink))
                openlink = _openlink;
            
            var pageManager = WX.CreatePageManager();
            pageManager.Load(new LoadOption()
            {
                openlink = openlink,
                success = (res) =>
                {
                    pageManager.Show(new ShowOption()
                    {
                        openlink = openlink,
                    });
                    callback?.Invoke(true);
                },
                fail = (err) =>
                {
                    Debug.LogError($"[GameClub] 加载游戏圈失败: {err.errMsg}");
                    callback?.Invoke(false);
                },
            });
        }
    }
}

#endif