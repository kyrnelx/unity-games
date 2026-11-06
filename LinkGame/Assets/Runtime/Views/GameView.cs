using UnityEngine;
using UnityEngine.UI;

namespace Kyrnel.LinkGame
{
    /// <summary>
    /// 对局界面视图（Views 层）：把玩法状态刷新到界面上（本关得分、关卡序号、累计总分）。
    /// 只负责"显示"，不参与任何规则计算与流程控制。
    /// </summary>
    public class GameView : MonoBehaviour
    {
        [SerializeField] private Text _scoreText;
        [SerializeField] private Text _levelText;
        [SerializeField] private Text _totalScoreText;

        /// <summary>刷新本关得分。</summary>
        public void SetScore(int score)
        {
            if (_scoreText != null)
            {
                _scoreText.text = score.ToString();
            }
        }

        /// <summary>刷新关卡序号。</summary>
        public void SetLevel(int level)
        {
            if (_levelText != null)
            {
                _levelText.text = $"第 {level} 关";
            }
        }

        /// <summary>刷新累计总分。</summary>
        public void SetTotalScore(int totalScore)
        {
            if (_totalScoreText != null)
            {
                _totalScoreText.text = totalScore.ToString();
            }
        }
    }
}
