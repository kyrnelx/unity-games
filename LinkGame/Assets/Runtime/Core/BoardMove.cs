using UnityEngine;

namespace Kyrnel.LinkGame
{
    /// <summary>
    /// 一次棋子移动记录：从 <see cref="From"/> 移动到 <see cref="To"/>。
    /// 由 <see cref="LinkBoard.ApplyFill"/> 生成，供视图层播放对应的滑动动画。
    /// </summary>
    public readonly struct BoardMove
    {
        public readonly Vector2Int From;
        public readonly Vector2Int To;

        public BoardMove(Vector2Int from, Vector2Int to)
        {
            From = from;
            To = to;
        }
    }
}