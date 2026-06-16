namespace CampusNightMarket.Common
{
    // 事件产生的效果类型，具体执行由 EventManager 或相关系统处理。
    public enum EventEffectType
    {
        // 无效果。
        None,
        // 修改资金。
        AddMoney,
        // 修改低端食材。
        AddLowFood,
        // 修改高端食材。
        AddHighFood,
        // 修改口碑。
        AddReputation,
        // 修改体力。
        AddEnergy,
        // 修改客流。
        ModifyTraffic,
        // 修改竞争强度。
        ModifyCompetition,
        // 修改卫生值。
        ModifyHygiene,
        // 触发卫生检查。
        TriggerInspection,
        // 让夜市停业。
        CloseMarket
    }
}
