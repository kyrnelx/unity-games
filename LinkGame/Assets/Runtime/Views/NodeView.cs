using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Kyrnel.LinkGame
{
    /// <summary>
    /// 单个节点视图（Views 层）：负责自身的贴图、选中反馈与生成/移动/消除动画。
    /// 不含任何游戏规则，点击只回调给外部（由 GameManager 转发给 LinkManager 判定）。
    /// </summary>
    public class NodeView : MonoBehaviour
    {
        /// <summary>所在棋盘坐标（由 NodeSpawner 维护）。</summary>
        public Vector2Int Position { get; set; }

        /// <summary>当前图案类型（用于外部快速判断）。</summary>
        public NodeType Type { get; set; }

        [SerializeField] private Image _image;
        [SerializeField] private Button _button;

        private Action<NodeView> _onClickAction;
        private readonly Color _normalColor = Color.white;
        private readonly Color _selectedColor = new Color(0.75f, 0.9f, 1f, 1f);

        /// <summary>动画是否可播放：失活对象上无法启动协程，此时直接落到动画终态。</summary>
        private bool CanAnimate => isActiveAndEnabled;

        /// <summary>初始化节点：绑定点击回调并应用图案。</summary>
        public void Initialize(Action<NodeView> onClickAction, NodeType nodeType, Sprite nodeSprite)
        {
            _onClickAction = onClickAction;

            if (_button != null)
            {
                _button.onClick.AddListener(OnClick);
            }

            Type = nodeType;

            if (_image != null)
            {
                _image.sprite = nodeSprite;
                _image.color = _normalColor;
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
            }
        }

        private void OnClick()
        {
            _onClickAction?.Invoke(this);
        }

        /// <summary>设置选中状态，通过改变图片颜色提供视觉反馈。</summary>
        public void SetSelected(bool selected)
        {
            if (_image != null)
            {
                _image.color = selected ? _selectedColor : _normalColor;
            }
        }

        /// <summary>应用新的类型与贴图（用于重排）。</summary>
        public void ApplyType(NodeType type, Sprite sprite)
        {
            Type = type;

            if (_image != null)
            {
                _image.sprite = sprite;
            }
        }

        /// <summary>生成动画：从 0 缩放弹出，可带延迟用于错峰。</summary>
        public void PlaySpawnAnimation(float duration, float delay)
        {
            if (!CanAnimate)
            {
                // 失活对象无法启动协程，直接落到终态（保持可见与正常缩放）
                transform.localScale = Vector3.one;
                return;
            }

            StartCoroutine(SpawnRoutine(duration, delay));
        }

        private IEnumerator SpawnRoutine(float duration, float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            transform.localScale = Vector3.zero;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                transform.localScale = Vector3.one * EaseOutBack(p);
                yield return null;
            }

            transform.localScale = Vector3.one;
        }

        /// <summary>滑动补位动画：从当前位置平滑移动到目标单元格位置。</summary>
        public void PlayMoveAnimation(Vector2 destination, float duration, Action onComplete)
        {
            if (!CanAnimate)
            {
                // 失活对象无法启动协程，直接落到终态
                RectTransform rect = transform as RectTransform;
                if (rect != null)
                {
                    rect.anchoredPosition = destination;
                }

                onComplete?.Invoke();
                return;
            }

            StartCoroutine(MoveRoutine(destination, duration, onComplete));
        }

        private IEnumerator MoveRoutine(Vector2 destination, float duration, Action onComplete)
        {
            RectTransform rect = transform as RectTransform;
            Vector2 start = rect != null ? rect.anchoredPosition : Vector2.zero;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                float eased = 1f - Mathf.Pow(1f - p, 3f); // EaseOutCubic

                if (rect != null)
                {
                    rect.anchoredPosition = Vector2.Lerp(start, destination, eased);
                }

                yield return null;
            }

            if (rect != null)
            {
                rect.anchoredPosition = destination;
            }

            onComplete?.Invoke();
        }

        /// <summary>消除动画：缩小消失，完成后回调。</summary>
        public void PlayClearAnimation(float duration, Action onComplete)
        {
            if (!CanAnimate)
            {
                // 失活对象无法启动协程，直接落到终态
                ApplyClearedVisual();
                onComplete?.Invoke();
                gameObject.SetActive(false);
                return;
            }

            StartCoroutine(ClearRoutine(duration, onComplete));
        }

        private IEnumerator ClearRoutine(float duration, Action onComplete)
        {
            float t = 0f;
            Vector3 startScale = transform.localScale;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                transform.localScale = startScale * (1f - EaseInCubic(p));
                yield return null;
            }

            ApplyClearedVisual();

            // 先回调再禁用 GameObject，避免协程被停止导致回调丢失
            onComplete?.Invoke();

            // 禁用整个 GameObject，彻底阻止 Canvas 延迟重建把旧画面重新画出来
            gameObject.SetActive(false);
        }

        /// <summary>消除后的最终表现：缩放归零并彻底隐藏渲染部件，避免微信小游戏上 Destroy 延迟导致残影。</summary>
        private void ApplyClearedVisual()
        {
            transform.localScale = Vector3.zero;

            if (_image != null)
            {
                _image.enabled = false;
            }

            if (_button != null)
            {
                _button.enabled = false;
            }
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private static float EaseInCubic(float t)
        {
            return t * t * t;
        }
    }
}
