# 当前开发交接与字段命名约定

本文档用于给其他组员快速了解当前 Gameplay、夜市与摊位模块的完成情况，以及后续接入时需要遵守的字段命名规则。

## 当前已完成内容

### 1. 基础目录与脚本骨架

当前代码主要位于：

```text
Assets/_Project/Scripts/Common/
Assets/_Project/Scripts/Core/
Assets/_Project/Scripts/Data/
Assets/_Project/Scripts/Market/
Assets/_Project/Scripts/Player/
Assets/_Project/Scripts/Turn/
```

已建立的基础类型包括：

1. 枚举：`GamePhase`、`TileType`、`ResourceType`、`OwnerType`、`StallType`、`EventTriggerType`、`EventEffectType`。
2. 配置类：`MapConfig`、`TileConfig`、`StallConfig`。
3. 运行时数据类：`GameRuntimeData`、`PlayerRuntimeData`、`MarketRuntimeData`、`StallRuntimeData`。
4. 管理器：`GameManager`、`TurnManager`、`MarketManager`、`StallManager`。

### 2. Gameplay 流程

`GameManager` 已负责：

1. 初始化新游戏。
2. 记录当前地图、当前天数、当前阶段。
3. 根据地图配置计算胜利目标。
4. 在限期日当天夜晚结束后进行胜负判定。

胜负规则以《系统策划案》为准：

1. 只在限期日当天夜晚结束后判定。
2. 如果资金未达到目标，进入 `GameLose`，游戏结束。
3. 如果资金达到目标，进入 `GameWin` 提示阶段，但游戏不结束。
4. 胜利后设置 `isEndlessMode = true`，贷款清零，之后可以继续无目标经营。

`TurnManager` 已负责：

1. 日开始、投骰、移动、地块交互、夜晚结算、日结束等阶段切换。
2. 记录本回合骰子点数 `CurrentDiceValue`。
3. 记录玩家选择的目标地块 `SelectedTargetTileId`。
4. 暴露 `PhaseChanged` 和 `DiceRolled` 事件，方便 UI 或其他系统监听。

### 3. 夜市与摊位系统

夜市与摊位字段以 `Docs/Design/夜市与摊位系统设计案.pdf` 为准。

`MarketManager` 已支持：

1. 根据地块创建夜市。
2. 夜市升级。
3. 建设摊位。
4. 升级摊位。
5. 检查夜市是否营业。
6. 添加和推进停业回合。
7. 刷新夜市总吸引力和总卫生值。
8. 提供夜市等级效果接口：
   - `GetTrafficCapacityMultiplier`：2级夜市客流承载 +20%。
   - `IsHighTierStallUnlocked`：3级夜市解锁高端摊位。
   - `HasReputationBonus`：4级夜市具备口碑加成。

夜市等级规则当前为：

| 夜市等级 | 最大摊位数 | 升级费用 | 效果 |
| --- | ---: | ---: | --- |
| 1 | 1 | 0 | 基础夜市 |
| 2 | 2 | 2000 | 客流承载+20% |
| 3 | 3 | 5000 | 解锁高端摊位 |
| 4 | 4 | 10000 | 口碑加成 |

`StallConfig` 当前字段包括：

1. `stallId`、`stallName`、`stallType`。
2. `buildCost`、`upgradeCost`。
3. `basePrice`、`baseAttraction`。
4. `lowFoodCost`、`highFoodCost`。
5. `hygiene`。
6. `studentPreference`、`teacherPreference`、`touristPreference`、`residentPreference`。
7. `unlockMarketLevel`。

摊位类型使用：

```csharp
StreetVendor // 摊贩
Store        // 门店
ChainStore   // 连锁店
```

## 其他组员需要完成或接入的内容

### 1. 地图系统

需要提供地图生成、地块查询和移动合法性判断。

建议后续接入点：

1. `TurnManager.SelectMoveTarget` 中需要调用地图系统判断目标地块是否可达。
2. `TileConfig.nextTileIds` 已作为地块连接关系字段。
3. `TileConfig.position` 可作为地图表现层摆放坐标。
4. 夜市创建时需要根据 `TileConfig.canPurchase`、`purchasePrice` 和地块归属进行判定。

代码中已有注释格式：

```csharp
// 这里需要 MapManager 根据骰子点数计算可达地块。
```

### 2. 资源与经济系统

需要负责资金、食材、贷款、利息、收入结算等。

需要接入的位置：

1. 创建夜市时扣除地块购买或建设费用。
2. 夜市升级时扣除 `upgradeCost`。
3. 摊位建设时扣除 `buildCost`。
4. 摊位升级时扣除 `upgradeCost`。
5. 夜晚根据客流、客单价、食材消耗、偏好倍率等计算收入。
6. 根据 `MapConfig.interestInterval`、`interestRate` 处理贷款利息。

当前代码中用注释标出了类似：

```csharp
// 这里需要 ResourceManager.CheckMoney(...)
// 这里需要 ResourceManager.SpendMoney(...)
// 这里需要 EconomyManager 结算夜晚收入。
```

### 3. 客流系统

需要根据地块、夜市、摊位吸引力和客群偏好分配客流。

可使用的字段：

1. `TileConfig.baseTraffic`：地块基础人流。
2. `TileConfig.consumePower`：消费能力倍率。
3. `TileConfig.studentRatio`、`teacherRatio`、`touristRatio`、`residentRatio`：客群比例。
4. `MarketRuntimeData.totalAttraction`：夜市总吸引力。
5. `StallConfig.studentPreference`、`teacherPreference`、`touristPreference`、`residentPreference`：摊位对不同客群的偏好倍率。
6. `MarketManager.GetTrafficCapacityMultiplier`：夜市2级客流承载倍率。

### 4. 卫生检查与事件系统

需要负责检查风险、停业处罚、随机事件效果。

可使用的字段和接口：

1. `StallConfig.hygiene`：摊位卫生值。
2. `MarketRuntimeData.totalHygiene`：夜市总卫生值。
3. `TileConfig.inspectionRate`：地块检查概率。
4. `MarketManager.AddClosedRounds`：增加夜市停业回合。
5. `MarketManager.AdvanceClosedRounds`：推进停业倒计时。
6. `MarketManager.IsMarketOpen`：判断夜市是否营业。

### 5. 数据配置系统

需要创建并维护正式的 `ScriptableObject` 配置资产。

需要配置：

1. `MapConfig`：地图基础规则和地块列表。
2. `TileConfig`：所有地块配置。
3. `StallConfig`：所有摊位配置。

后续如果做统一数据读取，建议提供类似接口：

```csharp
GetMapConfig(string mapId)
GetTileConfig(string tileId)
GetStallConfig(string stallId)
```

`MarketManager.RefreshMarketSummary` 当前已支持传入完整 `List<StallConfig>` 来刷新夜市汇总属性。

### 6. UI 系统

当前不处理 UI 交互。UI 组后续需要接：

1. 阶段变化显示：监听 `TurnManager.PhaseChanged`。
2. 骰子结果显示：监听 `TurnManager.DiceRolled`。
3. 夜市面板：读取 `MarketManager.Markets`。
4. 摊位建设、升级按钮：调用 `CanBuildStall`、`BuildStall`、`CanUpgradeStall`、`UpgradeStall`。
5. 胜利提示：`GamePhase.GameWin` 只是提示阶段，不代表游戏结束。
6. 失败界面：`GamePhase.GameLose` 才是游戏结束。

## 字段命名规定

### 1. 总体规则

1. 脚本、类、枚举、方法、属性使用英文命名，不使用中文拼音。
2. 类名、枚举名、方法名、属性名使用 `PascalCase`。
3. 字段和方法参数使用 `camelCase`。
4. 枚举成员使用 `PascalCase`。
5. 不要随意使用缩写，除非是通用缩写，例如 `Id`、`UI`。
6. 同一个概念只能保留一种英文命名，不要多个同义词混用。

示例：

```csharp
public class MarketRuntimeData
public enum StallType
public bool TryCreateMarket(string tileId, out MarketRuntimeData marketData)
public string stallId;
```

### 2. 统一后缀

配置类使用 `Config` 后缀：

```csharp
MapConfig
TileConfig
StallConfig
```

运行时数据类使用 `RuntimeData` 后缀：

```csharp
GameRuntimeData
PlayerRuntimeData
MarketRuntimeData
StallRuntimeData
```

管理器类使用 `Manager` 后缀：

```csharp
GameManager
TurnManager
MarketManager
StallManager
```

规则表或规则项使用 `Rule` 后缀：

```csharp
MarketLevelRule
```

### 3. ID 字段

所有配置或运行时引用对象的字符串 ID 使用 `xxxId`。

当前已有字段：

```csharp
mapId
tileId
startTileId
regionId
stallId
eventPoolId
currentWeatherId
currentTileId
SelectedTargetTileId
```

如果是多个 ID，使用 `xxxIds`：

```csharp
nextTileIds
```

不要写成：

```csharp
tileID
TileID
tileCode
tileNameId
```

除非确实是显示名称，否则不要用 `Name` 代替 `Id`。

### 4. 数值类型命名

金额或资源消耗使用：

```csharp
money
buildCost
upgradeCost
purchasePrice
rentCost
loan
lowFoodCost
highFoodCost
```

数量或容量使用：

```csharp
maxDay
currentDay
marketLevel
maxStallCount
level
closedRounds
```

倍率使用 `Multiplier`：

```csharp
victoryMultiplier
GetTrafficCapacityMultiplier()
```

比例使用 `Ratio`：

```csharp
studentRatio
teacherRatio
touristRatio
residentRatio
```

概率或比率使用 `Rate`：

```csharp
interestRate
inspectionRate
```

偏好倍率使用 `Preference`：

```csharp
studentPreference
teacherPreference
touristPreference
residentPreference
```

布尔值使用 `is`、`can`、`has` 开头：

```csharp
isGameOver
isWin
isEndlessMode
canPurchase
HasReputationBonus()
```

### 5. 数值类型选择

使用 `int` 的情况：

1. 金钱、食材等离散资源数量。
2. 天数、回合数、等级。
3. 摊位数量、最大容量。

使用 `float` 的情况：

1. 倍率。
2. 概率。
3. 比例。
4. 偏好。
5. 加权结果。
6. 平均值。

当前特别规定：

```csharp
MarketRuntimeData.totalAttraction // float
MarketRuntimeData.totalHygiene    // float
StallConfig.baseAttraction        // float
StallConfig.hygiene               // float
```

不要把加权、平均、倍率、概率类字段写成 `int`。

### 6. 已统一的词汇

体力统一使用 `Energy`，不要再使用 `Stamina`。

```csharp
energy
maxEnergy
```

夜市统一使用 `Market`。

摊位统一使用 `Stall`。

地块统一使用 `Tile`。

客流或人流优先使用 `Traffic`。

卫生使用 `Hygiene`。

吸引力使用 `Attraction`。

口碑使用 `Reputation`。

贷款使用 `Loan`。

利息使用 `Interest`。

### 7. 枚举命名

枚举类型和成员都使用英文 `PascalCase`。

当前摊位类型：

```csharp
public enum StallType
{
    StreetVendor,
    Store,
    ChainStore
}
```

不要写成：

```csharp
TanFan
MenDian
vendor_type
```

### 8. 注释规则

1. 每个公开类、枚举、关键字段和关键方法需要写简洁中文注释。
2. 注释说明“这个东西是什么/给谁用/什么时候用”，不要翻译代码本身。
3. 如果缺少其他系统接口，统一使用这种格式：

```csharp
// 这里需要 ResourceManager.CheckMoney(cost) 判断玩家资金是否足够。
// 这里需要 MapManager 根据骰子点数计算可达地块。
// 这里需要 CustomerManager/EconomyManager 在分配夜间客流时应用该倍率。
```

### 9. 新增字段前的检查

新增字段前先检查是否已有同义字段。

不要重复新增这些已经存在的概念：

```csharp
energy / maxEnergy
totalAttraction
totalHygiene
closedRounds
marketLevel
maxStallCount
unlockMarketLevel
```

如果字段来自某个具体设计案，以具体系统设计案为准，不要只按《系统策划案》的总起描述扩展字段。

## 当前暂不做的内容

1. 暂不做 UI 交互。
2. 暂不做正式美术表现。
3. 暂不强行实现其他组负责的系统。
4. 暂不在夜市模块中直接扣钱、直接生成客流、直接结算收入。
5. 暂不把所有配置资产填完整，配置资产需要后续根据数值表统一创建。

## 接入建议

其他组员接入时，优先调用已有 `Can...` 方法做规则判断，再调用实际执行方法。

例如：

```csharp
if (marketManager.CanBuildStall(marketData, stallConfig, out string reason))
{
    marketManager.BuildStall(tileId, stallConfig);
}
```

如果需要新增接口，建议先在对应模块中写清楚职责边界，不要让一个系统直接修改另一个系统的内部字段。跨系统调用时优先使用公开方法。
