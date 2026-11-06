using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kyrnel.LinkGame
{
    public static class NodeTypeHelper
    {
        private static readonly NodeType[] ValidNodeTypes = CacheValidNodeTypes();

        private static NodeType[] CacheValidNodeTypes()
        {
            var allTypes = (NodeType[])Enum.GetValues(typeof(NodeType));
            List<NodeType> list = new List<NodeType>();

            foreach (var type in allTypes)
            {
                if (type != NodeType.None)
                {
                    list.Add(type);
                }
            }

            return list.ToArray();
        }

        public static NodeType GetRandomType()
        {
            return GetRandomType(ValidNodeTypes.Length);
        }

        /// <summary>
        /// 从指定数量的类型池中随机取一种类型。
        /// typeCount 越小，可用的图案越少，匹配越容易。
        /// </summary>
        public static NodeType GetRandomType(int typeCount)
        {
            int count = Mathf.Clamp(typeCount, 1, ValidNodeTypes.Length);
            int randomIndex = UnityEngine.Random.Range(0, count);
            return ValidNodeTypes[randomIndex];
        }

        public static List<NodeType> GeneratePairedTypes(int totalCount)
        {
            return GeneratePairedTypes(totalCount, ValidNodeTypes.Length);
        }

        /// <summary>
        /// 生成成对的图案列表，仅使用前 typeCount 种图案。
        /// 用于按关卡控制难度：前几关类型少、容易匹配，随关卡逐渐增多。
        /// </summary>
        public static List<NodeType> GeneratePairedTypes(int totalCount, int typeCount)
        {
            if (totalCount % 2 != 0)
            {
                Debug.LogError($"[NodeTypeHelper] 方块总数 {totalCount} 不是偶数，连连看无法完全消除！");
            }

            List<NodeType> typesList = new List<NodeType>(totalCount);
            int pairCount = totalCount / 2;

            for (int i = 0; i < pairCount; i++)
            {
                NodeType randomType = GetRandomType(typeCount);
                typesList.Add(randomType);
                typesList.Add(randomType);
            }

            Shuffle(typesList);

            return typesList;
        }


        public static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int randomIndex = UnityEngine.Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }
    }
}