using CampusNightMarket.Player;
using UnityEngine;

namespace CampusNightMarket.Economy
{
    /// <summary>
    /// 资源管理器：负责玩家资金、食材、体力、口碑等资源的检查和增减操作。
    /// 注意：ResourceManager 只做数值操作，不做规则判断。
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        [SerializeField] private PlayerRuntimeData playerData;

        // ================================================================
        //  资金操作
        // ================================================================

        /// <summary>检查玩家资金是否足够（>= amount）。</summary>
        public bool CheckMoney(int amount)
        {
            if (playerData == null)
            {
                Debug.LogError("ResourceManager.CheckMoney failed: playerData is null.");
                return false;
            }

            return playerData.money >= amount;
        }

        /// <summary>扣除资金，扣费成功返回 true，资金不足返回 false。</summary>
        public bool SpendMoney(int amount)
        {
            if (playerData == null)
            {
                Debug.LogError("ResourceManager.SpendMoney failed: playerData is null.");
                return false;
            }

            if (playerData.money < amount)
            {
                Debug.LogWarning($"ResourceManager.SpendMoney failed: insufficient money. Required: {amount}, Current: {playerData.money}");
                return false;
            }

            playerData.money -= amount;
            return true;
        }

        /// <summary>增加资金。</summary>
        public void AddMoney(int amount)
        {
            if (playerData == null)
            {
                Debug.LogError("ResourceManager.AddMoney failed: playerData is null.");
                return;
            }

            if (amount < 0)
            {
                Debug.LogWarning("ResourceManager.AddMoney called with negative amount. Use SpendMoney instead.");
                return;
            }

            playerData.money += amount;
        }

        // ================================================================
        //  食材操作 — 低端食材
        // ================================================================

        /// <summary>检查低端食材数量是否足够。</summary>
        public bool CheckLowFood(int amount)
        {
            if (playerData == null)
            {
                Debug.LogError("ResourceManager.CheckLowFood failed: playerData is null.");
                return false;
            }

            return playerData.lowFood >= amount;
        }

        /// <summary>消耗低端食材，不足时返回 false。</summary>
        public bool ConsumeLowFood(int amount)
        {
            if (playerData == null)
            {
                Debug.LogError("ResourceManager.ConsumeLowFood failed: playerData is null.");
                return false;
            }

            if (playerData.lowFood < amount)
            {
                Debug.LogWarning($"ResourceManager.ConsumeLowFood failed: insufficient lowFood. Required: {amount}, Current: {playerData.lowFood}");
                return false;
            }

            playerData.lowFood -= amount;
            return true;
        }

        /// <summary>增加低端食材。</summary>
        public void AddLowFood(int amount)
        {
            if (playerData == null)
            {
                Debug.LogError("ResourceManager.AddLowFood failed: playerData is null.");
                return;
            }

            if (amount < 0)
            {
                Debug.LogWarning("ResourceManager.AddLowFood called with negative amount.");
                return;
            }

            playerData.lowFood += amount;
        }

        // ================================================================
        //  食材操作 — 高端食材
        // ================================================================

        /// <summary>检查高端食材数量是否足够。</summary>
        public bool CheckHighFood(int amount)
        {
            if (playerData == null)
            {
                Debug.LogError("ResourceManager.CheckHighFood failed: playerData is null.");
                return false;
            }

            return playerData.highFood >= amount;
        }

        /// <summary>消耗高端食材，不足时返回 false。</summary>
        public bool ConsumeHighFood(int amount)
        {
            if (playerData == null)
            {
                Debug.LogError("ResourceManager.ConsumeHighFood failed: playerData is null.");
                return false;
            }

            if (playerData.highFood < amount)
            {
                Debug.LogWarning($"ResourceManager.ConsumeHighFood failed: insufficient highFood. Required: {amount}, Current: {playerData.highFood}");
                return false;
            }

            playerData.highFood -= amount;
            return true;
        }

        /// <summary>增加高端食材。</summary>
        public void AddHighFood(int amount)
        {
            if (playerData == null)
            {
                Debug.LogError("ResourceManager.AddHighFood failed: playerData is null.");
                return;
            }

            if (amount < 0)
            {
                Debug.LogWarning("ResourceManager.AddHighFood called with negative amount.");
                return;
            }

            playerData.highFood += amount;
        }

        // ================================================================
        //  体力操作
        // ================================================================

        /// <summary>检查当前体力是否足够（>= amount）。</summary>
        public bool CheckEnergy(int amount)
        {
            if (playerData == null)
            {
                Debug.LogError("ResourceManager.CheckEnergy failed: playerData is null.");
                return false;
            }

            return playerData.energy >= amount;
        }

        /// <summary>消耗体力，不足时返回 false。</summary>
        public bool ConsumeEnergy(int amount)
        {
            if (playerData == null)
            {
                Debug.LogError("ResourceManager.ConsumeEnergy failed: playerData is null.");
                return false;
            }

            if (playerData.energy < amount)
            {
                Debug.LogWarning($"ResourceManager.ConsumeEnergy failed: insufficient energy. Required: {amount}, Current: {playerData.energy}");
                return false;
            }

            playerData.energy -= amount;
            return true;
        }

        /// <summary>恢复体力（不超过上限）。</summary>
        public void RestoreEnergy(int amount)
        {
            if (playerData == null)
            {
                Debug.LogError("ResourceManager.RestoreEnergy failed: playerData is null.");
                return;
            }

            if (amount < 0)
            {
                Debug.LogWarning("ResourceManager.RestoreEnergy called with negative amount.");
                return;
            }

            playerData.energy = Mathf.Min(playerData.energy + amount, playerData.maxEnergy);
        }

        // ================================================================
        //  口碑操作
        // ================================================================

        /// <summary>增加口碑值。</summary>
        public void AddReputation(int amount)
        {
            if (playerData == null)
            {
                Debug.LogError("ResourceManager.AddReputation failed: playerData is null.");
                return;
            }

            if (amount < 0)
            {
                Debug.LogWarning("ResourceManager.AddReputation called with negative amount.");
                return;
            }

            playerData.reputation += amount;
        }

        /// <summary>获取当前口碑值。</summary>
        public int GetCurrentReputation()
        {
            if (playerData == null)
            {
                Debug.LogError("ResourceManager.GetCurrentReputation failed: playerData is null.");
                return 0;
            }

            return playerData.reputation;
        }

        /// <summary>设置玩家数据引用（可在运行时动态绑定）。</summary>
        public void SetPlayerData(PlayerRuntimeData data)
        {
            playerData = data;
        }
    }
}