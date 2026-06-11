using System.Collections.Generic;
using CampusNightMarket.Common;
using CampusNightMarket.Data;
using UnityEngine;

namespace CampusNightMarket.Market
{
    // 夜市管理器：负责创建夜市、升级夜市、建设和升级摊位。
    public class MarketManager : MonoBehaviour
    {
        // 玩家当前拥有或系统登记的夜市运行时数据。
        [SerializeField] private List<MarketRuntimeData> markets = new List<MarketRuntimeData>();
        // 摊位统一等级上限；后续如策划给出不同摊位上限，可移动到 StallConfig。
        [SerializeField] private int maxStallLevel = 4;
        // 夜市等级规则。
        [SerializeField] private List<MarketLevelRule> levelRules = new List<MarketLevelRule>
        {
            new MarketLevelRule(1, 1, 0, "基础夜市"),
            new MarketLevelRule(2, 2, 2000, "客流承载+20%"),
            new MarketLevelRule(3, 3, 5000, "解锁高端摊位"),
            new MarketLevelRule(4, 4, 10000, "口碑加成")
        };

        // 当前全部夜市数据，供 UI 和结算系统读取。
        public List<MarketRuntimeData> Markets
        {
            get { return markets; }
        }

        // 根据地块配置创建夜市；正式流程应优先使用这个接口。
        public bool TryCreateMarket(TileConfig tileConfig, out MarketRuntimeData marketData)
        {
            marketData = null;

            if (!CanCreateMarket(tileConfig, out string reason))
            {
                Debug.LogWarning("Create market failed: " + reason);
                return false;
            }

            // 这里需要 ResourceManager.SpendMoney(tileConfig.purchasePrice) 扣除购买地块/建设夜市费用。
            // 如果扣费失败，应返回 false，避免创建夜市。
            return TryCreateMarket(tileConfig.tileId, out marketData);
        }

        // 在指定地块创建玩家夜市；如果该地块已有夜市则失败。
        public bool TryCreateMarket(string tileId, out MarketRuntimeData marketData)
        {
            marketData = null;

            if (string.IsNullOrEmpty(tileId) || GetMarket(tileId) != null)
            {
                return false;
            }

            marketData = new MarketRuntimeData
            {
                tileId = tileId,
                owner = OwnerType.Player,
                marketLevel = 1,
                maxStallCount = GetMaxStallCount(1)
            };

            markets.Add(marketData);
            return true;
        }

        // 检查指定地块是否允许创建夜市。
        public bool CanCreateMarket(TileConfig tileConfig, out string reason)
        {
            reason = string.Empty;

            if (tileConfig == null)
            {
                reason = "地块配置为空。";
                return false;
            }

            if (string.IsNullOrEmpty(tileConfig.tileId))
            {
                reason = "地块ID为空。";
                return false;
            }

            if (!tileConfig.canPurchase)
            {
                reason = "该地块不可购买，不能建设夜市。";
                return false;
            }

            if (GetMarket(tileConfig.tileId) != null)
            {
                reason = "该地块已经存在夜市。";
                return false;
            }

            // 这里需要 ResourceManager.CheckMoney(tileConfig.purchasePrice) 判断玩家资金是否足够。
            return true;
        }

        // 按地块ID查找夜市运行时数据。
        public MarketRuntimeData GetMarket(string tileId)
        {
            if (string.IsNullOrEmpty(tileId))
            {
                return null;
            }

            for (int i = 0; i < markets.Count; i++)
            {
                if (markets[i] != null && markets[i].tileId == tileId)
                {
                    return markets[i];
                }
            }

            return null;
        }

        // 升级指定地块上的夜市。
        public bool UpgradeMarket(string tileId)
        {
            MarketRuntimeData marketData = GetMarket(tileId);
            int upgradeCost;
            if (!CanUpgradeMarket(marketData, out upgradeCost, out string reason))
            {
                Debug.LogWarning("Upgrade market failed: " + reason);
                return false;
            }

            // 这里需要 ResourceManager.SpendMoney(upgradeCost) 扣除夜市升级费用。
            // 如果扣费失败，应返回 false，避免升级夜市。
            MarketLevelRule nextRule = GetNextLevelRule(marketData);

            marketData.marketLevel = nextRule.level;
            marketData.maxStallCount = nextRule.maxStallCount;
            return true;
        }

        // 检查夜市是否可以升级，并输出下一等级费用。
        public bool CanUpgradeMarket(MarketRuntimeData marketData, out int upgradeCost, out string reason)
        {
            upgradeCost = 0;
            reason = string.Empty;

            if (marketData == null)
            {
                reason = "夜市不存在。";
                return false;
            }

            MarketLevelRule nextRule = GetNextLevelRule(marketData);
            if (nextRule == null)
            {
                reason = "夜市已达到最高等级。";
                return false;
            }

            upgradeCost = nextRule.upgradeCost;
            // 这里需要 ResourceManager.CheckMoney(upgradeCost) 判断玩家资金是否足够。
            return true;
        }

        // 在指定夜市中建设一个新摊位；资源扣费后续接入 ResourceManager。
        public bool BuildStall(string tileId, StallConfig stallConfig)
        {
            MarketRuntimeData marketData = GetMarket(tileId);
            if (!CanBuildStall(marketData, stallConfig, out string reason))
            {
                Debug.LogWarning("Build stall failed: " + reason);
                return false;
            }

            // 这里需要 ResourceManager.SpendMoney(stallConfig.buildCost) 扣除摊位建设费用。
            // 如果扣费失败，应返回 false，避免新增摊位。
            StallRuntimeData stallData = new StallRuntimeData
            {
                stallId = stallConfig.stallId,
                level = 1
            };

            marketData.stallList.Add(stallData);
            RefreshMarketSummaryAfterBuild(marketData, stallConfig);
            return true;
        }

        // 检查指定夜市是否可以建设该摊位。
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
            return true;
        }

        // 升级指定夜市中的一个摊位。
        public bool UpgradeStall(string tileId, string stallId, StallConfig stallConfig)
        {
            MarketRuntimeData marketData = GetMarket(tileId);
            StallRuntimeData stallData = FindStall(marketData, stallId);
            if (!CanUpgradeStall(marketData, stallData, stallConfig, out string reason))
            {
                Debug.LogWarning("Upgrade stall failed: " + reason);
                return false;
            }

            // 这里需要 ResourceManager.SpendMoney(GetStallUpgradeCost(stallData, stallConfig)) 扣除摊位升级费用。
            // 如果扣费失败，应返回 false，避免升级摊位。
            float oldAttraction = stallConfig.baseAttraction * GetStallLevelCoefficient(stallData.level);
            stallData.level += 1;
            ApplyStallUpgradeSummaryDelta(marketData, stallConfig, oldAttraction, stallData.level);
            return true;
        }

        // 检查指定摊位是否可以升级。
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

            // 这里需要 ResourceManager.CheckMoney(GetStallUpgradeCost(stallData, stallConfig)) 判断玩家资金是否足够。
            return true;
        }

        // 获取指定夜市下一等级规则；没有下一等级时返回 null。
        public MarketLevelRule GetNextLevelRule(MarketRuntimeData marketData)
        {
            if (marketData == null)
            {
                return null;
            }

            MarketLevelRule bestRule = null;
            for (int i = 0; i < levelRules.Count; i++)
            {
                MarketLevelRule rule = levelRules[i];
                if (rule == null || rule.level <= marketData.marketLevel)
                {
                    continue;
                }

                if (bestRule == null || rule.level < bestRule.level)
                {
                    bestRule = rule;
                }
            }

            return bestRule;
        }

        // 获取升级到下一等级所需费用。
        public int GetNextUpgradeCost(MarketRuntimeData marketData)
        {
            MarketLevelRule nextRule = GetNextLevelRule(marketData);
            return nextRule == null ? 0 : nextRule.upgradeCost;
        }

        // 根据夜市等级读取最大摊位数，配置缺失时用等级本身作为保底。
        public int GetMaxStallCount(int marketLevel)
        {
            for (int i = 0; i < levelRules.Count; i++)
            {
                MarketLevelRule rule = levelRules[i];
                if (rule != null && rule.level == marketLevel)
                {
                    return rule.maxStallCount;
                }
            }

            return Mathf.Max(1, marketLevel);
        }

        // 夜市2级效果：客流承载+20%，具体客流生成由客流或经济系统使用该倍率。
        public float GetTrafficCapacityMultiplier(MarketRuntimeData marketData)
        {
            if (marketData == null || marketData.marketLevel < 2)
            {
                return 1f;
            }

            // 这里需要 CustomerManager/EconomyManager 在分配夜间客流时应用该倍率。
            return 1.2f;
        }

        // 夜市3级效果：解锁高端摊位，具体哪些摊位属于高端由 StallConfig.unlockMarketLevel 控制。
        public bool IsHighTierStallUnlocked(MarketRuntimeData marketData)
        {
            return marketData != null && marketData.marketLevel >= 3;
        }

        // 夜市4级效果：口碑加成，具体口碑数值由口碑或事件系统结算。
        public bool HasReputationBonus(MarketRuntimeData marketData)
        {
            // 这里需要 ReputationManager/EconomyManager 根据夜市等级4的效果计算口碑或收益加成。
            return marketData != null && marketData.marketLevel >= 4;
        }

        // 获取摊位当前等级升级到下一等级的费用。
        public int GetStallUpgradeCost(StallRuntimeData stallData, StallConfig stallConfig)
        {
            if (stallData == null || stallConfig == null)
            {
                return 0;
            }

            return Mathf.RoundToInt(stallConfig.upgradeCost * GetStallLevelCoefficient(stallData.level));
        }

        // 摊位等级系数，升级后可影响吸引力、收入或升级费用。
        public float GetStallLevelCoefficient(int level)
        {
            return 1f + Mathf.Max(0, level - 1) * 0.1f;
        }

        // 判断夜市是否正在营业；夜市停业时夜晚结算系统应跳过该夜市。
        public bool IsMarketOpen(string tileId)
        {
            MarketRuntimeData marketData = GetMarket(tileId);
            return marketData != null && marketData.closedRounds <= 0;
        }

        // 增加夜市停业回合数，供卫生检查或事件系统调用。
        public bool AddClosedRounds(string tileId, int rounds)
        {
            MarketRuntimeData marketData = GetMarket(tileId);
            if (marketData == null || rounds <= 0)
            {
                return false;
            }

            marketData.closedRounds += rounds;
            return true;
        }

        // 使用完整摊位配置列表刷新夜市汇总属性。
        public void RefreshMarketSummary(MarketRuntimeData marketData, List<StallConfig> stallConfigs)
        {
            if (marketData == null)
            {
                return;
            }

            marketData.totalAttraction = 0f;
            marketData.totalHygiene = 0f;

            if (marketData.stallList.Count == 0)
            {
                return;
            }

            float hygieneSum = 0f;
            int validStallCount = 0;

            for (int i = 0; i < marketData.stallList.Count; i++)
            {
                StallRuntimeData stallData = marketData.stallList[i];
                if (stallData == null)
                {
                    continue;
                }

                StallConfig stallConfig = FindStallConfig(stallConfigs, stallData.stallId);
                if (stallConfig == null)
                {
                    // 这里需要 DataManager.GetStall(stallData.stallId) 取得缺失的摊位配置。
                    continue;
                }

                marketData.totalAttraction += stallConfig.baseAttraction * GetStallLevelCoefficient(stallData.level);
                hygieneSum += stallConfig.hygiene;
                validStallCount += 1;
            }

            if (validStallCount > 0)
            {
                marketData.totalHygiene = hygieneSum / validStallCount;
            }

            // 这里需要 EconomyManager/CustomerManager 根据地块、口碑、夜市等级、事件等修正 totalAttraction。
            // 这里需要 InspectionManager 根据夜市等级、清洁事件等修正 totalHygiene。
        }

        // 每天结束或夜晚结算后推进停业倒计时。
        public void AdvanceClosedRounds()
        {
            for (int i = 0; i < markets.Count; i++)
            {
                if (markets[i] != null && markets[i].closedRounds > 0)
                {
                    markets[i].closedRounds -= 1;
                }
            }
        }

        // 在一个夜市内查找指定摊位。
        private StallRuntimeData FindStall(MarketRuntimeData marketData, string stallId)
        {
            if (marketData == null || string.IsNullOrEmpty(stallId))
            {
                return null;
            }

            for (int i = 0; i < marketData.stallList.Count; i++)
            {
                StallRuntimeData stallData = marketData.stallList[i];
                if (stallData != null && stallData.stallId == stallId)
                {
                    return stallData;
                }
            }

            return null;
        }

        // 在配置列表中按摊位ID查找摊位配置。
        private StallConfig FindStallConfig(List<StallConfig> stallConfigs, string stallId)
        {
            if (stallConfigs == null || string.IsNullOrEmpty(stallId))
            {
                return null;
            }

            for (int i = 0; i < stallConfigs.Count; i++)
            {
                StallConfig stallConfig = stallConfigs[i];
                if (stallConfig != null && stallConfig.stallId == stallId)
                {
                    return stallConfig;
                }
            }

            return null;
        }

        // 当前先按摊位基础值维护汇总，后续可接入地块、口碑和事件修正。
        // 注意：这里的算术逻辑为占位，事实上数值计算应该全部由数值系统完成。
        private void RefreshMarketSummaryAfterBuild(MarketRuntimeData marketData, StallConfig stallConfig)
        {
            marketData.totalAttraction += stallConfig.baseAttraction * GetStallLevelCoefficient(1);

            int stallCount = marketData.stallList.Count;
            if (stallCount <= 0)
            {
                marketData.totalHygiene = 0;
                return;
            }

            float oldTotalHygiene = marketData.totalHygiene * (stallCount - 1);
            marketData.totalHygiene = (oldTotalHygiene + stallConfig.hygiene) / stallCount;
        }

        // 摊位升级时只按升级前后的吸引力差值修正夜市总吸引力。
        // 这里需要 DataManager.GetStall(stallId) 获取全部摊位配置后，由数值系统完整重算夜市汇总属性。
        private void ApplyStallUpgradeSummaryDelta(
            MarketRuntimeData marketData,
            StallConfig changedStallConfig,
            float oldAttraction,
            int newLevel)
        {
            if (marketData == null || changedStallConfig == null)
            {
                return;
            }

            float newAttraction = changedStallConfig.baseAttraction * GetStallLevelCoefficient(newLevel);
            marketData.totalAttraction += newAttraction - oldAttraction;

            // 这里需要 EconomyManager/CustomerManager 根据摊位升级、夜市等级和事件效果修正 totalAttraction。
            // 这里需要 InspectionManager 判断摊位升级是否会影响卫生值或检查风险。
        }
    }
}
