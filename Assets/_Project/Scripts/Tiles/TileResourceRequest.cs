using System;
using CampusNightMarket.Common;

namespace CampusNightMarket.Tiles
{
    // 地块系统生成的资源请求，由ResourceManager处理后回传结果。
    [Serializable]
    public class TileResourceRequest
    {
        public string tileId;
        public TileResourceRequestType requestType;
        public ResourceType resourceType;
        public int amount;
        public string reason;
    }
}
