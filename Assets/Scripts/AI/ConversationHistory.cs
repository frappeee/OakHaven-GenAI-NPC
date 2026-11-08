using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages multi-turn conversation memory between player and NPC (Class Diagram Gambar 3.6).
/// Designed with a strict sliding window (TrimHistory) to conserve shared memory on Integrated Graphics.
/// </summary>
[Serializable]
public class ConversationHistory
{
    [SerializeField]
    public List<Message> turns = new List<Message>();

    [SerializeField]
    public int maxTokenWindow = 1024;

    [SerializeField]
    public int maxTurns = 4; // 2 round-trips: keeps token usage minimal for APU/integrated graphics

    public void Append(string role, string content)
    {
        if (string.IsNullOrEmpty(content)) return;
        turns.Add(new Message(role, content));
        TrimHistory(maxTurns);
    }

    public List<Message> GetHistory()
    {
        return turns;
    }

    public void Clear()
    {
        turns.Clear();
    }

    /// <summary>
    /// Trims oldest turns when history exceeds maximum allowed turns.
    /// </summary>
    public void TrimHistory(int max)
    {
        if (max <= 0)
        {
            turns.Clear();
            return;
        }

        while (turns.Count > max)
        {
            turns.RemoveAt(0);
        }
    }

    /// <summary>
    /// Returns the history formatted as a context string for prompt injection.
    /// </summary>
    public string ToContextString()
    {
        if (turns == null || turns.Count == 0) return "";
        List<string> formatted = new List<string>();
        foreach (var msg in turns)
        {
            string speaker = msg.role == "assistant" ? "NPC" : "Adventurer";
            formatted.Add($"{speaker}: {msg.content}");
        }
        return string.Join("\n", formatted);
    }
}

