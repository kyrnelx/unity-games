using Cysharp.Threading.Tasks;
using Kyrnel.UniKit;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;

namespace Game
{
    /// <summary>
    /// 热更层入口：由 AOT.Boot 通过反射调用。
    /// 最小流程：显示 Loading -> 加载游戏场景。
    /// </summary>
    public static class Boot
    {
        private const string PackageName = "DefaultPackage";
        private const string LoadingViewLocation = "LoadingView";
        private const string GameSceneLocation = "GameScene";

        // 场景句柄要留着，否则场景可能被资源系统释放。
        private static SceneHandle _gameSceneHandle;

        public static void Main()
        {
            MainAsync().Forget();
        }

        private static async UniTaskVoid MainAsync()
        {
            ResourcePackage package = YooAssets.GetPackage(PackageName);
            // 1. 显示 Loading 界面。
            //    LoadingView.prefab 上需要挂 LoadingView 脚本（它继承 ViewBase），
            //    UIManager 会把它实例化到 UISystem 的 Normal 层。
            AssetHandle loadingHandle = package.LoadAssetAsync<GameObject>(LoadingViewLocation);
            await loadingHandle;
            if (loadingHandle.Status == EOperationStatus.Succeeded)
            {
                if (loadingHandle.AssetObject is ViewBase loadingView)
                    UIManager.Instance.ShowView(loadingView, ViewLayer.Normal);
                else
                    Debug.LogWarning("[Boot] LoadingView.prefab 上没有挂 LoadingView 脚本");
            }
            else
            {
                Debug.LogWarning($"[Boot] 加载 Loading 界面失败：{loadingHandle.Error}");
            }

            // 2. 加载游戏场景（Single 模式会卸载 Launcher 场景，UISystem 和 Loading 一起销毁）
            _gameSceneHandle = package.LoadSceneAsync(GameSceneLocation, LoadSceneMode.Single);
            await _gameSceneHandle;
            if (_gameSceneHandle.Status != EOperationStatus.Succeeded)
            {
                Debug.LogError($"[Boot] 加载游戏场景失败：{_gameSceneHandle.Error}");
                loadingHandle.Release();
                return;
            }

            // 3. 释放 prefab 句柄
            loadingHandle.Release();

            Debug.Log("[Boot] 进入游戏场景");
        }
    }
}
