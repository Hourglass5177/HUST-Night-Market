using System.Collections.Generic;
using UnityEngine;

namespace CampusNightMarket.Data
{
    // 一张地图的静态配置，描述开局参数和包含的地块列表。
    [CreateAssetMenu(fileName = "MapConfig", menuName = "Campus Night Market/Map Config")]
    public class MapConfig : ScriptableObject
    {
        // 地图唯一ID，例如 MAP_HUST。
        public string mapId;
        // 地图显示名称。
        public string mapName;
        // 玩家开局所在的地块ID。
        public string startTileId;
        // 限定天数，超过后未达成目标则失败。
        public int maxDay = 30;
        // 开局自有资金。
        public int initialMoney = 3000;
        // 开局贷款金额。
        public int loanAmount = 7000;
        // 胜利目标倍率，用于计算目标资金。
        public float victoryMultiplier = 10f;
        // 利息结算周期，单位为天。
        public int interestInterval = 7;
        // 每期利息率。
        public float interestRate = 0.1f;
        // 本地图包含的全部地块配置。
        public List<TileConfig> tileList = new List<TileConfig>();
    }
}
