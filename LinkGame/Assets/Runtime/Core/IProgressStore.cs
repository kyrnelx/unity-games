namespace Kyrnel.LinkGame
{
    /// <summary>
    /// 进度持久化抽象（依赖倒置）：GameManager 不直接依赖 PlayerPrefs。
    /// </summary>
    public interface IProgressStore
    {
        int GetInt(string key, int defaultValue);
        void SetInt(string key, int value);
        void Save();
    }
}