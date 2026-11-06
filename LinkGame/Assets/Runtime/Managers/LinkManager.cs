using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kyrnel.LinkGame
{
    /// <summary>
    /// 连线玩法管理者（Managers 层的应用服务，纯 C#，不依赖 MonoBehaviour / 场景对象）：
    /// 组织 Core 层的棋盘、寻路、求解、计分与关卡进度，对外提供"一局游戏"的完整规则接口：
    /// 开局数据、点击交互（选中 / 切换 / 连接判定）、道具（提示 / 洗牌 / 自动消除）、补位与关卡结算。
    /// 视图层只消费本类返回的结果与事件，因此可以脱离场景在 EditMode 中单元测试。
    /// </summary>
    public sealed class LinkManager
    {
        /// <summary>无解时最多尝试重排的次数。</summary>
        private const int MaxShuffleTries = 20;

        private readonly LevelProgress _level;
        private readonly ScoreCalculator _scoreCalculator = new ScoreCalculator();
        private readonly BoardSizeConfig _boardSizeConfig = new BoardSizeConfig();

        private LinkBoard _board;
        private LinkPathfinder _pathfinder;
        private BoardSolver _solver;

        private bool _hasSelection;
        private Vector2Int _selected = new Vector2Int(-1, -1);

        /// <summary>本关积分变化事件，参数为当前本关得分。</summary>
        public event Action<int> OnScoreChanged;

        /// <summary>关卡变化事件，参数为新的关卡序号。</summary>
        public event Action<int> OnLevelChanged;

        /// <summary>累计总分变化事件，参数为结算后的累计总分。</summary>
        public event Action<int> OnTotalScoreChanged;

        /// <param name="progressStore">进度存储实现，缺省使用 PlayerPrefs（测试时可注入替身）。</param>
        public LinkManager(IProgressStore progressStore = null)
        {
            _level = new LevelProgress(progressStore ?? new PlayerPrefsProgressStore());
        }

        /// <summary>棋盘数据模型（视图同步图案时使用）。</summary>
        public LinkBoard Board => _board;

        /// <summary>棋盘列数。</summary>
        public int Width => _board != null ? _board.Width : 0;

        /// <summary>棋盘行数。</summary>
        public int Height => _board != null ? _board.Height : 0;

        /// <summary>棋盘上剩余的可消除节点数。</summary>
        public int RemainingCount => _board != null ? _board.RemainingCount : 0;

        /// <summary>棋盘是否已就绪。</summary>
        public bool HasBoard => _board != null;

        /// <summary>是否可以继续操作（棋盘已就绪且还有节点）。</summary>
        public bool CanPlay => _board != null && _board.RemainingCount > 0;

        /// <summary>本关是否已全部消除。</summary>
        public bool IsCleared => _board != null && _board.RemainingCount <= 0;

        /// <summary>本关随机到的填充方向。</summary>
        public FillDirection FillDirection { get; private set; }

        /// <summary>当前本关得分。</summary>
        public int Score { get; private set; }

        /// <summary>当前连击数（连续成功消除的次数）。</summary>
        public int Combo => _scoreCalculator.Combo;

        /// <summary>当前关卡序号。</summary>
        public int CurrentLevel => _level.CurrentLevel;

        /// <summary>累计总分。</summary>
        public int TotalScore => _level.TotalScore;

        /// <summary>当前是否有选中节点。</summary>
        public bool HasSelection => _hasSelection;

        /// <summary>当前选中的棋盘坐标。</summary>
        public Vector2Int SelectedPosition => _selected;

        /// <summary>读取关卡与总分存档。</summary>
        public void LoadProgress()
        {
            _level.Load();
        }

        /// <summary>
        /// 开始新一关：根据关卡决定棋盘尺寸、填充方向、图案种类数量与生成顺序，
        /// 并重建棋盘、寻路器与求解器。返回的开局数据供视图层铺盘。
        /// </summary>
        public LevelSetup StartLevel()
        {
            int level = _level.CurrentLevel;
            Vector2Int size = _boardSizeConfig.GetBoardSize(level);
            int total = size.x * size.y;

            if (total % 2 != 0)
            {
                this.Error($"棋盘格子数 {total} 必须为偶数。");
                return LevelSetup.Invalid(level);
            }

            // 第 1 关固定不填充，之后每关随机（不填充 / 上 / 下 / 左 / 右）
            FillDirection = level <= 1 ? FillDirection.None : RandomFillDirection();
            this.Log($"本关填充方向: {FillDirection}");

            List<NodeType> types = NodeTypeHelper.GeneratePairedTypes(total, _level.GetTypeCountForLevel());

            _board = new LinkBoard(size.x, size.y);
            _board.LoadTypes(types);
            _pathfinder = new LinkPathfinder(_board);
            _solver = new BoardSolver(_board, _pathfinder);

            // 每次开局随机选择一种生成顺序模式
            var orderGenerator = new SpawnOrderGenerator(size.x, size.y);
            var pattern = (SpawnOrderGenerator.SpawnPattern)UnityEngine.Random.Range(
                0, Enum.GetValues(typeof(SpawnOrderGenerator.SpawnPattern)).Length);
            List<Vector2Int> spawnOrder = orderGenerator.Generate(pattern);
            this.Log($"生成特效模式: {pattern}");

            Score = 0;
            _scoreCalculator.ResetCombo();
            ClearSelection();
            OnScoreChanged?.Invoke(Score);

            return new LevelSetup(true, level, size.x, size.y, FillDirection, types, spawnOrder);
        }

        /// <summary>
        /// 处理一次节点点击：维护选中状态并完成连接判定。
        /// 返回结果告诉视图层需要更新哪些高亮、是否需要播放连线与消除动画。
        /// </summary>
        public LinkResult Click(Vector2Int position)
        {
            if (_board == null || _board.GetTypeAt(position) == NodeType.None)
            {
                return LinkResult.Ignored(position);
            }

            if (!_hasSelection)
            {
                SetSelection(position);
                return LinkResult.Selected(position);
            }

            if (position == _selected)
            {
                ClearSelection();
                return LinkResult.Deselected(position);
            }

            Vector2Int previous = _selected;
            if (_board.GetTypeAt(previous) != _board.GetTypeAt(position))
            {
                SetSelection(position);
                return LinkResult.Switched(previous, position);
            }

            List<Vector2Int> path = _pathfinder.FindPath(previous, position);
            if (path == null)
            {
                this.Log("无法连接，切换选中。");
                _scoreCalculator.ResetCombo();
                SetSelection(position);
                return LinkResult.Switched(previous, position);
            }

            return CommitMatch(previous, position, path);
        }

        /// <summary>自动寻找一对可连接节点并直接结算（用于提示、自动消除与编辑器调试）。</summary>
        public LinkResult AutoMatch()
        {
            if (_solver == null || !_solver.TryFindLinkablePair(out Vector2Int first, out Vector2Int second))
            {
                return LinkResult.Ignored(new Vector2Int(-1, -1));
            }

            List<Vector2Int> path = _pathfinder.FindPath(first, second);
            if (path == null)
            {
                return LinkResult.Ignored(first);
            }

            return CommitMatch(first, second, path);
        }

        /// <summary>找出一对可连接的节点坐标（道具-提示）。无解时返回 false。</summary>
        public bool TryFindHint(out Vector2Int first, out Vector2Int second)
        {
            if (_solver == null)
            {
                first = default;
                second = default;
                return false;
            }

            return _solver.TryFindLinkablePair(out first, out second);
        }

        /// <summary>道具-洗牌：所有存活节点携带自身图案随机换位，返回移动记录供视图播放动画。</summary>
        public List<BoardMove> ShufflePositions()
        {
            if (_board == null)
            {
                return new List<BoardMove>();
            }

            ClearSelection();
            List<BoardMove> moves = _board.ShufflePositions();
            this.Log("道具-洗牌：已重新排列棋盘。");
            return moves;
        }

        /// <summary>按本关填充方向执行滑动补位，返回移动记录供视图播放动画。</summary>
        public List<BoardMove> ApplyFill()
        {
            return _board != null ? _board.ApplyFill(FillDirection) : new List<BoardMove>();
        }

        /// <summary>清除指定坐标的节点（消除动画结束后同步棋盘模型）。</summary>
        public void ClearAt(Vector2Int position)
        {
            if (_board != null)
            {
                _board.ClearAt(position);
            }
        }

        /// <summary>
        /// 确保棋盘有解，无解时重排图案。
        /// <paramref name="reordered"/> 表示是否真的执行过重排（视图需要同步刷新图案）。
        /// 返回棋盘最终是否可解。
        /// </summary>
        public bool EnsurePlayable(out bool reordered)
        {
            reordered = false;

            if (!CanPlay)
            {
                return true;
            }

            if (_solver.HasAnyLinkablePair())
            {
                return true;
            }

            reordered = true;
            bool playable = _solver.EnsurePlayable(MaxShuffleTries);
            if (playable)
            {
                this.Log("棋盘无解，已自动重排。");
            }

            return playable;
        }

        /// <summary>关卡结算并进入下一关，返回结算后的累计总分。</summary>
        public int SettleLevelAndAdvance()
        {
            int total = _level.SettleLevelScore(Score);
            this.Log($"关卡结算：本关得分 {Score}，累计总分 {total}");
            OnTotalScoreChanged?.Invoke(total);

            _level.Advance();
            OnLevelChanged?.Invoke(_level.CurrentLevel);
            return total;
        }

        /// <summary>取指定坐标的节点类型（棋盘未就绪或该格为空时返回 None）。</summary>
        public NodeType GetTypeAt(Vector2Int position)
        {
            return _board != null ? _board.GetTypeAt(position) : NodeType.None;
        }

        /// <summary>清空当前选中状态。</summary>
        public void ClearSelection()
        {
            _hasSelection = false;
            _selected = new Vector2Int(-1, -1);
        }

        /// <summary>累计积分、清空选中并广播分数变化，返回本次连接的结果。</summary>
        private LinkResult CommitMatch(Vector2Int first, Vector2Int second, List<Vector2Int> path)
        {
            int turns = Mathf.Max(0, path.Count - 2);
            int points = _scoreCalculator.AddScore(turns);
            Score += points;

            ClearSelection();

            this.Log($"消除成功: ({first.x},{first.y}) -> ({second.x},{second.y})，拐点数: {turns}，得分: {points}");
            OnScoreChanged?.Invoke(Score);

            return LinkResult.Matched(first, second, path, turns, points);
        }

        private void SetSelection(Vector2Int position)
        {
            _hasSelection = true;
            _selected = position;
        }

        private static FillDirection RandomFillDirection()
        {
            return (FillDirection)UnityEngine.Random.Range(0, Enum.GetValues(typeof(FillDirection)).Length);
        }
    }
}
