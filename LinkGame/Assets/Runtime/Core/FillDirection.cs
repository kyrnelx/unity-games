namespace Kyrnel.LinkGame
{
    /// <summary>
    /// 棋盘填充方向模式。
    /// 每关随机选择一种，决定消除后棋子向哪个方向滑动补位。
    /// </summary>
    public enum FillDirection
    {
        /// <summary>不填充：消除后不滑动补位，保留空位。</summary>
        None,

        /// <summary>向上填充：棋子向上滑动填补空位（空位会移动到最底部）。</summary>
        Up,

        /// <summary>向下填充：棋子向下滑动填补空位（类似重力下落）。</summary>
        Down,

        /// <summary>向左填充：棋子向左滑动填补空位。</summary>
        Left,

        /// <summary>向右填充：棋子向右滑动填补空位。</summary>
        Right,
    }
}