using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kyrnel.LinkGame
{
    /// <summary>
    /// 连线绘制器（棋盘表现基础件，非界面视图）：负责创建 <see cref="LinkLineGraphic"/> 并播放连线绘制动画。
    /// 属于渲染/特效部件：只负责把路径画出来，不含任何玩法判断与 UI 状态。
    /// </summary>
    public class LinkLineDrawer : MonoBehaviour
    {
        [SerializeField] private Color _color = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private float _thickness = 10f;
        [SerializeField] private float _duration = 0.22f;
        [SerializeField] private bool _rainbow = true;
        [SerializeField] private float _startHue = 0.08f;   // 起始：柔和桃金
        [SerializeField] private float _endHue = 0.85f;     // 结束：蓝紫
        [SerializeField] private float _saturation = 0.8f;  // 彩虹饱和度（越低越柔和）

        private LinkLineGraphic _lineGraphic;

        public float Duration => _duration;
        public float Thickness => _thickness;
        public Color Color => _color;

        /// <summary>是否使用彩虹渐变连线。</summary>
        public bool Rainbow => _rainbow;

        /// <summary>确保连线图形对象存在。</summary>
        public void EnsureGraphic(Transform parent)
        {
            if (_lineGraphic != null)
            {
                return;
            }

            GameObject go = new GameObject("LinkLine", typeof(RectTransform), typeof(CanvasRenderer), typeof(LinkLineGraphic));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            // 锚点/枢轴与节点坐标系统一（均为父容器中心 0.5），使 GetCellCenter 返回的坐标直接对齐节点
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(4096f, 4096f);

            _lineGraphic = go.GetComponent<LinkLineGraphic>();
            _lineGraphic.color = _color;
            _lineGraphic.Thickness = _thickness;
            _lineGraphic.Rainbow = _rainbow;
            _lineGraphic.StartHue = _startHue;
            _lineGraphic.EndHue = _endHue;
            _lineGraphic.RainbowSaturation = _saturation;
            _lineGraphic.raycastTarget = false;
            _lineGraphic.Hide();
        }

        /// <summary>显示沿路径的连线并播放渐进动画；返回协程。</summary>
        public IEnumerator PlayLineAnimation(IReadOnlyList<Vector2Int> path, BoardLayout layout)
        {
            EnsureGraphic(transform);

            List<Vector2> points = new List<Vector2>(path.Count);
            for (int i = 0; i < path.Count; i++)
            {
                points.Add(layout.GetCellCenter(path[i].x, path[i].y));
            }

            _lineGraphic.color = _color;
            _lineGraphic.Thickness = _thickness;
            _lineGraphic.Rainbow = _rainbow;
            _lineGraphic.StartHue = _startHue;
            _lineGraphic.EndHue = _endHue;
            _lineGraphic.RainbowSaturation = _saturation;
            _lineGraphic.Show(points);
            _lineGraphic.transform.SetAsLastSibling();

            _lineGraphic.SetProgress(0f);

            float t = 0f;
            while (t < _duration)
            {
                t += Time.deltaTime;
                _lineGraphic.SetProgress(t / _duration);
                yield return null;
            }

            _lineGraphic.SetProgress(1f);
            yield return new WaitForSeconds(0.08f);
            Hide();
        }

        public void Hide()
        {
            if (_lineGraphic != null)
            {
                _lineGraphic.Hide();
            }
        }
    }
}