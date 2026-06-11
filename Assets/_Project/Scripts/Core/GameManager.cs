using CampusNightMarket.Common;
using CampusNightMarket.Data;
using CampusNightMarket.Player;
using UnityEngine;

namespace CampusNightMarket.Core
{
    // 全局游戏管理器：负责新游戏初始化、全局阶段状态和胜负判定。
    public class GameManager : MonoBehaviour
    {
        // 默认启动地图，后续可由主菜单或 Bootstrap 注入。
        [SerializeField] private MapConfig initialMapConfig;

        // 当前游戏全局运行时数据。
        public GameRuntimeData RuntimeData { get; private set; } = new GameRuntimeData();

        // 根据指定地图配置初始化一局新游戏。
        public void InitializeNewGame(MapConfig mapConfig)
        {
            if (mapConfig == null)
            {
                Debug.LogError("GameManager.InitializeNewGame failed: mapConfig is null.");
                return;
            }

            RuntimeData.currentMapId = mapConfig.mapId;
            RuntimeData.currentDay = 1;
            RuntimeData.currentPhase = GamePhase.Init;
            RuntimeData.currentWeatherId = string.Empty;
            RuntimeData.isGameOver = false;
            RuntimeData.isWin = false;
            RuntimeData.victoryTarget = CalculateVictoryTarget(mapConfig);
        }

        // 使用 Inspector 中配置的默认地图初始化。
        public void InitializeWithDefaultMap()
        {
            InitializeNewGame(initialMapConfig);
        }

        // 设置当前阶段；游戏结束后不再允许普通阶段覆盖胜负状态。
        public void SetPhase(GamePhase nextPhase)
        {
            if (RuntimeData.isGameOver)
            {
                return;
            }

            RuntimeData.currentPhase = nextPhase;
        }

        // 推进到下一天。
        public void AdvanceDay()
        {
            RuntimeData.currentDay += 1;
        }

        // 根据玩家资金和地图天数限制检查胜负。
        public void EvaluateGameResult(PlayerRuntimeData playerData, MapConfig mapConfig)
        {
            if (playerData == null || mapConfig == null || RuntimeData.isGameOver)
            {
                return;
            }

            if (playerData.money >= RuntimeData.victoryTarget)
            {
                RuntimeData.isGameOver = true;
                RuntimeData.isWin = true;
                RuntimeData.currentPhase = GamePhase.GameWin;
                return;
            }

            if (RuntimeData.currentDay > mapConfig.maxDay)
            {
                RuntimeData.isGameOver = true;
                RuntimeData.isWin = false;
                RuntimeData.currentPhase = GamePhase.GameLose;
            }
        }

        // 胜利目标 = (初始资金 + 贷款) * 胜利倍率。
        public int CalculateVictoryTarget(MapConfig mapConfig)
        {
            if (mapConfig == null)
            {
                return 0;
            }

            return (mapConfig.initialMoney + mapConfig.loanAmount) * mapConfig.victoryMultiplier;
        }
    }
}
