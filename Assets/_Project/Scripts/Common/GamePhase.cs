namespace CampusNightMarket.Common
{
    // 游戏主流程阶段，TurnManager 和 GameManager 通过它协调单日循环。
    public enum GamePhase
    {
        // 初始化阶段，准备地图、玩家和运行时数据。
        Init,
        // 每天开始，适合刷新天气、体力和每日事件。
        DayStart,
        // 等待或执行投骰。
        RollDice,
        // 根据骰子点数选择可到达地块。
        ChooseMove,
        // 玩家棋子移动到目标地块。
        MovePlayer,
        // 到达地块后处理购买、资源、事件等交互。
        TileInteraction,
        // 夜晚营业与收益结算。
        NightSettlement,
        // 一天结束，推进天数并检查胜负。
        DayEnd,
        // 胜利状态。
        GameWin,
        // 失败状态。
        GameLose
    }
}
