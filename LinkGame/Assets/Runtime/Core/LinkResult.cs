using System.Collections.Generic;
using UnityEngine;

namespace Kyrnel.LinkGame
{
    /// <summary>
    /// 一次点击交互的结果分类。
    /// </summary>
    public enum LinkClickOutcome
    {
        /// <summary>未产生任何交互（空格子、越界或棋盘未就绪）。</summary>
        Ignored,

        /// <summary>首次选中该节点。</summary>
        Selected,

        /// <summary>再次点击已选中的节点，取消选中。</summary>
        Deselected,

        /// <summary>类型不匹配或无法连通，改为选中新节点。</summary>
        Switched,

        /// <summary>连接成功，两个节点可以消除。</summary>
        Matched,
    }

    /// <summary>
    /// 连线交互结果：描述本次点击发生了什么，供视图层更新选中高亮并播放动画。
    /// 纯数据契约，不依赖任何视图对象，可脱离场景单元测试。
    /// </summary>
    public readonly struct LinkResult
    {
        /// <summary>结果分类。</summary>
        public readonly LinkClickOutcome Outcome;

        /// <summary>交互前的选中位置（<see cref="LinkClickOutcome.Selected"/> / <see cref="LinkClickOutcome.Ignored"/> 时无效）。</summary>
        public readonly Vector2Int PreviousSelection;

        /// <summary>本次点击命中的位置。</summary>
        public readonly Vector2Int Position;

        /// <summary>连接路径（仅 <see cref="LinkClickOutcome.Matched"/> 时非空）。</summary>
        public readonly IReadOnlyList<Vector2Int> Path;

        /// <summary>拐点数（仅 <see cref="LinkClickOutcome.Matched"/> 时有效）。</summary>
        public readonly int Turns;

        /// <summary>本次获得的积分（仅 <see cref="LinkClickOutcome.Matched"/> 时有效）。</summary>
        public readonly int Points;

        public LinkResult(LinkClickOutcome outcome, Vector2Int previousSelection, Vector2Int position,
            IReadOnlyList<Vector2Int> path, int turns, int points)
        {
            Outcome = outcome;
            PreviousSelection = previousSelection;
            Position = position;
            Path = path;
            Turns = turns;
            Points = points;
        }

        /// <summary>是否成功连接（可以播放连线与消除动画）。</summary>
        public bool IsMatched => Outcome == LinkClickOutcome.Matched;

        /// <summary>交互前的选中位置是否有效。</summary>
        public bool HasPreviousSelection => PreviousSelection.x >= 0 && PreviousSelection.y >= 0;

        private static readonly Vector2Int None = new Vector2Int(-1, -1);

        /// <summary>未产生交互。</summary>
        public static LinkResult Ignored(Vector2Int position)
        {
            return new LinkResult(LinkClickOutcome.Ignored, None, position, null, 0, 0);
        }

        /// <summary>首次选中。</summary>
        public static LinkResult Selected(Vector2Int position)
        {
            return new LinkResult(LinkClickOutcome.Selected, None, position, null, 0, 0);
        }

        /// <summary>取消选中。</summary>
        public static LinkResult Deselected(Vector2Int position)
        {
            return new LinkResult(LinkClickOutcome.Deselected, position, position, null, 0, 0);
        }

        /// <summary>切换选中（从 previous 改选 position）。</summary>
        public static LinkResult Switched(Vector2Int previousSelection, Vector2Int position)
        {
            return new LinkResult(LinkClickOutcome.Switched, previousSelection, position, null, 0, 0);
        }

        /// <summary>连接成功。</summary>
        public static LinkResult Matched(Vector2Int first, Vector2Int second,
            IReadOnlyList<Vector2Int> path, int turns, int points)
        {
            return new LinkResult(LinkClickOutcome.Matched, first, second, path, turns, points);
        }
    }
}
