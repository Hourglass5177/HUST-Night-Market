namespace CampusNightMarket.Common
{
    // 随机事件的触发时机。
    public enum EventTriggerType
    {
        // 玩家到达某个地块时触发。
        OnArriveTile,
        // 每天开始时触发。
        OnDayStart,
        // 夜晚开始时触发。
        OnNightStart,
        // 结算过程中触发。
        OnSettlement,
        // 卫生检查时触发。
        OnInspection,
        // 手动或调试触发。
        Manual
    }
}
