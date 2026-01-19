using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "CyberGhost/Dialogue")]
public class DialogueData : ScriptableObject
{
    public int eventID; // 事件编号
    public List<DialogueLine> lines; // 对话行列表
}

[System.Serializable]
public class DialogueLine
{
    public int characterType; // 0:主角, 1:来电人, 2:未离
    [TextArea]
    public string content; // 对话内容
    public string keyword; // 需要收集的关键词
    public bool hasOptions; // 这一行之后是否弹出选项
    public List<DialogueOption> options; // 选项列表
    public bool autoJump; // 新增：是否在结束后自动跳转
    public int autoJumpIndex;
}

[System.Serializable]
public class DialogueOption
{
    public string optionText;
    public int nextLineIndex; // 跳到哪一行
    public float cityAcceptanceChange; // 城市认可度变化
    public float stabilityChange; // 未离稳定值变化
}
