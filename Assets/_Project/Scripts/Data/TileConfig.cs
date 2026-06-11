using System.Collections.Generic;
using CampusNightMarket.Common;
using UnityEngine;

namespace CampusNightMarket.Data
{
    // 单个地块的静态配置，地图、移动、购买和结算都会读取它。
    [CreateAssetMenu(fileName = "TileConfig", menuName = "Campus Night Market/Tile Config")]
    public class TileConfig : ScriptableObject
    {
        // 地块唯一ID，例如 TILE_HUST_001。
        public string tileId;
        // 地块显示名称。
        public string tileName;
        // 地块功能类型。
        public TileType tileType;
        // 所属区域ID，用于区域事件或区域倍率。
        public string regionId;
        // 地块在场景中的参考位置。
        public Vector3 position;
        // 相邻地块ID列表，用于路径和可达范围计算。
        public List<string> nextTileIds = new List<string>();

        // 是否允许玩家购买。
        public bool canPurchase;
        // 购买地块所需资金。
        public int purchasePrice;
        // 每晚租金或维护费。
        public int rentCost;

        // 基础客流量。
        public int baseTraffic;
        // 消费力倍率，影响摊位收入。
        public float consumePower = 1f;

        // 学生客群占比。
        public float studentRatio;
        // 教师客群占比。
        public float teacherRatio;
        // 游客客群占比。
        public float touristRatio;
        // 居民客群占比。
        public float residentRatio;

        // NPC竞争强度，通常会削减客流。
        public float competition;
        // 卫生检查基础触发概率。
        public float inspectionRate;

        // 资源地块的产出类型。
        public ResourceType resourceType;
        // 到达地块时使用的事件池ID。
        public string eventPoolId;
    }
}
