namespace CampusNightMarket.Tiles
{
    // 地块交互可执行操作，供TileManager和UI共同使用。
    public enum TileActionType
    {
        None,
        ViewInfo,
        PurchaseAndCreateMarket,
        OpenMarketPanel,
        CollectResource,
        OpenShop,
        TriggerEvent,
        TriggerSpecialRule,
        CompleteInteraction
    }
}
