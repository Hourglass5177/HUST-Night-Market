using CampusNightMarket.Common;
using CampusNightMarket.Core;
using CampusNightMarket.Data;
using CampusNightMarket.Economy;
using CampusNightMarket.Market;
using CampusNightMarket.Player;
using CampusNightMarket.RandomSystem;
using CampusNightMarket.Turn;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class S02GameUIController : MonoBehaviour
{
    [Header("System References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private ResourceManager resourceManager;
    [SerializeField] private WeatherManager weatherManager;
    [SerializeField] private PrototypeBootstrap prototypeBootstrap;

    [Header("Panel Controllers")]
    [SerializeField] private TileInteractionPanelController tileInteractionPanel;
    [SerializeField] private MarketPanelController marketPanel;
    [SerializeField] private MessageScrollBar messageScrollBar;

    [Header("Top HUD Text")]
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI phaseText;
    [SerializeField] private TextMeshProUGUI diceText;
    [SerializeField] private TextMeshProUGUI weatherText;

    [Header("Resource Text")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI loanText;
    [SerializeField] private TextMeshProUGUI energyText;
    [SerializeField] private TextMeshProUGUI lowFoodText;
    [SerializeField] private TextMeshProUGUI highFoodText;
    [SerializeField] private TextMeshProUGUI reputationText;

    [Header("Status Text")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Buttons")]
    [SerializeField] private Button rollDiceButton;
    [SerializeField] private Button openMarketPanelButton;
    [SerializeField] private Button endInteractionButton;
    [SerializeField] private Button continueAfterWinButton;

    private void Awake()
    {
        AutoBindMissingReferences();
        BindButtonEvents();
    }

    private void OnEnable()
    {
        if (turnManager != null)
        {
            turnManager.PhaseChanged += HandlePhaseChanged;
            turnManager.DiceRolled += HandleDiceRolled;
        }
    }

    private void OnDisable()
    {
        if (turnManager != null)
        {
            turnManager.PhaseChanged -= HandlePhaseChanged;
            turnManager.DiceRolled -= HandleDiceRolled;
        }
    }

    private void Update()
    {
        RefreshHud();
    }

    public void RefreshHud()
    {
        GameRuntimeData runtimeData = gameManager != null ? gameManager.RuntimeData : null;
        PlayerRuntimeData playerData = GetPlayerData();

        SetText(dayText, runtimeData != null ? "天数：" + runtimeData.currentDay : "天数：--");
        SetText(phaseText, runtimeData != null ? "第 " + runtimeData.currentDay + " 天" : "第 -- 天");
        SetText(diceText, turnManager != null && turnManager.CurrentDiceValue > 0
            ? "骰子：" + turnManager.CurrentDiceValue
            : "骰子：--");
        SetText(weatherText, "天气：" + GetWeatherLabel());

        SetText(moneyText, playerData != null ? "金钱：" + playerData.money : "金钱：--");
        SetText(loanText, playerData != null
            ? "贷款：" + playerData.loan + "（剩" + GetDaysUntilNextInterest(runtimeData) + "天）"
            : "贷款：--");
        SetText(energyText, playerData != null ? "体力：" + playerData.energy + "/" + playerData.maxEnergy : "体力：--");
        SetText(lowFoodText, playerData != null
            ? playerData.lowFood + "（低端）、" + playerData.highFood + "（高端）"
            : "--（低端）、--（高端）");
        SetText(highFoodText, string.Empty);
        SetText(reputationText, playerData != null ? "口碑：" + playerData.reputation : "口碑：--");

        if (rollDiceButton != null)
        {
            rollDiceButton.interactable = prototypeBootstrap != null && prototypeBootstrap.CanRollDiceForUI();
        }

        if (continueAfterWinButton != null)
        {
            continueAfterWinButton.gameObject.SetActive(
                runtimeData != null && runtimeData.currentPhase == GamePhase.GameWin);
        }
    }

    public void OnRollDiceClicked()
    {
        if (prototypeBootstrap == null)
        {
            AddMessage("PrototypeBootstrap is missing.", true);
            return;
        }

        prototypeBootstrap.RollDice();
        AddMessage("Rolled dice.");
        RefreshHud();
    }

    public void OnOpenMarketPanelClicked()
    {
        if (marketPanel == null)
        {
            marketPanel = FindMarketPanelController();
        }

        if (marketPanel != null)
        {
            marketPanel.Open();
            AddMessage("Opened market panel.");
            return;
        }

        AddMessage("Market panel is missing.", true);
    }

    public void OnEndInteractionClicked()
    {
        if (prototypeBootstrap != null)
        {
            prototypeBootstrap.ExecuteTileActionForUI(CampusNightMarket.Tiles.TileActionType.CompleteInteraction);
            AddMessage("Ended tile interaction.");
        }
    }

    public void OnContinueAfterWinClicked()
    {
        if (prototypeBootstrap != null)
        {
            prototypeBootstrap.ContinueAfterWin();
            AddMessage("Continued after victory.");
        }
    }

    public void AddMessage(string message, bool isWarning = false)
    {
        if (messageScrollBar != null)
        {
            messageScrollBar.AddNewMsg(message, isWarning);
        }

        SetText(statusText, message);
    }

    private void AutoBindMissingReferences()
    {
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (turnManager == null) turnManager = FindObjectOfType<TurnManager>();
        if (resourceManager == null) resourceManager = FindObjectOfType<ResourceManager>();
        if (weatherManager == null) weatherManager = FindObjectOfType<WeatherManager>();
        if (prototypeBootstrap == null) prototypeBootstrap = FindObjectOfType<PrototypeBootstrap>();
        if (tileInteractionPanel == null) tileInteractionPanel = FindObjectOfType<TileInteractionPanelController>();
        if (marketPanel == null) marketPanel = FindMarketPanelController();
        if (messageScrollBar == null) messageScrollBar = FindObjectOfType<MessageScrollBar>();

        Transform root = FindUiRoot();
        if (root == null)
        {
            return;
        }

        if (dayText == null) dayText = FindText(root, "day", "txt_Day", "Day");
        if (phaseText == null) phaseText = FindText(root, "TopBar_Status", "phase", "Phase");
        if (diceText == null) diceText = FindText(root, "dice", "Dice", "Progress");
        if (weatherText == null) weatherText = FindText(root, "weather", "Weather");
        if (moneyText == null) moneyText = FindText(root, "money", "Money");
        if (loanText == null) loanText = FindText(root, "Loan", "loan");
        if (energyText == null) energyText = FindText(root, "Stamina", "energy", "Energy");
        if (lowFoodText == null) lowFoodText = FindText(root, "food", "LowFood", "lowFood");
        if (highFoodText == null) highFoodText = FindText(root, "HighFood", "highFood");
        if (reputationText == null) reputationText = FindText(root, "Reputation", "reputation");
        // Do not auto-bind generic information/status text. Those names often belong to designed labels.

        if (rollDiceButton == null) rollDiceButton = FindButton(root, "RollDice", "Dice", "Btn_left");
        if (openMarketPanelButton == null) openMarketPanelButton = FindButton(root, "Night_Market", "Night_Market_Button", "MarketButton", "Menu");
        if (continueAfterWinButton == null) continueAfterWinButton = FindButton(root, "Continue", "btn_Continue");

        ApplyChineseFontToHudTexts();
    }

    private void BindButtonEvents()
    {
        if (rollDiceButton != null)
        {
            rollDiceButton.onClick.RemoveListener(OnRollDiceClicked);
            rollDiceButton.onClick.AddListener(OnRollDiceClicked);
        }

        if (openMarketPanelButton != null)
        {
            openMarketPanelButton.onClick.RemoveListener(OnOpenMarketPanelClicked);
            openMarketPanelButton.onClick.AddListener(OnOpenMarketPanelClicked);
        }

        if (endInteractionButton != null)
        {
            endInteractionButton.onClick.RemoveListener(OnEndInteractionClicked);
            endInteractionButton.onClick.AddListener(OnEndInteractionClicked);
        }

        if (continueAfterWinButton != null)
        {
            continueAfterWinButton.onClick.RemoveListener(OnContinueAfterWinClicked);
            continueAfterWinButton.onClick.AddListener(OnContinueAfterWinClicked);
        }
    }

    private PlayerRuntimeData GetPlayerData()
    {
        if (resourceManager != null && resourceManager.PlayerData != null)
        {
            return resourceManager.PlayerData;
        }

        return prototypeBootstrap != null ? prototypeBootstrap.PlayerData : null;
    }

    private string GetWeatherLabel()
    {
        WeatherData weather = weatherManager != null ? weatherManager.CurrentWeather : null;
        if (weather == null)
        {
            return "--";
        }

        return weather.weatherName +
               "  客流x" + weather.trafficModifier.ToString("0.00") +
               " 收入x" + weather.incomeModifier.ToString("0.00");
    }

    private int GetDaysUntilNextInterest(GameRuntimeData runtimeData)
    {
        MapConfig mapConfig = prototypeBootstrap != null ? prototypeBootstrap.MapConfigForUI : null;
        if (runtimeData == null || mapConfig == null || mapConfig.interestInterval <= 0)
        {
            return 0;
        }

        int currentDay = Mathf.Max(1, runtimeData.currentDay);
        int remainder = currentDay % mapConfig.interestInterval;
        return remainder == 0 ? mapConfig.interestInterval : mapConfig.interestInterval - remainder;
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        AddMessage("Phase: " + phase);
        RefreshHud();
    }

    private void HandleDiceRolled(int diceValue)
    {
        AddMessage("Dice: " + diceValue);
        RefreshHud();
    }

    private void SetText(TextMeshProUGUI target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }

    private void ApplyChineseFontToHudTexts()
    {
#if UNITY_EDITOR
        TMP_FontAsset chineseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/_Project/UI/Fonts/SC.asset");
        if (chineseFont == null)
        {
            chineseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/_Project/UI/Fonts/SourceHanSansSC-VF SDF 1.asset");
        }

        if (chineseFont == null)
        {
            return;
        }

        ApplyFont(dayText, chineseFont);
        ApplyFont(phaseText, chineseFont);
        ApplyFont(diceText, chineseFont);
        ApplyFont(weatherText, chineseFont);
        ApplyFont(moneyText, chineseFont);
        ApplyFont(loanText, chineseFont);
        ApplyFont(energyText, chineseFont);
        ApplyFont(lowFoodText, chineseFont);
        ApplyFont(highFoodText, chineseFont);
        ApplyFont(reputationText, chineseFont);
        ApplyFont(statusText, chineseFont);
#endif
    }

    private void ApplyFont(TextMeshProUGUI target, TMP_FontAsset font)
    {
        if (target != null && font != null)
        {
            target.font = font;
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

    private MarketPanelController FindMarketPanelController()
    {
        MarketPanelController controller = FindObjectOfType<MarketPanelController>();
        if (controller != null)
        {
            return controller;
        }

        MarketPanelController[] inactiveControllers =
            Resources.FindObjectsOfTypeAll<MarketPanelController>();
        for (int i = 0; i < inactiveControllers.Length; i++)
        {
            if (inactiveControllers[i] != null &&
                inactiveControllers[i].gameObject.scene.IsValid())
            {
                return inactiveControllers[i];
            }
        }

        Transform root = FindUiRoot();
        Transform panel = FindChildByName(root, "Pop_NightMarket", "MarketPanel");
        if (panel == null)
        {
            return null;
        }

        controller = panel.GetComponent<MarketPanelController>();
        if (controller == null)
        {
            controller = panel.gameObject.AddComponent<MarketPanelController>();
        }

        return controller;
    }

    private TextMeshProUGUI FindText(Transform root, params string[] names)
    {
        Transform target = FindChildByName(root, names);
        if (target == null)
        {
            return null;
        }

        return target.GetComponent<TextMeshProUGUI>() ??
               target.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private Button FindButton(Transform root, params string[] names)
    {
        Transform target = FindChildByName(root, names);
        if (target == null)
        {
            return null;
        }

        return target.GetComponent<Button>() ??
               target.GetComponentInChildren<Button>(true);
    }

    private Transform FindChildByName(Transform root, params string[] names)
    {
        if (root == null || names == null)
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < names.Length; i++)
        {
            string name = names[i];
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            for (int j = 0; j < children.Length; j++)
            {
                if (children[j].name == name)
                {
                    return children[j];
                }
            }
        }

        for (int i = 0; i < names.Length; i++)
        {
            string name = names[i];
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            for (int j = 0; j < children.Length; j++)
            {
                if (children[j].name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return children[j];
                }
            }
        }

        return null;
    }
}
