using System.Collections.Generic;
using UnityEngine;

namespace Kyrnel.LinkGame
{
    /// <summary>
    /// 节点图案配置资产（Configs 层）：维护"节点类型 → 贴图"的映射，供视图层创建节点时查询。
    /// 数据与表现分离：Core 层只认识 <see cref="NodeType"/>，具体贴图由本资产提供。
    /// </summary>
    [CreateAssetMenu(fileName = "NodeConfig", menuName = "LinkGame/NodeConfig")]
    public class NodeConfigSO : ScriptableObject
    {
        [SerializeField] private List<NodeTypeSpriteData> _typeSprites;

        private Dictionary<NodeType, Sprite> _spriteDict;

        /// <summary>构建类型到贴图的查找表，重复类型以第一条为准。</summary>
        public void Initialize()
        {
            _spriteDict = new Dictionary<NodeType, Sprite>();
            foreach (var item in _typeSprites)
            {
                if (item.sprite != null && !_spriteDict.ContainsKey(item.type))
                {
                    _spriteDict.Add(item.type, item.sprite);
                }
            }
        }

        /// <summary>取指定类型对应的贴图，未配置时返回 null 并给出警告。</summary>
        public Sprite GetSprite(NodeType type)
        {
            if (_spriteDict == null)
            {
                Initialize();
            }

            if (_spriteDict.TryGetValue(type, out Sprite sprite))
            {
                return sprite;
            }

            Debug.LogWarning($"[NodeConfigSO] 未找到类型 {type} 对应的 Sprite！");
            return null;
        }
    }
}
