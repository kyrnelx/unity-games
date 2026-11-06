using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kyrnel.LinkGame
{
    /// <summary>
    /// 连接线 UI 图元（棋盘表现基础件）。
    /// 通过 OnPopulateMesh 生成带厚度的折线网格，并支持按进度渐进绘制（连线动画）。
    /// 属于底层绘制部件，不是界面视图。
    /// </summary>
    public class LinkLineGraphic : MaskableGraphic
    {
        public float Thickness { get; set; } = 10f;

        /// <summary>是否使用彩虹渐变（沿路径平滑过渡）。</summary>
        public bool Rainbow { get; set; }

        /// <summary>彩虹起始色相（默认柔和桃金）。</summary>
        public float StartHue { get; set; } = 0.08f;

        /// <summary>彩虹结束色相（默认蓝紫）。</summary>
        public float EndHue { get; set; } = 0.85f;

        /// <summary>彩虹饱和度（越低越柔和）。</summary>
        public float RainbowSaturation { get; set; } = 0.8f;

        private readonly List<Vector2> _points = new List<Vector2>();
        private bool _visible;
        private float _progress = 1f;

        public void Show(List<Vector2> points)
        {
            _points.Clear();
            if (points != null)
            {
                _points.AddRange(points);
            }

            _visible = true;
            _progress = 1f;

            // 显示前确保 GameObject 处于激活状态，避免上次隐藏后残留禁用状态
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            SetVerticesDirty();
        }

        public void Hide()
        {
            _visible = false;
            _points.Clear();
            _progress = 0f;

            // 立即清空渲染几何，避免 Canvas 延迟重建导致连线残影
            if (canvasRenderer != null)
            {
                canvasRenderer.Clear();
            }

            SetVerticesDirty();

            // 禁用整个 GameObject，彻底阻止任何延迟重建把旧几何重新画出来
            gameObject.SetActive(false);
        }




        /// <summary>设置连线绘制进度（0~1），用于连线动画。</summary>
        public void SetProgress(float progress)
        {
            _progress = Mathf.Clamp01(progress);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (!_visible || _points.Count < 2 || _progress <= 0f)
            {
                return;
            }

            float halfThickness = Mathf.Max(0f, Thickness) * 0.5f;

            float totalLength = 0f;
            for (int i = 0; i < _points.Count - 1; i++)
            {
                totalLength += (_points[i + 1] - _points[i]).magnitude;
            }

            if (totalLength <= 0f)
            {
                return;
            }

            float targetLength = totalLength * _progress;
            float accumulated = 0f;

            for (int i = 0; i < _points.Count - 1; i++)
            {
                Vector2 a = _points[i];
                Vector2 b = _points[i + 1];
                float segLen = (b - a).magnitude;
                if (segLen <= 0f)
                {
                    continue;
                }

                if (accumulated >= targetLength)
                {
                    break;
                }

                Vector2 end = b;
                if (accumulated + segLen > targetLength)
                {
                    float t = (targetLength - accumulated) / segLen;
                    end = Vector2.Lerp(a, b, t);
                }

                Vector2 dir = (end - a).normalized;
                Vector2 normal = new Vector2(-dir.y, dir.x) * halfThickness;

                Vector2 p0 = a - normal;
                Vector2 p1 = a + normal;
                Vector2 p2 = end + normal;
                Vector2 p3 = end - normal;

                int baseIndex = vh.currentVertCount;


                Color colA = color;
                Color colB = color;
                if (Rainbow)
                {
                    // 线段起点/终点在总路径中的占比 -> 彩虹色相
                    float t0 = totalLength > 0f ? accumulated / totalLength : 0f;
                    float t1 = totalLength > 0f ? (accumulated + segLen) / totalLength : 1f;
                    // 柔和的彩虹：从起始色相平滑过渡到结束色相，饱和度适中
                    colA = Color.HSVToRGB(Mathf.Lerp(StartHue, EndHue, Mathf.Clamp01(t0)), RainbowSaturation, 1f);
                    colB = Color.HSVToRGB(Mathf.Lerp(StartHue, EndHue, Mathf.Clamp01(t1)), RainbowSaturation, 1f);
                    colA.a = color.a;
                    colB.a = color.a;
                }

                vh.AddVert(p0, colA, Vector2.zero);
                vh.AddVert(p1, colA, Vector2.zero);
                vh.AddVert(p2, colB, Vector2.zero);
                vh.AddVert(p3, colB, Vector2.zero);

                vh.AddTriangle(baseIndex, baseIndex + 1, baseIndex + 2);
                vh.AddTriangle(baseIndex, baseIndex + 2, baseIndex + 3);

                accumulated += segLen;
            }
        }
    }
}
