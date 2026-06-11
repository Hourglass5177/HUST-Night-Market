# GZY Gameplay 与夜市摊位模块开发说明

## 本次修改

为高致远负责的 Gameplay 系统和夜市/摊位系统创建最小可编译代码骨架。

已覆盖：

1. 基础枚举：游戏阶段、地块类型、资源类型、拥有者类型、摊位类型、事件触发与效果类型。
2. 基础配置：`MapConfig`、`TileConfig`、`StallConfig`。
3. 基础运行时数据：`GameRuntimeData`、`PlayerRuntimeData`、`MarketRuntimeData`、`StallRuntimeData`。
4. Gameplay 入口：`GameManager`、`TurnManager`。
5. 夜市与摊位入口：`MarketManager`、`StallManager`。

## 夜市与摊位专项设计对齐

夜市与摊位字段以 `Docs/Design/夜市与摊位系统设计案.pdf` 为准：

1. 摊位类型使用 `StreetVendor`、`Store`、`ChainStore`，对应摊贩、门店、连锁店。
2. 夜市运行时数据包含夜市等级、最大摊位数、当前摊位列表、总吸引力、总卫生值和停业回合数。
3. 夜市升级规则用 `MarketLevelRule` 表示，默认等级 1-4 分别对应最大摊位数 1-4，升级费用为 0、2000、5000、10000。
4. 摊位配置保留建设费用、升级费用、基础客单价、基础吸引力、低/高食材消耗、卫生值、客群偏好和解锁夜市等级。
5. 摊位最高等级暂不放在 `StallConfig` 中，先作为管理器统一规则。

## 修改文件

```text
Assets/_Project/Scripts/Common/
Assets/_Project/Scripts/Core/
Assets/_Project/Scripts/Data/
Assets/_Project/Scripts/Market/
Assets/_Project/Scripts/Player/
Assets/_Project/Scripts/Turn/
```

## Unity 内验证方式

1. 使用 Unity 2020.3.30f1c1 打开仓库根目录。
2. 等待 Unity 导入脚本。
3. 检查 Console 是否出现 C# 编译错误。
4. 在空 GameObject 上可尝试挂载：
   - `GameManager`
   - `TurnManager`
   - `MarketManager`
   - `StallManager`
5. 在 Project 面板中右键创建配置资产：
   - `Create/Campus Night Market/Map Config`
   - `Create/Campus Night Market/Tile Config`
   - `Create/Campus Night Market/Stall Config`

## 当前已知限制

1. 目前只提供模块接口和最小流程，不包含完整地图生成、UI 绑定、资源扣费和夜晚结算。
2. `TurnManager` 只维护阶段切换和骰子结果，目标地块合法性需要后续接入 `MapManager`。
3. `MarketManager` 只维护夜市和摊位运行时数据，购买费用、升级费用和资源扣减需要后续接入 `ResourceManager` / `EconomyManager`。
4. README 中的启动场景路径 `Assets/_Project/CampusNightMarket/Scenes/S00_Bootstrap.unity` 当前不存在，仓库目前只有 `Assets/Scenes/SampleScene.unity`。

## 胜负判定规则

胜负判定以 `Docs/Design/系统策划案.pdf` 为准：

1. 只在限期日当天夜晚结束后进行胜负判定。
2. 若资金不足胜利目标，进入 `GameLose`，并设置 `isGameOver = true`。
3. 若资金达到胜利目标，进入 `GameWin` 提示阶段，但不设置游戏结束。
4. 胜利后设置 `isEndlessMode = true`，贷款清零，确认胜利提示后可继续进入下一天并无限经营。
5. `GameWin` 不是终局状态，只是从限期目标模式切换到无尽模式时的提示阶段。

## 下一步建议

1. 让 `GameManager` 初始化 `PlayerRuntimeData`，并和贷款目标、最大天数连起来。
2. 让 `TurnManager` 接入地图模块，使用 `MapManager` 根据骰子点数计算可达地块。
3. 让 `MarketManager` 接入资源模块，建设和升级时先检查并扣除资金。
4. 创建正式 Bootstrap 场景或修正 README 中的运行路径。
