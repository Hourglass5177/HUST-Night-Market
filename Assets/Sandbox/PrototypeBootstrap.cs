using System.Collections.Generic;
using CampusNightMarket.Core;
using CampusNightMarket.Data;
using CampusNightMarket.Map;
using CampusNightMarket.Player;
using CampusNightMarket.Tiles;
using CampusNightMarket.Turn;
using UnityEngine;
using UnityEngine.Serialization;

// 原型场景入口：初始化测试地图，并串联投骰、选地块和玩家移动。
public class PrototypeBootstrap : MonoBehaviour
{
    [Header("系统引用")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private MapManager mapManager;
    [SerializeField] private TileManager tileManager;
    [SerializeField] private PlayerMover playerMover;

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

    private readonly Dictionary<string, ReachableTileResult> reachableTiles =
        new Dictionary<string, ReachableTileResult>();
    private PlayerRuntimeData playerData;
    private string statusMessage = "等待初始化";

    public PlayerRuntimeData PlayerData
    {
        get { return playerData; }
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
        turnManager.BeginFirstDay();
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

        ShowReachableTiles(diceValue);
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

        List<ReachableTileResult> results = mapManager.GetReachableTiles(
            playerData.currentTileId,
            diceValue,
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

        if (!turnManager.SelectMoveTarget(result.tileId))
        {
            statusMessage = "回合系统拒绝了移动目标。";
            return;
        }

        int requiredSteps = turnManager.CurrentMoveRequiredSteps;
        List<string> movePath = turnManager.CurrentMovePath;
        playerData.energy -= requiredSteps;
        tileView.SetSelected(true);
        statusMessage = "正在移动到 " + result.tileId;

        bool started = playerMover.MoveAlongPath(
            mapManager,
            movePath,
            () => CompleteMove(result.tileId));

        if (!started)
        {
            playerData.energy += requiredSteps;
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

        if (!tileManager.BeginTileInteraction(targetTileId, out string reason))
        {
            statusMessage = "地块交互启动失败：" + reason;
            Debug.LogWarning(statusMessage, this);
        }
    }

    // 原型阶段暂时跳过真正的夜晚结算，回到下一次投骰。
    public void AdvancePrototypeTurn()
    {
        reachableTiles.Clear();
        mapManager.ClearTileHighlights();
        turnManager.NotifyTileInteractionFinished();
        turnManager.NotifyNightSettlementFinished();
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

        playerData.energy = playerData.maxEnergy;
        tileManager.ResetDailyInteractionState();
        turnManager.StartRollDice();

        statusMessage =
            "原型回合已推进。当前位置：" + playerData.currentTileId +
            "，按空格继续投骰。";
    }

    private bool CanRollDice()
    {
        return playerData != null &&
               playerMover != null &&
               !playerMover.IsMoving &&
               reachableTiles.Count == 0 &&
               (tileManager == null || !tileManager.IsInteractionActive) &&
               turnManager != null &&
               turnManager.CanRollDice;
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
        playerData.energy = playerData.maxEnergy;
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

        if (autoAdvanceAfterInteraction)
        {
            AdvancePrototypeTurn();
        }
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

    private void ExecuteTileAction(TileActionType action)
    {
        if (!tileManager.ExecuteAction(action, out string reason))
        {
            statusMessage = reason;
        }
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

    private void OnGUI()
    {
        if (!showDebugGui)
        {
            return;
        }

        GUILayout.BeginArea(new Rect(16f, 16f, 420f, 420f), GUI.skin.box);
        GUILayout.Label("地图与地块系统原型");
        GUILayout.Label(statusMessage);

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
        GUILayout.EndArea();
    }
}
