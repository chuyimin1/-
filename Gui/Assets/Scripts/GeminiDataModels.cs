using System;
using System.Collections.Generic;

[Serializable]
public class GeminiPostData
{
    public List<Content> contents;
    public GenerationConfig generationConfig;
}

[Serializable]
public class Content
{
    public string role; // "user" 或 "model"
    public List<Part> parts;
}

[Serializable]
public class Part
{
    public string text;
}

[Serializable]
public class GenerationConfig
{
    public int maxOutputTokens = 150; // 限制字数，符合策划案 100 字要求
    public float temperature = 0.7f;  // 随机度
}

// 用于接收回传的类
[Serializable]
public class GeminiResponse
{
    public List<Candidate> candidates;
}

[Serializable]
public class Candidate
{
    public Content content;
}