using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Message
{
    public string role;
    public string content;

    public Message(string role, string content)
    {
        this.role = role;
        this.content = content;
    }
}

[Serializable]
public class PromptPayload
{
    public string model = "llama3.2:1b";
    public string prompt;
    public string system;
    public bool stream = false;

    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }
}

[Serializable]
public class APIResponse
{
    public string response;
    public bool done;
    public int eval_count;
    public long eval_duration;
    public long total_duration;

    // Calculates Tokens Per Second (TPS) for your thesis performance evaluation (Bab 1.5.1 / Bab 3)
    public float GetTPS()
    {
        if (eval_duration <= 0) return 0f;
        // eval_duration is reported in nanoseconds by Ollama
        return (eval_count / (eval_duration / 1_000_000_000f));
    }
}

[Serializable]
public class WorldStateVariables
{
    public string villageName = "Oakhaven";
    public string bossName = "Ashen Fenrir";
    public string loreSummary = "An ancient wolf named Ashen Fenrir woke up after 50 years and is starving. It is eating our livestock. The forest is full of lesser wolves.";
    public int playerLevel = 1;
    public bool bossDefeated = false;
}