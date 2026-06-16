using System;

namespace CampusNightMarket.Market
{
    // 夜市等级规则，“等级-最大摊位数-升级费用-效果”表。
    [Serializable]
    public class MarketLevelRule
    {
        // 夜市等级。
        public int level = 1;
        // 当前等级最大可容纳摊位数。
        public int maxStallCount = 1;
        // 从上一等级升到该等级所需费用，1级为初始等级，费用为0。
        public int upgradeCost;
        // 策划效果说明，例如“客流承载+20%”。
        public string effectDescription;

        public MarketLevelRule()
        {
        }

        public MarketLevelRule(int level, int maxStallCount, int upgradeCost, string effectDescription)
        {
            this.level = level;
            this.maxStallCount = maxStallCount;
            this.upgradeCost = upgradeCost;
            this.effectDescription = effectDescription;
        }
    }
}
