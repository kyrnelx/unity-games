using System.Collections.Generic;
using UnityEngine;

namespace Kyrnel.LinkGame
{
    /// <summary>
    /// 连连看棋盘的纯数据模型（不依赖任何 MonoBehaviour / 视图对象）。
    /// 维护节点类型的二维网格，并提供清空、统计等操作。
    /// </summary>
    public sealed class LinkBoard : ILinkBoard
    {
        private readonly NodeType[,] _grid;

        public int Width { get; }
        public int Height { get; }
        public int RemainingCount { get; private set; }

        public LinkBoard(int width, int height)
        {
            Width = width;
            Height = height;
            _grid = new NodeType[width, height];
        }

        /// <summary>批量填充类型数组，并重建存活计数。</summary>
        public void LoadTypes(IReadOnlyList<NodeType> types)
        {
            if (types == null || types.Count != Width * Height)
            {
                Debug.LogError($"[LinkBoard] 类型数量 {types?.Count} 与棋盘容量 {Width * Height} 不匹配。");
                return;
            }

            int index = 0;
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    _grid[x, y] = types[index++];
                }
            }

            RemainingCount = CountAlive();
        }

        public bool IsInside(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < Width && pos.y >= 0 && pos.y < Height;
        }

        public bool IsEmptyAt(Vector2Int pos)
        {
            return !IsInside(pos) || GetTypeAt(pos) == NodeType.None;
        }

        public NodeType GetTypeAt(Vector2Int pos)
        {
            if (!IsInside(pos))
            {
                return NodeType.None;
            }

            return _grid[pos.x, pos.y];
        }

        public void SetType(Vector2Int pos, NodeType type)
        {
            if (!IsInside(pos))
            {
                return;
            }

            NodeType old = _grid[pos.x, pos.y];
            _grid[pos.x, pos.y] = type;

            if (old == NodeType.None && type != NodeType.None)
            {
                RemainingCount++;
            }
            else if (old != NodeType.None && type == NodeType.None)
            {
                RemainingCount = Mathf.Max(0, RemainingCount - 1);
            }
        }

        public void ClearAt(Vector2Int pos)
        {
            SetType(pos, NodeType.None);
        }

        /// <summary>
        /// 洗牌：将所有存活棋子的坐标随机打乱（携带各自的类型移动到新位置）。
        /// 返回移动记录列表（from → to），供视图层播放交换滑动动画。
        /// </summary>
        public List<BoardMove> ShufflePositions()
        {
            List<Vector2Int> positions = CollectAlivePositions();
            if (positions.Count == 0)
            {
                return new List<BoardMove>();
            }

            List<NodeType> types = new List<NodeType>(positions.Count);
            for (int i = 0; i < positions.Count; i++)
            {
                types.Add(_grid[positions[i].x, positions[i].y]);
            }

            // 打乱目标坐标顺序，使每个棋子携带自己的类型移动到新位置
            List<Vector2Int> shuffled = new List<Vector2Int>(positions);
            NodeTypeHelper.Shuffle(shuffled);

            // 清空棋盘模型（保持 RemainingCount 不变，仅就地写入）
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    _grid[x, y] = NodeType.None;
                }
            }

            List<BoardMove> moves = new List<BoardMove>(positions.Count);
            for (int i = 0; i < positions.Count; i++)
            {
                _grid[shuffled[i].x, shuffled[i].y] = types[i];
                if (positions[i] != shuffled[i])
                {
                    moves.Add(new BoardMove(positions[i], shuffled[i]));
                }
            }

            return moves;
        }

        /// <summary>
        /// 按指定方向对存活棋子执行滑动补位：棋子沿填充方向滑动，压缩至该方向尽头，空位留在反方向。
        /// <see cref="FillDirection.None"/> 表示不填充。
        /// 返回所有移动记录（仅含真正发生位移的棋子），供视图层播放动画。
        /// </summary>
        public List<BoardMove> ApplyFill(FillDirection direction)
        {
            List<BoardMove> moves = new List<BoardMove>();

            switch (direction)
            {
                case FillDirection.None:
                    return moves;

                case FillDirection.Down:
                    // 棋子向下滑：每列从下往上收集棋子，写到最底部
                    for (int x = 0; x < Width; x++)
                    {
                        int writeY = 0;
                        for (int y = 0; y < Height; y++)
                        {
                            NodeType type = _grid[x, y];
                            if (type == NodeType.None)
                            {
                                continue;
                            }

                            if (y != writeY)
                            {
                                _grid[x, writeY] = type;
                                _grid[x, y] = NodeType.None;
                                moves.Add(new BoardMove(new Vector2Int(x, y), new Vector2Int(x, writeY)));
                            }

                            writeY++;
                        }
                    }
                    break;

                case FillDirection.Up:
                    // 棋子向上滑：每列从上往下收集棋子，写到最顶部
                    for (int x = 0; x < Width; x++)
                    {
                        int writeY = Height - 1;
                        for (int y = Height - 1; y >= 0; y--)
                        {
                            NodeType type = _grid[x, y];
                            if (type == NodeType.None)
                            {
                                continue;
                            }

                            if (y != writeY)
                            {
                                _grid[x, writeY] = type;
                                _grid[x, y] = NodeType.None;
                                moves.Add(new BoardMove(new Vector2Int(x, y), new Vector2Int(x, writeY)));
                            }

                            writeY--;
                        }
                    }
                    break;

                case FillDirection.Left:
                    // 棋子向左滑：每行从左往右收集棋子，写到最左边
                    for (int y = 0; y < Height; y++)
                    {
                        int writeX = 0;
                        for (int x = 0; x < Width; x++)
                        {
                            NodeType type = _grid[x, y];
                            if (type == NodeType.None)
                            {
                                continue;
                            }

                            if (x != writeX)
                            {
                                _grid[writeX, y] = type;
                                _grid[x, y] = NodeType.None;
                                moves.Add(new BoardMove(new Vector2Int(x, y), new Vector2Int(writeX, y)));
                            }

                            writeX++;
                        }
                    }
                    break;

                case FillDirection.Right:
                    // 棋子向右滑：每行从右往左收集棋子，写到最右边
                    for (int y = 0; y < Height; y++)
                    {
                        int writeX = Width - 1;
                        for (int x = Width - 1; x >= 0; x--)
                        {
                            NodeType type = _grid[x, y];
                            if (type == NodeType.None)
                            {
                                continue;
                            }

                            if (x != writeX)
                            {
                                _grid[writeX, y] = type;
                                _grid[x, y] = NodeType.None;
                                moves.Add(new BoardMove(new Vector2Int(x, y), new Vector2Int(writeX, y)));
                            }

                            writeX--;
                        }
                    }
                    break;
            }

            return moves;
        }

        /// <summary>将棋盘重置为空棋盘。</summary>
        public void ClearAll()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    _grid[x, y] = NodeType.None;
                }
            }

            RemainingCount = 0;
        }

        /// <summary>收集所有存活节点的坐标。</summary>
        public List<Vector2Int> CollectAlivePositions()
        {
            List<Vector2Int> positions = new List<Vector2Int>(RemainingCount);
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (_grid[x, y] != NodeType.None)
                    {
                        positions.Add(new Vector2Int(x, y));
                    }
                }
            }

            return positions;
        }

        private int CountAlive()
        {
            int count = 0;
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (_grid[x, y] != NodeType.None)
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }
}