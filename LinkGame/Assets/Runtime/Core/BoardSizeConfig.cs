using UnityEngine;

namespace Kyrnel.LinkGame
{
    /// <summary>
    /// 棋盘尺寸配置：第 1 关 5×6，第 2 关起直接扩展为 8×14 并封顶。
    /// 8（偶）× 14（偶）= 112 格（偶数），保证棋盘总数恒为偶数，无需额外修正。
    /// </summary>
    public sealed class BoardSizeConfig
    {
        public const int FirstLevelWidth = 5;
        public const int FirstLevelHeight = 6;
        public const int DefaultWidth = 8;
        public const int DefaultHeight = 14;
        public const int MaxWidth = DefaultWidth;
        public const int MaxHeight = DefaultHeight;

        /// <summary>根据当前关卡返回棋盘宽高。</summary>
        public Vector2Int GetBoardSize(int level)
        {
            // 第 1 关 5×6，第 2 关起固定 8×14 并封顶
            if (level <= 1)
            {
                return new Vector2Int(FirstLevelWidth, FirstLevelHeight);
            }

            return new Vector2Int(DefaultWidth, DefaultHeight);
        }
    }
}