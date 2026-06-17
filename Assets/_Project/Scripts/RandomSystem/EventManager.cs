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

        // ===== 事件池ID常量 =====
        public const string EVENT_POOL_ARRIVE = "EVENT_POOL_ARRIVE";
        public const string EVENT_POOL_DAY_START = "EVENT_POOL_DAY_START";
        public const string EVENT_POOL_NIGHT = "EVENT_POOL_NIGHT";
        public const string EVENT_POOL_INSPECTION = "EVENT_POOL_INSPECTION";

        private void Awake()
        {
            InitializeDefaultEventPool();
        }

        /// <summary>
        /// 初始化默认的事件池配置。
        /// 按策划案填充 EVENT_POOL_ARRIVE / DAY_START / NIGHT / INSPECTION 四个事件池。
        /// </summary>
        private void InitializeDefaultEventPool()
        {
            if (eventPool.Count > 0)
            {
                // 如果已在 Inspector 中手动配置，跳过初始化
                return;
            }

            // ==================== EVENT_POOL_ARRIVE（到达地块事件） ====================
            eventPool.Add(new GameEvent("EVT_ARRIVE_WALLET", "发现钱包", EventTriggerType.OnArriveTile, EventEffectType.AddMoney, 500, 0.15f, EVENT_POOL_ARRIVE)
            { eventDescription = "你在路边发现了一个钱包，里面有一些现金！" });

            eventPool.Add(new GameEvent("EVT_ARRIVE_VENDOR", "小贩推销", EventTriggerType.OnArriveTile, EventEffectType.AddMoney, -300, 0.15f, EVENT_POOL_ARRIVE)
            { eventDescription = "一个小贩向你推销高价纪念品，你不好意思拒绝就买了。" });

            eventPool.Add(new GameEvent("EVT_ARRIVE_STUDENT_HELP", "好心同学指路", EventTriggerType.OnArriveTile, EventEffectType.AddEnergy, 10, 0.12f, EVENT_POOL_ARRIVE)
            { eventDescription = "一位热心的同学帮你提了一段路，你节省了不少体力。" });

            eventPool.Add(new GameEvent("EVT_ARRIVE_FLYER", "路边发传单", EventTriggerType.OnArriveTile, EventEffectType.AddLowFood, 5, 0.15f, EVENT_POOL_ARRIVE)
            { eventDescription = "街边有人发传单送优惠券，可以兑换一些基础食材。" });

            eventPool.Add(new GameEvent("EVT_ARRIVE_THIEF", "遭遇小偷", EventTriggerType.OnArriveTile, EventEffectType.AddMoney, -800, 0.08f, EVENT_POOL_ARRIVE)
            { eventDescription = "一不留神，钱包被小偷偷走了！损失了不少现金。" });

            eventPool.Add(new GameEvent("EVT_ARRIVE_HIDDEN_SHOP", "发现隐藏小店", EventTriggerType.OnArriveTile, EventEffectType.AddHighFood, 8, 0.10f, EVENT_POOL_ARRIVE)
            { eventDescription = "你发现了一条小巷子里隐藏的食材店，高端食材应有尽有！" });

            eventPool.Add(new GameEvent("EVT_ARRIVE_RECRUIT", "社团招新拉人", EventTriggerType.OnArriveTile, EventEffectType.AddEnergy, -5, 0.12f, EVENT_POOL_ARRIVE)
            { eventDescription = "被社团招新的同学缠住介绍了好久，浪费了不少体力。" });

            eventPool.Add(new GameEvent("EVT_ARRIVE_SCHOLARSHIP", "奖学金发放", EventTriggerType.OnArriveTile, EventEffectType.AddMoney, 2000, 0.05f, EVENT_POOL_ARRIVE)
            { eventDescription = "好消息！你的奖学金到账了！" });

            eventPool.Add(new GameEvent("EVT_ARRIVE_FOOD_CHOICE", "美食街试吃", EventTriggerType.OnArriveTile, EventEffectType.AddLowFood, 3, 0.18f, EVENT_POOL_ARRIVE)
            { eventDescription = "美食街在搞试吃活动，你一路吃过来收获了不少食材。" });

            // ==================== EVENT_POOL_DAY_START（每天开始事件） ====================
            eventPool.Add(new GameEvent("EVT_DAY_CLUB", "社团活动", EventTriggerType.OnDayStart, EventEffectType.AddReputation, 15, 0.15f, EVENT_POOL_DAY_START)
            { eventDescription = "你参加的社团今天在校内举办了大型活动，夜市口碑随之提升！" });

            eventPool.Add(new GameEvent("EVT_DAY_FOOD_SALE", "食材促销", EventTriggerType.OnDayStart, EventEffectType.AddLowFood, 10, 0.15f, EVENT_POOL_DAY_START)
            { eventDescription = "今天菜市场食材大促销！你囤了不少便宜食材。" });

            eventPool.Add(new GameEvent("EVT_DAY_TAKEOUT_PROMO", "外卖平台推广", EventTriggerType.OnDayStart, EventEffectType.ModifyTraffic, 10, 0.12f, EVENT_POOL_DAY_START)
            { eventDescription = "外卖平台今天在校园做推广活动，预计今晚客流会增加。" });

            eventPool.Add(new GameEvent("EVT_DAY_PRICE_UP", "物价上涨", EventTriggerType.OnDayStart, EventEffectType.AddMoney, -200, 0.12f, EVENT_POOL_DAY_START)
            { eventDescription = "今天采购时发现物价全面上涨，比平时多花了不少钱。" });

            eventPool.Add(new GameEvent("EVT_DAY_PART_TIME", "学生兼职招聘", EventTriggerType.OnDayStart, EventEffectType.AddEnergy, 5, 0.15f, EVENT_POOL_DAY_START)
            { eventDescription = "有学生来你的摊位帮忙打零工，今天你能省不少体力！" });

            eventPool.Add(new GameEvent("EVT_DAY_INSPECTION_WARN", "检查通知", EventTriggerType.OnDayStart, EventEffectType.None, 0, 0.10f, EVENT_POOL_DAY_START)
            { eventDescription = "今天校园管理处发了通知，近期将严查夜市卫生状况。" });

            eventPool.Add(new GameEvent("EVT_DAY_HIGH_FOOD_GIFT", "供应商赠礼", EventTriggerType.OnDayStart, EventEffectType.AddHighFood, 5, 0.10f, EVENT_POOL_DAY_START)
            { eventDescription = "食材供应商给你送来了一些样品，是高级食材！" });

            // ==================== EVENT_POOL_NIGHT（夜晚开始事件） ====================
            eventPool.Add(new GameEvent("EVT_NIGHT_RAIN", "突然下雨", EventTriggerType.OnNightStart, EventEffectType.ModifyTraffic, -20, 0.15f, EVENT_POOL_NIGHT)
            { eventDescription = "傍晚突然下起了雨，今晚出门的人恐怕会减少。" });

            eventPool.Add(new GameEvent("EVT_NIGHT_FESTIVAL", "节日加成", EventTriggerType.OnNightStart, EventEffectType.ModifyTraffic, 30, 0.10f, EVENT_POOL_NIGHT)
            { eventDescription = "今天是校园文化节！周边人流暴增，今晚生意一定好！" });

            eventPool.Add(new GameEvent("EVT_NIGHT_EVENT", "周边举办活动", EventTriggerType.OnNightStart, EventEffectType.ModifyTraffic, 15, 0.12f, EVENT_POOL_NIGHT)
            { eventDescription = "附近有大型活动刚刚散场，大量人群涌向夜市方向。" });

            eventPool.Add(new GameEvent("EVT_NIGHT_BLACKOUT", "突发停电", EventTriggerType.OnNightStart, EventEffectType.ModifyTraffic, -25, 0.08f, EVENT_POOL_NIGHT)
            { eventDescription = "这一片区域突发停电！很多游客都提前离开了。" });

            eventPool.Add(new GameEvent("EVT_NIGHT_INFLUENCER", "网红探店", EventTriggerType.OnNightStart, EventEffectType.AddReputation, 40, 0.08f, EVENT_POOL_NIGHT)
            { eventDescription = "一位美食博主来到你的夜市拍摄视频，口碑大涨！" });

            eventPool.Add(new GameEvent("EVT_NIGHT_COMPETITOR", "竞争对手促销", EventTriggerType.OnNightStart, EventEffectType.ModifyTraffic, -15, 0.12f, EVENT_POOL_NIGHT)
            { eventDescription = "对面的夜市在做大型促销活动，分流了不少客人。" });

            eventPool.Add(new GameEvent("EVT_NIGHT_STUFFY", "天气闷热", EventTriggerType.OnNightStart, EventEffectType.ModifyTraffic, 15, 0.12f, EVENT_POOL_NIGHT)
            { eventDescription = "今晚天气闷热，大家都想出来逛逛夜市透透气。" });

            eventPool.Add(new GameEvent("EVT_NIGHT_PATROL", "夜市巡查", EventTriggerType.OnNightStart, EventEffectType.TriggerInspection, 0, 0.10f, EVENT_POOL_NIGHT)
            { eventDescription = "今晚有城管来夜市巡查，需要多加小心。" });

            // ==================== EVENT_POOL_INSPECTION（卫生检查事件） ====================
            eventPool.Add(new GameEvent("EVT_INSPECT_CONNECTION", "熟人检查", EventTriggerType.OnInspection, EventEffectType.None, 0, 0.15f, EVENT_POOL_INSPECTION)
            { eventDescription = "今天来检查的是你的熟人！他简单看了看就走了。" });

            eventPool.Add(new GameEvent("EVT_INSPECT_REPORT", "卫生举报", EventTriggerType.OnInspection, EventEffectType.ModifyHygiene, -10, 0.12f, EVENT_POOL_INSPECTION)
            { eventDescription = "有顾客向卫生部门举报你的夜市卫生状况，卫生评分下降。" });

            eventPool.Add(new GameEvent("EVT_INSPECT_SPOT_CHECK", "突击抽查", EventTriggerType.OnInspection, EventEffectType.CloseMarket, 1, 0.08f, EVENT_POOL_INSPECTION)
            { eventDescription = "卫生局进行突击抽查，你的夜市被要求停业整顿1天。" });

            eventPool.Add(new GameEvent("EVT_INSPECT_MODEL", "卫生模范", EventTriggerType.OnInspection, EventEffectType.AddReputation, 10, 0.12f, EVENT_POOL_INSPECTION)
            { eventDescription = "卫生检查员称赞你的夜市非常干净整洁，给予表扬！" });

            eventPool.Add(new GameEvent("EVT_INSPECT_BRIBE_CHOICE", "检查员暗示", EventTriggerType.OnInspection, EventEffectType.AddMoney, -500, 0.10f, EVENT_POOL_INSPECTION)
            {
                eventDescription = "检查员暗示可以给点好处费就睁一只眼闭一只眼…",
                requireChoice = true,
                choiceAText = "给好处费（-500元，跳过检查）",
                choiceAValue = -500,
                choiceBText = "公事公办（正常检查，若卫生不足可能受罚）",
                choiceBValue = 0
            });

            // ==================== 通用事件（Manual/调试用） ====================
            eventPool.Add(new GameEvent("EVT_MANUAL_TEST", "测试事件", EventTriggerType.Manual, EventEffectType.AddMoney, 100, 1.0f, "EVENT_POOL_MANUAL")
            { eventDescription = "这是一个调试测试事件。" });

            Debug.Log($"EventManager: 默认事件池已初始化，共加载 {eventPool.Count} 个事件。");
        }

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