namespace CampusNightMarket.Common
{
    // 地块类型
    public enum TileType
    {
        // 起点地块。
        Start,
        // 可购买并建设夜市的地块。
        Buildable,
        // 产出资金、食材、口碑或体力的资源点。
        Resource,
        // 商店或采购点。
        Shop,
        // 触发随机事件的地块。
        Event,
        // 传送或交通地块。
        Transport,
        // 预留特殊规则地块。
        Special
    }
}
