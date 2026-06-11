using CampusNightMarket.Common;
using UnityEngine;

namespace CampusNightMarket.Data
{
    // 摊位静态配置，描述一种摊位的建造成本、收入能力和客群偏好。
    [CreateAssetMenu(fileName = "StallConfig", menuName = "Campus Night Market/Stall Config")]
    public class StallConfig : ScriptableObject
    {
        // 摊位唯一ID，例如 STALL_BBQ。
        public string stallId;
        // 摊位显示名称。
        public string stallName;
        // 摊位类型。
        public StallType stallType;

        // 建设摊位所需资金。
        public int buildCost;
        // 基础升级费用。
        public int upgradeCost;

        // 基础客单价。
        public int basePrice;
        // 基础吸引力，用于客流分配权重。
        public int baseAttraction;

        // 每晚低端食材消耗，对应设计案“食材（低:高）”中的低端部分。
        public int lowFoodCost;
        // 每晚高端食材消耗，对应设计案“食材（低:高）”中的高端部分。
        public int highFoodCost;

        // 摊位卫生值，影响检查风险。
        public int hygiene = 80;

        // 学生偏好倍率。
        public float studentPreference = 1f;
        // 教师偏好倍率。
        public float teacherPreference = 1f;
        // 游客偏好倍率。
        public float touristPreference = 1f;
        // 居民偏好倍率。
        public float residentPreference = 1f;

        // 建设该摊位所需夜市等级。
        public int unlockMarketLevel = 1;
    }
}
