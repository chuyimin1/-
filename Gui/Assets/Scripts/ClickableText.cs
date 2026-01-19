using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using System.Text;
using System.Text.RegularExpressions;

public class ClickableText : MonoBehaviour, IPointerClickHandler
{
    private TMP_Text m_TextMeshPro;
    private bool hasCollected = false; // 避免单关键词重复收集
    public Color targetKeywordColor = Color.red;
    public float colorTolerance = 0.2f;

    void Awake()
    {
        m_TextMeshPro = GetComponent<TMP_Text>();
        if (m_TextMeshPro == null)
        {
            Debug.LogError("缺少 TMP_Text 组件！");
            enabled = false;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (hasCollected || m_TextMeshPro == null) return;

        // 步骤1：获取点击位置的字符索引（基于可见文字，不含富文本标签）
        int charIndex = TMP_TextUtilities.FindIntersectingCharacter(
            m_TextMeshPro,
            eventData.position,
            null,
            true // 开启字形范围检测，更精准
        );

        if (charIndex == -1 || charIndex >= m_TextMeshPro.textInfo.characterCount)
        {
            Debug.LogWarning("未点击到有效文字！");
            return;
        }

        // 步骤2：提取点击的完整纯文字关键词 + 首尾索引（解决变色遗漏问题）
        int keywordLeftIndex = 0;
        int keywordRightIndex = 0;
        string clickedKeyword = GetContinuousSameColorKeyword(charIndex, out keywordLeftIndex, out keywordRightIndex);

        if (string.IsNullOrEmpty(clickedKeyword))
        {
            Debug.LogWarning("未提取到有效关键词（颜色不匹配或无同色连续文字）！");
            return;
        }

        // 步骤3：清洗+收集线索+精准变色（传递完整首尾索引）
        HandleKeywordClick(clickedKeyword, keywordLeftIndex, keywordRightIndex);
    }

    /// <summary>
    /// 提取点击位置连续的同色纯文字，同时返回关键词的首尾索引（输出参数）
    /// </summary>
    private string GetContinuousSameColorKeyword(int startIndex, out int leftIndex, out int rightIndex)
    {
        // 初始化输出参数
        leftIndex = startIndex;
        rightIndex = startIndex;

        TMP_TextInfo textInfo = m_TextMeshPro.textInfo;
        TMP_CharacterInfo startChar = textInfo.characterInfo[startIndex];

        // 校验1：点击的文字是否可见
        if (!startChar.isVisible) return "";

        // 校验2：点击的文字颜色是否与目标颜色匹配（提高容错率）
        Color32 charColor = textInfo.meshInfo[startChar.materialReferenceIndex].colors32[startChar.vertexIndex];
        if (!IsColorSimilar(charColor, targetKeywordColor, colorTolerance))
        {
            return "";
        }

        // 步骤1：向前找连续同色字符的起始位置（找到“特”的索引）
        leftIndex = startIndex;
        while (leftIndex > 0)
        {
            TMP_CharacterInfo leftChar = textInfo.characterInfo[leftIndex - 1];
            if (!leftChar.isVisible) break;

            Color32 leftColor = textInfo.meshInfo[leftChar.materialReferenceIndex].colors32[leftChar.vertexIndex];
            if (!IsColorSimilar(leftColor, targetKeywordColor, colorTolerance)) break;

            leftIndex--;
        }

        // 步骤2：向后找连续同色字符的结束位置（找到“火”的索引）
        rightIndex = startIndex;
        while (rightIndex < textInfo.characterCount - 1)
        {
            TMP_CharacterInfo rightChar = textInfo.characterInfo[rightIndex + 1];
            if (!rightChar.isVisible) break;

            Color32 rightColor = textInfo.meshInfo[rightChar.materialReferenceIndex].colors32[rightChar.vertexIndex];
            if (!IsColorSimilar(rightColor, targetKeywordColor, colorTolerance)) break;

            rightIndex++;
        }

        // 步骤3：提取可见文字（不含富文本标签，完整提取“特别火”）
        StringBuilder sb = new StringBuilder();
        for (int i = leftIndex; i <= rightIndex; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (charInfo.isVisible && charInfo.character != 0)
            {
                sb.Append(charInfo.character);
            }
        }

        // 步骤4：过滤可能残留的特殊字符，返回纯文字
        string pureKeyword = Regex.Replace(sb.ToString(), @"[^a-zA-Z0-9\u4e00-\u9fa5]", "");
        return pureKeyword.Trim();
    }

    /// <summary>
    /// 优化：提高颜色匹配容错率，解决TMP颜色精度差异问题
    /// </summary>
    private bool IsColorSimilar(Color32 a, Color b, float tolerance)
    {
        Color aRGB = new Color(a.r / 255f, a.g / 255f, a.b / 255f);
        return Mathf.Abs(aRGB.r - b.r) < tolerance &&
               Mathf.Abs(aRGB.g - b.g) < tolerance &&
               Mathf.Abs(aRGB.b - b.b) < tolerance;
    }

    /// <summary>
    /// 处理线索收集和文字变色（基于完整首尾索引，确保无遗漏）
    /// </summary>
    private void HandleKeywordClick(string keyword, int keywordLeftIndex, int keywordRightIndex)
    {
        string cleanedKeyword = keyword.Trim().ToLower();
        Debug.Log($"【提取到纯文字关键词】：{cleanedKeyword}");

        if (ClueManager.Instance != null)
        {
            ClueManager.Instance.AddClue(cleanedKeyword);
        }

        // --- 修改开始：变灰逻辑增强 ---
        TMP_TextInfo textInfo = m_TextMeshPro.textInfo;
        Color32 greyColor = new Color32(128, 128, 128, 255);

        // 1. 遍历并修改顶点颜色
        for (int i = keywordLeftIndex; i <= keywordRightIndex && i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int matIndex = charInfo.materialReferenceIndex;
            for (int j = 0; j < 4; j++)
            {
                textInfo.meshInfo[matIndex].colors32[charInfo.vertexIndex + j] = greyColor;
            }
        }

        // 2. 【核心修复】：告诉 TMP 停止从原始文本（red标签）中获取颜色，并强制锁定顶点色
        // 这行如果不加，TMP 每帧渲染时会重新把 <color=red> 的颜色刷上去
        m_TextMeshPro.canvasRenderer.SetMesh(m_TextMeshPro.mesh);
        m_TextMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

        // 3. 【双重保险】：如果顶点色还是被覆盖，直接把字符串里的 red 换成颜色代码
        // 这样即便重新渲染，它也是灰色的
        m_TextMeshPro.text = m_TextMeshPro.text.Replace("red", "#808080");
        // --- 修改结束 ---

        hasCollected = true;
    }
}