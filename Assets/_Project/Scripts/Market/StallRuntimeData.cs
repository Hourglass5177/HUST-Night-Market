using System;

namespace CampusNightMarket.Market
{
    // 单个已建摊位的运行时状态，只保存会变化的最小数据。
    [Serializable]
    public class StallRuntimeData
    {
        // 对应 StallConfig 的摊位ID。
        public string stallId;
        // 当前摊位等级。
        public int level = 1;
    }
}
