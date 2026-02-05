using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.EventSystems;

public class PhoneSystem : MonoBehaviour
{
    [Header("剧本数据")]
    public DialogueData currentDialogue;
    private int currentLineIndex = -1;

    [Header("UI 容器")]
    public Transform messageContainer;
    public GameObject messagePrefab;
    public ScrollRect scrollRect;

    [Header("控制按钮")]
    public Button nextButton;
    public Image nextButtonImage;
    public Sprite lightSprite;  // 亮色
    public Sprite darkSprite;   // 灰色
    public Button hangUpButton;
    public GameObject optionGroup;
    public GameObject optionButtonPrefab;

    [Header("状态显示")]
    public GameObject incomingCallPanel;
    public GameObject phoneSystemUI;
    public Button answerButton;

    private Coroutine autoNextCoroutine;
    private bool isLockedForOptions = false;
    private bool isTyping = false;
    private List<string> allKeywordsInDialogue = new List<string>();
    public List<DialogueData> dailyDialogues;
    private int capturedOutcomeID = 0;
    public WeiLiChatController weiliChatController;
    public SocialMediaManager socialMediaManager;

    void InitKeywords()
    {
        allKeywordsInDialogue = new List<string>();
        if (currentDialogue != null)
        {
            foreach (var line in currentDialogue.lines)
            {
                if (!string.IsNullOrEmpty(line.keyword))
                {
                    string k = line.keyword.Trim();
                    if (!allKeywordsInDialogue.Contains(k))
                        allKeywordsInDialogue.Add(k);
                }
            }
        }
        Debug.Log($"[系统] 关键字初始化完成，共计: {allKeywordsInDialogue.Count}个");
    }

    public void LoadDialogueForDay(int dayIndex)
    {
        foreach (Transform child in messageContainer) Destroy(child.gameObject);

        int index = dayIndex - 1;
        if (index >= 0 && index < dailyDialogues.Count)
        {
            currentDialogue = dailyDialogues[index];
            currentLineIndex = -1;
            isTyping = false;
            isLockedForOptions = false;

            // 只重置按钮状态，不要在这里 SetActive(true) 面板
            if (nextButton != null) nextButton.gameObject.SetActive(true);
            if (hangUpButton != null) hangUpButton.gameObject.SetActive(false);

            InitKeywords();
            Debug.Log($"[电台] 已预加载第 {dayIndex} 天剧本");
        }
    }

    void Start()
    {
        // 1. 初始化线索列表（改为调用函数）
        InitKeywords();

        // 2. 初始状态设置
        if (optionGroup != null) optionGroup.SetActive(false);
        if (hangUpButton != null) hangUpButton.gameObject.SetActive(false);

        answerButton.onClick.RemoveAllListeners();
        answerButton.onClick.AddListener(OnAnswerButtonClicked);
    }

    void OnAnswerButtonClicked()
    {
        incomingCallPanel.SetActive(false);
        phoneSystemUI.SetActive(true);
        // 接听后置顶
        transform.SetAsLastSibling();
        NextLine();
    }

    void Update()
    {
        // 1. 基础状态检查
        if (isTyping || isLockedForOptions || hangUpButton.gameObject.activeSelf) return;

        // 获取下一行信息
        if (currentLineIndex < 0 || currentLineIndex >= currentDialogue.lines.Count) return;
        int nextIndex = currentLineIndex + 1;
        if (nextIndex >= currentDialogue.lines.Count) return;

        DialogueLine nextLine = currentDialogue.lines[nextIndex];

        // 【核心约束】：只有下一行不是接线员（characterType != 0）时，才允许快捷推进
        if (nextLine.characterType != 0)
        {
            // A. 处理空格键
            if (Input.GetKeyDown(KeyCode.Space))
            {
                HandleManualNext();
                return;
            }

            // B. 处理左键点击背景
            if (Input.GetMouseButtonDown(0))
            {
                // 如果点在 UI 上（因为 PhoneWindow 是 UI，所以这步是肯定的）
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    PointerEventData eventData = new PointerEventData(EventSystem.current);
                    eventData.position = Input.mousePosition;
                    List<RaycastResult> results = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(eventData, results);

                    if (results.Count > 0)
                    {
                        GameObject topObj = results[0].gameObject;

                        // 判断点击的是否为 PhoneWindow 的非功能区域
                        if (IsTargetUI(topObj))
                        {
                            HandleManualNext();
                        }
                    }
                }
            }
        }
    }

    // 判定函数：排除掉按钮、红字和选项，只允许背景类 UI 触发 NextLine
    private bool IsTargetUI(GameObject obj)
    {
        // 如果点中了 按钮、线索文字、或者选项按钮，不执行 NextLine
        if (obj.GetComponent<Button>() != null ||
            obj.GetComponent<ClickableText>() != null ||
            obj.GetComponent<Selectable>() != null)
        {
            return false;
        }

        // 检查这个物体是否属于 PhoneWindow 或其子级
        Transform current = obj.transform;
        while (current != null)
        {
            if (current == this.transform) return true;
            current = current.parent;
        }
        return false;
    }

    public void NextLine()
    {
        if (isTyping) return;

        currentLineIndex++;

        // --- 核心修复：只要还在对话范围内，就确保挂断按钮是隐藏的 ---
        if (currentLineIndex < currentDialogue.lines.Count)
        {
            // 强制隐藏挂断按钮，显示推进按钮
            if (hangUpButton != null) hangUpButton.gameObject.SetActive(false);
            if (nextButton != null) nextButton.gameObject.SetActive(true);

            DialogueLine line = currentDialogue.lines[currentLineIndex];

            isTyping = true;
            nextButton.interactable = false;
            nextButtonImage.sprite = darkSprite;
            nextButtonImage.raycastTarget = false;

            ShowLine(line);
        }
        else
        {
            // 只有真正结束了，才切换状态
            nextButton.gameObject.SetActive(false);
            hangUpButton.gameObject.SetActive(true);
        }
    }

    void ShowLine(DialogueLine line)
    {
        if (autoNextCoroutine != null) StopCoroutine(autoNextCoroutine);

        GameObject newMsg = Instantiate(messagePrefab, messageContainer);
        MessageUI msgUI = newMsg.GetComponent<MessageUI>();

        // 默认样式恢复
        msgUI.contentText.fontSize = 24;
        msgUI.contentText.fontStyle = FontStyles.Normal;

        // 判定身份：1 是来电人（左），0 是接线员（右），2 是未离（右）
        bool isLeft = (line.characterType == 1);

        if (line.characterType == 2)
        {
            msgUI.contentText.fontSize = 20;
            msgUI.contentText.fontStyle = FontStyles.Italic;
        }

        // --- 核心修复：文字对齐 ---
        // 如果在右边，文字设为右对齐；在左边，文字设为左对齐
        msgUI.contentText.alignment = isLeft ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;

        // 设置位置
        RectTransform rt = newMsg.GetComponent<RectTransform>();
        // 强制设置锚点和中心点
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(isLeft ? 0 : 1, 0.5f);

        // 设置偏移量：左边往右偏 20，右边往左偏 20（保证贴边且留一点呼吸感）
        // 如果你觉得右边还是空，可以把 -20 改成 0
        rt.anchoredPosition = new Vector2(isLeft ? 20 : -20, rt.anchoredPosition.y);

        // 设置颜色
        Color textColor = isLeft ? Color.black : new Color(0.5f, 0.5f, 0.5f, 1f);

        msgUI.SetText(line.content, textColor, line.keyword, () => {
            isTyping = false;
            UpdateControlUI(line);

            if (line.hasOptions) ShowOptions(line);
            else if (line.autoJump)
            {
                currentLineIndex = line.autoJumpIndex - 1;
                autoNextCoroutine = StartCoroutine(AutoNextDelay(1.0f));
            }
        });

        StartCoroutine(ScrollToBottom());
    }

    void UpdateControlUI(DialogueLine currentLine)
    {
        int nextIndex = currentLineIndex + 1;

        // 如果已经到剧本末尾了
        if (nextIndex >= currentDialogue.lines.Count)
        {
            nextButton.gameObject.SetActive(false);
            hangUpButton.gameObject.SetActive(true);
            return;
        }

        // --- 核心修复：只要还没到末尾，必须确保挂断按钮是关掉的 ---
        if (hangUpButton != null) hangUpButton.gameObject.SetActive(false);

        DialogueLine nextLine = currentDialogue.lines[nextIndex];
        bool nextIsOperator = (nextLine.characterType == 0);

        if (currentLine.hasOptions)
        {
            nextButton.gameObject.SetActive(false);
        }
        else
        {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = nextIsOperator;
            nextButtonImage.sprite = nextIsOperator ? lightSprite : darkSprite;
            nextButtonImage.raycastTarget = nextIsOperator;
        }
    }

    void HandleManualNext()
    {
        if (autoNextCoroutine != null) StopCoroutine(autoNextCoroutine);
        NextLine();
    }

    void ShowOptions(DialogueLine line)
    {
        isLockedForOptions = true;
        foreach (Transform child in optionGroup.transform) Destroy(child.gameObject);
        optionGroup.SetActive(true);
        nextButton.gameObject.SetActive(false);

        foreach (var opt in line.options)
        {
            GameObject btnObj = Instantiate(optionButtonPrefab, optionGroup.transform);
            btnObj.GetComponentInChildren<TextMeshProUGUI>().text = opt.optionText;
            btnObj.GetComponent<Button>().onClick.AddListener(() => SelectOption(opt));
        }
    }

    void SelectOption(DialogueOption opt)
    {
        // 如果这个选项带有结局编号（不为0），就记录下来
        if (opt.outcomeID != 0)
        {
            capturedOutcomeID = opt.outcomeID;
            Debug.Log($"[系统] 玩家选择记录了结局编号: {capturedOutcomeID}");
        }

        currentLineIndex = opt.nextLineIndex - 1;
        optionGroup.SetActive(false);
        isLockedForOptions = false;
        NextLine();
    }

    public void HangUp()
    {
        Debug.Log("[系统] 尝试挂断电话...");

        if (ClueManager.Instance == null) { Debug.LogError("找不到 ClueManager"); return; }

        if (ClueManager.Instance.AreAllCluesCollected(allKeywordsInDialogue))
        {
            // 1. 触发任务逻辑
            if (TaskManager.Instance != null)
            {
                TaskManager.Instance.phoneCallDone = true;
                TaskManager.Instance.CheckTasks();
            }

            // 2. 触发社媒 (加个保险，如果没拖入也不至于让后面卡死)
            if (socialMediaManager != null)
            {
                TriggerSocialPost(capturedOutcomeID);
            }
            else { Debug.LogWarning("未分配 socialMediaManager！"); }

            // 3. 触发未离 (加个保险)
            if (weiliChatController != null)
            {
                TriggerWeiLiResponse(capturedOutcomeID);
            }
            else { Debug.LogWarning("未分配 weiliChatController！"); }

            // 4. 【关键】最后关闭界面
            // 确保这一行在最后，且不被前面的错误阻断
            phoneSystemUI.SetActive(false);
            incomingCallPanel.SetActive(false);
            this.gameObject.SetActive(false);

            Debug.Log("[系统] 挂断成功，界面已关闭");
        }
        else
        {
            if (WeiLiReminder.Instance != null) WeiLiReminder.Instance.ShowClueTip();
        }
    }

    void TriggerSocialPost(int outcome)
    {
        if (socialMediaManager == null) return;

        string targetPostID = "";
        switch (outcome)
        {
            case 101: targetPostID = "101"; break; // 对应结局 101 的社媒编号
            case 102: targetPostID = "102"; break; // 对应结局 102 的社媒编号
            default: return; // 如果没有对应结局，不发送社媒
        }

        socialMediaManager.TriggerPost(targetPostID);
    }

    void TriggerWeiLiResponse(int outcome)
    {
        if (weiliChatController == null) return;

        string firstLine = "";
        switch (outcome)
        {
            case 101: firstLine = "......给代码上香，简直是赛博迷信......人类为了找个班上已经进化到这种地步了？"; break;
            case 102: firstLine = "......恭喜你，亲手制造了一个在雨夜里对着自动取款机磕头的疯子。"; break;
            default: firstLine = "电话打完了？那就继续盯着你的监视器。"; break;
        }

        weiliChatController.gameObject.SetActive(true);
        // 触发 2 回合的 Gemini 聊天
        weiliChatController.TriggerChat("电话后续", firstLine, 2);
    }

    IEnumerator ScrollToBottom()
    {
        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForEndOfFrame();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    IEnumerator AutoNextDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        NextLine();
    }
}