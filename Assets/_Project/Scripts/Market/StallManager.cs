using CampusNightMarket.Data;
using UnityEngine;

namespace CampusNightMarket.Market
{
    // 摊位规则辅助类，集中处理摊位建设、升级和等级系数计算。
    public class StallManager : MonoBehaviour
    {
        // 设计案暂未给每种摊位单独的最高等级，先使用统一上限。
        [SerializeField] private int maxStallLevel = 5;

        // 判断当前夜市是否满足摊位解锁条件，并且还有空余摊位容量。
        public bool CanBuildStall(MarketRuntimeData marketData, StallConfig stallConfig)
        {
            if (marketData == null || stallConfig == null)
            {
                return false;
            }

            return marketData.marketLevel >= stallConfig.unlockMarketLevel
                && marketData.stallList.Count < marketData.maxStallCount;
        }

        // 判断指定摊位是否还能继续升级。
        public bool CanUpgradeStall(StallRuntimeData stallData)
        {
            if (stallData == null)
            {
                return false;
            }

            return stallData.level < maxStallLevel;
        }

        // 摊位等级系数，后续可被收入、吸引力和升级费用共用。
        public float GetLevelCoefficient(int level)
        {
            return 1f + Mathf.Max(0, level - 1) * 0.1f;
        }

        // 获取当前等级下的升级费用。
        public int GetUpgradeCost(StallRuntimeData stallData, StallConfig stallConfig)
        {
            if (stallData == null || stallConfig == null)
            {
                return 0;
            }

            return Mathf.RoundToInt(stallConfig.upgradeCost * GetLevelCoefficient(stallData.level));
        }

        // 获取当前统一摊位等级上限。
        public int GetMaxStallLevel()
        {
            return maxStallLevel;
        }
    }
}
