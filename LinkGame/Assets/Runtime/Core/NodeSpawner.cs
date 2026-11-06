 using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kyrnel.LinkGame
{
    /// <summary>
    /// 节点视图工厂（棋盘表现基础件，非界面视图）：负责实例化、定位、重排与销毁 <see cref="NodeView"/>。
    /// 充当"节点实例 + 棋盘布局"的装配者，并把 NodeSize/Spacing 等布局参数真正作用到节点实例上；
    /// 不包含任何游戏规则，只维护"棋盘坐标 → 节点视图"的映射与布局计算。
    /// </summary>
    public class NodeSpawner : MonoBehaviour
    {
        [SerializeField] private NodeView _nodePrefab;
        [SerializeField] private Transform _nodeParent;
        [SerializeField] private NodeConfigSO  _nodeConfigSo;

        [Header("尺寸与布局")]
        [Tooltip("节点边长（像素），会直接应用到节点实例的 RectTransform 尺寸。")]
        [SerializeField] private float _nodeSize = 100f;

        [Tooltip("相邻节点之间的间距，不含节点自身尺寸（格子步长 = NodeSize + Spacing）。")]
        [SerializeField] private float _spacing = 10f;

        [Tooltip("整盘相对容器中心的 X 方向微调偏移。")]
        [SerializeField] private float _xPadding;

        [Tooltip("整盘相对容器中心的 Y 方向微调偏移。")]
        [SerializeField] private float _yPadding;

        [Header("动画")]
        [Tooltip("节点生成动画时长（秒）。")]
        [SerializeField] private float _spawnDuration = 0.3f;

        private NodeView[,] _nodeMap;
        private BoardLayout _layout;

        /// <summary>节点父容器（连线图形也挂载其下）；未指定时使用本组件所在节点。</summary>
        public Transform NodeParent => _nodeParent != null ? _nodeParent : transform;

        /// <summary>棋盘布局计算器。</summary>
        public BoardLayout Layout => _layout;

        /// <summary>创建节点视图容器并初始化布局。</summary>
        public void Initialize(int width, int height)
        {
            _layout = new BoardLayout(_nodeSize, _spacing, _xPadding, _yPadding);
            // 传入棋盘行列，使整盘以容器锚点 (0,0) 为中心对称展开（容器锚点需指向屏幕中心）
            _layout.Initialize(width, height);
            _nodeMap = new NodeView[width, height];
        }

        /// <summary>实例化一个节点并初始化，返回该节点。</summary>
        public NodeView SpawnNode(Action<NodeView> onClickAction, NodeType type, int x, int y)
        {
            if (_nodeMap == null)
            {
                Debug.LogError("[NodeSpawner] 未初始化棋盘，请先调用 Initialize。");
                return null;
            }

            if (_nodePrefab == null)
            {
                Debug.LogError("[NodeSpawner] 未设置 _nodePrefab，无法创建节点。");
                return null;
            }

            NodeView node = Instantiate(_nodePrefab, NodeParent);
            node.Position = new Vector2Int(x, y);
            _nodeMap[x, y] = node;

            // 关键：把 Inspector 的 NodeSize 真正应用到节点实例上。
            // 之前 _nodeSize 只参与布局步长计算，节点尺寸一直沿用预制体的 75×75，
            // 于是改 NodeSize 只改变格子间距、节点本身不缩放（表现为"不生效"）。
            RectTransform rect = node.GetComponent<RectTransform>();
            if (rect != null)
            {
                // 锚点/轴心统一到中心，使 anchoredPosition 即节点中心、尺寸以中心对齐
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(_layout.NodeSize, _layout.NodeSize);
                rect.anchoredPosition = _layout.GetCellBottomLeft(x, y);
            }

            Sprite sprite = GetSprite(type);
            node.Initialize(onClickAction, type, sprite);
            node.gameObject.name = $"Node_{x}_{y}_{type}";

            // 创建后立即播放生成动画
            node.PlaySpawnAnimation(_spawnDuration, 0f);

            return node;
        }

        /// <summary>按坐标获取节点视图，越界或已清除返回 null。</summary>
        public NodeView GetNode(int x, int y)
        {
            if (_nodeMap == null || x < 0 || x >= _nodeMap.GetLength(0) || y < 0 || y >= _nodeMap.GetLength(1))
            {
                return null;
            }

            return _nodeMap[x, y];
        }

        /// <summary>取图案贴图；未配置 NodeConfigSO 时返回 null（节点仍会创建，只是没有贴图）。</summary>
        private Sprite GetSprite(NodeType type)
        {
            return _nodeConfigSo != null ? _nodeConfigSo.GetSprite(type) : null;
        }

        /// <summary>
        /// 按棋盘补位移动记录播放滑动动画：将节点平滑移动到目标格子，并更新节点坐标与映射。
        /// 所有移动并行播放，全部完成后回调 <paramref name="onComplete"/>。
        /// 可直接被外部协程 yield 等待全部播放完成。
        /// </summary>
        public IEnumerator ApplyMoves(List<BoardMove> moves, float duration, Action onComplete)
        {
            if (moves == null || moves.Count == 0)
            {
                onComplete?.Invoke();
                yield break;
            }

            // 预计算阶段：先快照所有参与移动的节点引用、目标坐标与目标位置，
            // 避免交叉移动（如 A→B、B→A）时逐条更新 _nodeMap 相互覆盖导致节点丢失。
            int validCount = 0;
            NodeView[] movingNodes = new NodeView[moves.Count];
            Vector2Int[] targetCells = new Vector2Int[moves.Count];
            Vector2[] targetPositions = new Vector2[moves.Count];

            for (int i = 0; i < moves.Count; i++)
            {
                BoardMove move = moves[i];
                NodeView node = GetNode(move.From.x, move.From.y);
                if (node == null)
                {
                    continue;
                }

                movingNodes[validCount] = node;
                targetCells[validCount] = move.To;
                targetPositions[validCount] = _layout.GetCellCenter(move.To.x, move.To.y);
                validCount++;
            }

            if (validCount == 0)
            {
                onComplete?.Invoke();
                yield break;
            }

            // 先统一清空所有旧位置映射（利用快照中的节点当前位置），再写入新位置映射，避免交叉覆盖
            for (int i = 0; i < validCount; i++)
            {
                Vector2Int from = movingNodes[i].Position;
                _nodeMap[from.x, from.y] = null;
            }

            for (int i = 0; i < validCount; i++)
            {
                NodeView node = movingNodes[i];
                Vector2Int to = targetCells[i];
                _nodeMap[to.x, to.y] = node;
                node.Position = to;
            }

            // 并行播放所有滑动动画
            int pending = validCount;
            for (int i = 0; i < validCount; i++)
            {
                NodeView node = movingNodes[i];
                node.PlayMoveAnimation(targetPositions[i], duration, () => pending--);
            }

            // 等待所有滑动动画播放完成
            while (pending > 0)
            {
                yield return null;
            }

            onComplete?.Invoke();
        }

        /// <summary>将棋盘模型中的最新类型同步到节点视图（用于重排后刷新贴图）。</summary>
        public void SyncBoardToView(LinkBoard board)
        {
            if (_nodeMap == null || board == null)
            {
                return;
            }

            for (int x = 0; x < _nodeMap.GetLength(0); x++)
            {
                for (int y = 0; y < _nodeMap.GetLength(1); y++)
                {
                    NodeView node = _nodeMap[x, y];
                    if (node == null)
                    {
                        continue;
                    }

                    NodeType type = board.GetTypeAt(new Vector2Int(x, y));
                    if (node.Type != type)
                    {
                        node.ApplyType(type, GetSprite(type));
                    }
                }
            }
        }

        /// <summary>清除指定节点：从棋盘移除并销毁视图。</summary>
        public void ClearNode(NodeView node)
        {
            if (node == null)
            {
                return;
            }

            Vector2Int pos = node.Position;
            if (_nodeMap != null
                && pos.x >= 0 && pos.x < _nodeMap.GetLength(0)
                && pos.y >= 0 && pos.y < _nodeMap.GetLength(1)
                && _nodeMap[pos.x, pos.y] == node)
            {
                _nodeMap[pos.x, pos.y] = null;
            }

            // 先彻底隐藏再销毁，避免微信小游戏上 Destroy 延迟导致残影
            node.gameObject.SetActive(false);
            Destroy(node.gameObject);
        }

        /// <summary>清除整个棋盘的所有节点视图。</summary>
        public void ClearBoard()
        {
            if (_nodeMap == null)
            {
                return;
            }

            for (int x = 0; x < _nodeMap.GetLength(0); x++)
            {
                for (int y = 0; y < _nodeMap.GetLength(1); y++)
                {
                    NodeView node = _nodeMap[x, y];
                    if (node != null)
                    {
                        Destroy(node.gameObject);
                    }
                }
            }

            _nodeMap = null;
            _layout = null;
        }
    }
}
