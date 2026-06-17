using CampusNightMarket.Common;
using CampusNightMarket.Core;
using CampusNightMarket.Economy;
using CampusNightMarket.Player;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class S02PopupUIController : MonoBehaviour
{
    private const string MainMenuSceneName = "S01_MainMenu_test1";

    [Header("System References")]
    [SerializeField] private PrototypeBootstrap prototypeBootstrap;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private ResourceManager resourceManager;
    [SerializeField] private MessageScrollBar messageScrollBar;

    [Header("Popups")]
    [SerializeField] private GameObject loanPopup;
    [SerializeField] private GameObject settlementPopup;
    [SerializeField] private GameObject finalizePopup;
    [SerializeField] private GameObject menuPopup;

    [Header("Loan")]
    [SerializeField] private TextMeshProUGUI loanAmountText;
    [SerializeField] private TextMeshProUGUI loanDaysText;
    [SerializeField] private TMP_InputField loanInput;

    [Header("Settlement")]
    [SerializeField] private TextMeshProUGUI settlementIncomeText;
    [SerializeField] private TextMeshProUGUI settlementFoodText;
    [SerializeField] private TextMeshProUGUI settlementCostText;
    [SerializeField] private TextMeshProUGUI settlementPenaltyText;
    [SerializeField] private TextMeshProUGUI settlementReputationText;
    [SerializeField] private TextMeshProUGUI settlementNetText;

    [Header("Finalize")]
    [SerializeField] private TextMeshProUGUI finalizeTitleText;
    [SerializeField] private TextMeshProUGUI finalizeDaysText;
    [SerializeField] private TextMeshProUGUI finalizeIncomeText;
    [SerializeField] private TextMeshProUGUI finalizeReputationText;

    private int settlementMoneyBefore;
    private int settlementLowFoodBefore;
    private int settlementHighFoodBefore;
    private int settlementReputationBefore;
    private bool finalizeWasShown;

    private void Awake()
    {
        AutoBindMissingReferences();
        BindButtons();
        CloseStartupPopups();
    }

    private void Update()
    {
        RefreshLoanTexts();
        RefreshFinalizePopup();
    }

    public void OpenLoan()
    {
        if (loanPopup != null)
        {
            loanPopup.SetActive(true);
        }

        RefreshLoanTexts();
    }

    public void CloseLoan()
    {
        SetActive(loanPopup, false);
    }

    public void ConfirmLoanRepayment()
    {
        PlayerRuntimeData playerData = GetPlayerData();
        if (playerData == null)
        {
            AddMessage("Loan failed: player data missing.", true);
            return;
        }

        if (loanInput == null || !int.TryParse(loanInput.text, out int amount) || amount <= 0)
        {
            AddMessage("请输入有效还款金额。", true);
            return;
        }

        amount = Mathf.Min(amount, playerData.loan);
        amount = Mathf.Min(amount, playerData.money);
        if (amount <= 0)
        {
            AddMessage("资金不足或没有贷款需要偿还。", true);
            return;
        }

        playerData.money -= amount;
        playerData.loan -= amount;
        loanInput.text = string.Empty;
        AddMessage("已还款：" + amount);
        RefreshLoanTexts();
    }

    public void OpenMenu()
    {
        SetActive(menuPopup, true);
    }

    public void CloseMenu()
    {
        SetActive(menuPopup, false);
    }

    public void RunNightSettlementFromUI()
    {
        PlayerRuntimeData before = GetPlayerData();
        if (prototypeBootstrap == null || before == null)
        {
            AddMessage("无法结算：系统未绑定。", true);
            return;
        }

        settlementMoneyBefore = before.money;
        settlementLowFoodBefore = before.lowFood;
        settlementHighFoodBefore = before.highFood;
        settlementReputationBefore = before.reputation;

        prototypeBootstrap.AdvancePrototypeTurn();
        RefreshSettlementPopup();
        SetActive(menuPopup, false);
        SetActive(settlementPopup, true);
    }

    public void CloseSettlement()
    {
        SetActive(settlementPopup, false);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(MainMenuSceneName);
    }

    public void QuitGame()
    {
        AddMessage("退出游戏。");
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ContinueEndlessMode()
    {
        if (prototypeBootstrap != null)
        {
            prototypeBootstrap.ContinueAfterWin();
        }

        finalizeWasShown = false;
        SetActive(finalizePopup, false);
    }

    public void CloseFinalize()
    {
        finalizeWasShown = false;
        SetActive(finalizePopup, false);
    }

    private void AutoBindMissingReferences()
    {
        if (prototypeBootstrap == null) prototypeBootstrap = FindObjectOfType<PrototypeBootstrap>();
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (resourceManager == null) resourceManager = FindObjectOfType<ResourceManager>();
        if (messageScrollBar == null) messageScrollBar = FindObjectOfType<MessageScrollBar>();

        Transform root = FindUiRoot();
        if (root == null)
        {
            return;
        }

        if (loanPopup == null) loanPopup = FindChildByExactName(root, "Pop_Loan")?.gameObject;
        if (settlementPopup == null) settlementPopup = FindChildByExactName(root, "Pop_Settlement")?.gameObject;
        if (finalizePopup == null) finalizePopup = FindChildByExactName(root, "Pop_Finalize")?.gameObject;
        if (menuPopup == null) menuPopup = FindChildByExactName(root, "Pop_Menu")?.gameObject;

        if (loanInput == null && loanPopup != null)
        {
            Transform input = FindChildByExactName(loanPopup.transform, "input_Loan_Value");
            loanInput = input == null ? null : input.GetComponent<TMP_InputField>();
        }

        RepairLoanInputField();

        BindLoanTexts();
        BindSettlementTexts();
        BindFinalizeTexts();
    }

    private void BindButtons()
    {
        Transform root = FindUiRoot();
        if (root == null)
        {
            return;
        }

        BindButton(FindButtonByExactName(root, "Btn_ToNight"), RunNightSettlementFromUI);
        BindButton(FindButtonByExactName(root, "Menu"), OpenMenu);

        Button loanOpenButton = FindOutsidePopupButton(root, "Loan", loanPopup);
        if (loanOpenButton != null)
        {
            BindButton(loanOpenButton, OpenLoan);
        }

        if (loanPopup != null)
        {
            BindButton(FindButtonByExactName(loanPopup.transform, "exitButton"), CloseLoan);
            BindButton(FindLoanConfirmButton(loanPopup.transform), ConfirmLoanRepayment);
        }

        if (settlementPopup != null)
        {
            BindButton(FindButtonByExactName(settlementPopup.transform, "exitButton"), CloseSettlement);
            BindButton(FindFirstButtonByText(settlementPopup.transform, "继续次日经营"), CloseSettlement);
        }

        if (menuPopup != null)
        {
            BindButton(FindButtonByExactName(menuPopup.transform, "exitButton"), CloseMenu);
            BindButton(FindFirstButtonByText(menuPopup.transform, "结算"), RunNightSettlementFromUI);
            BindButton(FindFirstButtonByText(menuPopup.transform, "退出游戏"), QuitGame);
            BindButton(FindFirstButtonByText(menuPopup.transform, "返回主菜单"), ReturnToMainMenu);
        }

        if (finalizePopup != null)
        {
            BindButton(FindButtonByExactName(finalizePopup.transform, "exitButton"), CloseFinalize);
            BindButton(FindFirstButtonByText(finalizePopup.transform, "返回主菜单"), ReturnToMainMenu);
            BindButton(FindFirstButtonByText(finalizePopup.transform, "无尽模式"), ContinueEndlessMode);
        }
    }

    private void CloseStartupPopups()
    {
        SetActive(loanPopup, false);
        SetActive(settlementPopup, false);
        SetActive(finalizePopup, false);
        SetActive(menuPopup, false);
    }

    private void BindLoanTexts()
    {
        if (loanPopup == null)
        {
            return;
        }

        TextMeshProUGUI[] texts = loanPopup.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            string value = texts[i].text;
            if (loanAmountText == null && value.Contains("当前剩余贷款"))
            {
                loanAmountText = texts[i];
            }
            else if (loanDaysText == null && value.Contains("剩余天数"))
            {
                loanDaysText = texts[i];
            }
        }
    }

    private void BindSettlementTexts()
    {
        if (settlementPopup == null)
        {
            return;
        }

        TextMeshProUGUI[] texts = settlementPopup.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            string value = texts[i].text;
            if (settlementIncomeText == null && value.Contains("总收入")) settlementIncomeText = texts[i];
            else if (settlementFoodText == null && value.Contains("食材消耗")) settlementFoodText = texts[i];
            else if (settlementCostText == null && value.Contains("成本")) settlementCostText = texts[i];
            else if (settlementPenaltyText == null && value.Contains("罚款")) settlementPenaltyText = texts[i];
            else if (settlementReputationText == null && value.Contains("口碑变化")) settlementReputationText = texts[i];
            else if (settlementNetText == null && value.Contains("净收益")) settlementNetText = texts[i];
        }
    }

    private void BindFinalizeTexts()
    {
        if (finalizePopup == null)
        {
            return;
        }

        TextMeshProUGUI[] texts = finalizePopup.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            string value = texts[i].text;
            if (finalizeTitleText == null && value.Contains("胜利")) finalizeTitleText = texts[i];
            else if (finalizeDaysText == null && value.Contains("总经营天数")) finalizeDaysText = texts[i];
            else if (finalizeIncomeText == null && value.Contains("累计收入")) finalizeIncomeText = texts[i];
            else if (finalizeReputationText == null && value.Contains("最终口碑")) finalizeReputationText = texts[i];
        }
    }

    private void RefreshLoanTexts()
    {
        PlayerRuntimeData playerData = GetPlayerData();
        if (playerData == null)
        {
            return;
        }

        SetText(loanAmountText, "当前剩余贷款额：" + playerData.loan);
        SetText(loanDaysText, "剩余天数：" + GetDaysUntilNextInterest());
    }

    private void RefreshSettlementPopup()
    {
        PlayerRuntimeData playerData = GetPlayerData();
        if (playerData == null)
        {
            return;
        }

        int moneyDelta = playerData.money - settlementMoneyBefore;
        int lowFoodDelta = playerData.lowFood - settlementLowFoodBefore;
        int highFoodDelta = playerData.highFood - settlementHighFoodBefore;
        int reputationDelta = playerData.reputation - settlementReputationBefore;

        SetText(settlementIncomeText, "总收入：" + Mathf.Max(0, moneyDelta));
        SetText(settlementFoodText, "食材消耗：" + FormatSigned(lowFoodDelta) + " / " + FormatSigned(highFoodDelta));
        SetText(settlementCostText, "成本：已计入净收益");
        SetText(settlementPenaltyText, "罚款：见消息栏");
        SetText(settlementReputationText, "口碑变化：" + FormatSigned(reputationDelta));
        SetText(settlementNetText, "净收益：" + FormatSigned(moneyDelta));
    }

    private void RefreshFinalizePopup()
    {
        GameRuntimeData runtimeData = gameManager == null ? null : gameManager.RuntimeData;
        if (runtimeData == null)
        {
            return;
        }

        bool shouldShow = runtimeData.currentPhase == GamePhase.GameWin ||
                          runtimeData.currentPhase == GamePhase.GameLose ||
                          runtimeData.isGameOver;
        if (!shouldShow)
        {
            return;
        }

        if (finalizeWasShown)
        {
            return;
        }

        PlayerRuntimeData playerData = GetPlayerData();
        SetText(finalizeTitleText, runtimeData.isWin || runtimeData.currentPhase == GamePhase.GameWin ? "胜利！" : "失败！");
        SetText(finalizeDaysText, "总经营天数:" + runtimeData.currentDay);
        SetText(finalizeIncomeText, playerData == null ? "累计收入：--" : "累计收入：" + playerData.money);
        SetText(finalizeReputationText, playerData == null ? "最终口碑：--" : "最终口碑：" + playerData.reputation);
        SetActive(finalizePopup, true);
        finalizeWasShown = true;
    }

    private int GetDaysUntilNextInterest()
    {
        GameRuntimeData runtimeData = gameManager == null ? null : gameManager.RuntimeData;
        if (runtimeData == null || prototypeBootstrap == null || prototypeBootstrap.MapConfigForUI == null)
        {
            return 0;
        }

        int interval = prototypeBootstrap.MapConfigForUI.interestInterval;
        if (interval <= 0)
        {
            return 0;
        }

        int currentDay = Mathf.Max(1, runtimeData.currentDay);
        int remainder = currentDay % interval;
        return remainder == 0 ? interval : interval - remainder;
    }

    private PlayerRuntimeData GetPlayerData()
    {
        if (resourceManager != null && resourceManager.PlayerData != null)
        {
            return resourceManager.PlayerData;
        }

        return prototypeBootstrap == null ? null : prototypeBootstrap.PlayerData;
    }

    private Transform FindUiRoot()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
        }

        return canvas == null ? null : canvas.transform;
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

    private Button FindButtonByExactName(Transform root, string name)
    {
        Transform target = FindChildByExactName(root, name);
        return target == null ? null : target.GetComponent<Button>();
    }

    private void RepairLoanInputField()
    {
        if (loanInput == null)
        {
            return;
        }

        if (loanInput.textComponent == null)
        {
            TextMeshProUGUI text = FindInputTextComponent(loanInput);
            if (text == null)
            {
                text = CreateInputTextComponent(loanInput);
            }

            loanInput.textComponent = text;
        }

        if (!string.IsNullOrEmpty(loanInput.text) && loanInput.text.Contains("\u8bf7\u8f93\u5165"))
        {
            loanInput.SetTextWithoutNotify(string.Empty);
        }
    }

    private TextMeshProUGUI FindInputTextComponent(TMP_InputField input)
    {
        TextMeshProUGUI[] texts = input.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (input.placeholder != null && texts[i].gameObject == input.placeholder.gameObject)
            {
                continue;
            }

            return texts[i];
        }

        return null;
    }

    private TextMeshProUGUI CreateInputTextComponent(TMP_InputField input)
    {
        Transform parent = input.textViewport != null ? input.textViewport.transform : input.transform;
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(5f, 2f);
        rectTransform.offsetMax = new Vector2(-5f, -2f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI placeholderText = input.placeholder == null
            ? null
            : input.placeholder.GetComponent<TextMeshProUGUI>();

        if (placeholderText != null)
        {
            text.font = placeholderText.font;
            text.fontSize = placeholderText.fontSize;
            text.alignment = placeholderText.alignment;
        }
        else
        {
            text.fontSize = 20f;
            text.alignment = TextAlignmentOptions.MidlineLeft;
        }

        text.text = string.Empty;
        text.color = Color.black;
        text.raycastTarget = false;
        return text;
    }

    private Button FindOutsidePopupButton(Transform root, string name, GameObject popupToSkip)
    {
        if (root == null || string.IsNullOrEmpty(name))
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name != name)
            {
                continue;
            }

            if (popupToSkip != null && IsChildOf(children[i], popupToSkip.transform))
            {
                continue;
            }

            Button button = children[i].GetComponent<Button>();
            if (button != null)
            {
                return button;
            }
        }

        return null;
    }

    private Button FindLoanConfirmButton(Transform root)
    {
        Button button = FindButtonByExactName(root, "confirmButton");
        if (button != null) return button;

        button = FindButtonByExactName(root, "Btn_Confirm");
        if (button != null) return button;

        button = FindButtonByExactName(root, "Btn_LoanConfirm");
        if (button != null) return button;

        button = FindButtonByExactName(root, "Btn_RepayLoan");
        if (button != null) return button;

        button = FindFirstButtonByText(root, "\u786e\u8ba4");
        if (button != null) return button;

        button = FindFirstButtonByText(root, "\u8fd8\u6b3e");
        if (button != null) return button;

        return FindFirstButtonByText(root, "\u507f\u8fd8");
    }

    private Button FindFirstButtonByText(Transform root, string text)
    {
        if (root == null || string.IsNullOrEmpty(text))
        {
            return null;
        }

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            TextMeshProUGUI label = buttons[i].GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null && label.text.Contains(text))
            {
                return buttons[i];
            }
        }

        return null;
    }

    private void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private bool IsChildOf(Transform child, Transform parent)
    {
        if (child == null || parent == null)
        {
            return false;
        }

        Transform current = child;
        while (current != null)
        {
            if (current == parent)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private void SetText(TextMeshProUGUI target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }

    private string FormatSigned(int value)
    {
        return value >= 0 ? "+" + value : value.ToString();
    }

    private void AddMessage(string message, bool isWarning = false)
    {
        if (messageScrollBar != null)
        {
            messageScrollBar.AddNewMsg(message, isWarning);
        }
    }
}
