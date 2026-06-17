using System;
using CampusNightMarket.Common;

namespace CampusNightMarket.RandomSystem
{
    /// <summary>
    /// 单个随机事件的静态配置数据。
    /// 每个事件对应一个 EventTriggerType 触发时机和一个 EventEffectType 效果类型。
    /// </summary>
    [Serializable]
    public class GameEvent
    {
        /// <summary>事件唯一ID。</summary>
        public string eventId;

        /// <summary>事件显示名称。</summary>
        public string eventName;

        /// <summary>事件描述文本。</summary>
        public string eventDescription;

        /// <summary>触发时机。</summary>
        public EventTriggerType triggerType;

        /// <summary>效果类型。</summary>
        public EventEffectType effectType;

        /// <summary>
        /// 效果数值。正数=增益，负数=减益。
        /// 对应关系：
        ///   AddMoney → 金额变化
        ///   AddLowFood → 低端食材变化
        ///   AddHighFood → 高端食材变化
        ///   AddReputation → 口碑变化
        ///   AddEnergy → 体力变化
        ///   ModifyTraffic → 客流变化百分比(如 -20 表示 -20%)
        ///   ModifyCompetition → 竞争强度变化
        ///   ModifyHygiene → 卫生值变化
        ///   CloseMarket → 停业天数(正数)
        /// </summary>
        public int effectValue;

        /// <summary>触发概率（0.0~1.0）。</summary>
        public float probability = 0.1f;

        /// <summary>所属事件池ID（如 EVENT_POOL_ARRIVE、EVENT_POOL_INSPECTION）。</summary>
        public string eventPoolId;

        /// <summary>是否需要玩家选择（二选一事件）。</summary>
        public bool requireChoice;

        /// <summary>选项A的文本（requireChoice=true时生效）。</summary>
        public string choiceAText;

        /// <summary>选项A的效果值。</summary>
        public int choiceAValue;

        /// <summary>选项B的文本。</summary>
        public string choiceBText;

        /// <summary>选项B的效果值。</summary>
        public int choiceBValue;

        public GameEvent()
        {
        }

        public GameEvent(string eventId, string eventName, EventTriggerType triggerType, EventEffectType effectType, int effectValue, float probability, string eventPoolId)
        {
            this.eventId = eventId;
            this.eventName = eventName;
            this.triggerType = triggerType;
            this.effectType = effectType;
            this.effectValue = effectValue;
            this.probability = probability;
            this.eventPoolId = eventPoolId;
            this.requireChoice = false;
        }
    }

    /// <summary>
    /// 事件运行时数据：记录一个事件被触发后的状态。
    /// </summary>
    [Serializable]
    public class EventRuntimeData
    {
        /// <summary>对应 GameEvent.eventId。</summary>
        public string eventId;

        /// <summary>事件显示名称。</summary>
        public string eventName;

        /// <summary>事件描述文本。</summary>
        public string eventDescription;

        /// <summary>事件效果类型。</summary>
        public EventEffectType effectType;

        /// <summary>事件效果数值。</summary>
        public int effectValue;

        /// <summary>事件触发的天数。</summary>
        public int triggeredDay;

        /// <summary>事件触发的地块ID（如果是到达地块触发）。</summary>
        public string triggeredTileId;

        /// <summary>如果是二选一事件，记录玩家选择的选项（A/B）。</summary>
        public string playerChoice;
    }
}
