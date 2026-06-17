using CampusNightMarket.Tiles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TileInteractionPanelController : MonoBehaviour
{
    [Header("System References")]
    [SerializeField] private TileManager tileManager;
    [SerializeField] private PrototypeBootstrap prototypeBootstrap;
    [SerializeField] private MarketPanelController marketPanel;
    [SerializeField] private MessageScrollBar messageScrollBar;

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private bool hidePanelWhenInactive;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private TextMeshProUGUI ownerText;
    [SerializeField] private TextMeshProUGUI studentRatioText;
    [SerializeField] private TextMeshProUGUI teacherRatioText;
    [SerializeField] private TextMeshProUGUI touristRatioText;
    [SerializeField] private TextMeshProUGUI residentRatioText;
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("Action Buttons")]
    [SerializeField] private Button viewInfoButton;
    [SerializeField] private Button purchaseButton;
    [SerializeField] private Button openMarketButton;
    [SerializeField] private Button collectResourceButton;
    [SerializeField] private Button openShopButton;
    [SerializeField] private Button triggerEventButton;
    [SerializeField] private Button triggerSpecialButton;
    [SerializeField] private Button completeButton;

    private void Awake()
    {
        AutoBindMissingReferences();
        BindButtons();
    }

    private void Update()
    {
        Refresh();
    }

    public void Refresh()
    {
        TileInteractionInfo info = tileManager != null ? tileManager.CurrentInteraction : null;
        bool isActive = info != null && tileManager != null && tileManager.IsInteractionActive;
        if (panelRoot != null)
        {
            panelRoot.SetActive(!hidePanelWhenInactive || isActive);
        }

        if (!isActive)
        {
            SetText(titleText, "未选择地块");
            SetText(typeText, "--");
            SetText(ownerText, "--");
            SetCustomerRatioTexts(null);
            SetText(messageText, "移动到地块后显示地块信息。");
            HideActionButtons();
            return;
        }

        SetText(titleText, string.IsNullOrEmpty(info.tileName) ? info.tileId : info.tileName);
        SetText(typeText, info.tileType.ToString());
        SetText(ownerText, info.owner.ToString());
        SetCustomerRatioTexts(info);
        SetText(messageText, info.message);

        SetButtonVisible(viewInfoButton, info.HasAction(TileActionType.ViewInfo));
        SetButtonVisible(purchaseButton, info.HasAction(TileActionType.PurchaseAndCreateMarket));
        SetButtonVisible(openMarketButton, info.HasAction(TileActionType.OpenMarketPanel));
        SetButtonVisible(collectResourceButton, info.HasAction(TileActionType.CollectResource));
        SetButtonVisible(openShopButton, info.HasAction(TileActionType.OpenShop));
        SetButtonVisible(triggerEventButton, info.HasAction(TileActionType.TriggerEvent));
        SetButtonVisible(triggerSpecialButton, info.HasAction(TileActionType.TriggerSpecialRule));
        SetButtonVisible(completeButton, info.HasAction(TileActionType.CompleteInteraction));
    }

    public void ExecuteAction(TileActionType action)
    {
        if (action == TileActionType.OpenMarketPanel && marketPanel != null)
        {
            marketPanel.Open();
        }

        if (prototypeBootstrap != null)
        {
            prototypeBootstrap.ExecuteTileActionForUI(action);
            AddMessage("Tile action: " + action);
            Refresh();
            return;
        }

        if (tileManager == null)
        {
            AddMessage("TileManager is missing.", true);
            return;
        }

        if (!tileManager.ExecuteAction(action, out string reason))
        {
            AddMessage(reason, true);
        }
        else
        {
            AddMessage(string.IsNullOrEmpty(reason) ? "Tile action: " + action : reason);
        }

        Refresh();
    }

    private void AutoBindMissingReferences()
    {
        Transform root = FindUiRoot();
        if (panelRoot == null)
        {
            Transform panel = FindChildByName(root, "Right_InfoPanel", "TileInteraction", "information");
            panelRoot = panel != null ? panel.gameObject : gameObject;
        }

        if (tileManager == null) tileManager = FindObjectOfType<TileManager>();
        if (prototypeBootstrap == null) prototypeBootstrap = FindObjectOfType<PrototypeBootstrap>();
        if (marketPanel == null) marketPanel = FindObjectOfType<MarketPanelController>();
        if (messageScrollBar == null) messageScrollBar = FindObjectOfType<MessageScrollBar>();

        Transform panelRootTransform = panelRoot != null ? panelRoot.transform : transform;
        if (titleText == null) titleText = FindText(panelRootTransform, "TileTitle", "TileName");
        if (typeText == null) typeText = FindText(panelRootTransform, "TileType");
        if (ownerText == null) ownerText = FindText(panelRootTransform, "TileOwner");
        AutoBindCustomerRatioTexts(panelRootTransform);
        if (messageText == null) messageText = FindText(panelRootTransform, "TileMessage");

        if (viewInfoButton == null) viewInfoButton = FindButton(panelRootTransform, "View", "Info");
        if (purchaseButton == null) purchaseButton = FindButton(panelRootTransform, "Purchase", "Buy", "Create");
        if (openMarketButton == null) openMarketButton = FindButton(panelRootTransform, "OpenMarket", "MarketPanel");
        if (collectResourceButton == null) collectResourceButton = FindButton(panelRootTransform, "Collect", "Resource");
        if (openShopButton == null) openShopButton = FindButton(panelRootTransform, "Shop");
        if (triggerEventButton == null) triggerEventButton = FindButton(panelRootTransform, "Event");
        if (triggerSpecialButton == null) triggerSpecialButton = FindButton(panelRootTransform, "Special");
        if (completeButton == null) completeButton = FindButton(panelRootTransform, "Complete", "End", "Finish");
    }

    private void BindButtons()
    {
        BindButton(viewInfoButton, TileActionType.ViewInfo);
        BindButton(purchaseButton, TileActionType.PurchaseAndCreateMarket);
        BindButton(openMarketButton, TileActionType.OpenMarketPanel);
        BindButton(collectResourceButton, TileActionType.CollectResource);
        BindButton(openShopButton, TileActionType.OpenShop);
        BindButton(triggerEventButton, TileActionType.TriggerEvent);
        BindButton(triggerSpecialButton, TileActionType.TriggerSpecialRule);
        BindButton(completeButton, TileActionType.CompleteInteraction);
    }

    private void BindButton(Button button, TileActionType action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => ExecuteAction(action));
    }

    private void AddMessage(string message, bool isWarning = false)
    {
        if (messageScrollBar != null)
        {
            messageScrollBar.AddNewMsg(message, isWarning);
        }
    }

    private void SetButtonVisible(Button button, bool visible)
    {
        if (button != null)
        {
            if (IsReservedGlobalButton(button))
            {
                return;
            }

            button.gameObject.SetActive(visible);
        }
    }

    private bool IsReservedGlobalButton(Button button)
    {
        if (button == null)
        {
            return false;
        }

        string buttonName = button.name;
        return buttonName == "Btn_ToNight" ||
               buttonName == "Menu" ||
               buttonName == "Night_Market";
    }

    private void HideActionButtons()
    {
        SetButtonVisible(viewInfoButton, false);
        SetButtonVisible(purchaseButton, false);
        SetButtonVisible(openMarketButton, false);
        SetButtonVisible(collectResourceButton, false);
        SetButtonVisible(openShopButton, false);
        SetButtonVisible(triggerEventButton, false);
        SetButtonVisible(triggerSpecialButton, false);
        SetButtonVisible(completeButton, false);
    }

    private void SetText(TextMeshProUGUI target, string value)
    {
        if (target != null)
        {
            target.text = string.IsNullOrEmpty(value) ? "--" : value;
        }
    }

    private void SetCustomerRatioTexts(TileInteractionInfo info)
    {
        SetText(studentRatioText, "学生 " + FormatPercent(info == null ? -1f : info.studentRatio));
        SetText(teacherRatioText, "教师 " + FormatPercent(info == null ? -1f : info.teacherRatio));
        SetText(touristRatioText, "游客 " + FormatPercent(info == null ? -1f : info.touristRatio));
        SetText(residentRatioText, "居民 " + FormatPercent(info == null ? -1f : info.residentRatio));
    }

    private void AutoBindCustomerRatioTexts(Transform panelRootTransform)
    {
        if (studentRatioText != null &&
            teacherRatioText != null &&
            touristRatioText != null &&
            residentRatioText != null)
        {
            return;
        }

        Transform customerRoot = FindChildByName(panelRootTransform, "customer", "Customer");
        if (customerRoot == null)
        {
            customerRoot = panelRootTransform;
        }

        TextMeshProUGUI[] texts = customerRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TextMeshProUGUI text = texts[i];
            string key = (text.name + " " + text.text).ToLowerInvariant();

            if (studentRatioText == null && ContainsAny(key, "student", "学生"))
            {
                studentRatioText = text;
            }
            else if (teacherRatioText == null && ContainsAny(key, "teacher", "教师"))
            {
                teacherRatioText = text;
            }
            else if (touristRatioText == null && ContainsAny(key, "tourist", "游客"))
            {
                touristRatioText = text;
            }
            else if (residentRatioText == null && ContainsAny(key, "resident", "居民"))
            {
                residentRatioText = text;
            }
        }
    }

    private string FormatPercent(float ratio)
    {
        if (ratio < 0f)
        {
            return "--%";
        }

        return Mathf.RoundToInt(Mathf.Max(0f, ratio) * 100f) + "%";
    }

    private bool ContainsAny(string source, params string[] values)
    {
        if (string.IsNullOrEmpty(source) || values == null)
        {
            return false;
        }

        for (int i = 0; i < values.Length; i++)
        {
            if (!string.IsNullOrEmpty(values[i]) &&
                source.IndexOf(values[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
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
                if (IsReservedGlobalButtonName(children[j].name))
                {
                    continue;
                }

                if (children[j].name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return children[j];
                }
            }
        }

        return null;
    }

    private bool IsReservedGlobalButtonName(string buttonName)
    {
        return buttonName == "Btn_ToNight" ||
               buttonName == "Menu" ||
               buttonName == "Night_Market";
    }
}
