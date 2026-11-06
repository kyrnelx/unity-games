using UnityEngine;

namespace Kyrnel
{
    public class ResourcesRequest<T> : CustomYieldInstruction where T : Object
    {
        private ResourceRequest _request;
        public float Progress => _request.progress;
        private T _asset;
        public T Asset => _asset;
        private bool _isDone;
        public bool IsDone => _isDone;

        public override bool keepWaiting => !_isDone;

        public ResourcesRequest(string rAssetPath)
        {
            _request = Resources.LoadAsync<T>(rAssetPath);
            _request.completed += OnLoadingCompleted;
        }

        private void OnLoadingCompleted(AsyncOperation obj)
        {
            _asset = _request.asset as T;
            _isDone = true;
            _request.completed -= OnLoadingCompleted;
            if (_asset == null)
                Debug.LogError("资源加载失败");
        }
    }
}