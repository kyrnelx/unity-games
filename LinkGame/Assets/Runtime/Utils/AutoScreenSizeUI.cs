using UnityEngine;
using UnityEngine.UI;

namespace Kyrnel
{
    public class AutoScreenSizeUI : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private CanvasScaler _canvasScaler;
        [SerializeField] private RectTransform _rectTransform;

        private void Awake()
        {
            _canvas ??= FindObjectOfType<Canvas>();
            _canvasScaler ??= FindObjectOfType<CanvasScaler>();
            _rectTransform ??= GetComponent<RectTransform>();
        }

        private void Start()
        {
            // 获取屏幕实际宽高
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            // 获取设计宽高
            float refScreenWidth = _canvasScaler.referenceResolution.x;
            float refScreenHeight = _canvasScaler.referenceResolution.y;
            // 获取缩放系数
            float canvasScaleFactor = _canvasScaler.scaleFactor;
            // Canvas实际宽高 = 实际宽高 / 缩放系数
            float actualScreenWidth = screenWidth / canvasScaleFactor;
            float actualScreenHeight = screenHeight / canvasScaleFactor;
            // 设置当前对象为参考宽高
            _rectTransform.sizeDelta = new Vector2(refScreenWidth, refScreenHeight);
            // 调整的Scale = 实际宽高 / 参考宽高
            float xScale = actualScreenWidth / refScreenWidth;
            float yScale = actualScreenHeight / refScreenHeight;
            float uniformScale = Mathf.Max(xScale, yScale);
            transform.localScale = new Vector3(xScale, yScale, transform.localScale.z);
        }

        /*该脚本试图让当前 UI 元素（例如一张背景图）在屏幕尺寸变化时始终保持与屏幕大小一致，具体步骤为：

        获取屏幕像素尺寸（Screen.width/height）。

        获取 Canvas 缩放因子（canvasScaler.scaleFactor），反推 Canvas 逻辑像素尺寸。

        固定元素尺寸为参考分辨率（sizeDelta）。

        计算并应用缩放，使元素实际大小等于 Canvas 逻辑尺寸，从而实现“全屏适配”。*/
    }
}