using UnityEngine;
using System.Collections.Generic;

public class ClueManager : MonoBehaviour
{
    public static ClueManager Instance;

    // 存储已获得的线索名称
    public List<string> collectedClues = new List<string>();

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void AddClue(string clueName)
    {
        string k = clueName.Trim();
        if (!collectedClues.Contains(k))
        {
            collectedClues.Add(k);
        }
    }

    public bool AreAllCluesCollected(List<string> requiredKeywords)
    {
        foreach (string reqKey in requiredKeywords)
        {
            // 1. 清理剧本里的关键词
            string cleanReq = reqKey.Trim();
            bool found = false;

            // 2. 在已收集列表里寻找
            foreach (string collectedKey in collectedClues)
            {
                // 3. 双方都去掉空格进行对比
                if (collectedKey.Trim() == cleanReq)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Debug.LogError($"[比对失败] 没找到: '{cleanReq}'");
                return false;
            }
        }
        return true;
    }
}