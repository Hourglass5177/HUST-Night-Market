using System.Collections.Generic;
using CampusNightMarket.Common;
using CampusNightMarket.Core;
using CampusNightMarket.Data;
using CampusNightMarket.Economy;
using CampusNightMarket.Map;
using CampusNightMarket.Market;
using CampusNightMarket.Player;
using CampusNightMarket.RandomSystem;
using CampusNightMarket.Tiles;
using CampusNightMarket.Turn;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

// 原型场景入口：初始化测试地图，并串联投骰、选地块和玩家移动。
public class PrototypeBootstrap : MonoBehaviour
{
    [Header("系统引用")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private MapManager mapManager;
    [SerializeField] private TileManager tileManager;
    [SerializeField] private PlayerMover playerMover;
    [SerializeField] private ResourceManager resourceManager;
    [SerializeField] private EconomyManager economyManager;
    [SerializeField] private MarketManager marketManager;
    [SerializeField] private EventManager eventManager;
    [SerializeField] private WeatherManager weatherManager;
    [SerializeField] private InspectionManager inspectionManager;

    [Header("原型配置")]
    [SerializeField] private MapConfig mapConfig;
    [SerializeField] private int startingEnergy = 10;
    [SerializeField] private bool initializeOnStart = true;
    [SerializeField] private bool showDebugGui = true;
    [FormerlySerializedAs("autoAdvancePrototypeTurn")]
    [SerializeField] private bool autoAdvanceAfterInteraction = true;
    [SerializeField] private bool autoResolvePlaceholderResourceRequests = true;

    [Header("场景地块")]
    [SerializeField] private List<TileView> tileViews = new List<TileView>();

    [Header("摊位测试配置")]
    [SerializeField] private List<StallConfig> stallConfigs = new List<StallConfig>();

    private readonly Dictionary<string, ReachableTileResult> reachableTiles =
        new Dictionary<string, ReachableTileResult>();
    private PlayerRuntimeData playerData;
    private string statusMessage = "等待初始化";
    private string lastNightSettlementSummary = "上一晚结算：尚未发生";
    private string lastEventSummary = "事件：暂无";
    private string lastInspectionSummary = "卫生审查：暂无";
    private Vector2 debugScrollPosition;
    private int currentMoveStepBudget;
    private bool hasActiveMoveBudget;

    public PlayerRuntimeData PlayerData
    {
        get { return playerData; }
    }

    public MapConfig MapConfigForUI
    {
        get { return mapConfig; }
    }

    public bool CanRollDiceForUI()
    {
        return CanRollDice();
    }

    private void Start()
    {
        if (initializeOnStart)
        {
            InitializePrototype();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && CanRollDice())
        {
            RollDice();
        }
    }

    // 初始化地图、玩家数据、地块表现和第一天回合状态。
    public void InitializePrototype()
    {
        if (!ValidateReferences())
        {
            return;
        }

        if (!mapManager.InitializeMap(mapConfig, out string reason))
        {
            statusMessage = "地图初始化失败：" + reason;
            Debug.LogError(statusMessage, this);
            return;
        }

        playerData = new PlayerRuntimeData
        {
            currentTileId = mapConfig.startTileId,
            money = mapConfig.initialMoney,
            loan = mapConfig.loanAmount,
            energy = Mathf.Max(1, startingEnergy),
            maxEnergy = Mathf.Max(1, startingEnergy),
            nextInterestDay = mapConfig.interestInterval
        };

        BindEconomySystems();
        BindRandomSystems();
        EnsureS02UIControllers();
        gameManager.InitializeNewGame(mapConfig, playerData);
        turnManager.ConfigureMovement(mapManager, playerData);
        RegisterTileViews();

        if (!tileManager.InitializeTiles(mapConfig, out reason))
        {
            statusMessage = "地块初始化失败：" + reason;
            Debug.LogError(statusMessage, this);
            return;
        }

        SubscribeTileManagerEvents();
        RefreshAllTileOwners();
        playerMover.PlaceAt(mapManager, playerData.currentTileId);
        currentMoveStepBudget = 0;
        hasActiveMoveBudget = false;
        turnManager.BeginFirstDay();
        BeginPrototypeDaySystems();
        turnManager.StartRollDice();

        statusMessage = "初始化完成。点击投骰按钮或按空格。";
        Debug.Log(statusMessage, this);
    }

    // 可直接绑定到Unity UI Button的OnClick。
    public void RollDice()
    {
        if (!CanRollDice())
        {
            statusMessage = tileManager != null && tileManager.IsInteractionActive
                ? "请先结束当前地块交互。"
                : "当前状态不能投骰。";
            return;
        }

        if (reachableTiles.Count > 0)
        {
            statusMessage = "请先选择一个绿色地块。";
            return;
        }

        int diceValue = turnManager.RollDice();
        if (diceValue <= 0)
        {
            statusMessage = "当前阶段不能投骰。";
            return;
        }

        currentMoveStepBudget = Mathf.Min(diceValue, playerData.energy);
        hasActiveMoveBudget = currentMoveStepBudget > 0;
        ShowReachableTiles(currentMoveStepBudget);
    }

    private void RegisterTileViews()
    {
        if (tileViews == null || tileViews.Count == 0)
        {
            tileViews = new List<TileView>(FindObjectsOfType<TileView>());
        }

        for (int i = 0; i < tileViews.Count; i++)
        {
            TileView tileView = tileViews[i];
            if (tileView == null)
            {
                continue;
            }

            if (tileView.TileConfig == null)
            {
                TileConfig config = mapManager.GetTileConfig(tileView.TileId);
                if (config != null)
                {
                    tileView.BindConfig(config);
                }
            }

            if (!mapManager.RegisterTileView(tileView))
            {
                continue;
            }

            tileView.Clicked -= HandleTileClicked;
            tileView.Clicked += HandleTileClicked;
        }
    }

    private void ShowReachableTiles(int diceValue)
    {
        reachableTiles.Clear();
        mapManager.ClearTileHighlights();
        int allowedSteps = Mathf.Min(diceValue, playerData.energy);

        List<ReachableTileResult> results = mapManager.GetReachableTiles(
            playerData.currentTileId,
            allowedSteps,
            playerData.energy);

        for (int i = 0; i < results.Count; i++)
        {
            ReachableTileResult result = results[i];
            reachableTiles[result.tileId] = result;

            TileView tileView = mapManager.GetTileView(result.tileId);
            if (tileView != null)
            {
                tileView.SetReachable(true);
            }
        }

        statusMessage = results.Count == 0
            ? "没有可达地块，请检查体力和地图连接。"
            : "骰子：" + diceValue + "。请选择绿色地块。";
        Debug.Log(statusMessage, this);
    }

    private void HandleTileClicked(TileView tileView)
    {
        if (tileView == null || playerData == null || playerMover.IsMoving)
        {
            return;
        }

        if (!reachableTiles.TryGetValue(tileView.TileId, out ReachableTileResult result))
        {
            statusMessage = "该地块当前不可达：" + tileView.TileId;
            return;
        }

        EnsurePrototypePhase(CampusNightMarket.Common.GamePhase.ChooseMove);
        if (!turnManager.SelectMoveTarget(result.tileId))
        {
            statusMessage = "回合系统拒绝了移动目标。";
            return;
        }

        int requiredSteps = turnManager.CurrentMoveRequiredSteps;
        List<string> movePath = turnManager.CurrentMovePath;
        if (!ConsumeMoveEnergy(requiredSteps))
        {
            statusMessage = "体力不足，无法移动。";
            return;
        }

        currentMoveStepBudget = 0;
        hasActiveMoveBudget = false;

        tileView.SetSelected(true);
        statusMessage = "正在移动到 " + result.tileId;

        bool started = playerMover.MoveAlongPath(
            mapManager,
            movePath,
            () => CompleteMove(result.tileId));

        if (!started)
        {
            RestoreMoveEnergy(requiredSteps);
            currentMoveStepBudget = requiredSteps;
            hasActiveMoveBudget = true;
            statusMessage = "玩家移动启动失败。";
        }
    }

    private void CompleteMove(string targetTileId)
    {
        playerData.currentTileId = targetTileId;
        reachableTiles.Clear();
        mapManager.ClearTileHighlights();
        turnManager.NotifyPlayerMoveFinished();

        statusMessage =
            "到达 " + targetTileId + "，正在处理地块交互。";
        Debug.Log(statusMessage, this);

        TriggerRandomEvents(EventTriggerType.OnArriveTile, targetTileId, "到达事件");

        if (!tileManager.BeginTileInteraction(targetTileId, out string reason))
        {
            statusMessage = "地块交互启动失败：" + reason;
            Debug.LogWarning(statusMessage, this);
            PrepareNextRollAfterInteraction();
        }
    }

    // 原型阶段暂时跳过真正的夜晚结算，回到下一次投骰。
    public void AdvancePrototypeTurn()
    {
        reachableTiles.Clear();
        mapManager.ClearTileHighlights();
        EnsurePrototypePhase(CampusNightMarket.Common.GamePhase.TileInteraction);
        turnManager.NotifyTileInteractionFinished();
        EnsurePrototypePhase(CampusNightMarket.Common.GamePhase.NightSettlement);
        TriggerRandomEvents(EventTriggerType.OnNightStart, playerData.currentTileId, "夜晚事件");
        RunPrototypeNightSettlement();
        turnManager.NotifyNightSettlementFinished();
        EnsurePrototypePhase(CampusNightMarket.Common.GamePhase.DayEnd);
        ProcessPrototypeLoanInterest();
        turnManager.EndDay();

        if (gameManager.RuntimeData.currentPhase == CampusNightMarket.Common.GamePhase.GameWin)
        {
            statusMessage = "已达到胜利目标，等待确认后进入无尽模式。";
            return;
        }

        if (gameManager.RuntimeData.isGameOver)
        {
            statusMessage = "游戏失败，原型回合停止。";
            return;
        }

        RestoreEnergyToFull();
        currentMoveStepBudget = 0;
        hasActiveMoveBudget = false;
        tileManager.ResetDailyInteractionState();
        BeginPrototypeDaySystems();
        turnManager.StartRollDice();

        statusMessage =
            "原型回合已推进。当前位置：" + playerData.currentTileId +
            "，按空格继续投骰。";
    }

    private void EnsurePrototypePhase(CampusNightMarket.Common.GamePhase phase)
    {
        if (gameManager == null ||
            gameManager.RuntimeData == null ||
            gameManager.RuntimeData.isGameOver ||
            gameManager.RuntimeData.currentPhase == phase)
        {
            return;
        }

        gameManager.SetPhase(phase);
    }

    private void RunPrototypeNightSettlement()
    {
        if (playerData == null)
        {
            lastNightSettlementSummary = "上一晚结算：玩家数据为空";
            return;
        }

        int moneyBefore = playerData.money;
        int lowFoodBefore = playerData.lowFood;
        int highFoodBefore = playerData.highFood;
        int marketCount = marketManager == null || marketManager.Markets == null
            ? 0
            : marketManager.Markets.Count;

        if (economyManager != null)
        {
            economyManager.SettleAllMarketsNightIncome();
            TriggerRandomEvents(EventTriggerType.OnSettlement, playerData.currentTileId, "结算事件");
            RunPrototypeInspections();
        }
        else
        {
            lastNightSettlementSummary = "上一晚结算：未绑定EconomyManager";
            return;
        }

        if (marketManager != null)
        {
            marketManager.AdvanceClosedRounds();
        }

        int moneyDelta = playerData.money - moneyBefore;
        int lowFoodDelta = playerData.lowFood - lowFoodBefore;
        int highFoodDelta = playerData.highFood - highFoodBefore;
        lastNightSettlementSummary =
            "上一晚结算：夜市" + marketCount +
            "，资金 " + FormatSigned(moneyDelta) +
            "，低端食材 " + FormatSigned(lowFoodDelta) +
            "，高端食材 " + FormatSigned(highFoodDelta);
    }

    private string FormatSigned(int value)
    {
        return value >= 0 ? "+" + value : value.ToString();
    }

    private void ProcessPrototypeLoanInterest()
    {
        if (economyManager == null ||
            gameManager == null ||
            mapConfig == null ||
            gameManager.RuntimeData == null)
        {
            return;
        }

        economyManager.ProcessLoanInterest(
            gameManager.RuntimeData.currentDay,
            mapConfig.interestInterval,
            mapConfig.interestRate);
    }

    private bool CanRollDice()
    {
        return playerData != null &&
               playerMover != null &&
               !playerMover.IsMoving &&
               !hasActiveMoveBudget &&
               reachableTiles.Count == 0 &&
               (tileManager == null || !tileManager.IsInteractionActive) &&
               turnManager != null &&
               turnManager.CanRollDice;
    }

    private void BindEconomySystems()
    {
        if (resourceManager == null)
        {
            resourceManager = FindObjectOfType<ResourceManager>();
        }

        if (economyManager == null)
        {
            economyManager = FindObjectOfType<EconomyManager>();
        }

        if (marketManager == null)
        {
            marketManager = FindObjectOfType<MarketManager>();
        }

        if (resourceManager != null)
        {
            resourceManager.SetPlayerData(playerData);
        }

        if (marketManager != null)
        {
            marketManager.SetResourceManager(resourceManager);
        }

        if (economyManager != null)
        {
            economyManager.SetResourceManager(resourceManager);
            economyManager.SetMarketManager(marketManager);
            economyManager.SetGameManager(gameManager);
            economyManager.SetTileConfigs(mapConfig.tileList);
            economyManager.SetStallConfigs(stallConfigs);
        }

        if (tileManager != null)
        {
            tileManager.SetEconomyManager(economyManager);
        }
    }

    private void BindRandomSystems()
    {
        if (eventManager == null)
        {
            eventManager = FindObjectOfType<EventManager>();
            if (eventManager == null)
            {
                eventManager = gameObject.AddComponent<EventManager>();
            }
        }

        if (weatherManager == null)
        {
            weatherManager = FindObjectOfType<WeatherManager>();
            if (weatherManager == null)
            {
                weatherManager = gameObject.AddComponent<WeatherManager>();
            }
        }

        if (inspectionManager == null)
        {
            inspectionManager = FindObjectOfType<InspectionManager>();
            if (inspectionManager == null)
            {
                inspectionManager = gameObject.AddComponent<InspectionManager>();
            }
        }

        if (economyManager != null)
        {
            economyManager.SetWeatherManager(weatherManager);
        }

        if (inspectionManager != null)
        {
            inspectionManager.SetMarketManager(marketManager);
            inspectionManager.SetResourceManager(resourceManager);
        }

        if (eventManager != null)
        {
            eventManager.SetResourceManager(resourceManager);
            eventManager.SetMarketManager(marketManager);
            eventManager.SetInspectionManager(inspectionManager);
            eventManager.SetWeatherManager(weatherManager);
            eventManager.SetGameManager(gameManager);
        }
    }

    private void EnsureS02UIControllers()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        if (FindObjectOfType<S02GameUIController>() == null)
        {
            GameObject controllerObject = new GameObject("UIController");
            controllerObject.transform.SetParent(canvas.transform, false);
            controllerObject.AddComponent<S02GameUIController>();
        }

        if (FindObjectOfType<TileInteractionPanelController>() == null)
        {
            Transform interactionPanel = FindSceneChildByName(
                canvas.transform,
                "Right_InfoPanel",
                "TileInteraction",
                "information");
            GameObject target = interactionPanel != null ? interactionPanel.gameObject : canvas.gameObject;
            target.AddComponent<TileInteractionPanelController>();
        }

        if (FindObjectOfType<MarketPanelController>() == null)
        {
            Transform marketPanel = FindMarketPanelRoot(canvas.transform);
            GameObject target = marketPanel != null ? marketPanel.gameObject : canvas.gameObject;
            target.AddComponent<MarketPanelController>();
        }

        if (FindObjectOfType<S02PopupUIController>() == null)
        {
            GameObject controllerObject = new GameObject("PopupUIController");
            controllerObject.transform.SetParent(canvas.transform, false);
            controllerObject.AddComponent<S02PopupUIController>();
        }
    }

    private void BeginPrototypeDaySystems()
    {
        if (weatherManager != null &&
            gameManager != null &&
            gameManager.RuntimeData != null)
        {
            weatherManager.RefreshWeather(gameManager.RuntimeData.currentDay);
        }

        TriggerRandomEvents(EventTriggerType.OnDayStart, playerData.currentTileId, "每日事件");
    }

    private void TriggerRandomEvents(
        EventTriggerType triggerType,
        string contextTileId,
        string label)
    {
        if (eventManager == null)
        {
            return;
        }

        List<EventRuntimeData> events =
            eventManager.TriggerEvents(triggerType, contextTileId);
        if (events == null || events.Count == 0)
        {
            lastEventSummary = label + "：未触发";
            return;
        }

        lastEventSummary = label + "：触发 " + events.Count + " 个";
    }

    private void PickTilePoolEvent(string tileId)
    {
        if (eventManager == null || mapManager == null)
        {
            lastEventSummary = "地块事件：EventManager 未绑定";
            return;
        }

        TileConfig tileConfig = mapManager.GetTileConfig(tileId);
        if (tileConfig == null || string.IsNullOrEmpty(tileConfig.eventPoolId))
        {
            lastEventSummary = "地块事件：当前地块没有事件池";
            return;
        }

        EventRuntimeData runtimeData =
            eventManager.PickAndExecuteFromPool(tileConfig.eventPoolId, tileId);
        lastEventSummary = runtimeData == null
            ? "地块事件：事件池为空 " + tileConfig.eventPoolId
            : "地块事件：已触发 " + runtimeData.eventId;
    }

    private void RunPrototypeInspections()
    {
        if (inspectionManager == null || marketManager == null || mapConfig == null)
        {
            lastInspectionSummary = "卫生审查：系统未绑定";
            return;
        }

        TriggerRandomEvents(EventTriggerType.OnInspection, playerData.currentTileId, "审查事件");

        List<string> penalizedMarkets = inspectionManager.ExecuteNightlyInspections(
            marketManager.Markets,
            mapConfig.tileList);

        lastInspectionSummary = penalizedMarkets == null || penalizedMarkets.Count == 0
            ? "卫生审查：本晚无处罚"
            : "卫生审查：处罚 " + string.Join(", ", penalizedMarkets);
    }

    private Transform FindSceneChildByName(Transform root, params string[] names)
    {
        if (root == null || names == null)
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < names.Length; i++)
        {
            string targetName = names[i];
            if (string.IsNullOrEmpty(targetName))
            {
                continue;
            }

            for (int j = 0; j < children.Length; j++)
            {
                if (children[j].name.IndexOf(
                        targetName,
                        System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return children[j];
                }
            }
        }

        return null;
    }

    private Transform FindMarketPanelRoot(Transform root)
    {
        Transform panel = FindSceneChildByExactName(root, "Pop_NightMarket");
        if (panel != null)
        {
            return panel;
        }

        panel = FindSceneChildByExactName(root, "MarketPanel");
        if (panel != null)
        {
            return panel;
        }

        if (root == null)
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name.IndexOf(
                    "Night_Market",
                    System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            if (children[i].GetComponent<Button>() == null)
            {
                return children[i];
            }
        }

        return null;
    }

    private Transform FindSceneChildByExactName(Transform root, params string[] names)
    {
        if (root == null || names == null)
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < names.Length; i++)
        {
            string targetName = names[i];
            if (string.IsNullOrEmpty(targetName))
            {
                continue;
            }

            for (int j = 0; j < children.Length; j++)
            {
                if (children[j].name == targetName)
                {
                    return children[j];
                }
            }
        }

        return null;
    }

    private bool ConsumeMoveEnergy(int amount)
    {
        if (resourceManager != null)
        {
            return resourceManager.ConsumeEnergy(amount);
        }

        if (playerData.energy < amount)
        {
            return false;
        }

        playerData.energy -= amount;
        return true;
    }

    private void RestoreMoveEnergy(int amount)
    {
        if (resourceManager != null)
        {
            resourceManager.RestoreEnergy(amount);
            return;
        }

        playerData.energy = Mathf.Min(playerData.energy + amount, playerData.maxEnergy);
    }

    private void RestoreEnergyToFull()
    {
        if (resourceManager != null)
        {
            resourceManager.RestoreEnergy(playerData.maxEnergy);
            return;
        }

        playerData.energy = playerData.maxEnergy;
    }

    public void ContinueAfterWin()
    {
        if (gameManager == null ||
            gameManager.RuntimeData.currentPhase !=
            CampusNightMarket.Common.GamePhase.GameWin)
        {
            return;
        }

        turnManager.ContinueAfterWin();
        RestoreEnergyToFull();
        currentMoveStepBudget = 0;
        hasActiveMoveBudget = false;
        tileManager.ResetDailyInteractionState();
        turnManager.StartRollDice();
        statusMessage = "已进入无尽模式，可以继续投骰。";
    }

    private bool ValidateReferences()
    {
        if (gameManager == null ||
            turnManager == null ||
            mapManager == null ||
            tileManager == null ||
            playerMover == null ||
            mapConfig == null)
        {
            statusMessage = "初始化失败：请在PrototypeBootstrap中绑定全部系统引用和MapConfig。";
            Debug.LogError(statusMessage, this);
            return false;
        }

        return true;
    }

    private void SubscribeTileManagerEvents()
    {
        tileManager.InteractionStarted -= HandleInteractionChanged;
        tileManager.InteractionStarted += HandleInteractionChanged;
        tileManager.InteractionChanged -= HandleInteractionChanged;
        tileManager.InteractionChanged += HandleInteractionChanged;
        tileManager.InteractionCompleted -= HandleInteractionCompleted;
        tileManager.InteractionCompleted += HandleInteractionCompleted;
        tileManager.MessageChanged -= HandleTileMessageChanged;
        tileManager.MessageChanged += HandleTileMessageChanged;
        tileManager.TileOwnerChanged -= HandleTileOwnerChanged;
        tileManager.TileOwnerChanged += HandleTileOwnerChanged;
        tileManager.ResourceRequestCreated -= HandleResourceRequestCreated;
        tileManager.ResourceRequestCreated += HandleResourceRequestCreated;
    }

    private void HandleInteractionChanged(TileInteractionInfo interactionInfo)
    {
        if (interactionInfo != null)
        {
            statusMessage = interactionInfo.message;
        }
    }

    private void HandleInteractionCompleted(string tileId)
    {
        statusMessage = "已完成 " + tileId + " 的地块交互。";

        PrepareNextRollAfterInteraction();
    }

    private void PrepareNextRollAfterInteraction()
    {
        reachableTiles.Clear();
        mapManager.ClearTileHighlights();
        currentMoveStepBudget = 0;
        hasActiveMoveBudget = false;

        if (playerData == null || playerData.energy <= 0)
        {
            statusMessage = "No energy left. Click night settlement.";
            return;
        }

        EnsurePrototypePhase(CampusNightMarket.Common.GamePhase.DayStart);
        turnManager.StartRollDice();
        statusMessage = "Interaction finished. Roll dice again or click night settlement.";
    }

    private void HandleTileMessageChanged(string message)
    {
        statusMessage = message;
    }

    private void HandleTileOwnerChanged(string tileId, CampusNightMarket.Common.OwnerType owner)
    {
        TileView tileView = mapManager.GetTileView(tileId);
        if (tileView != null)
        {
            tileView.SetOwner(owner);
        }
    }

    private void HandleResourceRequestCreated(TileResourceRequest request)
    {
        if (request == null)
        {
            return;
        }

        statusMessage =
            "资源请求占位：" + request.requestType + " " +
            request.resourceType + " x" + request.amount;

        if (resourceManager != null)
        {
            bool succeeded = resourceManager.TryApplyResourceRequest(
                request,
                out string message);
            tileManager.ResolvePendingResourceRequest(succeeded, message);
            return;
        }

        if (autoResolvePlaceholderResourceRequests)
        {
            tileManager.ResolvePendingResourceRequest(
                true,
                "原型模拟ResourceManager处理成功；未直接修改玩家资源。");
        }
    }

    private void RefreshAllTileOwners()
    {
        List<TileConfig> allTiles = mapManager.GetAllTiles();
        for (int i = 0; i < allTiles.Count; i++)
        {
            TileConfig tileConfig = allTiles[i];
            TileView tileView = mapManager.GetTileView(tileConfig.tileId);
            if (tileView != null)
            {
                tileView.SetOwner(tileManager.GetTileOwner(tileConfig.tileId));
            }
        }
    }

    private MarketRuntimeData GetCurrentMarket()
    {
        return marketManager == null || playerData == null
            ? null
            : marketManager.GetMarket(playerData.currentTileId);
    }

    private StallConfig FindStallConfig(string stallId)
    {
        if (stallConfigs == null || string.IsNullOrEmpty(stallId))
        {
            return null;
        }

        for (int i = 0; i < stallConfigs.Count; i++)
        {
            if (stallConfigs[i] != null && stallConfigs[i].stallId == stallId)
            {
                return stallConfigs[i];
            }
        }

        return null;
    }

    private void BuildTestStall(StallConfig stallConfig)
    {
        if (economyManager == null || playerData == null || stallConfig == null)
        {
            statusMessage = "摊位测试失败：缺少EconomyManager、玩家数据或StallConfig。";
            return;
        }

        bool succeeded = economyManager.BuildStallTransaction(
            playerData.currentTileId,
            stallConfig);
        statusMessage = succeeded
            ? "已建设摊位：" + stallConfig.stallName
            : "建设摊位失败，请看Console中的原因。";
    }

    private void UpgradeFirstStall()
    {
        MarketRuntimeData marketData = GetCurrentMarket();
        if (economyManager == null ||
            marketData == null ||
            marketData.stallList == null ||
            marketData.stallList.Count == 0)
        {
            statusMessage = "摊位升级失败：当前夜市没有摊位。";
            return;
        }

        StallRuntimeData stallData = marketData.stallList[0];
        StallConfig stallConfig = FindStallConfig(stallData.stallId);
        if (stallConfig == null)
        {
            statusMessage = "摊位升级失败：找不到摊位配置 " + stallData.stallId;
            return;
        }

        bool succeeded = economyManager.UpgradeStallTransaction(
            marketData.tileId,
            stallData.stallId,
            stallConfig);
        statusMessage = succeeded
            ? "已升级摊位：" + stallConfig.stallName
            : "升级摊位失败，请看Console中的原因。";
    }

    private void ExecuteTileAction(TileActionType action)
    {
        if ((action == TileActionType.TriggerEvent ||
             action == TileActionType.TriggerSpecialRule) &&
            playerData != null)
        {
            PickTilePoolEvent(playerData.currentTileId);
        }

        if (!tileManager.ExecuteAction(action, out string reason))
        {
            statusMessage = reason;
        }
    }

    public void ExecuteTileActionForUI(TileActionType action)
    {
        ExecuteTileAction(action);
    }

    private string GetActionLabel(TileActionType action)
    {
        switch (action)
        {
            case TileActionType.ViewInfo:
                return "查看地块信息";
            case TileActionType.PurchaseAndCreateMarket:
                return "购买并创建夜市";
            case TileActionType.OpenMarketPanel:
                return "打开夜市面板";
            case TileActionType.CollectResource:
                return "收集资源";
            case TileActionType.OpenShop:
                return "打开原型商店";
            case TileActionType.TriggerEvent:
                return "触发事件";
            case TileActionType.TriggerSpecialRule:
                return "执行特殊规则";
            case TileActionType.CompleteInteraction:
                return "结束地块交互";
            default:
                return action.ToString();
        }
    }

    private string GetWeatherSummary()
    {
        if (weatherManager == null || weatherManager.CurrentWeather == null)
        {
            return "天气：暂无";
        }

        WeatherData weather = weatherManager.CurrentWeather;
        return "天气：" + weather.weatherName +
               " / 客流x" + weather.trafficModifier.ToString("0.00") +
               " / 收入x" + weather.incomeModifier.ToString("0.00");
    }

    private void OnGUI()
    {
        if (!showDebugGui)
        {
            return;
        }

        GUILayout.BeginArea(new Rect(16f, 16f, 460f, Screen.height - 32f), GUI.skin.box);
        debugScrollPosition = GUILayout.BeginScrollView(debugScrollPosition);
        GUILayout.Label("地图与地块系统原型");
        if (gameManager != null && gameManager.RuntimeData != null)
        {
            GUILayout.Label(
                "第 " + gameManager.RuntimeData.currentDay +
                " 天 / 阶段：" + gameManager.RuntimeData.currentPhase);
        }

        GUILayout.Label("每日规则：一次掷骰移动 + 一次落地交互，结束交互后进入夜晚结算。");
        GUILayout.Label(statusMessage);
        GUILayout.Label(lastNightSettlementSummary);
        GUILayout.Label(GetWeatherSummary());
        GUILayout.Label(lastEventSummary);
        GUILayout.Label(lastInspectionSummary);

        if (playerData != null)
        {
            GUILayout.Label("当前位置：" + playerData.currentTileId);
            GUILayout.Label("体力：" + playerData.energy + "/" + playerData.maxEnergy);
            GUILayout.Label("资金：" + playerData.money);
            GUILayout.Label(
                "食材：低端 " + playerData.lowFood +
                " / 高端 " + playerData.highFood);
            GUILayout.Label("口碑：" + playerData.reputation);
        }

        MarketRuntimeData currentMarket = GetCurrentMarket();
        if (currentMarket != null)
        {
            GUILayout.Space(8f);
            GUILayout.Label(
                "当前夜市：Lv." + currentMarket.marketLevel +
                " 摊位 " + currentMarket.stallList.Count +
                "/" + currentMarket.maxStallCount);
            GUILayout.Label(
                "吸引力：" + currentMarket.totalAttraction.ToString("0.0") +
                " 卫生：" + currentMarket.totalHygiene.ToString("0.0"));

            GUILayout.Label("可建设摊位：");
            bool hasStallConfig = false;
            if (stallConfigs != null)
            {
                for (int i = 0; i < stallConfigs.Count; i++)
                {
                    StallConfig stallConfig = stallConfigs[i];
                    if (stallConfig == null)
                    {
                        continue;
                    }

                    hasStallConfig = true;
                    bool canTryBuild =
                        currentMarket.stallList.Count < currentMarket.maxStallCount &&
                        currentMarket.marketLevel >= stallConfig.unlockMarketLevel;

                    GUI.enabled = canTryBuild;
                    if (GUILayout.Button(
                            stallConfig.stallName +
                            " / " + stallConfig.stallId +
                            "（建造 " + stallConfig.buildCost +
                            "，解锁Lv." + stallConfig.unlockMarketLevel + "）"))
                    {
                        BuildTestStall(stallConfig);
                    }
                }
            }

            if (!hasStallConfig)
            {
                GUI.enabled = false;
                GUILayout.Button("没有可用StallConfig，请在PrototypeBootstrap里拖入摊位配置");
            }

            GUI.enabled = currentMarket.stallList.Count > 0;
            if (GUILayout.Button("升级第一个摊位"))
            {
                UpgradeFirstStall();
            }

            GUI.enabled = true;
        }

        TileInteractionInfo interactionInfo =
            tileManager == null ? null : tileManager.CurrentInteraction;
        if (interactionInfo != null && tileManager.IsInteractionActive)
        {
            GUILayout.Space(8f);
            GUILayout.Label(
                "当前地块：" + interactionInfo.tileName +
                "（" + interactionInfo.tileType + "）");

            for (int i = 0; i < interactionInfo.availableActions.Count; i++)
            {
                TileActionType action = interactionInfo.availableActions[i];
                if (GUILayout.Button(GetActionLabel(action)))
                {
                    ExecuteTileAction(action);
                }
            }
        }

        if (tileManager != null &&
            tileManager.PendingResourceRequest != null &&
            !autoResolvePlaceholderResourceRequests)
        {
            GUILayout.Space(8f);
            GUILayout.Label("ResourceManager占位回传：");
            if (GUILayout.Button("模拟请求成功"))
            {
                tileManager.ResolvePendingResourceRequest(
                    true,
                    "原型模拟ResourceManager处理成功；未直接修改玩家资源。");
            }

            if (GUILayout.Button("模拟请求失败"))
            {
                tileManager.ResolvePendingResourceRequest(false, "原型模拟资源请求失败。");
            }
        }

        if (gameManager != null &&
            gameManager.RuntimeData.currentPhase ==
            CampusNightMarket.Common.GamePhase.GameWin)
        {
            if (GUILayout.Button("确认胜利并进入无尽模式"))
            {
                ContinueAfterWin();
            }
        }

        GUI.enabled = CanRollDice();
        if (GUILayout.Button("投骰（Space）"))
        {
            RollDice();
        }

        GUI.enabled = true;
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
}
