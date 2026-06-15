using System;
using System.Collections.Generic;
using CampusNightMarket.Common;
using CampusNightMarket.Core;
using CampusNightMarket.Map;
using CampusNightMarket.Player;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CampusNightMarket.Turn
{
    // 回合管理器：控制单日阶段流、投骰结果和玩家移动选择。
    public class TurnManager : MonoBehaviour
    {
        // 全局游戏管理器，用于读写当前阶段和推进天数。
        [SerializeField] private GameManager gameManager;
        // 骰子最小点数。
        [SerializeField] private int minDiceValue = 1;
        // 骰子最大点数。
        [SerializeField] private int maxDiceValue = 6;

        private MapManager mapManager;
        private PlayerRuntimeData playerRuntimeData;

        // 阶段变化事件，UI 或调试面板可以订阅。
        public event Action<GamePhase> PhaseChanged;
        // 投骰完成事件，UI 可以订阅后展示点数。
        public event Action<int> DiceRolled;

        // 当前骰子点数。
        public int CurrentDiceValue { get; private set; }
        // 玩家本回合选择的目标地块ID。
        public string SelectedTargetTileId { get; private set; }
        // 当前已经验证通过的移动路径。
        public List<string> CurrentMovePath { get; private set; } = new List<string>();
        // 当前移动需要消耗的步数。
        public int CurrentMoveRequiredSteps { get; private set; }
        // 当前是否处于允许投骰的阶段。
        public bool CanRollDice
        {
            get { return IsCurrentPhase(GamePhase.RollDice); }
        }

        // 绑定地图和玩家数据，之后目标合法性由TurnManager内部统一检查。
        public void ConfigureMovement(MapManager runtimeMapManager, PlayerRuntimeData playerData)
        {
            mapManager = runtimeMapManager;
            playerRuntimeData = playerData;
        }


        // 开始第一天，进入 DayStart 阶段。
        public void BeginFirstDay()
        {
            ChangePhase(GamePhase.DayStart);
        }

        // 进入投骰阶段，通常由“开始行动”按钮或 DayStart 后调用。
        public void StartRollDice()
        {
            if (!IsCurrentPhase(GamePhase.DayStart))
            {
                Debug.LogWarning("StartRollDice ignored because current phase is not DayStart.");
                return;
            }

            ChangePhase(GamePhase.RollDice);
        }

        // 执行投骰，并在投骰后进入选择移动阶段。
        public int RollDice()
        {
            if (!IsCurrentPhase(GamePhase.RollDice))
            {
                Debug.LogWarning("RollDice ignored because current phase is not RollDice.");
                return 0;
            }

            CurrentDiceValue = Random.Range(minDiceValue, maxDiceValue + 1);
            DiceRolled?.Invoke(CurrentDiceValue);
            ChangePhase(GamePhase.ChooseMove);
            return CurrentDiceValue;
        }

        // 选择目标地块；地图系统负责计算路径和实际步数。
        public bool SelectMoveTarget(string targetTileId)
        {
            if (!IsCurrentPhase(GamePhase.ChooseMove))
            {
                Debug.LogWarning("SelectMoveTarget ignored because current phase is not ChooseMove.");
                return false;
            }

            if (mapManager == null || playerRuntimeData == null)
            {
                Debug.LogWarning("SelectMoveTarget failed because movement dependencies are not configured.");
                return false;
            }

            if (!mapManager.IsMoveTargetValid(
                    playerRuntimeData.currentTileId,
                    targetTileId,
                    CurrentDiceValue,
                    playerRuntimeData.energy,
                    out int requiredSteps,
                    out string reason))
            {
                Debug.LogWarning("SelectMoveTarget failed: " + reason);
                return false;
            }

            if (!mapManager.TryGetPath(
                    playerRuntimeData.currentTileId,
                    targetTileId,
                    requiredSteps,
                    out List<string> pathTileIds))
            {
                Debug.LogWarning("SelectMoveTarget failed because no valid path was found.");
                return false;
            }

            SelectedTargetTileId = targetTileId;
            CurrentMoveRequiredSteps = requiredSteps;
            CurrentMovePath = pathTileIds;
            ChangePhase(GamePhase.MovePlayer);
            return true;
        }

        // 旧原型接口，仅为已有调用保留；新代码应调用只接收targetTileId的重载。
        [Obsolete("Use SelectMoveTarget(string targetTileId) after ConfigureMovement.")]
        public bool SelectMoveTarget(string targetTileId, int requiredSteps)
        {
            if (!IsCurrentPhase(GamePhase.ChooseMove))
            {
                Debug.LogWarning("SelectMoveTarget ignored because current phase is not ChooseMove.");
                return false;
            }

            if (string.IsNullOrEmpty(targetTileId) || requiredSteps < 1 || requiredSteps > CurrentDiceValue)
            {
                return false;
            }

            SelectedTargetTileId = targetTileId;
            CurrentMoveRequiredSteps = requiredSteps;
            CurrentMovePath.Clear();
            ChangePhase(GamePhase.MovePlayer);
            return true;
        }

        // 玩家移动动画或位置更新结束后调用。
        public void NotifyPlayerMoveFinished()
        {
            if (IsCurrentPhase(GamePhase.MovePlayer))
            {
                ChangePhase(GamePhase.TileInteraction);
            }
        }

        // 地块交互结束后调用，进入夜晚结算。
        public void NotifyTileInteractionFinished()
        {
            if (IsCurrentPhase(GamePhase.TileInteraction))
            {
                ChangePhase(GamePhase.NightSettlement);
            }
        }

        // 夜晚结算和结算面板处理结束后调用。
        public void NotifyNightSettlementFinished()
        {
            if (IsCurrentPhase(GamePhase.NightSettlement))
            {
                ChangePhase(GamePhase.DayEnd);
            }
        }

        // 结束当天，清空本回合选择并推进到下一天。
        public void EndDay()
        {
            if (!IsCurrentPhase(GamePhase.DayEnd))
            {
                return;
            }

            CurrentDiceValue = 0;
            SelectedTargetTileId = string.Empty;
            CurrentMoveRequiredSteps = 0;
            CurrentMovePath.Clear();

            if (gameManager != null)
            {
                gameManager.AdvanceDay();
                if (gameManager.RuntimeData.isGameOver || gameManager.RuntimeData.currentPhase == GamePhase.GameWin)
                {
                    return;
                }
            }

            ChangePhase(GamePhase.DayStart);
        }

        // 胜利提示确认后调用，进入无尽模式下的下一天。
        public void ContinueAfterWin()
        {
            if (gameManager == null)
            {
                return;
            }

            if (gameManager.RuntimeData.isEndlessMode && IsCurrentPhase(GamePhase.GameWin))
            {
                ChangePhase(GamePhase.DayStart);
            }
        }

        // 检查当前阶段；未绑定 GameManager 时放行，便于早期单独测试。
        private bool IsCurrentPhase(GamePhase phase)
        {
            return gameManager == null || gameManager.RuntimeData.currentPhase == phase;
        }

        // 统一切换阶段，并通知订阅者。
        private void ChangePhase(GamePhase phase)
        {
            if (gameManager != null)
            {
                gameManager.SetPhase(phase);
            }

            PhaseChanged?.Invoke(phase);
        }
    }
}
