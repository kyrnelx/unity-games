using System;
using UnityEngine;

namespace Kyrnel.LinkGame
{
    /// <summary>
    /// 节点图案配置项：把一种节点类型映射到对应的贴图。
    /// 由 <see cref="NodeConfigSO"/> 的配置列表使用。
    /// </summary>
    [Serializable]
    public struct NodeTypeSpriteData
    {
        public NodeType type;
        public Sprite sprite;
    }
}
