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
        // 统一摊位数量上限。
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
            MarketLevelRule nextRule = GetNextLevelRule(marketData);
            if (nextRule == null)
            {
                return false;
            }

            marketData.marketLevel = nextRule.level;
            marketData.maxStallCount = nextRule.maxStallCount;
            return true;
        }

        // 在指定夜市中建设一个新摊位；资源扣费后续接入 ResourceManager。
        public bool BuildStall(string tileId, StallConfig stallConfig)
        {
            MarketRuntimeData marketData = GetMarket(tileId);
            if (marketData == null || stallConfig == null)
            {
                return false;
            }

            if (marketData.stallList.Count >= marketData.maxStallCount)
            {
                return false;
            }

            if (marketData.marketLevel < stallConfig.unlockMarketLevel)
            {
                return false;
            }

            StallRuntimeData stallData = new StallRuntimeData
            {
                stallId = stallConfig.stallId,
                level = 1
            };

            marketData.stallList.Add(stallData);
            RefreshMarketSummaryAfterBuild(marketData, stallConfig);
            return true;
        }

        // 升级指定夜市中的一个摊位。
        public bool UpgradeStall(string tileId, string stallId, StallConfig stallConfig)
        {
            MarketRuntimeData marketData = GetMarket(tileId);
            if (marketData == null || stallConfig == null || string.IsNullOrEmpty(stallId))
            {
                return false;
            }

            StallRuntimeData stallData = FindStall(marketData, stallId);
            if (stallData == null || stallData.level >= maxStallLevel)
            {
                return false;
            }

            stallData.level += 1;
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

        // 在一个夜市内查找指定摊位。
        private StallRuntimeData FindStall(MarketRuntimeData marketData, string stallId)
        {
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

        // 当前先按摊位基础值维护汇总，后续可接入地块、口碑和事件修正。
        // 注意：这里的算术逻辑为占位，事实上数值计算应该全部由数值系统完成。
        private void RefreshMarketSummaryAfterBuild(MarketRuntimeData marketData, StallConfig stallConfig)
        {
            marketData.totalAttraction += stallConfig.baseAttraction;

            int stallCount = marketData.stallList.Count;
            if (stallCount <= 0)
            {
                marketData.totalHygiene = 0;
                return;
            }

            float oldTotalHygiene = marketData.totalHygiene * (stallCount - 1);
            marketData.totalHygiene = (oldTotalHygiene + stallConfig.hygiene) / stallCount;
        }
    }
}
