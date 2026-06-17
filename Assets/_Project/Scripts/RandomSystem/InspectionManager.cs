using System.Collections.Generic;
using CampusNightMarket.Data;
using CampusNightMarket.Economy;
using CampusNightMarket.Market;
using UnityEngine;

namespace CampusNightMarket.RandomSystem
{
    public class InspectionManager : MonoBehaviour
    {
        [Header("依赖引用")]
        [SerializeField] private MarketManager marketManager;
        [SerializeField] private ResourceManager resourceManager;

        [Header("审查参数")]
        [SerializeField] private float hygieneThresholdLow = 50f;
        [SerializeField] private float hygieneThresholdMedium = 70f;
        [SerializeField] private float inspectionBaseRate = 0.05f;
        [SerializeField] private int penaltyClosedRounds = 2;
        [SerializeField] private int penaltyFineAmount = 1000;

        public int PenaltyFineAmount
        {
            get { return penaltyFineAmount; }
        }

        public int PenaltyClosedRounds
        {
            get { return penaltyClosedRounds; }
        }

        public List<string> ExecuteNightlyInspections(
            List<MarketRuntimeData> markets,
            List<TileConfig> tileConfigs)
        {
            List<string> penalizedMarkets = new List<string>();
            if (markets == null || markets.Count == 0)
            {
                return penalizedMarkets;
            }

            for (int i = 0; i < markets.Count; i++)
            {
                MarketRuntimeData market = markets[i];
                if (market == null || market.closedRounds > 0)
                {
                    continue;
                }

                TileConfig tileConfig = FindTileConfig(tileConfigs, market.tileId);
                if (TryTriggerInspection(market, tileConfig) && !ExecuteInspection(market))
                {
                    penalizedMarkets.Add(market.tileId);
                }
            }

            return penalizedMarkets;
        }

        public bool TryTriggerInspection(MarketRuntimeData market, TileConfig tileConfig)
        {
            if (market == null)
            {
                return false;
            }

            float inspectionRate = inspectionBaseRate;
            if (tileConfig != null)
            {
                inspectionRate += tileConfig.inspectionRate;
            }

            if (market.totalHygiene < hygieneThresholdLow)
            {
                inspectionRate += 0.25f;
            }
            else if (market.totalHygiene < hygieneThresholdMedium)
            {
                inspectionRate += 0.10f;
            }

            inspectionRate = Mathf.Clamp01(inspectionRate);
            float roll = Random.value;
            bool triggered = roll < inspectionRate;

            if (triggered)
            {
                Debug.Log(
                    $"InspectionManager: Inspection triggered for {market.tileId}. " +
                    $"rate={inspectionRate:P0}, roll={roll:P0}, hygiene={market.totalHygiene:F1}");
            }

            return triggered;
        }

        public bool ExecuteInspection(MarketRuntimeData market)
        {
            if (market == null)
            {
                return true;
            }

            if (market.totalHygiene >= hygieneThresholdMedium)
            {
                Debug.Log(
                    $"InspectionManager: Market {market.tileId} passed. " +
                    $"hygiene={market.totalHygiene:F1}");
                return true;
            }

            if (marketManager != null)
            {
                marketManager.AddClosedRounds(market.tileId, penaltyClosedRounds);
            }

            if (resourceManager != null && penaltyFineAmount > 0)
            {
                resourceManager.SpendMoney(penaltyFineAmount);
            }

            Debug.LogWarning(
                $"InspectionManager: Market {market.tileId} failed. " +
                $"hygiene={market.totalHygiene:F1}, closed={penaltyClosedRounds}, fine={penaltyFineAmount}");
            return false;
        }

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

            if (market.totalHygiene >= hygieneThresholdLow)
            {
                return "中风险";
            }

            return "高风险";
        }

        public float GetHygieneReputationBonus(MarketRuntimeData market)
        {
            if (market == null)
            {
                return 1f;
            }

            if (market.totalHygiene >= 90f)
            {
                return 1.1f;
            }

            if (market.totalHygiene >= 80f)
            {
                return 1.05f;
            }

            if (market.totalHygiene >= 70f)
            {
                return 1f;
            }

            return 0.95f;
        }

        public void SetMarketManager(MarketManager manager)
        {
            marketManager = manager;
        }

        public void SetResourceManager(ResourceManager manager)
        {
            resourceManager = manager;
        }

        private TileConfig FindTileConfig(List<TileConfig> tileConfigs, string tileId)
        {
            if (tileConfigs == null || string.IsNullOrEmpty(tileId))
            {
                return null;
            }

            for (int i = 0; i < tileConfigs.Count; i++)
            {
                TileConfig tileConfig = tileConfigs[i];
                if (tileConfig != null && tileConfig.tileId == tileId)
                {
                    return tileConfig;
                }
            }

            return null;
        }
    }
}
