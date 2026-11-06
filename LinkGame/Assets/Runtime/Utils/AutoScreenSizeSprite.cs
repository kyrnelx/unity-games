using UnityEngine;

namespace Kyrnel
{
    public class AutoScreenSizeSprite : MonoBehaviour
    {
        [SerializeField] private Camera _targetCamera;
        [SerializeField] private float _distanceFromCamera = 20f;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        void Start()
        {
            _spriteRenderer ??= GetComponent<SpriteRenderer>();
            _targetCamera ??= Camera.main;
            ScaleSpriteToFillScreen();
        }

        public void ScaleSpriteToFillScreen()
        {
            // ========== 第 0 步：防御检查（避免空引用报错） ==========
            // 如果没拖拽赋值，就自动去场景里找；如果还是没有，直接退出。
            if (_spriteRenderer == null || _targetCamera == null)
                return;

            // ========== 第 1 步：放置物体——跟着摄像机走 ==========
            // 目的：把 Sprite 放在摄像机正前方“D”距离处。
            // 直觉理解：四棱锥从尖点往外扩散，我们要把物体放在扩散路径的“D”位置截面上。
            transform.position = _targetCamera.transform.position
                                 + _targetCamera.transform.forward * _distanceFromCamera;

            // ========== 第 2 步：获取 Sprite 的“物理世界尺寸” ==========
            // 注意：SpriteRenderer 的 bounds 单位是“米”（Unity 世界单位）。
            // 这个尺寸由图片导入时的 "Pixels Per Unit (PPU)" 决定。
            float spriteWidth = _spriteRenderer.sprite.bounds.size.x;
            float spriteHeight = _spriteRenderer.sprite.bounds.size.y;

            // ========== 第 3 步：计算“D距离处”视锥截面的垂直高度（核心几何） ==========
            // 公式：cameraHeight = 2 * D * Tan(FOV / 2)
            // 推导：半高 / D = Tan(半角)  =>  全高 = 2 * D * Tan(半角)
            // 注意：Mathf.Tan 需要弧度，所以要把角度乘以 Mathf.Deg2Rad 转弧度。
            float halfFovRad = _targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float cameraHeight = 2.0f * _distanceFromCamera * Mathf.Tan(halfFovRad);

            // ========== 第 4 步：计算“D距离处”视锥截面的水平宽度 ==========
            // 直觉：屏幕是个矩形，宽度 = 高度 * (屏幕宽 / 屏幕高)
            // 例：高 10米，宽高比 1.777（16:9），则宽为 17.77米。
            float cameraWidth = cameraHeight * _targetCamera.aspect;

            // ========== 第 5 步：计算缩放倍数（把 Sprite 拉满屏幕） ==========
            // 原理：缩放倍数 = 目标世界尺寸 / 原始世界尺寸
            // 举例：如果屏幕宽 20米，Sprite 原始宽 2米，放大倍数就是 10倍。
            Vector3 newScale = transform.localScale;
            newScale.x = cameraWidth / spriteWidth; // 横向拉伸适配
            newScale.y = cameraHeight / spriteHeight; // 纵向拉伸适配
            transform.localScale = newScale;

            // ========== 第 6 步：始终面向摄像机（Billboard 公告牌效果） ==========
            // 如果不做这一步，当摄像机旋转时，Sprite 会侧着身子，看起来像一张薄纸片。
            // LookRotation 让 Sprite 的 Z 轴始终指向摄像机方向。
            Vector3 directionToCamera = transform.position - _targetCamera.transform.position;
            transform.rotation = Quaternion.LookRotation(directionToCamera);
        }
    }
}