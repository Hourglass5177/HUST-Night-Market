using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class MessageScrollBar : MonoBehaviour
{
    [Header("滚动框Content")]
    public Transform content;
    [Header("单条消息预制体")]
    public TextMeshProUGUI msgItemPrefab;
    [Header("消息显示")]
    [SerializeField] private float minItemHeight = 28f;
    [SerializeField] private float itemSpacing = 4f;

    /// <summary>
    /// 新增一条消息
    /// msg：消息文本
    /// isWarning：true=红色警告，false=白色普通文字
    /// </summary>
    public void AddNewMsg(string msg, bool isWarning = false)
    {
        EnsureLayout();

        // 实例化消息到Content下
        TextMeshProUGUI newMsg = Instantiate(msgItemPrefab, content);
        newMsg.text = msg;
        newMsg.enableWordWrapping = true;
        newMsg.overflowMode = TextOverflowModes.Overflow;

        // 区分警告颜色
        newMsg.color = isWarning ? Color.red : Color.white;

        FitMessageHeight(newMsg);
        RebuildContentLayout();

        // 自动滚动到最新消息底部
        Invoke(nameof(ScrollToBottom), 0.01f);
    }

    // 滚动视图拉到底，显示最新消息
    void ScrollToBottom()
    {
        RebuildContentLayout();
        ScrollRect scrollRect = GetComponent<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0;
        }
    }

    // 清空所有历史消息
    public void ClearAllMsg()
    {
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }
    }

    private void EnsureLayout()
    {
        if (content == null)
        {
            return;
        }

        VerticalLayoutGroup layoutGroup = content.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = content.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = itemSpacing;
        layoutGroup.padding.left = 8;
        layoutGroup.padding.right = 8;
        layoutGroup.padding.top = 4;
        layoutGroup.padding.bottom = 4;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void FitMessageHeight(TextMeshProUGUI message)
    {
        if (message == null)
        {
            return;
        }

        RectTransform rectTransform = message.rectTransform;
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.sizeDelta = new Vector2(0f, rectTransform.sizeDelta.y);

        message.ForceMeshUpdate();
        float preferredHeight = Mathf.Max(minItemHeight, message.preferredHeight + 6f);

        LayoutElement layoutElement = message.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = message.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.minHeight = preferredHeight;
        layoutElement.preferredHeight = preferredHeight;
        layoutElement.flexibleHeight = 0f;
    }

    private void RebuildContentLayout()
    {
        if (content == null)
        {
            return;
        }

        RectTransform contentRect = content as RectTransform;
        if (contentRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }
    }
}
