using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace AOT
{
    /// <summary>
    /// AOT 层启动流程（最小版）：
    /// 1. 初始化 YooAsset
    /// 2. 初始化资源包
    /// 3. 请求版本号 + 加载清单
    /// 4. 加载热更程序集 HotUpdate.dll
    /// 5. 反射执行热更入口 Game.Boot.Main()
    ///
    /// 暂时不做：下载进度、断线重连、失败重试、AOT 补充元数据。
    /// </summary>
    public static class Boot
    {
        private const string PackageName = "DefaultPackage";
        private const string HotUpdateAssemblyName = "HotUpdate";
        private const string HotUpdateDllLocation = "HotUpdate.dll";
        private const string HotUpdateEntryType = "Game.Boot";
        private const string HotUpdateEntryMethod = "Main";

        // 本地 CDN 地址（Bundles/Android/DefaultPackage/{版本号}）。
        // 先写死，下一步可以改成从配置或远端读取。
        private const string RemoteRoot = "http://127.0.0.1:8000/Bundles/Android/DefaultPackage/2026-10-21-905";

        public static async UniTask RunAsync()
        {
            try
            {
                // 1. 初始化 YooAsset，并创建（或复用）资源包
                YooAssets.Initialize();
                ResourcePackage package = GetOrCreatePackage();

                // 2. 初始化资源包
                InitializePackageOperation initOperation = package.InitializePackageAsync(CreateInitializeOptions());
                await initOperation;
                if (initOperation.Status != EOperationStatus.Succeeded)
                {
                    Debug.LogError($"[Boot] 资源包初始化失败：{initOperation.Error}");
                    return;
                }

                // 3. 请求版本号，再加载清单
                RequestPackageVersionOperation versionOperation = package.RequestPackageVersionAsync();
                await versionOperation;
                if (versionOperation.Status != EOperationStatus.Succeeded)
                {
                    Debug.LogError($"[Boot] 请求资源版本失败：{versionOperation.Error}");
                    return;
                }

                LoadPackageManifestOperation manifestOperation = package.LoadPackageManifestAsync(
                    new LoadPackageManifestOptions(versionOperation.PackageVersion, 60));
                await manifestOperation;
                if (manifestOperation.Status != EOperationStatus.Succeeded)
                {
                    Debug.LogError($"[Boot] 加载资源清单失败：{manifestOperation.Error}");
                    return;
                }

                // 4. 加载热更程序集
                Assembly hotUpdateAssembly = await LoadHotUpdateAssemblyAsync(package);
                if (hotUpdateAssembly == null)
                    return;

                // 5. 反射执行热更入口
                Type entryType = hotUpdateAssembly.GetType(HotUpdateEntryType);
                MethodInfo entryMethod = entryType?.GetMethod(
                    HotUpdateEntryMethod, BindingFlags.Public | BindingFlags.Static);
                if (entryMethod == null)
                {
                    Debug.LogError($"[Boot] 热更程序集里找不到入口 {HotUpdateEntryType}.{HotUpdateEntryMethod}()");
                    return;
                }

                entryMethod.Invoke(null, null);
                Debug.Log("[Boot] 热更入口已执行");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static ResourcePackage GetOrCreatePackage()
        {
            return YooAssets.TryGetPackage(PackageName, out ResourcePackage package)
                ? package
                : YooAssets.CreatePackage(PackageName);
        }

        private static InitializePackageOptions CreateInitializeOptions()
        {
#if UNITY_EDITOR
            // 编辑器模拟模式：直接读工程里的资源，不用打 Bundle，也不用开 CDN。
            var buildResult = EditorSimulateBuildInvoker.Build(PackageName, (int)EBundleType.VirtualAssetBundle);
            var options = new EditorSimulateModeOptions
            {
                EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(buildResult.PackageRootDirectory)
            };
            return options;
#else
            // 联机模式：内置资源 + 沙盒缓存，缺失的资源从远端拿。
            var options = new HostPlayModeOptions
            {
                BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters()
            };
            options.BuiltinFileSystemParameters.AddParameter(EFileSystemParameter.CopyBuiltinPackageManifest, true);
            options.CacheFileSystemParameters = FileSystemParameters.CreateDefaultSandboxFileSystemParameters(new RemoteService(RemoteRoot));
            return options;
#endif
        }

        private static async UniTask<Assembly> LoadHotUpdateAssemblyAsync(ResourcePackage package)
        {
#if UNITY_EDITOR
            // 编辑器里 Unity 已经编译并加载了 HotUpdate.dll，直接拿现成的程序集。
            await UniTask.Yield();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == HotUpdateAssemblyName)
                    return assembly;
            }

            Debug.LogError($"[Boot] 编辑器里没有找到 {HotUpdateAssemblyName}.dll，请确认 HotUpdate.asmdef 编译成功");
            return null;
#else
            // 真机：从资源包读取 HotUpdate.dll.bytes，再加载成程序集。
            AssetHandle handle = package.LoadAssetAsync<TextAsset>(HotUpdateDllLocation);
            await handle;
            if (handle.Status != EOperationStatus.Succeeded)
            {
                Debug.LogError($"[Boot] 加载热更 DLL 失败：{handle.Error}");
                handle.Release();
                return null;
            }

            TextAsset dllAsset = handle.AssetObject as TextAsset;
            byte[] dllBytes = dllAsset != null ? dllAsset.bytes : null;
            handle.Release();

            if (dllBytes == null || dllBytes.Length == 0)
            {
                Debug.LogError($"[Boot] 热更 DLL 内容为空：{HotUpdateDllLocation}");
                return null;
            }

            return Assembly.Load(dllBytes);
#endif
        }

        /// <summary>
        /// 极简远端地址服务：YooAsset 拿文件名过来，我们拼成完整 URL。
        /// </summary>
        private sealed class RemoteService : IRemoteService
        {
            private readonly string _root;

            public RemoteService(string root)
            {
                _root = root.TrimEnd('/');
            }

            public IReadOnlyList<string> GetRemoteUrls(string fileName)
            {
                return new[] { $"{_root}/{fileName}" };
            }
        }
    }
}
