using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System; // 必须加上这个，否则 [Serializable] 报错

public class GeminiManager : MonoBehaviour
{
    [Header("设置")]
    public string apiKey = "在这里填入你的API_KEY";
    private string url =
"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key=";


    public delegate void OnResponseReceived(string response);

    public void SendToGemini(string playerInput, OnResponseReceived callback)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<WeiLi_task>" + GetWeiLiTask() + "</WeiLi_task>");
        sb.AppendLine("<Chat_example>" + GetChatExample() + "</Chat_example>");
        sb.AppendLine("<Chat_history>" + GetLongTermHistory() + "</Chat_history>");
        sb.AppendLine("<Chat_event>" + GetCurrentEvent() + "</Chat_event>");
        sb.AppendLine("<Chat_event_example>" + GetEventExample() + "</Chat_event_example>");
        sb.AppendLine("<Chat_history_current>");
        sb.AppendLine(GetRealTimeChat());
        sb.AppendLine("接线员:\"" + playerInput + "\"");
        sb.AppendLine("未离:");
        sb.AppendLine("</Chat_history_current>");

        StartCoroutine(PostRoutine(sb.ToString(), callback));
    }

    IEnumerator PostRoutine(string prompt, OnResponseReceived callback)
    {
        GeminiPostData postData = new GeminiPostData();
        postData.contents = new List<Content> {
            new Content { parts = new List<Part> { new Part { text = prompt } } }
        };
        postData.generationConfig = new GenerationConfig();

        string jsonPayload = JsonUtility.ToJson(postData);

        using (UnityWebRequest request = new UnityWebRequest(url + apiKey, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(request.downloadHandler.text);
                if (response == null || response.candidates == null || response.candidates.Count == 0)
                {
                    Debug.LogError("Gemini 返回为空 candidates");
                    callback?.Invoke("……（未离沉默了）");
                    yield break;
                }

                var candidate = response.candidates[0];

                if (candidate.content == null || candidate.content.parts == null || candidate.content.parts.Count == 0)
                {
                    Debug.LogError("Gemini 返回但没有 text parts");
                    callback?.Invoke("……（未离没有说话）");
                    yield break;
                }

                string aiText = "";

                foreach (var part in candidate.content.parts)
                {
                    if (!string.IsNullOrEmpty(part.text))
                    {
                        aiText += part.text;
                    }
                }

                if (string.IsNullOrEmpty(aiText))
                {
                    Debug.LogWarning("Gemini parts 里没有 text 字段");
                    aiText = "……（未离正在思考）";
                }
                callback?.Invoke(aiText);
            }
            else
            {
                Debug.LogError("Gemini 报错: " + request.error);
                callback?.Invoke("……(网络波形不稳定，未离暂时无法回应)");
            }
        }
    }


    // 占位函数
    string GetWeiLiTask() => "扮演未离";
    string GetChatExample() => "毒舌风格";
    string GetLongTermHistory() => "";
    string GetCurrentEvent() => "";
    string GetEventExample() => "";
    string GetRealTimeChat() => "";
}