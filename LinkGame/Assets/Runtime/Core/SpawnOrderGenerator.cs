using System.Collections.Generic;
using UnityEngine;

namespace Kyrnel.LinkGame
{
    /// <summary>
    /// 节点生成顺序的纯逻辑生成器。
    /// 支持逐行、逐列、从外到内螺旋、从内到外螺旋与随机五种模式。
    /// </summary>
    public sealed class SpawnOrderGenerator
    {
        /// <summary>节点生成顺序模式。</summary>
        public enum SpawnPattern
        {
            RowByRow,       // 逐行：从左到右、从下到上
            ColumnByColumn, // 逐列：从下到上、从左到右
            OutsideIn,      // 从外到内：螺旋向中心
            InsideOut,      // 从内到外：螺旋向外
            Random,         // 随机顺序
        }

        private readonly int _mapWidth;
        private readonly int _mapHeight;

        public SpawnOrderGenerator(int mapWidth, int mapHeight)
        {
            _mapWidth = mapWidth;
            _mapHeight = mapHeight;
        }

        /// <summary>根据指定模式生成节点的生成顺序（坐标列表）。</summary>
        public List<Vector2Int> Generate(SpawnPattern pattern)
        {
            List<Vector2Int> order = new List<Vector2Int>(_mapWidth * _mapHeight);

            switch (pattern)
            {
                case SpawnPattern.RowByRow:
                    for (int x = 0; x < _mapWidth; x++)
                    {
                        for (int y = 0; y < _mapHeight; y++)
                        {
                            order.Add(new Vector2Int(x, y));
                        }
                    }
                    break;

                case SpawnPattern.ColumnByColumn:
                    for (int y = 0; y < _mapHeight; y++)
                    {
                        for (int x = 0; x < _mapWidth; x++)
                        {
                            order.Add(new Vector2Int(x, y));
                        }
                    }
                    break;

                case SpawnPattern.OutsideIn:
                    order = GenerateSpiral(true);
                    break;

                case SpawnPattern.InsideOut:
                    order = GenerateSpiral(false);
                    break;

                case SpawnPattern.Random:
                    for (int x = 0; x < _mapWidth; x++)
                    {
                        for (int y = 0; y < _mapHeight; y++)
                        {
                            order.Add(new Vector2Int(x, y));
                        }
                    }

                    NodeTypeHelper.Shuffle(order);
                    break;
            }

            return order;
        }

        /// <summary>
        /// 生成螺旋顺序。outsideIn 为 true 时从外圈向中心，false 时从中心向外圈。
        /// </summary>
        public List<Vector2Int> GenerateSpiral(bool outsideIn)
        {
            List<Vector2Int> spiral = new List<Vector2Int>(_mapWidth * _mapHeight);

            int top = 0;
            int bottom = _mapHeight - 1;
            int left = 0;
            int right = _mapWidth - 1;

            while (top <= bottom && left <= right)
            {
                // 上边：从左到右
                for (int x = left; x <= right; x++)
                {
                    spiral.Add(new Vector2Int(x, top));
                }

                top++;

                // 右边：从上到下
                for (int y = top; y <= bottom; y++)
                {
                    spiral.Add(new Vector2Int(right, y));
                }

                right--;

                if (top <= bottom)
                {
                    // 下边：从右到左
                    for (int x = right; x >= left; x--)
                    {
                        spiral.Add(new Vector2Int(x, bottom));
                    }

                    bottom--;
                }

                if (left <= right)
                {
                    // 左边：从下到上
                    for (int y = bottom; y >= top; y--)
                    {
                        spiral.Add(new Vector2Int(left, y));
                    }

                    left++;
                }
            }

            if (outsideIn)
            {
                return spiral;
            }

            // 从内到外：反转螺旋顺序
            spiral.Reverse();
            return spiral;
        }
    }
}