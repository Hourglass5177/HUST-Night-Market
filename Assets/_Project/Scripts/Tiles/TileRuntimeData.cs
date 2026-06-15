using System;
using CampusNightMarket.Common;

namespace CampusNightMarket.Tiles
{
    // 单个地块的局内状态，不修改静态TileConfig资产。
    [Serializable]
    public class TileRuntimeData
    {
        public string tileId;
        public OwnerType owner;
        public bool isDiscovered;
        public bool isInteractionCompletedToday;
    }
}
