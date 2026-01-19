using UnityEngine;
using TMPro;
using System.Collections;

public class WeiLiReminder : MonoBehaviour
{
    public static WeiLiReminder Instance; // 类型必须和类名一致

    public GameObject tipPanel;
    public CanvasGroup canvasGroup;

    void Awake()
    {
        Instance = this;
        if (tipPanel != null) tipPanel.SetActive(false);
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
    }

    public void ShowClueTip()
    {
        // 停止之前的淡入淡出，防止重叠
        StopAllCoroutines();
        StartCoroutine(FadeRoutine());
    }

    IEnumerator FadeRoutine()
    {
        tipPanel.SetActive(true);

        // 淡入
        float elapsed = 0;
        while (elapsed < 0.2f)
        {
            canvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / 0.2f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(1.0f);

        // 淡出
        elapsed = 0;
        while (elapsed < 0.2f)
        {
            canvasGroup.alpha = Mathf.Lerp(1, 0, elapsed / 0.2f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        tipPanel.SetActive(false);
    }
}