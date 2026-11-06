using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AOT
{
    /// <summary>
    /// 启动场景（Launcher.unity）里唯一需要挂载的组件。
    /// 它只做一件事：把启动流程交给 Boot。
    /// </summary>
    public class Launcher : MonoBehaviour
    {
        private void Start()
        {
            Boot.RunAsync().Forget();
        }
    }
}
