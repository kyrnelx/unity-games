using System.Collections.Generic;
using UnityEngine;

namespace Kyrnel.LinkGame
{
    /// <summary>
    /// 一关的开局数据：棋盘尺寸、填充方向、节点类型表与节点生成顺序。
    /// 由 <see cref="LinkManager.StartLevel"/> 产出，视图层据此铺盘（按 SpawnOrder 逐个创建节点）。
    /// </summary>
    public readonly struct LevelSetup
    {
        /// <summary>开局数据是否有效（棋盘格子数必须为偶数）。</summary>
        public readonly bool IsValid;

        /// <summary>本关关卡序号。</summary>
        public readonly int Level;

        /// <summary>棋盘列数。</summary>
        public readonly int Width;

        /// <summary>棋盘行数。</summary>
        public readonly int Height;

        /// <summary>本关的填充方向。</summary>
        public readonly FillDirection FillDirection;

        /// <summary>按 (x * Height + y) 顺序存放的节点类型表。</summary>
        public readonly IReadOnlyList<NodeType> Types;

        /// <summary>节点生成顺序坐标列表。</summary>
        public readonly IReadOnlyList<Vector2Int> SpawnOrder;

        public LevelSetup(bool isValid, int level, int width, int height, FillDirection fillDirection,
            IReadOnlyList<NodeType> types, IReadOnlyList<Vector2Int> spawnOrder)
        {
            IsValid = isValid;
            Level = level;
            Width = width;
            Height = height;
            FillDirection = fillDirection;
            Types = types;
            SpawnOrder = spawnOrder;
        }

        /// <summary>生成失败时的空开局数据。</summary>
        public static LevelSetup Invalid(int level)
        {
            return new LevelSetup(false, level, 0, 0, FillDirection.None, null, null);
        }

        /// <summary>按棋盘坐标取节点类型（类型表索引与 <see cref="LinkBoard"/> 的存储顺序一致）。</summary>
        public NodeType GetTypeAt(Vector2Int position)
        {
            if (Types == null || position.x < 0 || position.x >= Width || position.y < 0 || position.y >= Height)
            {
                return NodeType.None;
            }

            return Types[position.x * Height + position.y];
        }
    }
}
