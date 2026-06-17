using System;
using System.Collections.Generic;
using CampusNightMarket.Common;
using CampusNightMarket.Data;
using CampusNightMarket.Economy;
using CampusNightMarket.Map;
using CampusNightMarket.Market;
using CampusNightMarket.RandomSystem;
using UnityEngine;

namespace CampusNightMarket.Tiles
{
    // 地块管理器：维护地块运行时状态，并编排玩家落地后的交互。
    public class TileManager : MonoBehaviour
    {
        [Header("系统引用")]
        [SerializeField] private MapManager mapManager;
        [SerializeField] private MarketManager marketManager;
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private EventManager eventManager;

        [Header("原型交互设置")]
        [SerializeField] private bool autoCompleteStart = true;
        [SerializeField] private bool autoCompleteResource = true;
        [SerializeField] private bool autoCompleteEvent = true;
        [SerializeField] private bool autoCompleteSpecial = true;

        private readonly Dictionary<string, TileRuntimeData> runtimeDataById =
            new Dictionary<string, TileRuntimeData>();

        private TileConfig currentTileConfig;
        private TileRuntimeData currentRuntimeData;
        private TileInteractionInfo currentInteraction;
        private TileResourceRequest pendingResourceRequest;
        private bool pendingRequestCreatesMarket;
        private bool pendingRequestCompletesInteraction;
        private bool isInteractionActive;

        public TileInteractionInfo CurrentInteraction
        {
            get { return currentInteraction; }
        }

        public bool IsInteractionActive
        {
            get { return isInteractionActive; }
        }

        public TileResourceRequest PendingResourceRequest
        {
            get { return pendingResourceRequest; }
        }

        public event Action<TileInteractionInfo> InteractionStarted;
        public event Action<TileInteractionInfo> InteractionChanged;
        public event Action<string> InteractionCompleted;
        public event Action<string> MessageChanged;
        public event Action<string, OwnerType> TileOwnerChanged;
        public event Action<TileResourceRequest> ResourceRequestCreated;

        // 为当前地图建立全部地块运行时状态。
        public bool InitializeTiles(MapConfig mapConfig, out string reason)
        {
            reason = string.Empty;
            runtimeDataById.Clear();
            ClearCurrentInteraction();

            if (mapManager == null)
            {
                reason = "TileManager未绑定MapManager。";
                return false;
            }

            if (mapConfig == null || mapConfig.tileList == null)
            {
                reason = "地图配置为空。";
                return false;
            }

            for (int i = 0; i < mapConfig.tileList.Count; i++)
            {
                TileConfig tileConfig = mapConfig.tileList[i];
                if (tileConfig == null || string.IsNullOrEmpty(tileConfig.tileId))
                {
                    reason = "存在无法初始化的地块配置，索引：" + i;
                    runtimeDataById.Clear();
                    return false;
                }

                OwnerType owner = OwnerType.None;
                if (marketManager != null && marketManager.GetMarket(tileConfig.tileId) != null)
                {
                    owner = OwnerType.Player;
                }

                runtimeDataById[tileConfig.tileId] = new TileRuntimeData
                {
                    tileId = tileConfig.tileId,
                    owner = owner,
                    isDiscovered = tileConfig.tileId == mapConfig.startTileId,
                    isInteractionCompletedToday = false
                };
            }

            return true;
        }

        public void SetEconomyManager(EconomyManager manager)
        {
            economyManager = manager;
        }

        public void SetEventManager(EventManager manager)
        {
            eventManager = manager;
        }

        public TileRuntimeData GetTileRuntimeData(string tileId)
        {
            if (string.IsNullOrEmpty(tileId))
            {
                return null;
            }

            runtimeDataById.TryGetValue(tileId, out TileRuntimeData runtimeData);
            return runtimeData;
        }

        public OwnerType GetTileOwner(string tileId)
        {
            TileRuntimeData runtimeData = GetTileRuntimeData(tileId);
            return runtimeData == null ? OwnerType.None : runtimeData.owner;
        }

        public bool SetTileOwner(string tileId, OwnerType owner)
        {
            TileRuntimeData runtimeData = GetTileRuntimeData(tileId);
            if (runtimeData == null)
            {
                return false;
            }

            runtimeData.owner = owner;
            TileOwnerChanged?.Invoke(tileId, owner);
            return true;
        }

        // 根据地块配置和局内状态生成UI可用的操作列表。
        public TileInteractionInfo GetInteractionInfo(string tileId)
        {
            TileConfig tileConfig = mapManager == null ? null : mapManager.GetTileConfig(tileId);
            TileRuntimeData runtimeData = GetTileRuntimeData(tileId);
            if (tileConfig == null || runtimeData == null)
            {
                return null;
            }

            TileInteractionInfo info = new TileInteractionInfo
            {
                tileId = tileConfig.tileId,
                tileName = string.IsNullOrEmpty(tileConfig.tileName)
                    ? tileConfig.tileId
                    : tileConfig.tileName,
                tileType = tileConfig.tileType,
                owner = runtimeData.owner,
                studentRatio = tileConfig.studentRatio,
                teacherRatio = tileConfig.teacherRatio,
                touristRatio = tileConfig.touristRatio,
                residentRatio = tileConfig.residentRatio
            };

            info.availableActions.Add(TileActionType.ViewInfo);

            switch (tileConfig.tileType)
            {
                case TileType.Start:
                    info.message = "到达起点。";
                    info.availableActions.Add(TileActionType.CompleteInteraction);
                    break;

                case TileType.Buildable:
                    AddBuildableActions(tileConfig, runtimeData, info);
                    break;

                case TileType.Resource:
                    if (runtimeData.isInteractionCompletedToday)
                    {
                        info.message = "该资源地块今天已经领取过。";
                        info.availableActions.Add(TileActionType.CompleteInteraction);
                    }
                    else if (tileConfig.resourceType == ResourceType.None)
                    {
                        info.message = "资源地块未配置资源类型。";
                        info.availableActions.Add(TileActionType.CompleteInteraction);
                    }
                    else
                    {
                        info.message =
                            "可以收集：" + GetResourceDisplayName(tileConfig.resourceType) +
                            " x" + Mathf.Max(0, tileConfig.resourceAmount);
                        info.availableActions.Add(TileActionType.CollectResource);
                        info.availableActions.Add(TileActionType.CompleteInteraction);
                    }
                    break;

                case TileType.Shop:
                    info.message = "商店系统尚未接入，可打开原型商店。";
                    info.availableActions.Add(TileActionType.OpenShop);
                    info.availableActions.Add(TileActionType.CompleteInteraction);
                    break;

                case TileType.Event:
                    if (runtimeData.isInteractionCompletedToday)
                    {
                        info.message = "该事件地块今天已经触发过。";
                        info.availableActions.Add(TileActionType.CompleteInteraction);
                    }
                    else
                    {
                        info.message = string.IsNullOrEmpty(tileConfig.eventPoolId)
                            ? "事件地块未配置事件池。"
                            : "将触发事件池：" + tileConfig.eventPoolId;
                        info.availableActions.Add(TileActionType.TriggerEvent);
                    }
                    break;

                case TileType.Special:
                    if (runtimeData.isInteractionCompletedToday)
                    {
                        info.message = "该特殊地块今天已经处理过。";
                        info.availableActions.Add(TileActionType.CompleteInteraction);
                    }
                    else
                    {
                        info.message = "特殊规则原型：执行一次地块检查。";
                        info.availableActions.Add(TileActionType.TriggerSpecialRule);
                    }
                    break;

                default:
                    info.message = "该地块没有可执行交互。";
                    info.availableActions.Add(TileActionType.CompleteInteraction);
                    break;
            }

            return info;
        }

        public bool BeginTileInteraction(string tileId, out string reason)
        {
            reason = string.Empty;
            if (isInteractionActive)
            {
                reason = "已有地块交互正在进行。";
                return false;
            }

            currentTileConfig = mapManager == null ? null : mapManager.GetTileConfig(tileId);
            currentRuntimeData = GetTileRuntimeData(tileId);
            if (currentTileConfig == null || currentRuntimeData == null)
            {
                reason = "找不到地块配置或运行时数据：" + tileId;
                return false;
            }

            currentRuntimeData.isDiscovered = true;
            currentInteraction = GetInteractionInfo(tileId);
            isInteractionActive = true;
            InteractionStarted?.Invoke(currentInteraction);
            PublishMessage(currentInteraction.message);

            ExecuteAutomaticInteraction();
            return true;
        }

        public bool ExecuteAction(TileActionType action, out string reason)
        {
            reason = string.Empty;
            if (!isInteractionActive || currentInteraction == null)
            {
                reason = "当前没有地块交互。";
                return false;
            }

            if (!currentInteraction.HasAction(action))
            {
                reason = "当前地块不能执行该操作：" + action;
                return false;
            }

            switch (action)
            {
                case TileActionType.ViewInfo:
                    PublishMessage(BuildTileInformation(currentTileConfig, currentRuntimeData));
                    return true;

                case TileActionType.PurchaseAndCreateMarket:
                    return TryPurchaseCurrentTile(out reason);

                case TileActionType.OpenMarketPanel:
                    PublishMessage("夜市管理UI尚未接入。当前地块已拥有夜市。");
                    return true;

                case TileActionType.CollectResource:
                    return CollectCurrentResource(out reason);

                case TileActionType.OpenShop:
                    PublishMessage("原型商店已打开。正式商品和交易等待Shop/Resource系统接入。");
                    return true;

                case TileActionType.TriggerEvent:
                    return TriggerCurrentEvent(out reason);

                case TileActionType.TriggerSpecialRule:
                    return TriggerCurrentSpecialRule(out reason);

                case TileActionType.CompleteInteraction:
                    return CompleteCurrentInteraction();

                default:
                    reason = "未实现的地块操作：" + action;
                    return false;
            }
        }

        public bool CompleteCurrentInteraction()
        {
            if (!isInteractionActive || currentTileConfig == null)
            {
                return false;
            }

            string completedTileId = currentTileConfig.tileId;
            ClearCurrentInteraction();
            InteractionCompleted?.Invoke(completedTileId);
            return true;
        }

        // 每天开始时清除“今日已交互”状态。
        public void ResetDailyInteractionState()
        {
            foreach (TileRuntimeData runtimeData in runtimeDataById.Values)
            {
                runtimeData.isInteractionCompletedToday = false;
            }
        }

        private void AddBuildableActions(
            TileConfig tileConfig,
            TileRuntimeData runtimeData,
            TileInteractionInfo info)
        {
            if (runtimeData.owner == OwnerType.Player ||
                marketManager != null && marketManager.GetMarket(tileConfig.tileId) != null)
            {
                info.owner = OwnerType.Player;
                info.message = "这是玩家拥有的夜市地块。";
                info.availableActions.Add(TileActionType.OpenMarketPanel);
                info.availableActions.Add(TileActionType.CompleteInteraction);
                return;
            }

            if (runtimeData.owner == OwnerType.NPC)
            {
                info.message = "该地块由NPC占用，当前不能购买。";
                info.availableActions.Add(TileActionType.CompleteInteraction);
                return;
            }

            if (!tileConfig.canPurchase)
            {
                info.message = "该地块当前不可购买。";
                info.availableActions.Add(TileActionType.CompleteInteraction);
                return;
            }

            info.message =
                "可以请求资源系统扣除 " + tileConfig.purchasePrice +
                " 资金并创建夜市。";
            info.availableActions.Add(TileActionType.PurchaseAndCreateMarket);
            info.availableActions.Add(TileActionType.CompleteInteraction);
        }

        private bool TryPurchaseCurrentTile(out string reason)
        {
            reason = string.Empty;
            if (marketManager == null)
            {
                reason = "TileManager未绑定MarketManager。";
                return false;
            }

            if (currentTileConfig == null ||
                currentRuntimeData == null ||
                currentTileConfig.tileType != TileType.Buildable)
            {
                reason = "当前不是可建设地块。";
                return false;
            }

            if (currentRuntimeData.owner != OwnerType.None)
            {
                reason = "当前地块已有所有者。";
                return false;
            }

            if (economyManager != null)
            {
                if (!economyManager.PurchaseTile(currentTileConfig))
                {
                    reason = "经济系统拒绝购买该地块。";
                    return false;
                }

                currentRuntimeData.owner = OwnerType.Player;
                currentRuntimeData.isInteractionCompletedToday = true;
                TileOwnerChanged?.Invoke(currentTileConfig.tileId, OwnerType.Player);
                RefreshCurrentInteraction("已通过经济系统购买地块并创建夜市。");
                return true;
            }

            if (!marketManager.CanCreateMarket(currentTileConfig, out reason))
            {
                return false;
            }

            if (pendingResourceRequest != null)
            {
                reason = "已有资源请求等待处理。";
                return false;
            }

            pendingRequestCreatesMarket = true;
            pendingRequestCompletesInteraction = false;
            CreateResourceRequest(
                TileResourceRequestType.Spend,
                ResourceType.Money,
                currentTileConfig.purchasePrice,
                "购买地块并创建夜市");
            return true;
        }

        private bool CollectCurrentResource(out string reason)
        {
            reason = string.Empty;
            if (currentTileConfig == null ||
                currentRuntimeData == null ||
                currentTileConfig.tileType != TileType.Resource)
            {
                reason = "当前不是资源地块。";
                return false;
            }

            if (currentRuntimeData.isInteractionCompletedToday)
            {
                reason = "该资源地块今天已经领取过。";
                return false;
            }

            if (currentTileConfig.resourceType == ResourceType.None)
            {
                reason = "资源类型未配置。";
                return false;
            }

            int amount = Mathf.Max(0, currentTileConfig.resourceAmount);
            if (amount <= 0)
            {
                reason = "资源数量需要大于0。";
                return false;
            }

            pendingRequestCreatesMarket = false;
            pendingRequestCompletesInteraction = true;
            CreateResourceRequest(
                TileResourceRequestType.Grant,
                currentTileConfig.resourceType,
                amount,
                "领取资源地块奖励");
            return true;
        }

        // ResourceManager处理请求后调用；地块系统只根据结果推进交互。
        public bool ResolvePendingResourceRequest(bool succeeded, string message)
        {
            if (pendingResourceRequest == null || !isInteractionActive)
            {
                return false;
            }

            TileResourceRequest resolvedRequest = pendingResourceRequest;
            bool createsMarket = pendingRequestCreatesMarket;
            bool completesInteraction = pendingRequestCompletesInteraction;
            pendingResourceRequest = null;
            pendingRequestCreatesMarket = false;
            pendingRequestCompletesInteraction = false;

            if (!succeeded)
            {
                RefreshCurrentInteraction(
                    string.IsNullOrEmpty(message) ? "资源请求被拒绝。" : message);
                return true;
            }

            if (createsMarket)
            {
                if (marketManager == null ||
                    !marketManager.TryCreateMarket(
                        currentTileConfig.tileId,
                        out MarketRuntimeData marketData))
                {
                    pendingRequestCompletesInteraction = false;
                    CreateResourceRequest(
                        TileResourceRequestType.Grant,
                        ResourceType.Money,
                        resolvedRequest.amount,
                        "夜市创建失败，退回地块购买费用");
                    return true;
                }

                currentRuntimeData.owner = OwnerType.Player;
                currentRuntimeData.isInteractionCompletedToday = true;
                TileOwnerChanged?.Invoke(currentTileConfig.tileId, OwnerType.Player);
                RefreshCurrentInteraction(
                    string.IsNullOrEmpty(message)
                        ? "资源系统确认扣款，夜市创建成功。"
                        : message);
                return true;
            }

            if (completesInteraction)
            {
                currentRuntimeData.isInteractionCompletedToday = true;
                RefreshCurrentInteraction(
                    string.IsNullOrEmpty(message) ? "资源奖励已发放。" : message);

                if (autoCompleteResource)
                {
                    CompleteCurrentInteraction();
                }
            }
            else
            {
                RefreshCurrentInteraction(
                    string.IsNullOrEmpty(message)
                        ? "资源请求处理完成。"
                        : message);
            }

            return true;
        }

        private bool TriggerCurrentEvent(out string reason)
        {
            reason = string.Empty;
            if (currentTileConfig == null || currentTileConfig.tileType != TileType.Event)
            {
                reason = "当前不是事件地块。";
                return false;
            }

            currentRuntimeData.isInteractionCompletedToday = true;
            RefreshCurrentInteraction(ExecuteCurrentTilePoolEvent("事件地块"));

            if (autoCompleteEvent)
            {
                CompleteCurrentInteraction();
            }

            return true;
        }

        private bool TriggerCurrentSpecialRule(out string reason)
        {
            reason = string.Empty;
            if (currentTileConfig == null || currentTileConfig.tileType != TileType.Special)
            {
                reason = "当前不是特殊地块。";
                return false;
            }

            currentRuntimeData.isInteractionCompletedToday = true;
            RefreshCurrentInteraction(ExecuteCurrentTilePoolEvent("特殊地块"));

            if (autoCompleteSpecial)
            {
                CompleteCurrentInteraction();
            }

            return true;
        }

        private string ExecuteCurrentTilePoolEvent(string label)
        {
            if (currentTileConfig == null || string.IsNullOrEmpty(currentTileConfig.eventPoolId))
            {
                return label + "未配置事件池。";
            }

            if (eventManager == null)
            {
                return label + "事件池未触发：EventManager 未绑定。";
            }

            EventRuntimeData runtimeData =
                eventManager.PickAndExecuteFromPool(
                    currentTileConfig.eventPoolId,
                    currentTileConfig.tileId);

            return runtimeData == null
                ? label + "事件池为空：" + currentTileConfig.eventPoolId
                : label + "已触发：" + GetEventDisplayText(runtimeData);
        }

        private string GetEventDisplayText(EventRuntimeData runtimeData)
        {
            if (runtimeData == null)
            {
                return string.Empty;
            }

            string eventName = string.IsNullOrEmpty(runtimeData.eventName)
                ? runtimeData.eventId
                : runtimeData.eventName;
            if (string.IsNullOrEmpty(runtimeData.eventDescription))
            {
                return eventName;
            }

            return eventName + " - " + runtimeData.eventDescription;
        }

        private void ExecuteAutomaticInteraction()
        {
            if (currentInteraction == null)
            {
                return;
            }

            switch (currentInteraction.tileType)
            {
                case TileType.Start:
                    if (autoCompleteStart)
                    {
                        CompleteCurrentInteraction();
                    }
                    break;

                case TileType.Resource:
                    if (currentInteraction.HasAction(TileActionType.CollectResource))
                    {
                        ExecuteAction(TileActionType.CollectResource, out string resourceReason);
                        if (!string.IsNullOrEmpty(resourceReason))
                        {
                            PublishMessage(resourceReason);
                        }
                    }
                    else if (autoCompleteResource)
                    {
                        CompleteCurrentInteraction();
                    }
                    break;

                case TileType.Event:
                    if (currentInteraction.HasAction(TileActionType.TriggerEvent))
                    {
                        ExecuteAction(TileActionType.TriggerEvent, out string eventReason);
                        if (!string.IsNullOrEmpty(eventReason))
                        {
                            PublishMessage(eventReason);
                        }
                    }
                    break;

                case TileType.Special:
                    if (currentInteraction.HasAction(TileActionType.TriggerSpecialRule))
                    {
                        ExecuteAction(
                            TileActionType.TriggerSpecialRule,
                            out string specialReason);
                        if (!string.IsNullOrEmpty(specialReason))
                        {
                            PublishMessage(specialReason);
                        }
                    }
                    break;
            }
        }

        private void RefreshCurrentInteraction(string message)
        {
            currentInteraction = GetInteractionInfo(currentTileConfig.tileId);
            currentInteraction.message = message;
            InteractionChanged?.Invoke(currentInteraction);
            PublishMessage(message);
        }

        private string BuildTileInformation(
            TileConfig tileConfig,
            TileRuntimeData runtimeData)
        {
            return
                tileConfig.tileName + " [" + tileConfig.tileType + "] " +
                "所有者：" + runtimeData.owner +
                "，基础客流：" + tileConfig.baseTraffic +
                "，消费力：" + tileConfig.consumePower;
        }

        private string GetResourceDisplayName(ResourceType resourceType)
        {
            switch (resourceType)
            {
                case ResourceType.Money:
                    return "资金";
                case ResourceType.LowFood:
                    return "低端食材";
                case ResourceType.HighFood:
                    return "高端食材";
                case ResourceType.Reputation:
                    return "口碑";
                case ResourceType.Energy:
                    return "体力";
                default:
                    return "未知资源";
            }
        }

        private void PublishMessage(string message)
        {
            MessageChanged?.Invoke(message);
            Debug.Log(message, this);
        }

        private void ClearCurrentInteraction()
        {
            pendingResourceRequest = null;
            pendingRequestCreatesMarket = false;
            pendingRequestCompletesInteraction = false;
            isInteractionActive = false;
            currentTileConfig = null;
            currentRuntimeData = null;
            currentInteraction = null;
        }

        private void CreateResourceRequest(
            TileResourceRequestType requestType,
            ResourceType resourceType,
            int amount,
            string reason)
        {
            pendingResourceRequest = new TileResourceRequest
            {
                tileId = currentTileConfig.tileId,
                requestType = requestType,
                resourceType = resourceType,
                amount = Mathf.Max(0, amount),
                reason = reason
            };

            PublishMessage(
                "等待ResourceManager处理：" + requestType + " " +
                GetResourceDisplayName(resourceType) + " x" +
                pendingResourceRequest.amount + "。");
            ResourceRequestCreated?.Invoke(pendingResourceRequest);
        }
    }
}
