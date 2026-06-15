using System.Collections.Generic;
using CampusNightMarket.Common;
using CampusNightMarket.Data;
using CampusNightMarket.Market;
using UnityEngine;

namespace CampusNightMarket.RandomSystem
{
    /// <summary>
    /// 卫生审查系统：负责卫生检查触发、风险评级、停业处罚。
    /// 利用 TileConfig.inspectionRate 和 MarketRuntimeData.totalHygiene 进行判定。
    /// </summary>
    public class InspectionManager : MonoBehaviour
    {
        [Header("依赖引用")]
        [SerializeField] private MarketManager marketManager;

        [Header("检查参数")]
        [SerializeField] private float hygieneThresholdLow = 50f;     // 低卫生阈值（高风险）
        [SerializeField] private float hygieneThresholdMedium = 70f;  // 中卫生阈值
        [SerializeField] private float inspectionBaseRate = 0.05f;    // 基础检查概率 5%
        [SerializeField] private int penaltyClosedRounds = 2;         // 检查不通过时的停业天数
        [SerializeField] private int penaltyFineAmount = 1000;        // 检查不通过时的罚款金额

        /// <summary>
        /// 夜晚结算时对所有夜市执行卫生检查。
        /// 返回被处罚的夜市列表（tileId 列表）。
        /// </summary>
        public List<string> ExecuteNightlyInspections(List<MarketRuntimeData> markets, List<TileConfig> tileConfigs)
        {
            List<string> penalizedMarkets = new List<string>();

            if (markets == null || markets.Count == 0)
            {
                return penalizedMarkets;
            }

            for (int i = 0; i < markets.Count; i++)
            {
                MarketRuntimeData market = markets[i];
                if (market == null)
                {
                    continue;
                }

                // 跳过已停业的夜市
                if (market.closedRounds > 0)
                {
                    continue;
                }

                // 查找地块配置
                TileConfig tileConfig = FindTileConfig(tileConfigs, market.tileId);

                // 判定是否触发检查
                if (TryTriggerInspection(market, tileConfig))
                {
                    // 执行检查
                    bool passed = ExecuteInspection(market);
                    if (!passed)
                    {
                        penalizedMarkets.Add(market.tileId);
                    }
                }
            }

            return penalizedMarkets;
        }

        /// <summary>
        /// 判定是否触发卫生检查。
        /// 检查概率 = 地块基础检查概率 + 卫生值修正。
        /// 卫生值越低，触发概率越高。
        /// </summary>
        public bool TryTriggerInspection(MarketRuntimeData market, TileConfig tileConfig)
        {
            if (market == null)
            {
                return false;
            }

            float inspectionRate = inspectionBaseRate;

            // 叠加地块检查概率
            if (tileConfig != null)
            {
                inspectionRate += tileConfig.inspectionRate;
            }

            // 卫生值修正：卫生值越低，概率越高
            if (market.totalHygiene < hygieneThresholdLow)
            {
                inspectionRate += 0.25f; // 低卫生额外+25%
            }
            else if (market.totalHygiene < hygieneThresholdMedium)
            {
                inspectionRate += 0.10f; // 中低卫生额外+10%
            }

            // 历史停业修正：曾经被处罚过的夜市更容易被检查
            // 这里简化处理，后续由事件系统完善

            // 实际判定
            float roll = Random.value;
            bool triggered = roll < inspectionRate;

            if (triggered)
            {
                Debug.Log($"InspectionManager: Inspection triggered for tile '{market.tileId}'. " +
                         $"Rate={inspectionRate:P2}, Roll={roll:P2}, Hygiene={market.totalHygiene:F1}");
            }

            return triggered;
        }

        /// <summary>
        /// 执行卫生检查。返回 true=通过，false=不通过（处罚）。
        /// 通过标准：夜市总卫生值 >= 中卫生阈值。
        /// </summary>
        public bool ExecuteInspection(MarketRuntimeData market)
        {
            if (market == null)
            {
                return true;
            }

            if (market.totalHygiene >= hygieneThresholdMedium)
            {
                // 卫生合格，检查通过
                Debug.Log($"InspectionManager: Market on '{market.tileId}' passed inspection. Hygiene={market.totalHygiene:F1}");
                return true;
            }

            // 卫生不合格，执行处罚
            if (marketManager != null)
            {
                // 停业处罚
                marketManager.AddClosedRounds(market.tileId, penaltyClosedRounds);
            }

            Debug.LogWarning($"InspectionManager: Market on '{market.tileId}' FAILED inspection! " +
                            $"Hygiene={market.totalHygiene:F1}, Closed for {penaltyClosedRounds} rounds, Fine={penaltyFineAmount}");

            return false;
        }

        /// <summary>
        /// 强制对指定夜市执行卫生检查（供事件系统调用）。
        /// </summary>
        public bool ForceInspection(string tileId)
        {
            if (marketManager == null)
            {
                Debug.LogError("InspectionManager.ForceInspection failed: marketManager is null.");
                return false;
            }

            MarketRuntimeData market = marketManager.GetMarket(tileId);
            if (market == null)
            {
                Debug.LogWarning($"InspectionManager: Market not found for tile '{tileId}'.");
                return false;
            }

            Debug.Log($"InspectionManager: Forced inspection on tile '{tileId}'.");
            return ExecuteInspection(market);
        }

        /// <summary>
        /// 获取指定夜市的检查风险等级。
        /// </summary>
        public string GetInspectionRiskLevel(MarketRuntimeData market)
        {
            if (market == null)
            {
                return "未知";
            }

            if (market.totalHygiene >= hygieneThresholdMedium)
            {
                return "低风险";
            }
            else if (market.totalHygiene >= hygieneThresholdLow)
            {
                return "中风险";
            }
            else
            {
                return "高风险";
            }
        }

        /// <summary>
        /// 根据夜市总卫生值修正总吸引力（口碑系统）。
        /// 卫生值越高，口碑加成越高。
        /// </summary>
        public float GetHygieneReputationBonus(MarketRuntimeData market)
        {
            if (market == null)
            {
                return 1f;
            }

            // 卫生值 ≥ 90：+10% 口碑
            // 卫生值 ≥ 80：+5%
            // 卫生值 ≥ 70：无加成
            // 卫生值 < 70：-5%
            if (market.totalHygiene >= 90f)
            {
                return 1.1f;
            }
            else if (market.totalHygiene >= 80f)
            {
                return 1.05f;
            }
            else if (market.totalHygiene >= 70f)
            {
                return 1f;
            }
            else
            {
                return 0.95f;
            }
        }

        /// <summary>
        /// 设置 MarketManager 引用。
        /// </summary>
        public void SetMarketManager(MarketManager manager)
        {
            marketManager = manager;
        }

        /// <summary>
        /// 按 tileId 查找地块配置。
        /// </summary>
        private TileConfig FindTileConfig(List<TileConfig> tileConfigs, string tileId)
        {
            if (tileConfigs == null || string.IsNullOrEmpty(tileId))
            {
                return null;
            }

            for (int i = 0; i < tileConfigs.Count; i++)
            {
                if (tileConfigs[i] != null && tileConfigs[i].tileId == tileId)
                {
                    return tileConfigs[i];
                }
            }

            return null;
        }
    }
}