using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class KeywordCollector : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        var text = GetComponent<TextMeshProUGUI>();
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(text, eventData.position, null);

        if (linkIndex != -1)
        {
            TMP_LinkInfo linkInfo = text.textInfo.linkInfo[linkIndex];
            // 将点击的关键词从红色替换为灰色
            text.text = text.text.Replace("red", "#808080");
            Debug.Log("已收集线索: " + linkInfo.GetLinkID());
        }
    }
}