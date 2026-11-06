using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kyrnel.LinkGame
{
    /// <summary>
    /// 场景入口与流程编排（组合根）：
    /// 把 Managers 层的玩法服务（<see cref="LinkManager"/>、<see cref="AudioManager"/>）、
    /// 棋盘表现基础件（<see cref="NodeSpawner"/>、<see cref="LinkLineDrawer"/>）与 Views 层的界面视图（GameView、NodeView）
    /// 装配起来，只负责输入分流、动画协程与关卡推进，玩法规则本身全部位于 <see cref="LinkManager"/>。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private float _clearDuration = 0.15f;
        [SerializeField] private float _fillDuration = 0.25f;

        [Header("视图")]
        [SerializeField] private NodeSpawner _nodeSpawner;
        [SerializeField] private LinkLineDrawer lineDrawer;
        [FormerlySerializedAs("_hud")]
        [SerializeField] private GameView _view;

        [Header("服务")]
        [SerializeField] private AudioManager _audio;
        [Tooltip("平台服务（激励视频广告）。未指定时会在 Awake 自动查找；缺失则道具跳过广告直接使用。")]
        [SerializeField] private PlatformManager _platform;

        private LinkManager _link;

        private bool _isBusy;
        private Coroutine _clearRoutine;
        private Coroutine _spawnRoutine;

        /// <summary>当前本关得分。</summary>
        public int Score => _link != null ? _link.Score : 0;

        /// <summary>当前连击数（连续成功消除的次数）。</summary>
        public int Combo => _link != null ? _link.Combo : 0;

        /// <summary>积分变化事件，参数为当前总分。</summary>
        public event Action<int> OnScoreChanged;

        /// <summary>是否处于可操作状态（有棋盘且还有剩余节点）。</summary>
        private bool CanPlay => _link != null && _link.CanPlay;

        private void Awake()
        {
            ResolveReferences();

            _link = new LinkManager();
            SubscribeLinkEvents(true);

            _link.LoadProgress();
            if (_view != null)
            {
                _view.SetLevel(_link.CurrentLevel);
                _view.SetTotalScore(_link.TotalScore);
            }

            if (_audio != null)
            {
                _audio.Initialize();
            }

            StartGame();
        }

        private void OnDestroy()
        {
            SubscribeLinkEvents(false);
        }

        /// <summary>
        /// 补齐 Inspector 里漏拖的引用（组件换到别的 GameObject 后原引用会变成 None）。
        /// 查找包含失活对象，因此挂在默认隐藏 UI 上的 View 也能找到。
        /// </summary>
        private void ResolveReferences()
        {
            if (_nodeSpawner == null)
            {
                _nodeSpawner = FindSceneComponent<NodeSpawner>();
                if (_nodeSpawner != null)
                {
                    this.Log($"自动补齐引用：NodeSpawner @ {_nodeSpawner.gameObject.name}");
                }
            }

            if (lineDrawer == null)
            {
                lineDrawer = FindSceneComponent<LinkLineDrawer>();
                if (lineDrawer != null)
                {
                    this.Log($"自动补齐引用：LinkLineDrawer @ {lineDrawer.gameObject.name}");
                }
            }

            if (_view == null)
            {
                _view = FindSceneComponent<GameView>();
                if (_view != null)
                {
                    this.Log($"自动补齐引用：GameView @ {_view.gameObject.name}");
                }
            }

            if (_audio == null)
            {
                _audio = FindSceneComponent<AudioManager>();
                if (_audio != null)
                {
                    this.Log($"自动补齐引用：AudioManager @ {_audio.gameObject.name}");
                }
            }

            if (_platform == null)
            {
                _platform = FindSceneComponent<PlatformManager>();
                if (_platform != null)
                {
                    this.Log($"自动补齐引用：PlatformManager @ {_platform.gameObject.name}");
                }
                else
                {
                    this.Warning("未找到 PlatformManager，道具将跳过广告直接使用。");
                }
            }

            if (_nodeSpawner == null)
            {
                this.Error("未找到 NodeSpawner，棋盘无法铺盘！请检查场景中是否挂载了该组件。");
            }

            if (_view == null)
            {
                this.Warning("未找到 GameView，界面数值不会刷新。");
            }
        }

        /// <summary>
        /// 查找场景中的第一个 T 组件，<b>包含失活对象</b>。
        /// Unity 自带的 FindObjectOfType 只返回激活对象，而 View 往往挂在默认隐藏的 UI 上，
        /// 因此这里用 Resources.FindObjectsOfTypeAll 并过滤掉预制体资源（它们不属于任何场景）。
        /// </summary>
        private static T FindSceneComponent<T>() where T : Component
        {
            T[] all = Resources.FindObjectsOfTypeAll<T>();
            for (int i = 0; i < all.Length; i++)
            {
                T item = all[i];
                if (item != null && item.gameObject.scene.IsValid())
                {
                    return item;
                }
            }

            return null;
        }

        /// <summary>订阅/退订玩法服务事件，把逻辑层的变化转成视图刷新。</summary>
        private void SubscribeLinkEvents(bool subscribe)
        {
            if (_link == null)
            {
                return;
            }

            if (subscribe)
            {
                _link.OnScoreChanged += HandleScoreChanged;
                _link.OnLevelChanged += HandleLevelChanged;
                _link.OnTotalScoreChanged += HandleTotalScoreChanged;
            }
            else
            {
                _link.OnScoreChanged -= HandleScoreChanged;
                _link.OnLevelChanged -= HandleLevelChanged;
                _link.OnTotalScoreChanged -= HandleTotalScoreChanged;
            }
        }

        private void HandleScoreChanged(int score)
        {
            if (_view != null)
            {
                _view.SetScore(score);
            }

            OnScoreChanged?.Invoke(score);
        }

        private void HandleLevelChanged(int level)
        {
            if (_view != null)
            {
                _view.SetLevel(level);
            }
        }

        private void HandleTotalScoreChanged(int totalScore)
        {
            if (_view != null)
            {
                _view.SetTotalScore(totalScore);
            }
        }

        /// <summary>开始新的一局：从玩法服务取开局数据，再由视图层铺盘。</summary>
        public void StartGame()
        {
            StopActiveRoutines();
            ClearBoard();

            LevelSetup setup = _link.StartLevel();
            if (!setup.IsValid)
            {
                return;
            }

            if (_nodeSpawner != null)
            {
                _nodeSpawner.Initialize(setup.Width, setup.Height);
            }

            if (lineDrawer != null)
            {
                Transform parent = _nodeSpawner != null ? _nodeSpawner.NodeParent : transform;
                lineDrawer.EnsureGraphic(parent);
            }

            _isBusy = false;
            _spawnRoutine = StartCoroutine(SpawnBoardRoutine(setup));
        }

        /// <summary>
        /// 边生成边播放：按开局数据给出的生成顺序逐个创建节点，
        /// 每创建一个节点暂停一帧，形成不同的渐进生成特效。
        /// </summary>
        private IEnumerator SpawnBoardRoutine(LevelSetup setup)
        {
            _isBusy = true;

            for (int i = 0; i < setup.SpawnOrder.Count; i++)
            {
                Vector2Int position = setup.SpawnOrder[i];
                if (_nodeSpawner != null)
                {
                    _nodeSpawner.SpawnNode(OnClickNode, setup.GetTypeAt(position), position.x, position.y);
                }

                yield return null;
            }

            _isBusy = false;
            _spawnRoutine = null;

            EnsurePlayable();
        }

        /// <summary>节点点击入口：把点击转成玩法服务的一次交互，并按结果驱动视图。</summary>
        private void OnClickNode(NodeView node)
        {
            if (_isBusy || node == null)
            {
                return;
            }

            if (_audio != null)
            {
                _audio.PlayClickSound();
            }

            LinkResult result = _link.Click(node.Position);
            ApplySelectionVisual(result);

            if (!result.IsMatched || _nodeSpawner == null)
            {
                return;
            }

            NodeView first = _nodeSpawner.GetNode(result.PreviousSelection.x, result.PreviousSelection.y);
            NodeView second = _nodeSpawner.GetNode(result.Position.x, result.Position.y);
            if (first == null || second == null)
            {
                return;
            }

            _clearRoutine = StartCoroutine(ClearLinkedPair(first, second, result.Path));
        }

        /// <summary>把连线结果的选中状态映射为节点高亮。</summary>
        private void ApplySelectionVisual(LinkResult result)
        {
            switch (result.Outcome)
            {
                case LinkClickOutcome.Selected:
                    SetNodeSelected(result.Position, true);
                    break;

                case LinkClickOutcome.Deselected:
                case LinkClickOutcome.Matched:
                    SetNodeSelected(result.PreviousSelection, false);
                    break;

                case LinkClickOutcome.Switched:
                    SetNodeSelected(result.PreviousSelection, false);
                    SetNodeSelected(result.Position, true);
                    break;
            }
        }

        private void SetNodeSelected(Vector2Int position, bool selected)
        {
            if (_nodeSpawner == null)
            {
                return;
            }

            NodeView node = _nodeSpawner.GetNode(position.x, position.y);
            if (node != null)
            {
                node.SetSelected(selected);
            }
        }

        /// <summary>消除流程：连线动画 → 消除动画 → 补位 → 关卡推进。</summary>
        private IEnumerator ClearLinkedPair(NodeView first, NodeView second, IReadOnlyList<Vector2Int> path)
        {
            _isBusy = true;

            if (lineDrawer != null && _nodeSpawner != null)
            {
                yield return StartCoroutine(lineDrawer.PlayLineAnimation(path, _nodeSpawner.Layout));
            }

            if (_audio != null)
            {
                _audio.PlayMatchSound();
            }

            yield return StartCoroutine(PlayClearAnimation(first, second));

            // 按本关填充方向滑动补位：玩法数据先行，再驱动视图动画
            List<BoardMove> moves = _link.ApplyFill();
            if (moves.Count > 0 && _nodeSpawner != null)
            {
                yield return StartCoroutine(PlayFillAnimation(moves));
            }

            _isBusy = false;
            _clearRoutine = null;

            if (_link.IsCleared)
            {
                this.Log("全部消除，进入下一关。");
                _link.SettleLevelAndAdvance();
                yield return new WaitForSeconds(0.35f);
                StartGame();
                yield break;
            }

            EnsurePlayable();
        }

        /// <summary>滑动补位动画：让视图层按移动记录播放滑动。</summary>
        private IEnumerator PlayFillAnimation(List<BoardMove> moves)
        {
            float duration = Mathf.Max(0.05f, _fillDuration);
            yield return _nodeSpawner.ApplyMoves(moves, duration, null);
        }

        /// <summary>消除动画：两个节点同时缩小消失后销毁，并同步棋盘模型。</summary>
        private IEnumerator PlayClearAnimation(NodeView first, NodeView second)
        {
            if (first == null || second == null)
            {
                yield break;
            }

            Vector2Int firstPosition = first.Position;
            Vector2Int secondPosition = second.Position;

            first.PlayClearAnimation(_clearDuration, null);
            second.PlayClearAnimation(_clearDuration, null);

            yield return new WaitForSeconds(_clearDuration);

            _link.ClearAt(firstPosition);
            _link.ClearAt(secondPosition);

            if (_nodeSpawner != null)
            {
                _nodeSpawner.ClearNode(first);
                _nodeSpawner.ClearNode(second);
            }
        }

        // ============ 道具入口：点击 → 播放激励视频 → 完整观看后发放道具并立即使用 ============

        /// <summary>道具-提示：观看激励视频后直接使用提示。</summary>
        public void UseHint()
        {
            RequestPropWithAd(UseHintInternal);
        }

        /// <summary>道具-洗牌：观看激励视频后直接使用洗牌。</summary>
        public void UseShuffle()
        {
            RequestPropWithAd(UseShuffleInternal);
        }

        /// <summary>道具-自动消除：观看激励视频后直接使用自动消除。</summary>
        public void UseAutoMatch()
        {
            RequestPropWithAd(UseAutoMatchInternal);
        }

        /// <summary>
        /// 道具通用流程：播放激励视频，完整观看后发放道具并立即使用。
        /// 未完整观看则不发放；未挂 PlatformManager 时跳过广告直接使用。
        /// </summary>
        private void RequestPropWithAd(Action useProp)
        {
            if (_isBusy || !CanPlay)
            {
                return;
            }

            if (_platform == null)
            {
                this.Warning("未找到 PlatformManager，跳过广告直接使用道具。");
                useProp();
                return;
            }

            _platform.OpenAd(isSuccess =>
            {
                if (!isSuccess)
                {
                    this.Log("广告未完整观看，不发放道具。");
                    return;
                }

                useProp();
            });
        }

        /// <summary>
        /// 道具实现-提示：找出一对可连接的节点并短暂高亮闪烁，帮助玩家发现可消除组合。
        /// </summary>
        private void UseHintInternal()
        {
            if (_isBusy || !CanPlay)
            {
                return;
            }

            if (!_link.TryFindHint(out Vector2Int first, out Vector2Int second))
            {
                this.Log("提示：当前无解，自动执行洗牌。");
                UseShuffleInternal();
                return;
            }

            NodeView firstNode = _nodeSpawner != null ? _nodeSpawner.GetNode(first.x, first.y) : null;
            NodeView secondNode = _nodeSpawner != null ? _nodeSpawner.GetNode(second.x, second.y) : null;
            if (firstNode == null || secondNode == null)
            {
                return;
            }

            StartCoroutine(HintRoutine(firstNode, secondNode));
        }

        private IEnumerator HintRoutine(NodeView first, NodeView second)
        {
            for (int i = 0; i < 3; i++)
            {
                first.SetSelected(true);
                second.SetSelected(true);
                yield return new WaitForSeconds(0.18f);
                first.SetSelected(false);
                second.SetSelected(false);
                yield return new WaitForSeconds(0.18f);
            }
        }

        /// <summary>道具实现-洗牌：把棋盘上所有存活节点重新随机排列，并播放滑动动画。</summary>
        private void UseShuffleInternal()
        {
            if (_isBusy || !CanPlay)
            {
                return;
            }

            StartCoroutine(ShuffleRoutine());
        }

        private IEnumerator ShuffleRoutine()
        {
            _isBusy = true;

            List<BoardMove> moves = _link.ShufflePositions();
            if (moves.Count > 0 && _nodeSpawner != null)
            {
                float duration = Mathf.Max(0.05f, _fillDuration);
                yield return _nodeSpawner.ApplyMoves(moves, duration, null);
            }

            _isBusy = false;
        }

        /// <summary>道具实现-自动消除：自动找出一对可连接节点并立即消除，效果与点击一致。</summary>
        private void UseAutoMatchInternal()
        {
            if (_isBusy || !CanPlay)
            {
                return;
            }

            if (!TryAutoMatch())
            {
                this.Log("自动消除：当前无解，尝试洗牌。");
                UseShuffleInternal();
            }
        }

        /// <summary>自动连接一对可消除节点并播放消除流程（道具-自动消除与编辑器调试共用）。</summary>
        private bool TryAutoMatch()
        {
            LinkResult result = _link.AutoMatch();
            if (!result.IsMatched)
            {
                return false;
            }

            ApplySelectionVisual(result);

            if (_nodeSpawner == null)
            {
                return false;
            }

            NodeView first = _nodeSpawner.GetNode(result.PreviousSelection.x, result.PreviousSelection.y);
            NodeView second = _nodeSpawner.GetNode(result.Position.x, result.Position.y);
            if (first == null || second == null)
            {
                return false;
            }

            _clearRoutine = StartCoroutine(ClearLinkedPair(first, second, result.Path));
            return true;
        }

        /// <summary>确保棋盘有解，无解时让玩法服务重排并刷新视图。</summary>
        private void EnsurePlayable()
        {
            if (!CanPlay)
            {
                return;
            }

            if (_link.EnsurePlayable(out bool reordered))
            {
                if (reordered && _nodeSpawner != null)
                {
                    _nodeSpawner.SyncBoardToView(_link.Board);
                }

                return;
            }

            this.Warning("重排后仍无解，请重新开始。");
        }

        private void ClearBoard()
        {
            _link.ClearSelection();

            if (lineDrawer != null)
            {
                lineDrawer.Hide();
            }

            if (_nodeSpawner != null)
            {
                _nodeSpawner.ClearBoard();
            }
        }

        private void StopActiveRoutines()
        {
            if (_clearRoutine != null)
            {
                StopCoroutine(_clearRoutine);
                _clearRoutine = null;
            }

            if (_spawnRoutine != null)
            {
                StopCoroutine(_spawnRoutine);
                _spawnRoutine = null;
            }
        }

#if UNITY_EDITOR
        private void Update()
        {
            // 编辑器调试：按空格自动连接一对可消除的节点
            if (Input.GetKeyDown(KeyCode.Space))
            {
                this.Log("空格按下，触发自动连接。");
                AutoConnect();
            }
        }

        /// <summary>
        /// 自动寻找一对可连接的节点并消除，用于编辑器调试。
        /// </summary>
        private void AutoConnect()
        {
            if (_isBusy)
            {
                this.Log("自动连接被忽略：当前正在播放动画（_isBusy=true）。");
                return;
            }

            if (!CanPlay)
            {
                this.Log("自动连接被忽略：棋盘为空或未初始化。");
                return;
            }

            if (TryAutoMatch())
            {
                return;
            }

            this.Log("自动连接：当前无解，尝试重排。");
            EnsurePlayable();
        }
#endif
    }
}
