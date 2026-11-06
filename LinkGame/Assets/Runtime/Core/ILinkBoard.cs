using System.Collections.Generic;
using UnityEngine;

namespace Kyrnel.LinkGame
{
    /// <summary>
    /// 连连看棋盘的数据抽象。
    /// 寻路、解检测等纯逻辑只依赖该接口，便于脱离场景在 EditMode 中进行单元测试。
    /// </summary>
    public interface ILinkBoard
    {
        int Width { get; }
        int Height { get; }
        int RemainingCount { get; }

        bool IsInside(Vector2Int pos);
        bool IsEmptyAt(Vector2Int pos);
        NodeType GetTypeAt(Vector2Int pos);
        List<Vector2Int> CollectAlivePositions();
    }
}
