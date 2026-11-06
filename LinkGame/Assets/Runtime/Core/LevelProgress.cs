namespace Kyrnel.LinkGame
{
    /// <summary>
    /// 关卡与累计总分的纯逻辑管理。
    /// 通过 <see cref="IProgressStore"/> 读写进度，不直接依赖 PlayerPrefs。
    /// </summary>
    public sealed class LevelProgress
    {
        public const int MaxLevel = 999;                 // 最大关卡数
        public const string LevelPrefsKey = "LinkGame_Level";          // 关卡存档键
        public const string TotalScorePrefsKey = "LinkGame_TotalScore"; // 总分存档键

        private readonly IProgressStore _store;

        public int CurrentLevel { get; private set; }
        public int TotalScore { get; private set; }

        public LevelProgress(IProgressStore store)
        {
            _store = store;
        }

        /// <summary>从持久化存储读取当前关卡与累计总分（默认第 1 关、总分 0）。</summary>
        public void Load()
        {
            CurrentLevel = _store.GetInt(LevelPrefsKey, 1);
            CurrentLevel = UnityEngine.Mathf.Clamp(CurrentLevel, 1, MaxLevel);

            TotalScore = _store.GetInt(TotalScorePrefsKey, 0);
        }

        /// <summary>进入下一关，超过最大关卡后回到第 1 关，并保存进度。</summary>
        public void Advance()
        {
            CurrentLevel++;
            if (CurrentLevel > MaxLevel)
            {
                CurrentLevel = 1;
            }

            _store.SetInt(LevelPrefsKey, CurrentLevel);
            _store.Save();
        }

        /// <summary>
        /// 关卡结束结算：将本关得分累加到总分并持久化存储。
        /// 返回结算后的累计总分。
        /// </summary>
        public int SettleLevelScore(int levelScore)
        {
            TotalScore += levelScore;
            _store.SetInt(TotalScorePrefsKey, TotalScore);
            _store.Save();
            return TotalScore;
        }

        /// <summary>
        /// 根据当前关卡计算本关使用的图案种类数。
        /// 第 1 关 4 种，第 2 关起直接使用全部 12 种。
        /// </summary>
        public int GetTypeCountForLevel()
        {
            // 第 1 关 4 种，第 2 关起全部 12 种
            return CurrentLevel <= 1 ? 4 : 12;
        }
    }
}