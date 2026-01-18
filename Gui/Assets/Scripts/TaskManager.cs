using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance;

    [Header("任务状态")]
    public int currentDay = 1;
    public bool phoneCallDone = false;
    public bool clueRecorded = false;
    public bool chatDone = false;

    [Header("UI 引用")]
    public TMP_Text dateText;
    public TMP_Text taskText;
    public Button endDayButton;
    public Button startWorkButton;
    public GameObject ringPanel;
    public GameObject phoneWindow;
    // 将所有需要初始化的窗口（PhoneWindow, RingPanel, ClueLog等）都拖进这个列表
    public List<GameObject> allWindows;
    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 初始状态设置
        dateText.text = "第 " + currentDay + " 天";
        taskText.text = "完成今日的电台工作";
        UpdateDateUI();
        startWorkButton.gameObject.SetActive(true);
        endDayButton.gameObject.SetActive(false);

        // 核心修复：使用协程强制在第一帧关闭，防止被其他脚本意外开启
        StartCoroutine(InitialSetupRoutine());
    }

    IEnumerator InitialSetupRoutine()
    {
        // 等待一帧，确保所有物体的 Awake 和 Start 逻辑都跑完了
        yield return new WaitForEndOfFrame();

        if (ringPanel != null) ringPanel.SetActive(false);
        if (phoneWindow != null) phoneWindow.SetActive(false);

        Debug.Log("TaskManager: 已执行强制初始隐藏。");
    }

    void UpdateDateUI()
    {
        dateText.text = "第 " + currentDay + " 天";
        taskText.text = "完成今日的电台工作";
    }

    public void EndDay()
    {
        currentDay++; // 日期加一
        ResetDailyState(); // 重置状态
        UpdateDateUI();    // 更新UI

        Debug.Log("新的一天开始了：第 " + currentDay + " 天");
    }

    public void ResetDailyState()
    {
        // 1. 重置任务布尔值
        phoneCallDone = false;
        clueRecorded = false;
        chatDone = false;

        // 2. 隐藏所有窗口
        foreach (GameObject window in allWindows)
        {
            if (window != null) window.SetActive(false);
        }

        // 3. 恢复按钮初始状态
        startWorkButton.gameObject.SetActive(true);
        endDayButton.gameObject.SetActive(false);

        // 4. (可选) 如果你的 PhoneSystem 挂在某个窗口上，记得清理掉之前的对话历史
        // 这里建议在 PhoneSystem 开启时再 Instantiate 消息，关闭时 Destroy 所有子物体
    }

    // --- 在 TaskManager.cs 中修改 StartWork ---
    public void StartWork()
    {
        Debug.Log("TaskManager: 尝试开启工作...");
        if (!phoneCallDone)
        {
            // 确保这两个引用在 Inspector 里都拖对了
            if (ringPanel != null)
            {
                ringPanel.SetActive(true); // 激活来电界面
                Debug.Log("TaskManager: Ring 界面已激活");
            }

            startWorkButton.gameObject.SetActive(false); // 隐藏开始按钮
        }
    }

    public void CompletePhoneTask()
    {
        phoneCallDone = true;
        if (ringPanel != null) ringPanel.SetActive(false);
        CheckTasks();
    }

    public void CheckTasks()
    {
        if (!phoneCallDone)
        {
            startWorkButton.gameObject.SetActive(true);
            taskText.text = "完成今日的电台工作";
        }
        else if (phoneCallDone && !chatDone)
        {
            startWorkButton.gameObject.SetActive(false);
            taskText.text = "和未离聊聊";
        }
        else if (phoneCallDone && clueRecorded && chatDone)
        {
            endDayButton.gameObject.SetActive(true);
            taskText.text = "整理线索，或结束今天";
        }
    }
}