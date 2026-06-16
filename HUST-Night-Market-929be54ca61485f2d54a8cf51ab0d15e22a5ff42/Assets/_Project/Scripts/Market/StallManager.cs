using CampusNightMarket.Data;
using UnityEngine;

namespace CampusNightMarket.Market
{
    // 摊位规则辅助类，集中处理摊位建设、升级和等级系数计算。
    public class StallManager : MonoBehaviour
    {
        // 摊位统一等级上限；后续如策划给出不同摊位上限，可移动到 StallConfig。
        [SerializeField] private int maxStallLevel = 4;

        // 判断当前夜市是否满足摊位解锁条件，并且还有空余摊位容量。
        public bool CanBuildStall(MarketRuntimeData marketData, StallConfig stallConfig)
        {
            if (marketData == null || stallConfig == null)
            {
                return false;
            }

            return marketData.marketLevel >= stallConfig.unlockMarketLevel
                && marketData.closedRounds <= 0
                && marketData.stallList.Count < marketData.maxStallCount;
        }

        // 返回摊位不能建设的原因，便于后续 UI 或日志显示。
        public bool CanBuildStall(MarketRuntimeData marketData, StallConfig stallConfig, out string reason)
        {
            reason = string.Empty;

            if (marketData == null)
            {
                reason = "夜市不存在。";
                return false;
            }

            if (stallConfig == null)
            {
                reason = "摊位配置为空。";
                return false;
            }

            if (string.IsNullOrEmpty(stallConfig.stallId))
            {
                reason = "摊位ID为空。";
                return false;
            }

            if (marketData.closedRounds > 0)
            {
                reason = "夜市正在停业，暂时不能建设摊位。";
                return false;
            }

            if (marketData.stallList.Count >= marketData.maxStallCount)
            {
                reason = "夜市摊位容量已满。";
                return false;
            }

            if (marketData.marketLevel < stallConfig.unlockMarketLevel)
            {
                reason = "夜市等级不足，无法建设该摊位。";
                return false;
            }

            // 这里需要 ResourceManager.CheckMoney(stallConfig.buildCost) 判断玩家资金是否足够。
            // 这里只检查夜市与摊位系统自身规则，不直接扣资源。
            return true;
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

        // 判断指定夜市中的摊位是否还能继续升级。
        public bool CanUpgradeStall(MarketRuntimeData marketData, StallRuntimeData stallData, StallConfig stallConfig, out string reason)
        {
            reason = string.Empty;

            if (marketData == null)
            {
                reason = "夜市不存在。";
                return false;
            }

            if (stallData == null)
            {
                reason = "摊位不存在。";
                return false;
            }

            if (stallConfig == null)
            {
                reason = "摊位配置为空。";
                return false;
            }

            if (marketData.closedRounds > 0)
            {
                reason = "夜市正在停业，暂时不能升级摊位。";
                return false;
            }

            if (stallData.level >= maxStallLevel)
            {
                reason = "摊位已达到最高等级。";
                return false;
            }

            // 这里需要 ResourceManager.CheckMoney(GetUpgradeCost(stallData, stallConfig)) 判断玩家资金是否足够。
            return true;
        }

        // 返回摊位不能升级的原因，便于后续 UI 或日志显示。
        public bool CanUpgradeStall(StallRuntimeData stallData, out string reason)
        {
            reason = string.Empty;

            if (stallData == null)
            {
                reason = "摊位不存在。";
                return false;
            }

            if (stallData.level >= maxStallLevel)
            {
                reason = "摊位已达到最高等级。";
                return false;
            }

            // 这里需要 ResourceManager.CheckMoney(升级费用) 判断玩家资金是否足够。
            // 升级费用可由 GetUpgradeCost(stallData, stallConfig) 计算。
            return true;
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
