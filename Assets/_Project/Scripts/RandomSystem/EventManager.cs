using System.Collections.Generic;
using CampusNightMarket.Common;
using CampusNightMarket.Core;
using CampusNightMarket.Economy;
using CampusNightMarket.Market;
using UnityEngine;

namespace CampusNightMarket.RandomSystem
{
    public class EventManager : MonoBehaviour
    {
        [Header("依赖引用")]
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private MarketManager marketManager;
        [SerializeField] private InspectionManager inspectionManager;
        [SerializeField] private WeatherManager weatherManager;
        [SerializeField] private GameManager gameManager;

        [Header("事件配置")]
        [SerializeField] private List<GameEvent> eventPool = new List<GameEvent>();
        [SerializeField] private bool seedDefaultEventsWhenEmpty = true;

        private void Awake()
        {
            SeedDefaultEventsIfNeeded();
        }

        public List<EventRuntimeData> TriggerEvents(
            EventTriggerType triggerType,
            string contextTileId = "")
        {
            SeedDefaultEventsIfNeeded();

            List<EventRuntimeData> triggeredEvents = new List<EventRuntimeData>();
            List<GameEvent> candidates = GetCandidates(triggerType);
            for (int i = 0; i < candidates.Count; i++)
            {
                GameEvent gameEvent = candidates[i];
                if (gameEvent == null || Random.value > Mathf.Clamp01(gameEvent.probability))
                {
                    continue;
                }

                ExecuteEventEffect(gameEvent, contextTileId);
                triggeredEvents.Add(CreateRuntimeData(gameEvent, contextTileId));
                Debug.Log(
                    $"EventManager: Event '{gameEvent.eventName}' triggered. " +
                    $"effect={gameEvent.effectType}, value={gameEvent.effectValue}");
            }

            return triggeredEvents;
        }

        public EventRuntimeData PickAndExecuteFromPool(
            string eventPoolId,
            string contextTileId = "")
        {
            if (string.IsNullOrEmpty(eventPoolId))
            {
                return null;
            }

            SeedDefaultEventsIfNeeded();

            List<GameEvent> poolEvents = new List<GameEvent>();
            for (int i = 0; i < eventPool.Count; i++)
            {
                GameEvent gameEvent = eventPool[i];
                if (gameEvent != null && gameEvent.eventPoolId == eventPoolId)
                {
                    poolEvents.Add(gameEvent);
                }
            }

            if (poolEvents.Count == 0)
            {
                Debug.LogWarning($"EventManager: No events configured for pool '{eventPoolId}'.");
                return null;
            }

            GameEvent picked = poolEvents[Random.Range(0, poolEvents.Count)];
            ExecuteEventEffect(picked, contextTileId);
            Debug.Log(
                $"EventManager: Pool event '{picked.eventName}' picked from '{eventPoolId}'.");
            return CreateRuntimeData(picked, contextTileId);
        }

        public void ExecuteEventEffect(GameEvent gameEvent)
        {
            ExecuteEventEffect(gameEvent, string.Empty);
        }

        public void MakeEventChoice(EventRuntimeData eventRuntime, bool chooseA)
        {
            if (eventRuntime == null)
            {
                return;
            }

            GameEvent gameEvent = FindEvent(eventRuntime.eventId);
            if (gameEvent == null || !gameEvent.requireChoice)
            {
                return;
            }

            int chosenValue = chooseA ? gameEvent.choiceAValue : gameEvent.choiceBValue;
            eventRuntime.playerChoice = chooseA ? "A" : "B";

            GameEvent choiceEvent = new GameEvent
            {
                eventId = gameEvent.eventId + "_CHOICE",
                eventName = gameEvent.eventName,
                effectType = gameEvent.effectType,
                effectValue = chosenValue
            };
            ExecuteEventEffect(choiceEvent, eventRuntime.triggeredTileId);
        }

        public void AddEvent(GameEvent gameEvent)
        {
            if (gameEvent != null)
            {
                eventPool.Add(gameEvent);
            }
        }

        public void SetResourceManager(ResourceManager manager)
        {
            resourceManager = manager;
        }

        public void SetMarketManager(MarketManager manager)
        {
            marketManager = manager;
        }

        public void SetInspectionManager(InspectionManager manager)
        {
            inspectionManager = manager;
        }

        public void SetWeatherManager(WeatherManager manager)
        {
            weatherManager = manager;
        }

        public void SetGameManager(GameManager manager)
        {
            gameManager = manager;
        }

        private void ExecuteEventEffect(GameEvent gameEvent, string contextTileId)
        {
            if (gameEvent == null)
            {
                return;
            }

            switch (gameEvent.effectType)
            {
                case EventEffectType.AddMoney:
                    ApplyMoney(gameEvent.effectValue);
                    break;
                case EventEffectType.AddLowFood:
                    ApplyLowFood(gameEvent.effectValue);
                    break;
                case EventEffectType.AddHighFood:
                    ApplyHighFood(gameEvent.effectValue);
                    break;
                case EventEffectType.AddReputation:
                    if (resourceManager != null && gameEvent.effectValue > 0)
                    {
                        resourceManager.AddReputation(gameEvent.effectValue);
                    }
                    break;
                case EventEffectType.AddEnergy:
                    ApplyEnergy(gameEvent.effectValue);
                    break;
                case EventEffectType.ModifyHygiene:
                    ModifyPlayerMarketsHygiene(gameEvent.effectValue);
                    break;
                case EventEffectType.TriggerInspection:
                    TriggerInspection(contextTileId);
                    break;
                case EventEffectType.CloseMarket:
                    ClosePlayerMarkets(gameEvent.effectValue, contextTileId);
                    break;
                case EventEffectType.ModifyTraffic:
                case EventEffectType.ModifyCompetition:
                case EventEffectType.None:
                default:
                    Debug.Log(
                        $"EventManager: Event '{gameEvent.eventName}' effect is currently informational.");
                    break;
            }
        }

        private void ApplyMoney(int value)
        {
            if (resourceManager == null)
            {
                return;
            }

            if (value >= 0)
            {
                resourceManager.AddMoney(value);
            }
            else
            {
                resourceManager.SpendMoney(-value);
            }
        }

        private void ApplyLowFood(int value)
        {
            if (resourceManager == null)
            {
                return;
            }

            if (value >= 0)
            {
                resourceManager.AddLowFood(value);
            }
            else
            {
                resourceManager.ConsumeLowFood(-value);
            }
        }

        private void ApplyHighFood(int value)
        {
            if (resourceManager == null)
            {
                return;
            }

            if (value >= 0)
            {
                resourceManager.AddHighFood(value);
            }
            else
            {
                resourceManager.ConsumeHighFood(-value);
            }
        }

        private void ApplyEnergy(int value)
        {
            if (resourceManager == null)
            {
                return;
            }

            if (value >= 0)
            {
                resourceManager.RestoreEnergy(value);
            }
            else
            {
                resourceManager.ConsumeEnergy(-value);
            }
        }

        private void ModifyPlayerMarketsHygiene(int value)
        {
            if (marketManager == null || marketManager.Markets == null)
            {
                return;
            }

            for (int i = 0; i < marketManager.Markets.Count; i++)
            {
                MarketRuntimeData market = marketManager.Markets[i];
                if (market != null && market.owner == OwnerType.Player)
                {
                    market.totalHygiene = Mathf.Clamp(market.totalHygiene + value, 0f, 100f);
                }
            }
        }

        private void TriggerInspection(string contextTileId)
        {
            if (inspectionManager == null || marketManager == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(contextTileId) &&
                marketManager.GetMarket(contextTileId) != null)
            {
                inspectionManager.ForceInspection(contextTileId);
                return;
            }

            for (int i = 0; i < marketManager.Markets.Count; i++)
            {
                MarketRuntimeData market = marketManager.Markets[i];
                if (market != null && market.owner == OwnerType.Player)
                {
                    inspectionManager.ForceInspection(market.tileId);
                    return;
                }
            }
        }

        private void ClosePlayerMarkets(int rounds, string contextTileId)
        {
            if (marketManager == null || rounds <= 0)
            {
                return;
            }

            if (!string.IsNullOrEmpty(contextTileId) &&
                marketManager.GetMarket(contextTileId) != null)
            {
                marketManager.AddClosedRounds(contextTileId, rounds);
                return;
            }

            for (int i = 0; i < marketManager.Markets.Count; i++)
            {
                MarketRuntimeData market = marketManager.Markets[i];
                if (market != null && market.owner == OwnerType.Player)
                {
                    marketManager.AddClosedRounds(market.tileId, rounds);
                }
            }
        }

        private EventRuntimeData CreateRuntimeData(GameEvent gameEvent, string contextTileId)
        {
            return new EventRuntimeData
            {
                eventId = gameEvent.eventId,
                triggeredDay = GetCurrentDay(),
                triggeredTileId = contextTileId,
                playerChoice = string.Empty
            };
        }

        private int GetCurrentDay()
        {
            return gameManager != null && gameManager.RuntimeData != null
                ? gameManager.RuntimeData.currentDay
                : 1;
        }

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

        private GameEvent FindEvent(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                return null;
            }

            for (int i = 0; i < eventPool.Count; i++)
            {
                GameEvent gameEvent = eventPool[i];
                if (gameEvent != null && gameEvent.eventId == eventId)
                {
                    return gameEvent;
                }
            }

            return null;
        }

        private void SeedDefaultEventsIfNeeded()
        {
            if (!seedDefaultEventsWhenEmpty || eventPool == null || eventPool.Count > 0)
            {
                return;
            }

            eventPool.Add(new GameEvent("EV_DAY_SPONSOR", "社团赞助", EventTriggerType.OnDayStart, EventEffectType.AddMoney, 300, 0.12f, ""));
            eventPool.Add(new GameEvent("EV_NIGHT_SUPPLY", "临期食材补给", EventTriggerType.OnNightStart, EventEffectType.AddLowFood, 10, 0.16f, ""));
            eventPool.Add(new GameEvent("EV_SETTLEMENT_BUZZ", "夜市口碑发酵", EventTriggerType.OnSettlement, EventEffectType.AddReputation, 1, 0.12f, ""));
            eventPool.Add(new GameEvent("EV_INSPECTION_NOTICE", "突击卫生提醒", EventTriggerType.OnInspection, EventEffectType.TriggerInspection, 0, 0.08f, ""));

            eventPool.Add(new GameEvent("EV_MAIN_CLUBS_REP", "社团打卡", EventTriggerType.Manual, EventEffectType.AddReputation, 1, 1f, "EVENT_MAIN_CLUBS"));
            eventPool.Add(new GameEvent("EV_MAIN_ACADEMIC_MONEY", "讲座散场客流", EventTriggerType.Manual, EventEffectType.AddMoney, 300, 1f, "EVENT_MAIN_ACADEMIC"));
            eventPool.Add(new GameEvent("EV_MAIN_DORM_FOOD", "寝室拼单", EventTriggerType.Manual, EventEffectType.AddLowFood, 10, 1f, "EVENT_MAIN_DORM"));
            eventPool.Add(new GameEvent("EV_GATEWAY_ENERGY", "校门补给", EventTriggerType.Manual, EventEffectType.AddEnergy, 1, 1f, "EVENT_GATEWAY"));
            eventPool.Add(new GameEvent("EV_EAST_HIGH_FOOD", "东校区采购", EventTriggerType.Manual, EventEffectType.AddHighFood, 4, 1f, "EVENT_EAST_CAMPUS"));
            eventPool.Add(new GameEvent("EV_EAST_DORM_LOW_FOOD", "东区寝室团购", EventTriggerType.Manual, EventEffectType.AddLowFood, 12, 1f, "EVENT_EAST_DORM"));
            eventPool.Add(new GameEvent("EV_GUANGGU_GATEWAY_MONEY", "光谷入口人潮", EventTriggerType.Manual, EventEffectType.AddMoney, 500, 1f, "EVENT_GUANGGU_GATEWAY"));
            eventPool.Add(new GameEvent("EV_GUANGGU_HYGIENE", "商圈卫生压力", EventTriggerType.Manual, EventEffectType.ModifyHygiene, -8, 1f, "EVENT_GUANGGU_BUSINESS"));
            eventPool.Add(new GameEvent("EV_MAIN_CENTER_REP", "主校区中心曝光", EventTriggerType.Manual, EventEffectType.AddReputation, 2, 1f, "SPECIAL_MAIN_CENTER"));
            eventPool.Add(new GameEvent("EV_GUANGGU_CORE_MONEY", "光谷核心客流爆发", EventTriggerType.Manual, EventEffectType.AddMoney, 800, 1f, "SPECIAL_GUANGGU_CORE"));
        }
    }
}
