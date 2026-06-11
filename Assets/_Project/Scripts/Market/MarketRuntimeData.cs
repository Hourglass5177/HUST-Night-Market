using System;
using System.Collections.Generic;
using CampusNightMarket.Common;

namespace CampusNightMarket.Market
{
    // 单个夜市的运行时状态，一个可建设地块最多对应一个夜市。
    [Serializable]
    public class MarketRuntimeData
    {
        // 夜市所在的地块ID。
        public string tileId;
        // 夜市当前拥有者。
        public OwnerType owner;
        // 夜市等级，影响摊位解锁和客流倍率。
        public int marketLevel = 1;
        // 当前等级允许容纳的最大摊位数。
        public int maxStallCount = 1;
        // 夜市中已建设的摊位列表。
        public List<StallRuntimeData> stallList = new List<StallRuntimeData>();
        // 夜市总吸引力，由摊位、地块和口碑加权得到。
        public float totalAttraction;
        // 夜市总卫生值，由摊位卫生属性加权得到。
        public float totalHygiene;
        // 剩余停业回合数，大于0时夜晚不营业。
        public int closedRounds;
    }
}
