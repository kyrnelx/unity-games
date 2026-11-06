using UnityEngine;
using UnityEngine.UI;

namespace Kyrnel.UniKit
{
    /// <summary>
    /// 【通俗白话】新手引导的挖洞遮罩，负责把目标按钮露出来，同时让玩家只能点到目标，点别的地方没反应。
    /// 【专业原理】基于 UGUI 顶点重建与事件过滤机制的挖洞遮罩。
    /// 1. 继承 MaskableGraphic：接入 Canvas 渲染管线，支持重写 OnPopulateMesh 自定义网格，并支持父容器裁剪。
    /// 2. 实现 ICanvasRaycastFilter：重写射线检测判定，实现特定区域的事件穿透与全屏阻挡。
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class GuideHollowMask : MaskableGraphic, ICanvasRaycastFilter
    {
        // 目标 UI 的 RectTransform（要镂空露出来的按钮）
        private RectTransform _target;

        // 【通俗白话】洞的几何中心坐标（X, Y），表示洞在当前遮罩的哪个位置
        // 【专业原理】镂空区域在当前 Mask 本地坐标系（Local Space）下的中心点偏移向量
        private Vector2 _center;

        // 【通俗白话】洞的长和宽
        // 【专业原理】镂空包围盒的实际像素宽高（SizeDelta）
        private Vector2 _size;

        /// <summary>
        /// 外部调用入口：动态指定引导要高亮的目标
        /// </summary>
        public void SetTarget(RectTransform target)
        {
            _target = target;
            RefreshHole();
        }

        /// <summary>
        /// 计算镂空范围并标记脏标记
        /// </summary>
        public void RefreshHole()
        {
            // 【通俗白话】如果没有目标，就把洞的大小重置为0（退化为全屏黑遮罩）
            if (_target == null)
            {
                _size = Vector2.zero;
                SetVerticesDirty();
                return;
            }

            // 【通俗白话】拿到目标按钮在游戏世界里的 4 个角（左下、左上、右上、右下）
            // 【专业原理】获取目标 RectTransform 在世界空间中的四角坐标（世界空间绝对包围盒）
            Vector3[] worldCorners = new Vector3[4];
            _target.GetWorldCorners(worldCorners);

            // 【通俗白话】判断画布类型，如果不是全屏覆盖（Overlay），就需要指定事件相机配合计算
            // 【专业原理】Overlay 模式不需要事件相机（Camera 为 null），Camera/World 模式需使用对应绑定的相机投射
            Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

            // 【通俗白话】跨层级坐标转换：把目标按钮的世界坐标，转换成“我这个遮罩自己坐标系下”的左下角与右上角坐标
            // 【专业原理】通过 RectTransformUtility 进行两步投影：WorldSpace -> ScreenSpace -> LocalSpace，
            // 解决引导遮罩与目标 UI 处于不同 Canvas、不同父节点层级导致的坐标系不一致问题。
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, RectTransformUtility.WorldToScreenPoint(cam, worldCorners[0]), cam, out Vector2 localMin);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, RectTransformUtility.WorldToScreenPoint(cam, worldCorners[2]), cam, out Vector2 localMax);

            // 【通俗白话】求平均数算中点：（左下角 + 右上角）÷ 2 = 洞的正中心点坐标
            // 【专业原理】计算包围盒的几何中心：Center = (Min + Max) * 0.5f
            _center = (localMin + localMax) * 0.5f;

            // 【通俗白话】算宽度和高度：右上角 X 减 左下角 X = 宽度，右上角 Y 减 左下角 Y = 高度
            // 【专业原理】计算本地包围盒尺寸：Size = (|Max.x - Min.x|, |Max.y - Min.y|)
            _size = new Vector2(Mathf.Abs(localMax.x - localMin.x), Mathf.Abs(localMax.y - localMin.y));

            // 【通俗白话】通知 Unity：“洞的位置变了，赶紧重新画一下界面！”
            // 【专业原理】标记顶点数据脏标记（Dirty Flag），通知 Canvas 将本组件加入顶点重建队列（Rebuild），
            // 会在当前帧的渲染前安全触发 OnPopulateMesh。
            SetVerticesDirty();
        }

        /// <summary>
        /// UGUI 网格构建核心函数
        /// </summary>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            // 【通俗白话】清空 UGUI 原本画的默认实心大矩形
            // 【专业原理】重置顶点缓冲区与三角面索引缓存，准备手动填充自定义顶点数据
            vh.Clear();

            // selfRect 就是整个手机屏幕（遮罩自身）的外边界
            Rect selfRect = rectTransform.rect;

            // 如果没有目标，直接画一个铺满全屏的实心遮罩
            if (_size == Vector2.zero)
            {
                AddQuad(vh, selfRect.min, selfRect.max);
                return;
            }

            // 【通俗白话】从中心点往外推，算出中间那个“洞”的左下角与右上角边缘
            // 【专业原理】基于中心点和尺寸，推导挖空区域在本地坐标系下的 Min/Max 范围
            Vector2 holeMin = _center - _size * 0.5f;
            Vector2 holeMax = _center + _size * 0.5f;

            // ========================【核心网格镂空算法】========================
            // 【通俗白话】
            // 我们不在中间画黑布，而是在中间那个洞的四周，动态拼出“左、右、上、下” 4 块黑色矩形把四周盖住。
            // 中间因为根本没有生成任何顶点和三角面片，底层的按钮自然就露出来了！
            //
            // 【专业原理】
            // 采用 4-Quad 分割算法规避复杂几何布尔运算。以挖空区域为边界，将全屏遮罩划分为 4 个独立的四边形图元：
            // 1. 左侧四边形：X 从屏幕最左 (selfRect.xMin) 到 洞左沿 (holeMin.x)
            AddQuad(vh, new Vector2(selfRect.xMin, selfRect.yMin), new Vector2(holeMin.x, selfRect.yMax));

            // 2. 右侧四边形：X 从洞右沿 (holeMax.x) 到 屏幕最右 (selfRect.xMax)
            AddQuad(vh, new Vector2(holeMax.x, selfRect.yMin), new Vector2(selfRect.xMax, selfRect.yMax));

            // 3. 上方四边形：Y 从洞上沿 (holeMax.y) 到 屏幕顶沿 (selfRect.yMax)，夹在洞的左右边界内
            AddQuad(vh, new Vector2(holeMin.x, holeMax.y), new Vector2(holeMax.x, selfRect.yMax));

            // 4. 下方四边形：Y 从屏幕底沿 (selfRect.yMin) 到 洞下沿 (holeMin.y)，夹在洞的左右边界内
            AddQuad(vh, new Vector2(holeMin.x, selfRect.yMin), new Vector2(holeMax.x, holeMin.y));
        }

        /// <summary>
        /// 辅助函数：向 VertexHelper 里写入一个矩形（由 4 个顶点和 2 个三角形面片组成）
        /// </summary>
        private void AddQuad(VertexHelper vh, Vector2 min, Vector2 max)
        {
            // 【通俗白话】拿到排队叫号：当前画板上已经钉了多少个点了？
            // 【专业原理】获取当前顶点缓冲区的基准偏移量（Base Vertex Index），避免多边形索引错位
            int startIndex = vh.currentVertCount;

            // 【通俗白话】拿一张纯净的白纸模板，填入我们在 Inspector 里调的半透明颜色
            // 【专业原理】使用 UIVertex 默认静态模板（已内置法线、切线与空UV），只修改需要的属性
            UIVertex vert = UIVertex.simpleVert;
            vert.color = color;

            // 【通俗白话】在画板上钉 4 个点（左下、左上、右上、右下）
            // 【专业原理】按固定顺序向顶点流添加 4 个角点的位置数据
            vert.position = new Vector3(min.x, min.y);
            vh.AddVert(vert); // 顶点索引: startIndex + 0 (左下)

            vert.position = new Vector3(min.x, max.y);
            vh.AddVert(vert); // 顶点索引: startIndex + 1 (左上)

            vert.position = new Vector3(max.x, max.y);
            vh.AddVert(vert); // 顶点索引: startIndex + 2 (右上)

            vert.position = new Vector3(max.x, min.y);
            vh.AddVert(vert); // 顶点索引: startIndex + 3 (右下)

            // 【通俗白话】显卡只会画三角形。我们拉毛线把这 4 个点连成 2 个三角形，拼成一个矩形
            // 【专业原理】遵循顺时针绕序（Clockwise Winding）提交三角形索引（Index Buffer），
            // 三角形 1: (0, 1, 2)  三角形 2: (2, 3, 0)
            vh.AddTriangle(startIndex + 0, startIndex + 1, startIndex + 2);
            vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex + 0);
        }

        /// <summary>
        /// 核心交互：射线检测过滤（决定点击穿透还是被遮罩吃掉）
        /// </summary>
        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            // 没有目标时，阻挡全屏所有点击
            if (_target == null) return true;

            // 【通俗白话】判断玩家手指点击的那个屏幕像素点，是不是点在了目标按钮的矩形框框里面
            // 【专业原理】使用 UGUI 原生碰撞工具判定屏幕坐标 sp 是否处于目标 RectTransform 包围盒内
            bool isInHole = RectTransformUtility.RectangleContainsScreenPoint(_target, sp, eventCamera);

            // 【通俗白话】
            // 如果点在洞里 (isInHole == true) -> 返回 false，告诉引擎“我这个遮罩不吃这个点击”，点击直接穿透给底下的按钮！
            // 如果点在洞外 (isInHole == false) -> 返回 true，告诉引擎“我收下了这个点击”，遮罩吃掉点击，四周被锁死防误触！
            //
            // 【专业原理】
            // ICanvasRaycastFilter 契约逻辑：
            // - 返回 false：将该组件从 EventSystem 的命中列表中剔除，射线继续向下穿透检测底层 Graphic。
            // - 返回 true：本组件作为 RaycastTarget 拦截事件，阻断射线继续投递。
            return !isInHole;
        }
    }
}