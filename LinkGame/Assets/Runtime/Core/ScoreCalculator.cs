namespace Kyrnel.LinkGame
{
    /// <summary>
    /// 积分规则的纯数据配置。
    /// </summary>
    public class ScoreConfig
    {
        public const int BaseScore = 10;         // 每次成功连接的基础分
        public const int TurnBonusPerLevel = 5;  // 每少一个拐点额外加分
        public const int MaxTurnBonusLevel = 3;  // 拐点少于该值才可获得拐点奖励
        public const int ComboBonusPerLevel = 2; // 每层连击额外加分
    }

    /// <summary>
    /// 积分计算器：基础分 + 拐点奖励 + 连击奖励。
    /// 纯逻辑实现，可在 EditMode 中单元测试。
    /// </summary>
    public sealed class ScoreCalculator
    {
        /// <summary>当前连击数（连续成功消除的次数）。</summary>
        public int Combo { get; private set; }

        /// <summary>计算一次成功连接获得的积分并累加连击。</summary>
        public int AddScore(int turns)
        {
            int points = CalculateScore(turns);
            Combo++;
            return points;
        }

        /// <summary>连接失败时重置连击。</summary>
        public void ResetCombo()
        {
            Combo = 0;
        }

        /// <summary>
        /// 计算一次成功连接获得的积分。
        /// 基础分 + 拐点奖励（拐点越少分越高）+ 连击奖励。
        /// </summary>
        private int CalculateScore(int turns)
        {
            int turnBonus = UnityEngine.Mathf.Max(0, ScoreConfig.MaxTurnBonusLevel - turns) * ScoreConfig.TurnBonusPerLevel;
            int comboBonus = Combo * ScoreConfig.ComboBonusPerLevel;
            return ScoreConfig.BaseScore + turnBonus + comboBonus;
        }
    }
}