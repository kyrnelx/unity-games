using UnityEngine;

namespace Kyrnel.LinkGame
{
    /// <summary>
    /// 棋盘布局计算：根据节点尺寸、间距与边距计算每个格子的锚点位置。
    /// 调用 <see cref="Initialize"/> 传入棋盘行列后，整盘会以容器锚点 (0,0)
    /// 为中心向四周对称展开，棋盘尺寸变化时始终居中，不再偏向角落；
    /// <see cref="XPad"/> / <see cref="YPad"/> 则作为整盘相对中心的微调偏移。
    /// </summary>
    public sealed class BoardLayout
    {
        private readonly float _nodeSize;
        private readonly float _spacing;
        private readonly float _xPadding;
        private readonly float _yPadding;

        private float _offsetX;
        private float _offsetY;
        private int _columns = 1;
        private int _rows = 1;
        private bool _initialized;

        public BoardLayout(float nodeSize, float spacing, float xPadding, float yPadding)
        {
            _nodeSize = nodeSize;
            _spacing = spacing;
            _xPadding = xPadding;
            _yPadding = yPadding;
        }

        /// <summary>单个格子的步长（节点尺寸 + 间距）。</summary>
        public float Step => _nodeSize + _spacing;

        /// <summary>节点（格子）边长。视图层据此设置节点实际尺寸，保证与布局一致。</summary>
        public float NodeSize => _nodeSize;

        /// <summary>相邻格子之间的间距。</summary>
        public float Spacing => _spacing;

        /// <summary>整盘尺寸（宽 × 高），便于外部按其摆放背景等。</summary>
        public Vector2 BoardSize => new Vector2(
            Step * (_columns - 1) + _nodeSize,
            Step * (_rows - 1) + _nodeSize);

        /// <summary>
        /// 传入棋盘行列数，计算使整盘以 (0,0) 为中心的偏移量。
        /// </summary>
        public void Initialize(int columns, int rows)
        {
            _columns = Mathf.Max(1, columns);
            _rows = Mathf.Max(1, rows);

            // 将整盘平移到以 (0,0) 为中心：pivot=0.5 时 anchoredPosition 即节点中心（格线）。
            // 首格中心 = -Step*(n-1)/2，末格中心 = +Step*(n-1)/2，两者互为相反数即居中。
            // padding 在此作为整盘相对中心的微调偏移叠加。
            _offsetX = -Step * (_columns - 1) * 0.5f + _xPadding;
            _offsetY = -Step * (_rows - 1) * 0.5f + _yPadding;
            _initialized = true;
        }

        /// <summary>整盘 X 方向偏移（相对中心）。</summary>
        public float XPad => _xPadding;

        /// <summary>整盘 Y 方向偏移（相对中心）。</summary>
        public float YPad => _yPadding;

        /// <summary>格子左下角锚点（节点 Pivot 在中心时的 anchoredPosition）。</summary>
        public Vector2 GetCellBottomLeft(int x, int y)
        {
            if (_initialized)
            {
                return new Vector2(_offsetX + Step * x, _offsetY + Step * y);
            }

            // 未初始化的旧行为：从 padding 起算（兼容性保留）
            return new Vector2(_xPadding + Step * x, _yPadding + Step * y);
        }

        /// <summary>
        /// 格子中心点。节点 pivot=0.5 时 anchoredPosition 即中心，
        /// 因此与 GetCellBottomLeft 返回同一位置。
        /// </summary>
        public Vector2 GetCellCenter(int x, int y)
        {
            return GetCellBottomLeft(x, y);
        }
    }
}