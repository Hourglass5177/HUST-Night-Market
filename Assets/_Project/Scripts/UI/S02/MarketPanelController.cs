using System.Collections.Generic;
using CampusNightMarket.Data;
using CampusNightMarket.Economy;
using CampusNightMarket.Market;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MarketPanelController : MonoBehaviour
{
    [Header("System References")]
    [SerializeField] private MarketManager marketManager;
    [SerializeField] private EconomyManager economyManager;
    [SerializeField] private MessageScrollBar messageScrollBar;

    [Header("Stall Configs")]
    [SerializeField] private List<StallConfig> stallConfigs = new List<StallConfig>();

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI selectedMarketNameText;

    [Header("Market List")]
    [SerializeField] private Transform marketListContent;
    [SerializeField] private Button marketButtonPrefab;

    [Header("Market Detail Text")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI stallCountText;
    [SerializeField] private TextMeshProUGUI attractionText;
    [SerializeField] private TextMeshProUGUI hygieneText;
    [SerializeField] private TextMeshProUGUI closedRoundsText;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button upgradeMarketButton;

    private readonly List<StallRowView> stallRows = new List<StallRowView>();
    private MarketRuntimeData selectedMarket;

    private void Awake()
    {
        AutoBindMissingReferences();
        BindStaticButtons();
        Close();
    }

    public void Open()
    {
        AutoBindMissingReferences();
        BindStaticButtons();

        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        panelRoot.SetActive(true);
        RefreshAll();
    }

    public void Close()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        panelRoot.SetActive(false);
    }

    public void RefreshAll()
    {
        LoadEditorAssetsIfNeeded();
        RefreshMarketList();

        if ((selectedMarket == null || !ContainsMarket(selectedMarket)) &&
            marketManager != null &&
            marketManager.Markets != null &&
            marketManager.Markets.Count > 0)
        {
            selectedMarket = marketManager.Markets[0];
        }

        RefreshSelectedMarket();
    }

    public void SelectMarket(MarketRuntimeData market)
    {
        selectedMarket = market;
        RefreshSelectedMarket();
    }

    public void UpgradeSelectedMarket()
    {
        if (selectedMarket == null || economyManager == null)
        {
            AddMessage("No market selected.", true);
            return;
        }

        bool succeeded = economyManager.UpgradeMarketTransaction(selectedMarket.tileId);
        AddMessage(succeeded ? "Market upgraded: " + selectedMarket.tileId : "Market upgrade failed.", !succeeded);
        RefreshAll();
    }

    public void BuildStall(StallConfig stallConfig)
    {
        if (selectedMarket == null || stallConfig == null || economyManager == null)
        {
            AddMessage("Cannot build stall.", true);
            return;
        }

        bool succeeded = economyManager.BuildStallTransaction(selectedMarket.tileId, stallConfig);
        AddMessage(succeeded ? "Built stall: " + GetStallName(stallConfig) : "Build stall failed.", !succeeded);
        RefreshAll();
    }

    public void UpgradeStall(StallConfig stallConfig)
    {
        if (selectedMarket == null || stallConfig == null || economyManager == null)
        {
            AddMessage("Cannot upgrade stall.", true);
            return;
        }

        bool succeeded = economyManager.UpgradeStallTransaction(
            selectedMarket.tileId,
            stallConfig.stallId,
            stallConfig);
        AddMessage(succeeded ? "Upgraded stall: " + GetStallName(stallConfig) : "Upgrade stall failed.", !succeeded);
        RefreshAll();
    }

    public void RemoveStall(StallConfig stallConfig)
    {
        if (selectedMarket == null || stallConfig == null || marketManager == null)
        {
            AddMessage("Cannot remove stall.", true);
            return;
        }

        bool succeeded = marketManager.RemoveStall(
            selectedMarket.tileId,
            stallConfig.stallId,
            stallConfigs,
            out string reason);
        AddMessage(succeeded ? "Removed stall: " + GetStallName(stallConfig) : reason, !succeeded);
        RefreshAll();
    }

    private void RefreshMarketList()
    {
        ClearChildren(marketListContent);

        if (marketManager == null || marketManager.Markets == null)
        {
            return;
        }

        for (int i = 0; i < marketManager.Markets.Count; i++)
        {
            MarketRuntimeData market = marketManager.Markets[i];
            if (market == null)
            {
                continue;
            }

            Button button = CreateButton(marketButtonPrefab, marketListContent);
            if (button == null)
            {
                continue;
            }

            MarketRuntimeData capturedMarket = market;
            SetButtonText(button, capturedMarket.tileId + "\nLv." + capturedMarket.marketLevel);
            button.interactable = true;
            button.onClick.AddListener(() => SelectMarket(capturedMarket));
        }
    }

    private void RefreshSelectedMarket()
    {
        bool hasMarket = selectedMarket != null;
        SetText(selectedMarketNameText, hasMarket ? selectedMarket.tileId : "暂无夜市");
        SetText(levelText, hasMarket ? "夜市等级：Lv" + selectedMarket.marketLevel : "夜市等级：Lv-");
        SetText(stallCountText, hasMarket
            ? "最大摊位数：" + selectedMarket.maxStallCount + "  /  当前摊位：" + selectedMarket.stallList.Count
            : "最大摊位数：-  /  当前摊位：-");
        SetText(attractionText, hasMarket ? "吸引力：" + selectedMarket.totalAttraction.ToString("0.0") : "吸引力：--");
        SetText(hygieneText, hasMarket ? "卫生值：" + selectedMarket.totalHygiene.ToString("0.0") : "卫生值：--");
        SetText(closedRoundsText, hasMarket ? "停业天数：" + selectedMarket.closedRounds : "停业天数：--");

        RefreshUpgradeMarketButton(hasMarket);
        RefreshStallRows(hasMarket);
    }

    private void RefreshUpgradeMarketButton(bool hasMarket)
    {
        if (upgradeMarketButton == null)
        {
            return;
        }

        int upgradeCost = hasMarket && marketManager != null
            ? marketManager.GetNextUpgradeCost(selectedMarket)
            : 0;
        bool canUpgrade = hasMarket &&
                          marketManager != null &&
                          marketManager.CanUpgradeMarket(selectedMarket, out upgradeCost, out string _);

        upgradeMarketButton.interactable = canUpgrade;
        SetButtonText(upgradeMarketButton, upgradeCost > 0 ? "升级夜市 $" + upgradeCost : "升级夜市");
    }

    private void RefreshStallRows(bool hasMarket)
    {
        EnsureStallRows();

        for (int i = 0; i < stallRows.Count; i++)
        {
            StallConfig config = i < stallConfigs.Count ? stallConfigs[i] : null;
            StallRuntimeData runtimeData = hasMarket && config != null
                ? FindStall(selectedMarket, config.stallId)
                : null;
            stallRows[i].Refresh(this, selectedMarket, config, runtimeData);
        }
    }

    private void AutoBindMissingReferences()
    {
        Transform root = FindUiRoot();
        if (panelRoot == null || panelRoot.GetComponent<Button>() != null)
        {
            Transform panel = FindChildByExactName(root, "Pop_NightMarket");
            panelRoot = panel != null ? panel.gameObject : gameObject;
        }

        if (marketManager == null) marketManager = FindObjectOfType<MarketManager>();
        if (economyManager == null) economyManager = FindObjectOfType<EconomyManager>();
        if (messageScrollBar == null) messageScrollBar = FindObjectOfType<MessageScrollBar>();

        Transform panelRootTransform = panelRoot != null ? panelRoot.transform : transform;
        if (marketListContent == null) marketListContent = FindChildByExactName(panelRootTransform, "NightMarketListContent");
        if (selectedMarketNameText == null) selectedMarketNameText = FindText(panelRootTransform, "Txt_SelectedMarketName");
        if (levelText == null) levelText = FindText(panelRootTransform, "Txt_MarketLevel");
        if (stallCountText == null) stallCountText = FindText(panelRootTransform, "Txt_MarketStallCount");
        if (attractionText == null) attractionText = FindText(panelRootTransform, "Txt_MarketAttraction");
        if (hygieneText == null) hygieneText = FindText(panelRootTransform, "Txt_MarketHygiene");
        if (closedRoundsText == null) closedRoundsText = FindText(panelRootTransform, "Txt_MarketClosedRounds");
        if (closeButton == null) closeButton = FindButton(panelRootTransform, "Btn_CloseNightMarket");
        if (upgradeMarketButton == null) upgradeMarketButton = FindButton(panelRootTransform, "Btn_upgradeMarket");

        EnsureStallRows();
        LoadEditorAssetsIfNeeded();
    }

    private void BindStaticButtons()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        if (upgradeMarketButton != null)
        {
            upgradeMarketButton.onClick.RemoveListener(UpgradeSelectedMarket);
            upgradeMarketButton.onClick.AddListener(UpgradeSelectedMarket);
        }
    }

    private void EnsureStallRows()
    {
        if (panelRoot == null)
        {
            return;
        }

        stallRows.Clear();
        for (int i = 1; i <= 12; i++)
        {
            string rowName = "StallRow_" + i.ToString("00");
            Transform row = FindChildByExactName(panelRoot.transform, rowName);
            if (row != null)
            {
                stallRows.Add(new StallRowView(row, i));
            }
        }
    }

    private Button CreateButton(Button prefab, Transform parent)
    {
        if (parent == null)
        {
            return null;
        }

        Button button;
        if (prefab != null)
        {
            button = Instantiate(prefab, parent);
        }
        else
        {
            GameObject buttonObject = new GameObject("MarketListButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            button = buttonObject.GetComponent<Button>();

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(buttonObject.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.fontSize = 18;
        }

        button.onClick.RemoveAllListeners();
        button.gameObject.SetActive(true);
        return button;
    }

    private void SetButtonText(Button button, string value)
    {
        if (button == null)
        {
            return;
        }

        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null)
        {
            text.text = value;
        }
    }

    private void ClearChildren(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    private bool ContainsMarket(MarketRuntimeData market)
    {
        return marketManager != null &&
               marketManager.Markets != null &&
               marketManager.Markets.Contains(market);
    }

    private StallRuntimeData FindStall(MarketRuntimeData market, string stallId)
    {
        if (market == null || market.stallList == null || string.IsNullOrEmpty(stallId))
        {
            return null;
        }

        for (int i = 0; i < market.stallList.Count; i++)
        {
            if (market.stallList[i] != null && market.stallList[i].stallId == stallId)
            {
                return market.stallList[i];
            }
        }

        return null;
    }

    private string GetStallName(StallConfig config)
    {
        if (config == null)
        {
            return "--";
        }

        return string.IsNullOrEmpty(config.stallName) ? config.stallId : config.stallName;
    }

    private void AddMessage(string message, bool isWarning = false)
    {
        if (messageScrollBar != null)
        {
            messageScrollBar.AddNewMsg(message, isWarning);
        }
    }

    private void SetText(TextMeshProUGUI target, string value)
    {
        if (target != null)
        {
            target.text = string.IsNullOrEmpty(value) ? "--" : value;
        }
    }

    private Transform FindUiRoot()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
        }

        return canvas != null ? canvas.transform : null;
    }

    private TextMeshProUGUI FindText(Transform root, string name)
    {
        Transform target = FindChildByExactName(root, name);
        if (target == null)
        {
            return null;
        }

        return target.GetComponent<TextMeshProUGUI>() ??
               target.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private Button FindButton(Transform root, string name)
    {
        Transform target = FindChildByExactName(root, name);
        if (target == null)
        {
            return null;
        }

        return target.GetComponent<Button>() ??
               target.GetComponentInChildren<Button>(true);
    }

    private Transform FindChildByExactName(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name))
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == name)
            {
                return children[i];
            }
        }

        return null;
    }

    private void LoadEditorAssetsIfNeeded()
    {
#if UNITY_EDITOR
        if (marketButtonPrefab == null)
        {
            marketButtonPrefab = AssetDatabase.LoadAssetAtPath<Button>(
                "Assets/_Project/Prefabs/UI/PF_UI_Btn_stall.prefab");
        }

        if (stallConfigs == null)
        {
            stallConfigs = new List<StallConfig>();
        }

        if (stallConfigs.Count == 0)
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:StallConfig",
                new[] { "Assets/_Project/Data/Configs/Stalls" });
            System.Array.Sort(guids, (left, right) =>
                string.Compare(
                    AssetDatabase.GUIDToAssetPath(left),
                    AssetDatabase.GUIDToAssetPath(right),
                    System.StringComparison.OrdinalIgnoreCase));

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                StallConfig config = AssetDatabase.LoadAssetAtPath<StallConfig>(path);
                if (config != null)
                {
                    stallConfigs.Add(config);
                }
            }
        }
#endif
    }

    private class StallRowView
    {
        private readonly GameObject root;
        private readonly TextMeshProUGUI nameText;
        private readonly TextMeshProUGUI buildCostText;
        private readonly TextMeshProUGUI upgradeCostText;
        private readonly TextMeshProUGUI priceText;
        private readonly TextMeshProUGUI attractionText;
        private readonly TextMeshProUGUI foodCostText;
        private readonly TextMeshProUGUI hygieneText;
        private readonly TextMeshProUGUI statusText;
        private readonly Button buildButton;
        private readonly Button upgradeButton;
        private readonly Button removeButton;

        public StallRowView(Transform row, int index)
        {
            root = row.gameObject;
            List<TextMeshProUGUI> dataTexts = GetDataTexts(row);
            nameText = GetText(dataTexts, 0);
            buildCostText = GetText(dataTexts, 1);
            upgradeCostText = GetText(dataTexts, 2);
            priceText = GetText(dataTexts, 3);
            attractionText = GetText(dataTexts, 4);
            foodCostText = GetText(dataTexts, 5);
            hygieneText = GetText(dataTexts, 6);
            statusText = GetText(dataTexts, 7);

            buildButton = FindButton(row, "Btn_Stall" + index.ToString("00") + "_Build");
            upgradeButton = FindButton(row, "Btn_Stall" + index.ToString("00") + "_Upgrade");
            removeButton = FindButton(row, "Btn_Stall" + index.ToString("00") + "_Remove");
        }

        public void Refresh(MarketPanelController controller, MarketRuntimeData market, StallConfig config, StallRuntimeData runtimeData)
        {
            bool hasConfig = config != null;
            root.SetActive(hasConfig);

            if (!hasConfig)
            {
                return;
            }

            bool owned = runtimeData != null;
            int upgradeCost = owned && controller.marketManager != null
                ? controller.marketManager.GetStallUpgradeCost(runtimeData, config)
                : config.upgradeCost;

            SetText(nameText, controller.GetStallName(config));
            SetText(buildCostText, "$" + config.buildCost);
            SetText(upgradeCostText, "$" + upgradeCost);
            SetText(priceText, "$" + config.basePrice);
            SetText(attractionText, config.baseAttraction.ToString("0.0"));
            SetText(foodCostText, config.lowFoodCost + "/" + config.highFoodCost);
            SetText(hygieneText, config.hygiene.ToString("0.0"));
            SetText(statusText, owned ? "Lv." + runtimeData.level : "未拥有");

            bool canBuild = market != null &&
                            !owned &&
                            controller.marketManager != null &&
                            controller.marketManager.CanBuildStall(market, config, out string _);
            bool canUpgrade = market != null &&
                              owned &&
                              controller.marketManager != null &&
                              controller.marketManager.CanUpgradeStall(market, runtimeData, config, out string _);

            BindButton(buildButton, "新建", canBuild, () => controller.BuildStall(config));
            BindButton(upgradeButton, "升级", canUpgrade, () => controller.UpgradeStall(config));
            BindButton(removeButton, "拆除", owned, () => controller.RemoveStall(config));
        }

        private static List<TextMeshProUGUI> GetDataTexts(Transform row)
        {
            List<TextMeshProUGUI> result = new List<TextMeshProUGUI>();
            TextMeshProUGUI[] texts = row.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].GetComponentInParent<Button>() == null)
                {
                    result.Add(texts[i]);
                }
            }

            result.Sort((left, right) =>
                left.transform.GetSiblingIndex().CompareTo(right.transform.GetSiblingIndex()));
            return result;
        }

        private static TextMeshProUGUI GetText(List<TextMeshProUGUI> texts, int index)
        {
            return texts != null && index >= 0 && index < texts.Count ? texts[index] : null;
        }

        private static Button FindButton(Transform root, string name)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == name)
                {
                    return children[i].GetComponent<Button>();
                }
            }

            return null;
        }

        private static void BindButton(Button button, string label, bool interactable, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.gameObject.SetActive(true);
            button.interactable = interactable;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);

            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
            {
                text.text = label;
            }
        }

        private static void SetText(TextMeshProUGUI target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
