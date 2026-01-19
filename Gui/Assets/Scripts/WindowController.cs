using UnityEngine;
using TMPro;

public class WindowController : MonoBehaviour
{
    public GameObject phoneWindow;
    public GameObject clueBoard;
    public GameObject socialMedia;
    public GameObject weiLiChat;

    public TMP_Text dateText;
    public TMP_Text taskText;

    private GameObject currentActiveWindow;

    void Start()
    {
        SetActiveWindow(phoneWindow);
        UpdateDateAndTask();
    }

    public void SetActiveWindow(GameObject newWindow)
    {
        if (currentActiveWindow != null)
            currentActiveWindow.SetActive(false);

        newWindow.SetActive(true);
        currentActiveWindow = newWindow;

        // 把窗口放到最前层（解决你设计图中的需求）
        currentActiveWindow.transform.SetAsLastSibling();
    }

    void UpdateDateAndTask()
    {
        dateText.text = System.DateTime.Now.ToString("yyyy-MM-dd");
        taskText.text = "完成今日的电台工作";
    }
}

