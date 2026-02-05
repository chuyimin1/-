using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "WeiLiDialogue", menuName = "CyberGhost/WeiLiDialogue")]
public class WeiLiDialogueData : ScriptableObject
{
    public string dialogueID;       // 对话编号，如 0101
    [TextArea(3, 10)]
    public string context;          // 前情提要（发给给AI的背景，不显示给玩家）
    [TextArea(3, 5)]
    public string openingLine;      // 固定开场白（显示给玩家）
    public int maxTurns = 2;        // 对话回合数（策划案要求有限回合）

    // 这里的 context 应该包含策划案里“输入的前情提要”
}
