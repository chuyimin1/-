using UnityEngine;
using System.Collections.Generic;

public class SocialMediaManager : MonoBehaviour
{
    public Transform postContainer;    // Content 物体
    public GameObject postPrefab;      // 文章块预制体
    public List<SocialPostData> allPosts; // 预存的所有社媒数据

    // 触发函数：通过编号弹出新文章
    public void TriggerPost(string postID)
    {
        SocialPostData data = allPosts.Find(p => p.postID == postID);
        if (data == null) return;

        // 实例化文章块
        GameObject newPost = Instantiate(postPrefab, postContainer);

        // --- 核心逻辑：新发布的出现在最上方 ---
        newPost.transform.SetAsFirstSibling();

        newPost.GetComponent<PostItem>().Setup(data);

        // 应用数值影响 (示例)
        ApplyInfluence(data.influenceA, data.influenceB);

        // 强制刷新 UI 布局
        Canvas.ForceUpdateCanvases();
    }

    private void ApplyInfluence(float a, float b)
    {
        Debug.Log($"社媒发布！数值 A 变动: {a}, 数值 B 变动: {b}");
        // 这里对接你的属性管理系统
    }
}