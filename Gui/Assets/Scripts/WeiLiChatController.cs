using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class WeiLiChatController : MonoBehaviour
{
    [Header("UI 组件")]
    public TMP_InputField playerInputField;   // 玩家输入框
    public Transform chatContent;              // 气泡生成的父物体
    public GameObject playerBubblePrefab;      // 玩家气泡预制体
    public GameObject weiliBubblePrefab;       // 未离气泡预制体
    public GameObject typingIndicator;         // "未离正在输入..." 提示
    public ScrollRect chatScrollRect;

    [Header("系统引用")]
    public GeminiManager geminiManager;        // 刚才写的那个脚本

    private int currentTurns = 0;              // 当前对话回合
    private int maxTurns = 2;                  // 策划案要求的2回合限制
    private List<string> currentSessionChat = new List<string>(); // 本轮对话记录

    public GameObject dateDividerPrefab;
    public Button sendButton;
    public Color activeColor = Color.green; // 策划案中的绿色
    public Color disabledColor = Color.gray;
    public TaskManager taskManager;
    private GameObject currentEndDivider;

    void Start()
    {
        playerInputField.characterLimit = 100;
        playerInputField.onValueChanged.AddListener(OnInputValueChanged);

        // 初始状态：禁用输入框，提示文本设为默认
        playerInputField.interactable = false;
        playerInputField.placeholder.GetComponent<TMP_Text>().text = "等待未离发起对话...";

        RefreshButtonState("");
    }

    public void TriggerChat(string eventBackground, string openingLine, int turns)
    {
        if (currentEndDivider != null)
        {
            Destroy(currentEndDivider);
        }
        currentTurns = 0;
        maxTurns = turns;
        currentSessionChat.Clear();

        int day = taskManager.currentDay;
        AddDateDivider($"第 {day} 天");

        if (NotificationBadge.Instance != null)
        {
            NotificationBadge.Instance.ShowBadge();
        }

        // 确保开始时是禁用的
        playerInputField.interactable = false;
        playerInputField.placeholder.GetComponent<TMP_Text>().text = "未离正在输入...";

        StartCoroutine(WeiLiOpening(openingLine));
    }

    System.Collections.IEnumerator WeiLiOpening(string line)
    {
        typingIndicator.SetActive(true);
        yield return new UnityEngine.WaitForSeconds(1.5f);
        typingIndicator.SetActive(false);

        CreateBubble(weiliBubblePrefab, line);
        currentSessionChat.Add("未离:\"" + line + "\"");

        // 未离说完开场白，玩家终于可以说话了
        playerInputField.interactable = true;
        playerInputField.placeholder.GetComponent<TMP_Text>().text = "输入你的回复...";
        // 强制聚焦，方便玩家直接打字
        playerInputField.ActivateInputField();
    }

    void OnInputValueChanged(string currentText)
    {
        RefreshButtonState(currentText);
    }

    void RefreshButtonState(string text)
    {
        bool hasText = !string.IsNullOrWhiteSpace(text);
        sendButton.interactable = hasText;

        // 1. 获取背景 Image 组件并修改颜色
        Image btnImage = sendButton.GetComponent<Image>();
        if (btnImage != null)
        {
            btnImage.color = hasText ? activeColor : disabledColor;
        }

        // 2. 获取按钮下的文字组件并修改颜色
        TMP_Text btnText = sendButton.GetComponentInChildren<TMP_Text>();
        if (btnText != null)
        {
            // 当有文字时设为白色 (Color.white)，无文字时设为浅灰色或保持不变
            btnText.color = hasText ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
        }
    }

    void CreateDateDivider(string dateStr)
    {
        GameObject divider = Instantiate(dateDividerPrefab, chatContent);
        divider.GetComponentInChildren<TMP_Text>().text = dateStr;
        divider.transform.SetAsLastSibling(); // 确保在最下面
    }

    public void AddDateDivider(string dayText)
    {
        GameObject divider = Instantiate(dateDividerPrefab, chatContent);
        TMP_Text textComp = divider.GetComponentInChildren<TMP_Text>();

        if (textComp != null)
        {
            // 修正：添加 $ 符号进行字符串插值，并确保引号成对
            textComp.text = $"{dayText}";
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent as RectTransform);
        // 建议：确保新生成的日期也在最下方
        divider.transform.SetAsLastSibling();
    }

    void CreateBubble(GameObject prefab, string content)
    {
        // 1. 生成气泡
        GameObject container = Instantiate(prefab, chatContent);

        // 2. 找到气泡里的文字组件并赋值
        TMP_Text textComp = container.GetComponentInChildren<TMP_Text>();
        if (textComp != null)
        {
            textComp.text = content;

            // --- 核心修复：手动控制收缩与换行逻辑 ---
            // 设置最大宽度限制（例如 400）
            float maxWidth = 400f;

            // 强制更新文字网格以获取真实的 preferredWidth
            textComp.ForceMeshUpdate();
            float preferredWidth = textComp.preferredWidth;

            // 取 preferredWidth 和 maxWidth 之间的最小值
            // 如果文字短，宽度就是 preferredWidth (实现收缩)
            // 如果文字长，宽度被锁定在 maxWidth (实现换行)
            RectTransform textRT = textComp.rectTransform;
            textRT.sizeDelta = new Vector2(Mathf.Min(preferredWidth, maxWidth), textRT.sizeDelta.y);
        }

        // 3. 强制刷新布局
        // 注意：先刷新生成的物体本身，再刷新父容器
        LayoutRebuilder.ForceRebuildLayoutImmediate(container.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent.GetComponent<RectTransform>());

        // 4. 让滚动条自动滚到底部
        StartCoroutine(ForceScrollToBottom());

        // 5. 确保“正在输入”提示永远在最下面
        typingIndicator.transform.SetAsLastSibling();
    }

    System.Collections.IEnumerator ForceScrollToBottom()
    {
        // 强制等待两帧，确保 LayoutGroup 和 ContentSizeFitter 已经捕捉到新气泡/分割线
        yield return new WaitForEndOfFrame();
        yield return new WaitForFixedUpdate();

        if (chatScrollRect != null)
        {
            // 第一次强制计算
            LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent as RectTransform);
            chatScrollRect.verticalNormalizedPosition = 0f;

            // 【保险逻辑】在接下来三帧内持续锁定到底部，防止异步计算导致的“回弹”
            for (int i = 0; i < 3; i++)
            {
                yield return null; // 等待下一帧
                chatScrollRect.verticalNormalizedPosition = 0f;
            }

            Debug.Log("[聊天系统] 滚动条已强制锁定到底部");
        }
    }

    // 当玩家按下回车发送消息
    public void OnPlayerSubmit()
    {
        string text = playerInputField.text;
        if (string.IsNullOrEmpty(text) || currentTurns >= maxTurns) return;

        // 1. 生成玩家气泡
        CreateBubble(playerBubblePrefab, text);
        playerInputField.text = ""; // 清空输入框
        playerInputField.interactable = false; // 发送期间禁用输入，防止连发

        // 2. 记录本轮对话
        currentSessionChat.Add("接线员:\"" + text + "\"");

        // 3. 请求 Gemini
        typingIndicator.SetActive(true); // 显示"正在输入"
        geminiManager.SendToGemini(text, (response) => {
            ReceiveAIResponse(response);
        });
    }

    // 接收到 AI 的毒舌回复
    void ReceiveAIResponse(string aiMessage)
    {
        typingIndicator.SetActive(false);
        playerInputField.interactable = true;

        // 生成未离气泡
        CreateBubble(weiliBubblePrefab, aiMessage);
        currentSessionChat.Add("未离:\"" + aiMessage + "\"");

        currentTurns++;

        // 如果回合到了，结束对话
        if (currentTurns >= maxTurns)
        {
            EndChat();
        }
    }

    void EndChat()
    {
        playerInputField.interactable = false;
        playerInputField.text = "";
        playerInputField.placeholder.GetComponent<TMP_Text>().text = "—— 对话已结束 ——";

        if (taskManager != null)
        {
            taskManager.chatDone = true;
            taskManager.CheckTasks(); 
            Debug.Log("[系统] 检测到对话完成，TaskManager.ChatDone 已设为 True");
        }

        // --- 新增：在对话结束时生成一条特殊的“结束分割线” ---
        currentEndDivider = Instantiate(dateDividerPrefab, chatContent);
        currentEndDivider.GetComponentInChildren<TMP_Text>().text = "———— 对话结束 ————";
        currentEndDivider.transform.SetAsLastSibling();

        // 强制刷新布局并滚动到底部
        StartCoroutine(ForceScrollToBottom());

        Debug.Log("对话结束，已插入结束分割线。");
    }
}