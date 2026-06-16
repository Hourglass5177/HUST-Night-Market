using System.Collections.Generic;
using CampusNightMarket.Common;
using CampusNightMarket.Economy;
using CampusNightMarket.Market;
using CampusNightMarket.Player;
using UnityEngine;

namespace CampusNightMarket.RandomSystem
{
    /// <summary>
    /// 随机事件管理器：负责在指定时机触发事件、抽取事件和执行事件效果。
    /// 利用 Common 中的 EventTriggerType 和 EventEffectType 枚举。
    /// </summary>
    public class EventManager : MonoBehaviour
    {
        [Header("依赖引用")]
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private MarketManager marketManager;

        [Header("事件配置")]
        [SerializeField] private List<GameEvent> eventPool = new List<GameEvent>();

        [Header("事件参数")]
        [SerializeField] private float baseTriggerChance = 0.15f; // 基础触发概率

        /// <summary>
        /// 在指定时机触发随机事件。
        /// 遍历事件池中所有符合 triggerType 的事件，按概率抽取并执行。
        /// </summary>
        /// <param name="triggerType">触发时机</param>
        /// <param name="contextTileId">触发上下文的地块ID（到达地块时传入）</param>
        public List<EventRuntimeData> TriggerEvents(EventTriggerType triggerType, string contextTileId = "")
        {
            List<EventRuntimeData> triggeredEvents = new List<EventRuntimeData>();

            // 过滤出当前触发时机的事件
            List<GameEvent> candidates = GetCandidates(triggerType);

            for (int i = 0; i < candidates.Count; i++)
            {
                GameEvent gameEvent = candidates[i];

                // 按概率判定是否触发
                if (Random.value > gameEvent.probability)
                {
                    continue;
                }

                // 执行事件效果
                ExecuteEventEffect(gameEvent);

                // 记录触发历史
                EventRuntimeData runtimeData = new EventRuntimeData
                {
                    eventId = gameEvent.eventId,
                    triggeredDay = GetCurrentDay(),
                    triggeredTileId = contextTileId,
                    playerChoice = string.Empty
                };

                triggeredEvents.Add(runtimeData);
                Debug.Log($"EventManager: Event '{gameEvent.eventName}' triggered. Effect: {gameEvent.effectType} = {gameEvent.effectValue}");
            }

            return triggeredEvents;
        }

        /// <summary>
        /// 从指定事件池中随机抽取一个事件并执行。
        /// </summary>
        public EventRuntimeData PickAndExecuteFromPool(string eventPoolId, string contextTileId = "")
        {
            if (string.IsNullOrEmpty(eventPoolId))
            {
                return null;
            }

            // 过滤出属于该事件池的事件
            List<GameEvent> poolEvents = new List<GameEvent>();
            for (int i = 0; i < eventPool.Count; i++)
            {
                if (eventPool[i] != null && eventPool[i].eventPoolId == eventPoolId)
                {
                    poolEvents.Add(eventPool[i]);
                }
            }

            if (poolEvents.Count == 0)
            {
                return null;
            }

            // 随机抽取一个
            GameEvent picked = poolEvents[Random.Range(0, poolEvents.Count)];

            // 执行效果
            ExecuteEventEffect(picked);

            EventRuntimeData runtimeData = new EventRuntimeData
            {
                eventId = picked.eventId,
                triggeredDay = GetCurrentDay(),
                triggeredTileId = contextTileId,
                playerChoice = string.Empty
            };

            Debug.Log($"EventManager: Pool event '{picked.eventName}' picked from pool '{eventPoolId}'.");
            return runtimeData;
        }

        /// <summary>
        /// 执行单个事件的效果。
        /// 根据 EventEffectType 调用对应的 ResourceManager 或 MarketManager 方法。
        /// </summary>
        public void ExecuteEventEffect(GameEvent gameEvent)
        {
            if (gameEvent == null)
            {
                return;
            }

            if (resourceManager == null)
            {
                Debug.LogError("EventManager.ExecuteEventEffect failed: resourceManager is null.");
                return;
            }

            switch (gameEvent.effectType)
            {
                case EventEffectType.AddMoney:
                    if (gameEvent.effectValue >= 0)
                        resourceManager.AddMoney(gameEvent.effectValue);
                    else
                        resourceManager.SpendMoney(-gameEvent.effectValue);
                    break;

                case EventEffectType.AddLowFood:
                    if (gameEvent.effectValue >= 0)
                        resourceManager.AddLowFood(gameEvent.effectValue);
                    else
                        resourceManager.ConsumeLowFood(-gameEvent.effectValue);
                    break;

                case EventEffectType.AddHighFood:
                    if (gameEvent.effectValue >= 0)
                        resourceManager.AddHighFood(gameEvent.effectValue);
                    else
                        resourceManager.ConsumeHighFood(-gameEvent.effectValue);
                    break;

                case EventEffectType.AddReputation:
                    resourceManager.AddReputation(Mathf.Abs(gameEvent.effectValue));
                    break;

                case EventEffectType.AddEnergy:
                    if (gameEvent.effectValue >= 0)
                        resourceManager.RestoreEnergy(gameEvent.effectValue);
                    else
                        resourceManager.ConsumeEnergy(-gameEvent.effectValue);
                    break;

                case EventEffectType.ModifyTraffic:
                    // 客流修改由 CustomerManager 处理，此处只记录日志
                    Debug.Log($"EventManager: Traffic modifier event. Value={gameEvent.effectValue}%.");
                    // 这里需要 CustomerManager 应用该倍率。
                    break;

                case EventEffectType.ModifyCompetition:
                    // 竞争强度修改由地图系统处理
                    Debug.Log($"EventManager: Competition modifier event. Value={gameEvent.effectValue}.");
                    // 这里需要 MapManager 修改地块竞争强度。
                    break;

                case EventEffectType.ModifyHygiene:
                    // 卫生值修改由 InspectionManager 处理
                    Debug.Log($"EventManager: Hygiene modifier event. Value={gameEvent.effectValue}.");
                    // 这里需要 InspectionManager 修改夜市总卫生值。
                    break;

                case EventEffectType.TriggerInspection:
                    Debug.Log($"EventManager: Forced inspection triggered by event.");
                    // 这里需要 InspectionManager.ForceInspection() 强制触发检查。
                    break;

                case EventEffectType.CloseMarket:
                    if (marketManager != null && gameEvent.effectValue > 0)
                    {
                        // 关闭所有玩家夜市
                        List<MarketRuntimeData> markets = marketManager.Markets;
                        for (int i = 0; i < markets.Count; i++)
                        {
                            if (markets[i] != null && markets[i].owner == OwnerType.Player)
                            {
                                marketManager.AddClosedRounds(markets[i].tileId, gameEvent.effectValue);
                            }
                        }
                        Debug.Log($"EventManager: All player markets closed for {gameEvent.effectValue} rounds.");
                    }
                    break;

                case EventEffectType.None:
                default:
                    Debug.Log($"EventManager: Event '{gameEvent.eventName}' has no effect (None).");
                    break;
            }
        }

        /// <summary>
        /// 手动触发二选一事件（由 UI 调用）。
        /// </summary>
        public void MakeEventChoice(EventRuntimeData eventRuntime, bool chooseA)
        {
            if (eventRuntime == null)
            {
                return;
            }

            // 查找对应的事件配置
            GameEvent gameEvent = FindEvent(eventRuntime.eventId);
            if (gameEvent == null || !gameEvent.requireChoice)
            {
                return;
            }

            int chosenValue = chooseA ? gameEvent.choiceAValue : gameEvent.choiceBValue;
            eventRuntime.playerChoice = chooseA ? "A" : "B";

            // 应用选择效果
            GameEvent choiceEvent = new GameEvent
            {
                eventId = gameEvent.eventId + "_CHOICE",
                effectType = gameEvent.effectType,
                effectValue = chosenValue
            };
            ExecuteEventEffect(choiceEvent);

            Debug.Log($"EventManager: Choice made for '{gameEvent.eventName}': {(chooseA ? "A" : "B")}, value={chosenValue}");
        }

        /// <summary>
        /// 获取当前天数（通过 GameManager 或默认值）。
        /// </summary>
        private int GetCurrentDay()
        {
            // 简化处理，后续接入 GameManager
            return 1;
        }

        /// <summary>
        /// 过滤出指定触发时机的事件候选列表。
        /// </summary>
        private List<GameEvent> GetCandidates(EventTriggerType triggerType)
        {
            List<GameEvent> candidates = new List<GameEvent>();

            for (int i = 0; i < eventPool.Count; i++)
            {
                GameEvent gameEvent = eventPool[i];
                if (gameEvent != null && gameEvent.triggerType == triggerType)
                {
                    candidates.Add(gameEvent);
                }
            }

            return candidates;
        }

        /// <summary>
        /// 按 eventId 查找事件配置。
        /// </summary>
        private GameEvent FindEvent(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                return null;
            }

            for (int i = 0; i < eventPool.Count; i++)
            {
                if (eventPool[i] != null && eventPool[i].eventId == eventId)
                {
                    return eventPool[i];
                }
            }

            return null;
        }

        /// <summary>
        /// 添加一个新事件到事件池。
        /// </summary>
        public void AddEvent(GameEvent gameEvent)
        {
            if (gameEvent != null)
            {
                eventPool.Add(gameEvent);
            }
        }

        /// <summary>
        /// 设置 ResourceManager 引用。
        /// </summary>
        public void SetResourceManager(ResourceManager manager)
        {
            resourceManager = manager;
        }

        /// <summary>
        /// 设置 MarketManager 引用。
        /// </summary>
        public void SetMarketManager(MarketManager manager)
        {
            marketManager = manager;
        }
    }
}