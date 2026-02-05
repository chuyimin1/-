using UnityEngine;

[CreateAssetMenu(fileName = "NewSocialPost", menuName = "CyberGhost/SocialPost")]
public class SocialPostData : ScriptableObject
{
    public string postID;         // 社媒编号
    public Sprite avatar;         // 头像
    public string nickname;       // 昵称
    public string title;          // 标题
    [TextArea(5, 10)]
    public string content;        // 内容
    public Sprite postImage;      // 配图 (如果没有则隐藏)

    [Header("数值影响")]
    public float influenceA;      // 对重要数值 A 的影响
    public float influenceB;      // 对重要数值 B 的影响
}
