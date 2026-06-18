using CampusNightMarket.Common;
using CampusNightMarket.Tiles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
    [SerializeField] private TextMeshProUGUI purchasePriceText;
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
        TileInteractionInfo activeInfo = tileManager != null ? tileManager.CurrentInteraction : null;
        bool isActive = activeInfo != null && tileManager != null && tileManager.IsInteractionActive;
        TileInteractionInfo info = isActive ? activeInfo : GetCurrentPlayerTileInfo();

        if (panelRoot != null)
        {
            panelRoot.SetActive(!hidePanelWhenInactive || info != null);
        }

        HideRemovedInfoRows();

        if (info == null)
        {
            SetText(titleText, "\u540d\u79f0:--");
            SetText(typeText, "\u7c7b\u578b:--");
            SetText(ownerText, "--");
            SetText(purchasePriceText, "\u8d2d\u4e70\u4ef7\u683c:--");
            SetCustomerRatioTexts(null);
            SetText(messageText, "\u79fb\u52a8\u5230\u5730\u5757\u540e\u663e\u793a\u5730\u5757\u4fe1\u606f\u3002");
            HideActionButtons();
            return;
        }

        SetText(titleText, "\u540d\u79f0:" + (string.IsNullOrEmpty(info.tileName) ? info.tileId : info.tileName));
        SetText(typeText, "\u7c7b\u578b:" + GetTileTypeLabel(info.tileType));
        SetText(ownerText, info.owner.ToString());
        SetText(purchasePriceText, "\u8d2d\u4e70\u4ef7\u683c:" + Mathf.Max(0, info.purchasePrice));
        SetCustomerRatioTexts(info);
        SetText(messageText, isActive ? info.message : "\u5f53\u524d\u6240\u5728\u5730\u5757\u3002");

        if (!isActive)
        {
            HideActionButtons();
            return;
        }

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

    private TileInteractionInfo GetCurrentPlayerTileInfo()
    {
        if (tileManager == null ||
            prototypeBootstrap == null ||
            prototypeBootstrap.PlayerData == null ||
            string.IsNullOrEmpty(prototypeBootstrap.PlayerData.currentTileId))
        {
            return null;
        }

        return tileManager.GetInteractionInfo(prototypeBootstrap.PlayerData.currentTileId);
    }

    private void AutoBindMissingReferences()
    {
        Transform root = FindUiRoot();
        if (panelRoot == null)
        {
            Transform panel = FindChildByExactName(root, "Right_InfoPanel");
            panelRoot = panel != null ? panel.gameObject : gameObject;
        }

        if (tileManager == null) tileManager = FindObjectOfType<TileManager>();
        if (prototypeBootstrap == null) prototypeBootstrap = FindObjectOfType<PrototypeBootstrap>();
        if (marketPanel == null) marketPanel = FindObjectOfType<MarketPanelController>();
        if (messageScrollBar == null) messageScrollBar = FindObjectOfType<MessageScrollBar>();

        Transform panelRootTransform = panelRoot != null ? panelRoot.transform : transform;
        Transform information = FindChildByExactName(panelRootTransform, "information");
        Transform priceRoot = FindChildByExactName(panelRootTransform, "Image4");

        if (titleText == null) titleText = FindText(information, "Tilename");
        if (typeText == null) typeText = FindText(information, "Tiletype");
        if (purchasePriceText == null) purchasePriceText = FindText(priceRoot, "Tileprice");

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

        HideRemovedInfoRows();
        ApplyChineseFontToInfoTexts();
        SharpenInfoPanelTexts();
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
        if (button == null || IsReservedGlobalButton(button))
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
        if (button == null || IsReservedGlobalButton(button))
        {
            return;
        }

        button.gameObject.SetActive(visible);
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
        SetText(studentRatioText, "\u5b66\u751f " + FormatPercent(info == null ? -1f : info.studentRatio));
        SetText(teacherRatioText, "\u6559\u5e08 " + FormatPercent(info == null ? -1f : info.teacherRatio));
        SetText(touristRatioText, "\u6e38\u5ba2 " + FormatPercent(info == null ? -1f : info.touristRatio));
        SetText(residentRatioText, "\u5c45\u6c11 " + FormatPercent(info == null ? -1f : info.residentRatio));
    }

    private void ApplyChineseFontToInfoTexts()
    {
        TMP_FontAsset chineseFont = LoadConfiguredChineseFont();
        if (chineseFont == null)
        {
            chineseFont = FindSceneChineseFont();
        }

        if (chineseFont == null)
        {
            return;
        }

        ApplyFont(titleText, chineseFont);
        ApplyFont(typeText, chineseFont);
        ApplyFont(ownerText, chineseFont);
        ApplyFont(purchasePriceText, chineseFont);
        ApplyFont(studentRatioText, chineseFont);
        ApplyFont(teacherRatioText, chineseFont);
        ApplyFont(touristRatioText, chineseFont);
        ApplyFont(residentRatioText, chineseFont);
        ApplyFont(messageText, chineseFont);
    }

    private void ApplyFont(TextMeshProUGUI target, TMP_FontAsset font)
    {
        if (target != null && font != null)
        {
            target.font = font;
        }
    }

    private void SharpenInfoPanelTexts()
    {
        if (panelRoot == null)
        {
            return;
        }

        TextMeshProUGUI[] texts = panelRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            SharpenText(texts[i]);
        }
    }

    private void SharpenText(TextMeshProUGUI text)
    {
        if (text == null)
        {
            return;
        }

        text.fontSize = Mathf.Max(text.fontSize, 20f);
        text.fontWeight = FontWeight.Bold;
        text.fontStyle |= FontStyles.Bold;
        text.extraPadding = true;
        text.isTextObjectScaleStatic = true;
        text.UpdateMeshPadding();
        text.SetAllDirty();
    }

    private TMP_FontAsset LoadConfiguredChineseFont()
    {
#if UNITY_EDITOR
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/_Project/UI/Fonts/SC.asset");
        if (font == null)
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/_Project/UI/Fonts/SourceHanSansSC-VF SDF 1.asset");
        }

        if (font == null)
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/_Project/UI/Fonts/SourceHanSansSC-VF SDF.asset");
        }

        if (font == null)
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/_Project/UI/Fonts/Font_SourceHanSans.asset");
        }

        return font;
#else
        return null;
#endif
    }

    private TMP_FontAsset FindSceneChineseFont()
    {
        TextMeshProUGUI[] texts = FindObjectsOfType<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null || texts[i].font == null || string.IsNullOrEmpty(texts[i].text))
            {
                continue;
            }

            if (ContainsChinese(texts[i].text))
            {
                return texts[i].font;
            }
        }

        return null;
    }

    private bool ContainsChinese(string value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] >= '\u4e00' && value[i] <= '\u9fff')
            {
                return true;
            }
        }

        return false;
    }

    private string GetTileTypeLabel(TileType tileType)
    {
        switch (tileType)
        {
            case TileType.Start:
                return "\u8d77\u70b9";
            case TileType.Buildable:
                return "\u53ef\u8d2d\u4e70\u5730\u5757";
            case TileType.Resource:
                return "\u8d44\u6e90\u5730\u5757";
            case TileType.Shop:
                return "\u5546\u5e97\u5730\u5757";
            case TileType.Event:
                return "\u4e8b\u4ef6\u5730\u5757";
            case TileType.Special:
                return "\u7279\u6b8a\u5730\u5757";
            default:
                return tileType.ToString();
        }
    }

    private void HideRemovedInfoRows()
    {
        HideTextsContaining(
            "\u533a\u57df",
            "\u6708\u7ef4\u62a4\u4f63\u91d1",
            "\u7ade\u4e89\u5f3a\u5ea6");
    }

    private void HideTextsContaining(params string[] labels)
    {
        if (panelRoot == null || labels == null)
        {
            return;
        }

        TextMeshProUGUI[] texts = panelRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null || string.IsNullOrEmpty(texts[i].text))
            {
                continue;
            }

            for (int j = 0; j < labels.Length; j++)
            {
                if (string.IsNullOrEmpty(labels[j]) || !texts[i].text.Contains(labels[j]))
                {
                    continue;
                }

                Transform row = texts[i].transform.parent;
                if (row != null && row != panelRoot.transform)
                {
                    row.gameObject.SetActive(false);
                }
                else
                {
                    texts[i].gameObject.SetActive(false);
                }
                break;
            }
        }
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

            if (studentRatioText == null && ContainsAny(key, "student", "\u5b66\u751f"))
            {
                studentRatioText = text;
            }
            else if (teacherRatioText == null && ContainsAny(key, "teacher", "\u6559\u5e08"))
            {
                teacherRatioText = text;
            }
            else if (touristRatioText == null && ContainsAny(key, "tourist", "\u6e38\u5ba2"))
            {
                touristRatioText = text;
            }
            else if (residentRatioText == null && ContainsAny(key, "resident", "\u5c45\u6c11"))
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

    private Button FindButton(Transform root, params string[] names)
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

                Button button = children[j].GetComponent<Button>();
                if (button != null && children[j].name == name)
                {
                    return button;
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
                if (IsReservedGlobalButtonName(children[j].name))
                {
                    continue;
                }

                Button button = children[j].GetComponent<Button>();
                if (button != null &&
                    children[j].name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return button;
                }
            }
        }

        return null;
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
