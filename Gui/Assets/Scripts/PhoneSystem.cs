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

    void Start()
    {
        // 1. 初始化线索列表
        allKeywordsInDialogue = new List<string>();
        if (currentDialogue != null)
        {
            foreach (var line in currentDialogue.lines)
            {
                if (!string.IsNullOrEmpty(line.keyword))
                {
                    string k = line.keyword.Trim();
                    if (!allKeywordsInDialogue.Contains(k)) allKeywordsInDialogue.Add(k);
                }
            }
        }

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
        // 如果正在打字、有选项、或者已经结束了，不响应点击
        if (isTyping || isLockedForOptions || hangUpButton.gameObject.activeSelf) return;

        // 获取当前行和下一行信息
        if (currentLineIndex < 0 || currentLineIndex >= currentDialogue.lines.Count) return;

        int nextIndex = currentLineIndex + 1;
        if (nextIndex >= currentDialogue.lines.Count) return;

        DialogueLine nextLine = currentDialogue.lines[nextIndex];

        // 【核心修复】：只有当下一行是“来电人(1)”或“未离(2)”时，才允许通过左键/空格推进
        if (nextLine.characterType != 0)
        {
            if (Input.GetKeyDown(KeyCode.Space) || (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject()))
            {
                HandleManualNext();
            }
        }
    }

    public void NextLine()
    {
        if (isTyping) return;

        currentLineIndex++;
        if (currentLineIndex < currentDialogue.lines.Count)
        {
            DialogueLine line = currentDialogue.lines[currentLineIndex];

            // 开始新行：按钮立刻变灰且不可阻挡射线
            isTyping = true;
            nextButton.interactable = false;
            nextButtonImage.sprite = darkSprite;
            nextButtonImage.raycastTarget = false;

            ShowLine(line);
        }
        else
        {
            nextButton.gameObject.SetActive(false);
            hangUpButton.gameObject.SetActive(true);
        }
    }

    void ShowLine(DialogueLine line)
    {
        if (autoNextCoroutine != null) StopCoroutine(autoNextCoroutine);

        GameObject newMsg = Instantiate(messagePrefab, messageContainer);
        MessageUI msgUI = newMsg.GetComponent<MessageUI>();

        // 设置位置和颜色
        RectTransform rt = newMsg.GetComponent<RectTransform>();
        bool isLeft = (line.characterType == 1);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(isLeft ? 0 : 1, 0.5f);
        rt.anchoredPosition = new Vector2(isLeft ? 20 : -20, rt.anchoredPosition.y);

        msgUI.SetText(line.content, isLeft ? Color.black : new Color(0.5f, 0.5f, 0.5f, 1f), line.keyword, () => {
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
        if (nextIndex >= currentDialogue.lines.Count)
        {
            nextButton.gameObject.SetActive(false);
            hangUpButton.gameObject.SetActive(true);
            return;
        }

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
            // 【重要】：只有按钮亮起时才拦截点击，灰色时点击穿透到背景
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
        currentLineIndex = opt.nextLineIndex - 1;
        optionGroup.SetActive(false);
        isLockedForOptions = false;
        NextLine();
    }

    public void HangUp()
    {
        if (ClueManager.Instance == null) return;

        if (ClueManager.Instance.AreAllCluesCollected(allKeywordsInDialogue))
        {
            gameObject.SetActive(false);
            if (TaskManager.Instance != null)
            {
                TaskManager.Instance.phoneCallDone = true;
                TaskManager.Instance.CheckTasks();
            }
        }
        else
        {
            if (WeiLiReminder.Instance != null) WeiLiReminder.Instance.ShowClueTip();
        }
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