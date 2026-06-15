using System;
using System.Collections.Generic;

namespace CampusNightMarket.Map
{
    // 单个可达地块的查询结果，包含最少步数和对应移动路径。
    [Serializable]
    public class ReachableTileResult
    {
        public string tileId;
        public int requiredSteps;
        public List<string> pathTileIds = new List<string>();

        public ReachableTileResult(string targetTileId, int steps, List<string> path)
        {
            tileId = targetTileId;
            requiredSteps = steps;
            pathTileIds = path ?? new List<string>();
        }
    }
}
