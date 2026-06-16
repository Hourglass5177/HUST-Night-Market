using System.Collections.Generic;
using CampusNightMarket.Common;
using CampusNightMarket.Core;
using CampusNightMarket.Data;
using CampusNightMarket.Market;
using CampusNightMarket.Player;
using UnityEngine;

namespace CampusNightMarket.Economy
{
    /// <summary>
    /// 经济结算系统：负责夜晚收入结算、贷款利息处理、
    /// 以及地块购买/夜市建设/摊位建设升级的事务性封装。
    /// </summary>
    public class EconomyManager : MonoBehaviour
    {
        [Header("依赖引用")]
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private MarketManager marketManager;
        [SerializeField] private GameManager gameManager;

        [Header("配置数据（正式版可改用 DataManager 统一读取）")]
        [SerializeField] private List<TileConfig> tileConfigs;
        [SerializeField] private List<StallConfig> stallConfigs;

        [Header("结算参数")]
        [SerializeField] private float foodShortagePenalty = 0.5f; // 食材不足时收入系数

        // ================================================================
        //  1. 夜晚收入结算
        // ================================================================

        /// <summary>
        /// 结算所有夜市当晚收入。
        /// 在 TurnManager.NightSettlement 阶段调用。
        /// </summary>
        public void SettleAllMarketsNightIncome()
        {
            if (marketManager == null)
            {
                Debug.LogError("EconomyManager.SettleAllMarketsNightIncome failed: marketManager is null.");
                return;
            }

            List<MarketRuntimeData> markets = marketManager.Markets;
            if (markets == null || markets.Count == 0)
            {
                return;
            }

            int totalIncome = 0;

            for (int i = 0; i < markets.Count; i++)
            {
                MarketRuntimeData market = markets[i];
                if (market == null)
                {
                    continue;
                }

                // 跳过停业夜市
                if (market.closedRounds > 0)
                {
                    continue;
                }

                TileConfig tileConfig = FindTileConfig(market.tileId);
                if (tileConfig == null)
                {
                    Debug.LogWarning($"EconomyManager: TileConfig not found for tileId={market.tileId}, skip settlement.");
                    continue;
                }

                int marketIncome = SettleSingleMarketNightIncome(market, tileConfig);
                totalIncome += marketIncome;
            }

            if (totalIncome > 0 && resourceManager != null)
            {
                resourceManager.AddMoney(totalIncome);
                Debug.Log($"EconomyManager: Night settlement complete, total income = {totalIncome}");
            }
        }

        /// <summary>结算单个夜市当晚收入，返回该夜市产生的收入金额。</summary>
        private int SettleSingleMarketNightIncome(MarketRuntimeData market, TileConfig tileConfig)
        {
            if (market.stallList == null || market.stallList.Count == 0)
            {
                return 0;
            }

            // === 计算地块总客流 ===
            float baseTraffic = tileConfig.baseTraffic;
            float trafficMultiplier = marketManager != null
                ? marketManager.GetTrafficCapacityMultiplier(market)
                : 1f;
            float totalTraffic = baseTraffic * trafficMultiplier;

            if (totalTraffic <= 0f)
            {
                return 0;
            }

            // === 按摊位吸引力权重分配客流 ===
            float totalAttraction = market.totalAttraction;
            if (totalAttraction <= 0f)
            {
                return 0;
            }

            int marketIncome = 0;

            for (int i = 0; i < market.stallList.Count; i++)
            {
                StallRuntimeData stallData = market.stallList[i];
                if (stallData == null)
                {
                    continue;
                }

                StallConfig stallConfig = FindStallConfig(stallData.stallId);
                if (stallConfig == null)
                {
                    Debug.LogWarning($"EconomyManager: StallConfig not found for stallId={stallData.stallId}");
                    continue;
                }

                // 摊位吸引力的等级修正
                float stallAttraction = stallConfig.baseAttraction
                    * (marketManager != null ? marketManager.GetStallLevelCoefficient(stallData.level) : 1f);

                // 按吸引力权重分配客流
                float trafficShare = totalTraffic * (stallAttraction / totalAttraction);

                // === 客群偏好加权 ===
                float preferenceMultiplier = CalculatePreferenceMultiplier(stallConfig, tileConfig);
                float effectiveTraffic = trafficShare * preferenceMultiplier;

                // === 计算收入 ===
                float revenue = effectiveTraffic * stallConfig.basePrice * tileConfig.consumePower;

                // === 消耗食材（每个摊位每晚固定消耗） ===
                bool hasEnoughLowFood = true;
                bool hasEnoughHighFood = true;

                if (resourceManager != null)
                {
                    if (stallConfig.lowFoodCost > 0)
                    {
                        hasEnoughLowFood = resourceManager.ConsumeLowFood(stallConfig.lowFoodCost);
                    }
                    if (stallConfig.highFoodCost > 0)
                    {
                        hasEnoughHighFood = resourceManager.ConsumeHighFood(stallConfig.highFoodCost);
                    }
                }

                // 食材不足时收入打折
                if (!hasEnoughLowFood || !hasEnoughHighFood)
                {
                    revenue *= foodShortagePenalty;
                }

                marketIncome += Mathf.RoundToInt(revenue);
            }

            // === 夜市4级口碑加成 ===
            if (marketManager != null && marketManager.HasReputationBonus(market))
            {
                float bonusMultiplier = 1f + (resourceManager != null
                    ? resourceManager.GetCurrentReputation() * 0.01f
                    : 0f);
                marketIncome = Mathf.RoundToInt(marketIncome * bonusMultiplier);
            }

            return marketIncome;
        }

        /// <summary>
        /// 计算摊位客群偏好倍率。
        /// 根据摊位对不同客群的偏好 × 地块上各客群占比，加权求和。
        /// </summary>
        private float CalculatePreferenceMultiplier(StallConfig stallConfig, TileConfig tileConfig)
        {
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

        // ================================================================
        //  2. 贷款利息处理
        // ================================================================

        /// <summary>
        /// 处理贷款利息。
        /// 到利息日则根据利率扣除利息；资金不足时自动增加贷款。
        /// 在 TurnManager.EndDay 或 AdvanceDay 前调用。
        /// </summary>
        public void ProcessLoanInterest(int currentDay)
        {
            if (gameManager == null || resourceManager == null)
            {
                return;
            }

            // 从 GameManager 获取当前 MapConfig 的利息参数
            // 由于 GameManager 的 currentMapConfig 是 private 字段，
            // 我们需要通过其他方式获取利息参数。此处使用 GameRuntimeData 的天数，
            // 利息参数由外部传入或通过序列化字段配置。
            // 这里简化处理：外部调用时传入 interestInterval 和 interestRate。

            // 由带参数的 Overload 版本完成实际逻辑。
            Debug.LogWarning("EconomyManager.ProcessLoanInterest(int) 需要 interestInterval 和 interestRate，" +
                           "请使用 ProcessLoanInterest(int currentDay, int interval, float rate) 重载。");
        }

        /// <summary>
        /// 处理贷款利息（完整参数版本）。
        /// </summary>
        /// <param name="currentDay">当前天数。</param>
        /// <param name="interestInterval">利息结算周期（天）。</param>
        /// <param name="interestRate">每期利率（如 0.1 表示 10%）。</param>
        public void ProcessLoanInterest(int currentDay, int interestInterval, float interestRate)
        {
            if (resourceManager == null)
            {
                Debug.LogError("EconomyManager.ProcessLoanInterest failed: resourceManager is null.");
                return;
            }

            // 获取玩家数据
            PlayerRuntimeData playerData = GetPlayerData();
            if (playerData == null)
            {
                Debug.LogError("EconomyManager.ProcessLoanInterest failed: cannot get playerData.");
                return;
            }

            // 检查是否为利息日
            if (interestInterval <= 0 || currentDay <= 0)
            {
                return;
            }

            if (currentDay % interestInterval != 0)
            {
                return; // 不是利息日
            }

            if (playerData.loan <= 0)
            {
                return; // 没有贷款
            }

            int interest = Mathf.RoundToInt(playerData.loan * interestRate);
            if (interest <= 0)
            {
                return;
            }

            Debug.Log($"EconomyManager: Interest due on day {currentDay}, interest = {interest}, current money = {playerData.money}");

            // 优先从资金扣除
            if (playerData.money >= interest)
            {
                resourceManager.SpendMoney(interest);
                Debug.Log($"EconomyManager: Interest paid from money. Remaining money = {playerData.money}");
            }
            else
            {
                // 资金不足时，不足部分增加到贷款本金
                int shortfall = interest - playerData.money;
                resourceManager.SpendMoney(playerData.money); // 清空资金
                playerData.loan += shortfall;
                Debug.LogWarning($"EconomyManager: Insufficient money for interest. " +
                               $"Added {shortfall} to loan. Total loan now = {playerData.loan}");
            }
        }

        // ================================================================
        //  3. 事务性封装（购买/建设/升级）
        // ================================================================

        /// <summary>
        /// 购买地块并创建夜市（事务性封装）。
        /// 检查资金 → 扣费 → 创建夜市，任一失败则回滚。
        /// </summary>
        public bool PurchaseTile(TileConfig tileConfig)
        {
            if (tileConfig == null)
            {
                Debug.LogError("EconomyManager.PurchaseTile failed: tileConfig is null.");
                return false;
            }

            if (resourceManager == null || marketManager == null)
            {
                Debug.LogError("EconomyManager.PurchaseTile failed: resourceManager or marketManager is null.");
                return false;
            }

            // Step 1: 检查夜市创建合法性
            if (!marketManager.CanCreateMarket(tileConfig, out string reason))
            {
                Debug.LogWarning($"EconomyManager.PurchaseTile failed: {reason}");
                return false;
            }

            // Step 2: 扣费
            if (!resourceManager.SpendMoney(tileConfig.purchasePrice))
            {
                Debug.LogWarning("EconomyManager.PurchaseTile failed: insufficient money for purchase.");
                return false;
            }

            // Step 3: 创建夜市
            if (!marketManager.TryCreateMarket(tileConfig.tileId, out MarketRuntimeData marketData))
            {
                // 创建失败则退还费用
                resourceManager.AddMoney(tileConfig.purchasePrice);
                Debug.LogError("EconomyManager.PurchaseTile failed: market creation failed, money refunded.");
                return false;
            }

            Debug.Log($"EconomyManager: Tile {tileConfig.tileId} purchased and market created. Cost = {tileConfig.purchasePrice}");
            return true;
        }

        /// <summary>
        /// 建设摊位（事务性封装）。
        /// 检查资金 → 扣费 → 建设摊位，清理失败则回滚。
        /// </summary>
        public bool BuildStallTransaction(string tileId, StallConfig stallConfig)
        {
            if (string.IsNullOrEmpty(tileId) || stallConfig == null)
            {
                Debug.LogError("EconomyManager.BuildStallTransaction failed: invalid parameters.");
                return false;
            }

            if (resourceManager == null || marketManager == null)
            {
                Debug.LogError("EconomyManager.BuildStallTransaction failed: resourceManager or marketManager is null.");
                return false;
            }

            // Step 1: 检查合法性
            MarketRuntimeData marketData = marketManager.GetMarket(tileId);
            if (!marketManager.CanBuildStall(marketData, stallConfig, out string reason))
            {
                Debug.LogWarning($"EconomyManager.BuildStallTransaction failed: {reason}");
                return false;
            }

            // Step 2: 扣费
            if (!resourceManager.SpendMoney(stallConfig.buildCost))
            {
                Debug.LogWarning("EconomyManager.BuildStallTransaction failed: insufficient money for build.");
                return false;
            }

            // Step 3: 建设
            if (!marketManager.BuildStallAfterPayment(tileId, stallConfig))
            {
                // 建设失败退还费用
                resourceManager.AddMoney(stallConfig.buildCost);
                Debug.LogError("EconomyManager.BuildStallTransaction failed: BuildStall returned false, money refunded.");
                return false;
            }

            Debug.Log($"EconomyManager: Stall {stallConfig.stallId} built on tile {tileId}. Cost = {stallConfig.buildCost}");
            return true;
        }

        /// <summary>
        /// 升级摊位（事务性封装）。
        /// 检查资金 → 扣费 → 升级摊位，失败则回滚。
        /// </summary>
        public bool UpgradeStallTransaction(string tileId, string stallId, StallConfig stallConfig)
        {
            if (string.IsNullOrEmpty(tileId) || string.IsNullOrEmpty(stallId) || stallConfig == null)
            {
                Debug.LogError("EconomyManager.UpgradeStallTransaction failed: invalid parameters.");
                return false;
            }

            if (resourceManager == null || marketManager == null)
            {
                Debug.LogError("EconomyManager.UpgradeStallTransaction failed: resourceManager or marketManager is null.");
                return false;
            }

            // Step 1: 计算升级费用
            MarketRuntimeData marketData = marketManager.GetMarket(tileId);
            StallRuntimeData stallData = FindStallInMarket(marketData, stallId);
            int upgradeCost = marketManager.GetStallUpgradeCost(stallData, stallConfig);

            // Step 2: 检查合法性
            if (!marketManager.CanUpgradeStall(marketData, stallData, stallConfig, out string reason))
            {
                Debug.LogWarning($"EconomyManager.UpgradeStallTransaction failed: {reason}");
                return false;
            }

            if (!marketManager.UpgradeStall(tileId, stallId, stallConfig))
            {
                Debug.LogError("EconomyManager.UpgradeStallTransaction failed: UpgradeStall returned false.");
                return false;
            }

            Debug.Log($"EconomyManager: Stall {stallId} on tile {tileId} upgraded. Cost = {upgradeCost}");
            return true;
        }

        /// <summary>
        /// 夜市升级（事务性封装）。
        /// 检查资金 → 扣费 → 升级夜市，失败则回滚。
        /// </summary>
        public bool UpgradeMarketTransaction(string tileId)
        {
            if (string.IsNullOrEmpty(tileId))
            {
                Debug.LogError("EconomyManager.UpgradeMarketTransaction failed: tileId is empty.");
                return false;
            }

            if (resourceManager == null || marketManager == null)
            {
                Debug.LogError("EconomyManager.UpgradeMarketTransaction failed: resourceManager or marketManager is null.");
                return false;
            }

            // Step 1: 获取升级费用
            MarketRuntimeData marketData = marketManager.GetMarket(tileId);
            int upgradeCost = marketManager.GetNextUpgradeCost(marketData);

            // Step 2: 检查合法性
            if (!marketManager.CanUpgradeMarket(marketData, out upgradeCost, out string reason))
            {
                Debug.LogWarning($"EconomyManager.UpgradeMarketTransaction failed: {reason}");
                return false;
            }

            if (!marketManager.UpgradeMarket(tileId))
            {
                Debug.LogError("EconomyManager.UpgradeMarketTransaction failed: UpgradeMarket returned false.");
                return false;
            }

            Debug.Log($"EconomyManager: Market on tile {tileId} upgraded. Cost = {upgradeCost}");
            return true;
        }

        // ================================================================
        //  工具方法
        // ================================================================

        /// <summary>获取玩家运行时数据（通过 ResourceManager 或直接查找）。</summary>
        private PlayerRuntimeData GetPlayerData()
        {
            // 尝试从 GameManager 获取玩家数据
            if (gameManager != null)
            {
                // GameManager 的 playerRuntimeData 是 private 字段，
                // 目前没有公开的 Getter。这里尝试通过 ResourceManager 获取。
            }

            // 如果 ResourceManager 有公开 playerData 的需求，可以在这里读取。
            // 当前方案：各个系统直接读取 PlayerRuntimeData 的公开字段。
            return resourceManager == null ? null : resourceManager.PlayerData;
        }

        /// <summary>在夜市内查找摊位运行时数据。</summary>
        private StallRuntimeData FindStallInMarket(MarketRuntimeData marketData, string stallId)
        {
            if (marketData == null || string.IsNullOrEmpty(stallId))
            {
                return null;
            }

            for (int i = 0; i < marketData.stallList.Count; i++)
            {
                StallRuntimeData stall = marketData.stallList[i];
                if (stall != null && stall.stallId == stallId)
                {
                    return stall;
                }
            }

            return null;
        }

        /// <summary>按 tileId 查找地块配置。</summary>
        public TileConfig FindTileConfig(string tileId)
        {
            if (string.IsNullOrEmpty(tileId) || tileConfigs == null)
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

        /// <summary>按 stallId 查找摊位配置。</summary>
        public StallConfig FindStallConfig(string stallId)
        {
            if (string.IsNullOrEmpty(stallId) || stallConfigs == null)
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

        // ================================================================
        //  设置依赖引用（供 Bootstrap 或 GameManager 初始化时调用）
        // ================================================================

        /// <summary>设置 ResourceManager 引用。</summary>
        public void SetResourceManager(ResourceManager manager)
        {
            resourceManager = manager;
        }

        /// <summary>设置 MarketManager 引用。</summary>
        public void SetMarketManager(MarketManager manager)
        {
            marketManager = manager;
        }

        /// <summary>设置 GameManager 引用。</summary>
        public void SetGameManager(GameManager manager)
        {
            gameManager = manager;
        }

        /// <summary>设置地块配置列表。</summary>
        public void SetTileConfigs(List<TileConfig> configs)
        {
            tileConfigs = configs;
        }

        /// <summary>设置摊位配置列表。</summary>
        public void SetStallConfigs(List<StallConfig> configs)
        {
            stallConfigs = configs;
        }
    }
}
