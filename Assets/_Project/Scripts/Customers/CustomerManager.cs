using System.Collections.Generic;
using CampusNightMarket.Common;
using CampusNightMarket.Data;
using CampusNightMarket.Market;
using UnityEngine;

namespace CampusNightMarket.Customers
{
    /// <summary>
    /// 客流系统：负责客流分配、客群偏好计算和竞争强度削减。
    /// EconomyManager 的夜晚收入结算依赖此模块。
    /// </summary>
    public class CustomerManager : MonoBehaviour
    {
        [Header("客流参数")]
        [SerializeField] private float competitionPenaltyFactor = 0.5f; // 竞争强度转换系数
        [SerializeField] private float minTrafficRatio = 0.01f;        // 摊位最低客流占比

        /// <summary>
        /// 为指定夜市的每个摊位分配当晚客流。
        /// 返回字典：stallId → 分配到的客流数(int)
        /// </summary>
        public Dictionary<string, int> AllocateTraffic(MarketRuntimeData market, TileConfig tileConfig)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();

            if (market == null || tileConfig == null || market.stallList == null || market.stallList.Count == 0)
            {
                return result;
            }

            // === 1. 计算地块基础客流 ===
            float baseTraffic = tileConfig.baseTraffic;

            // === 2. 应用竞争强度削减 ===
            float effectiveTraffic = ApplyCompetitionPenalty(baseTraffic, tileConfig.competition);

            if (effectiveTraffic <= 0f)
            {
                return result;
            }

            // === 3. 按吸引力权重分配 ===
            float totalAttraction = market.totalAttraction;
            if (totalAttraction <= 0f)
            {
                return result;
            }

            for (int i = 0; i < market.stallList.Count; i++)
            {
                StallRuntimeData stallData = market.stallList[i];
                if (stallData == null)
                {
                    continue;
                }

                // 暂时以基础吸引力比例分配（详细配置由 DataManager 提供后完善）
                // 当前简化处理：等比例分配
                float trafficShare = effectiveTraffic * (1f / market.stallList.Count);
                result[stallData.stallId] = Mathf.RoundToInt(trafficShare);
            }

            return result;
        }

        /// <summary>
        /// 为指定夜市的每个摊位分配当晚客流（完整版本——需要 StallConfig 列表）。
        /// 推荐使用此版本，按摊位吸引力权重分配。
        /// </summary>
        public Dictionary<string, int> AllocateTraffic(MarketRuntimeData market, TileConfig tileConfig, List<StallConfig> stallConfigs)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();

            if (market == null || tileConfig == null || stallConfigs == null)
            {
                return result;
            }

            if (market.stallList == null || market.stallList.Count == 0)
            {
                return result;
            }

            // === 1. 计算地块基础客流 ===
            float baseTraffic = tileConfig.baseTraffic;

            // === 2. 应用竞争强度削减 ===
            float effectiveTraffic = ApplyCompetitionPenalty(baseTraffic, tileConfig.competition);

            if (effectiveTraffic <= 0f)
            {
                return result;
            }

            // === 3. 计算各摊位吸引力（含等级系数） ===
            float totalAttraction = 0f;
            Dictionary<string, float> stallAttractions = new Dictionary<string, float>();

            for (int i = 0; i < market.stallList.Count; i++)
            {
                StallRuntimeData stallData = market.stallList[i];
                if (stallData == null || string.IsNullOrEmpty(stallData.stallId))
                {
                    continue;
                }

                StallConfig stallConfig = FindStallConfig(stallConfigs, stallData.stallId);
                if (stallConfig == null)
                {
                    continue;
                }

                // 摊位吸引力 = 基础吸引力 × 等级系数(1 + (level-1)*0.1)
                float attraction = stallConfig.baseAttraction * (1f + Mathf.Max(0, stallData.level - 1) * 0.1f);
                stallAttractions[stallData.stallId] = attraction;
                totalAttraction += attraction;
            }

            if (totalAttraction <= 0f)
            {
                return result;
            }

            // === 4. 按吸引力权重分配客流 ===
            for (int i = 0; i < market.stallList.Count; i++)
            {
                StallRuntimeData stallData = market.stallList[i];
                if (stallData == null || string.IsNullOrEmpty(stallData.stallId))
                {
                    continue;
                }

                if (stallAttractions.ContainsKey(stallData.stallId))
                {
                    float ratio = Mathf.Max(minTrafficRatio, stallAttractions[stallData.stallId] / totalAttraction);
                    int traffic = Mathf.RoundToInt(effectiveTraffic * ratio);
                    result[stallData.stallId] = traffic;
                }
            }

            return result;
        }

        /// <summary>
        /// 计算客群偏好倍率。
        /// 根据摊位对不同客群的偏好 × 地块上各客群占比，加权求和。
        /// </summary>
        public float CalculatePreferenceMultiplier(StallConfig stallConfig, TileConfig tileConfig)
        {
            if (stallConfig == null || tileConfig == null)
            {
                return 1f;
            }

            float totalRatio = tileConfig.studentRatio + tileConfig.teacherRatio
                             + tileConfig.touristRatio + tileConfig.residentRatio;

            if (totalRatio <= 0f)
            {
                return 1f;
            }

            float weighted = 0f;
            weighted += stallConfig.studentPreference * (tileConfig.studentRatio / totalRatio);
            weighted += stallConfig.teacherPreference * (tileConfig.teacherRatio / totalRatio);
            weighted += stallConfig.touristPreference * (tileConfig.touristRatio / totalRatio);
            weighted += stallConfig.residentPreference * (tileConfig.residentRatio / totalRatio);

            return Mathf.Max(0.1f, weighted);
        }

        /// <summary>
        /// 应用竞争强度削减。
        /// competition 越高，客流削减越多。
        /// </summary>
        public float ApplyCompetitionPenalty(float baseTraffic, float competition)
        {
            if (competition <= 0f)
            {
                return baseTraffic;
            }

            // 竞争削减比例 = competition × competitionPenaltyFactor
            // 例如 competition=50, factor=0.5 → 削减25%
            float penalty = Mathf.Min(1f, competition * competitionPenaltyFactor / 100f);
            return baseTraffic * (1f - penalty);
        }

        /// <summary>
        /// 获取指定夜市的客流承载倍率（夜市2级效果）。
        /// </summary>
        public float GetTrafficCapacityMultiplier(MarketRuntimeData market)
        {
            if (market == null || market.marketLevel < 2)
            {
                return 1f;
            }

            return 1.2f;
        }

        /// <summary>
        /// 在配置列表中查找摊位配置。
        /// </summary>
        private StallConfig FindStallConfig(List<StallConfig> stallConfigs, string stallId)
        {
            if (stallConfigs == null || string.IsNullOrEmpty(stallId))
            {
                return null;
            }

            for (int i = 0; i < stallConfigs.Count; i++)
            {
                if (stallConfigs[i] != null && stallConfigs[i].stallId == stallId)
                {
                    return stallConfigs[i];
                }
            }

            return null;
        }
    }
}