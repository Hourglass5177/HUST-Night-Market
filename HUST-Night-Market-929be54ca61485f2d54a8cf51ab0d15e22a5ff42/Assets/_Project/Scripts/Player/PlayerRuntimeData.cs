using System;

namespace CampusNightMarket.Player
{
    // 玩家运行时状态，只保存当前数值，不保存静态配置。
    [Serializable]
    public class PlayerRuntimeData
    {
        // 玩家当前所在地块ID。
        public string currentTileId;

        // 当前资金。
        public int money;

        // 当前低端食材数量。
        public int lowFood;
        // 当前高端食材数量。
        public int highFood;

        // 当前口碑值。
        public int reputation;

        // 当前体力
        public int energy;
        // 体力上限。
        public int maxEnergy;

        // 当前剩余贷款。
        public int loan;
        // 下一次结算利息的天数。
        public int nextInterestDay;
    }
}
