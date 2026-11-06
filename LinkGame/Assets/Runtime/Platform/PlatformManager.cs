namespace Kyrnel.Platform
{
    /// <summary>
    /// 平台服务总入口：按编译目标选择具体平台实现。
    /// 编辑器使用 <see cref="MockPlatform"/>，WebGL（微信小游戏，定义了 WECHAT）使用 WeChatPlatform，
    /// 其余目标回退到 <see cref="MockPlatform"/>，保证 <see cref="Platform"/> 永不为 null。
    /// </summary>
    public class PlatformManager
    {
        public IPlatform Platform { get; private set; }

        public void Initialize()
        {
            if (Platform != null)
            {
                return;
            }

#if UNITY_EDITOR
            Platform = new MockPlatform();

#elif WECHAT
            Platform = new WeChatPlatform();
#elif ALIPAY

            Platform = new AlipayPlatform();
#else
            // 未适配的平台：回退到模拟实现，避免调用方拿到 null
            Platform = new MockPlatform();
#endif
        }
    }
}
