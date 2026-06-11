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
        // 当前玩家运行时数据，后续可由 PlayerManager 或 Bootstrap 注入。
        [SerializeField] private PlayerRuntimeData playerRuntimeData;

        // 当前使用的地图配置，供推进天数和胜负判定使用。
        private MapConfig currentMapConfig;

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

            currentMapConfig = mapConfig;
            RuntimeData.currentMapId = mapConfig.mapId;
            RuntimeData.currentDay = 1;
            RuntimeData.currentPhase = GamePhase.Init;
            RuntimeData.currentWeatherId = string.Empty;
            RuntimeData.isGameOver = false;
            RuntimeData.isWin = false;
            RuntimeData.isEndlessMode = false;
            RuntimeData.victoryTarget = CalculateVictoryTarget(mapConfig);
        }

        // 初始化新游戏并同时绑定玩家运行时数据。
        public void InitializeNewGame(MapConfig mapConfig, PlayerRuntimeData playerData)
        {
            playerRuntimeData = playerData;
            InitializeNewGame(mapConfig);
        }

        // 使用 Inspector 中配置的默认地图初始化。
        public void InitializeWithDefaultMap()
        {
            InitializeNewGame(initialMapConfig);
        }

        // 绑定玩家运行时数据，便于 AdvanceDay 内部检查胜负。
        public void SetPlayerRuntimeData(PlayerRuntimeData playerData)
        {
            playerRuntimeData = playerData;
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

        // 限期日当天夜晚结束后先判定胜负；失败才停止，胜利则进入无尽模式并继续推进天数。
        public void AdvanceDay()
        {
            EvaluateGameResult(playerRuntimeData, currentMapConfig);
            if (RuntimeData.isGameOver)
            {
                return;
            }

            RuntimeData.currentDay += 1;
        }

        // 根据“限期日当天晚上结束后判定”的规则检查胜负。
        public void EvaluateGameResult(PlayerRuntimeData playerData, MapConfig mapConfig)
        {
            if (playerData == null || mapConfig == null || RuntimeData.isGameOver || RuntimeData.isEndlessMode)
            {
                return;
            }

            if (RuntimeData.currentDay < mapConfig.maxDay)
            {
                return;
            }

            if (playerData.money >= RuntimeData.victoryTarget)
            {
                RuntimeData.isWin = true;
                RuntimeData.isGameOver = false;
                RuntimeData.isEndlessMode = true;
                RuntimeData.currentPhase = GamePhase.GameWin;
                playerData.loan = 0;
                return;
            }

            RuntimeData.isWin = false;
            RuntimeData.isGameOver = true;
            RuntimeData.currentPhase = GamePhase.GameLose;
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
