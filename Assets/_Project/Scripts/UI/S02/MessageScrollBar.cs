using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class MessageScrollBar : MonoBehaviour
{
    [Header("滚动框Content")]
    public Transform content;
    [Header("单条消息预制体")]
    public TextMeshProUGUI msgItemPrefab;

    /// <summary>
    /// 新增一条消息
    /// msg：消息文本
    /// isWarning：true=红色警告，false=白色普通文字
    /// </summary>
    public void AddNewMsg(string msg, bool isWarning = false)
    {
        // 实例化消息到Content下
        TextMeshProUGUI newMsg = Instantiate(msgItemPrefab, content);
        newMsg.text = msg;

        // 区分警告颜色
        newMsg.color = isWarning ? Color.red : Color.white;

        // 自动滚动到最新消息底部
        Invoke(nameof(ScrollToBottom), 0.01f);
    }

    // 滚动视图拉到底，显示最新消息
    void ScrollToBottom()
    {
        GetComponent<ScrollRect>().verticalNormalizedPosition = 0;
    }

    // 清空所有历史消息
    public void ClearAllMsg()
    {
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }
    }
}