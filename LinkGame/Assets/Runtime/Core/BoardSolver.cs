using System.Collections.Generic;
using UnityEngine;

namespace Kyrnel.LinkGame
{
    /// <summary>
    /// 棋盘求解器：负责解检测（是否存在可消除对）与无解重排。
    /// 纯逻辑实现，依赖 <see cref="ILinkBoard"/> 与 <see cref="LinkPathfinder"/>。
    /// </summary>
    public sealed class BoardSolver
    {
        private readonly ILinkBoard _board;
        private readonly LinkPathfinder _pathfinder;

        public BoardSolver(ILinkBoard board, LinkPathfinder pathfinder)
        {
            _board = board;
            _pathfinder = pathfinder;
        }

        /// <summary>是否存在任意可连接的同类型节点对。</summary>
        public bool HasAnyLinkablePair()
        {
            List<Vector2Int> nodes = _board.CollectAlivePositions();
            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = i + 1; j < nodes.Count; j++)
                {
                    Vector2Int a = nodes[i];
                    Vector2Int b = nodes[j];
                    if (_board.GetTypeAt(a) != _board.GetTypeAt(b))
                    {
                        continue;
                    }

                    if (_pathfinder.FindPath(a, b) != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 找到一对可连接的节点（用于自动连接调试）。
        /// 返回 null 表示无解。
        /// </summary>
        public bool TryFindLinkablePair(out Vector2Int first, out Vector2Int second)
        {
            first = default;
            second = default;

            List<Vector2Int> nodes = _board.CollectAlivePositions();
            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = i + 1; j < nodes.Count; j++)
                {
                    Vector2Int a = nodes[i];
                    Vector2Int b = nodes[j];
                    if (_board.GetTypeAt(a) != _board.GetTypeAt(b))
                    {
                        continue;
                    }

                    if (_pathfinder.FindPath(a, b) != null)
                    {
                        first = a;
                        second = b;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 确保棋盘有解：若无解则对存活节点洗牌（交换任意两格类型），
        /// 最多尝试 <paramref name="maxShuffleTries"/> 次。
        /// 返回是否最终有解。
        /// </summary>
        public bool EnsurePlayable(int maxShuffleTries)
        {
            if (_board.RemainingCount <= 0)
            {
                return true;
            }

            if (HasAnyLinkablePair())
            {
                return true;
            }

            for (int i = 0; i < maxShuffleTries; i++)
            {
                ShuffleRemaining();
                if (HasAnyLinkablePair())
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 对棋盘内所有存活节点进行类型洗牌。
        /// 需要 <see cref="ILinkBoard"/> 支持写入，故额外依赖 <see cref="LinkBoard"/>。
        /// </summary>
        public void ShuffleRemaining()
        {
            if (!(_board is LinkBoard writable))
            {
                Debug.LogWarning("[BoardSolver] 棋盘不可写，跳过重排。");
                return;
            }

            List<Vector2Int> positions = writable.CollectAlivePositions();
            List<NodeType> types = new List<NodeType>(positions.Count);
            for (int i = 0; i < positions.Count; i++)
            {
                types.Add(writable.GetTypeAt(positions[i]));
            }

            NodeTypeHelper.Shuffle(types);

            for (int i = 0; i < positions.Count; i++)
            {
                writable.SetType(positions[i], types[i]);
            }
        }
    }
}