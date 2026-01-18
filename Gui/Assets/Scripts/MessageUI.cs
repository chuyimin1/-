using UnityEngine;
using TMPro;
using System.Collections;
using System;
using UnityEngine.EventSystems; // 必须有这个，负责检测UI点击

// 核心修复：必须加上 IPointerClickHandler 接口，否则 OnPointerClick 函数永远不会触发
public class MessageUI : MonoBehaviour, IPointerClickHandler
{
    public TextMeshProUGUI contentText;
    public float typingSpeed = 0.05f;
    private string currentKeyword;

    public void SetText(string fullText, Color textColor, string keyword, Action onComplete = null)
    {
        contentText.color = textColor;
        currentKeyword = keyword;

        string processedText = fullText;
        if (!string.IsNullOrEmpty(keyword))
        {
            // 给红字加上 link 标签，这样它才能被检测到点击
            processedText = fullText.Replace(keyword, $"<link=\"clue\"><color=red>{keyword}</color></link>");
        }

        StartCoroutine(TypeText(processedText, onComplete));
    }

    // 当玩家点击气泡时，Unity 会自动调用这个函数
    public void OnPointerClick(PointerEventData eventData)
    {
        // 检查点击位置是否在包含 <link> 标签的文字上
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(contentText, eventData.position, eventData.pressEventCamera);

        if (linkIndex != -1)
        {
            TMP_LinkInfo linkInfo = contentText.textInfo.linkInfo[linkIndex];
            if (linkInfo.GetLinkID() == "clue")
            {
                CollectClue();
            }
        }
    }

    void CollectClue()
    {
        if (!string.IsNullOrEmpty(currentKeyword))
        {
            Debug.Log($"收集到线索：{currentKeyword}");

            // 1. 调用线索管理器记录线索
            if (ClueManager.Instance != null)
            {
                ClueManager.Instance.AddClue(currentKeyword);
            }

            // 2. 核心反馈：把红色替换成灰色 (Hex代码: #808080)
            // 这里我们直接修改文字内容，实现变灰效果
            contentText.text = contentText.text.Replace("red", "#808080");
        }
    }

    IEnumerator TypeText(string text, Action onComplete)
    {
        contentText.text = text;
        contentText.maxVisibleCharacters = 0;
        int totalCharacters = text.Length;
        int visibleCount = 0;

        while (visibleCount <= totalCharacters)
        {
            contentText.maxVisibleCharacters = visibleCount;
            visibleCount++;
            yield return new WaitForSeconds(typingSpeed);
        }
        onComplete?.Invoke();
    }
}