using System;
using System.Collections.Generic;
using CampusNightMarket.Common;

namespace CampusNightMarket.Tiles
{
    // 当前地块可以展示和执行的交互信息。
    [Serializable]
    public class TileInteractionInfo
    {
        public string tileId;
        public string tileName;
        public TileType tileType;
        public OwnerType owner;
        public List<TileActionType> availableActions = new List<TileActionType>();
        public string message;

        public bool HasAction(TileActionType action)
        {
            return availableActions != null && availableActions.Contains(action);
        }
    }
}
