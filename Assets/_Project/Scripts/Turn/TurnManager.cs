using System;
using CampusNightMarket.Common;
using CampusNightMarket.Core;
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

        // 阶段变化事件，UI 或调试面板可以订阅。
        public event Action<GamePhase> PhaseChanged;
        // 投骰完成事件，UI 可以订阅后展示点数。
        public event Action<int> DiceRolled;

        // 当前骰子点数。
        public int CurrentDiceValue { get; private set; }
        // 玩家本回合选择的目标地块ID。
        public string SelectedTargetTileId { get; private set; }

        // 开始第一天，进入 DayStart 阶段。
        public void BeginFirstDay()
        {
            ChangePhase(GamePhase.DayStart);
        }

        // 进入投骰阶段，通常由“开始行动”按钮或 DayStart 后调用。
        public void StartRollDice()
        {
            ChangePhase(GamePhase.RollDice);
        }

        // 执行投骰，并在投骰后进入选择移动阶段。
        public int RollDice()
        {
            if (!IsCurrentPhase(GamePhase.RollDice))
            {
                Debug.LogWarning("RollDice ignored because current phase is not RollDice.");
                return CurrentDiceValue;
            }

            CurrentDiceValue = Random.Range(minDiceValue, maxDiceValue + 1);
            DiceRolled?.Invoke(CurrentDiceValue);
            ChangePhase(GamePhase.ChooseMove);
            return CurrentDiceValue;
        }

        // 选择目标地块；目标合法性目前只检查步数，后续接入 MapManager。
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

            if (gameManager != null)
            {
                gameManager.AdvanceDay();
            }

            ChangePhase(GamePhase.DayStart);
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
