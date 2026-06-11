using System;
using CampusNightMarket.Common;

namespace CampusNightMarket.Core
{
    // 一局游戏的全局运行时状态，保存会随游玩变化的数据。
    [Serializable]
    public class GameRuntimeData
    {
        // 当前地图ID。
        public string currentMapId;
        // 当前天数，从1开始。
        public int currentDay;
        // 当前游戏阶段。
        public GamePhase currentPhase;
        // 当前天气ID，后续由 WeatherManager 写入。
        public string currentWeatherId;
        // 游戏是否已经结束。
        public bool isGameOver;
        // 游戏结束时是否胜利。
        public bool isWin;
        // 本局胜利所需目标资金。
        public int victoryTarget;
    }
}
