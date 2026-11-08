using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ContextInjector : MonoBehaviour
{
    [Header("World State")]
    public WorldStateVariables worldState = new WorldStateVariables();

    [Header("Lore Database")]
    [SerializeField] private WorldFactsData loreDatabase;

    private static ContextInjector instance;
    public static ContextInjector Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ContextInjector>();
            }
            return instance;
        }
        private set => instance = value;
    }

    private void Awake()
    {
        if (instance == null || instance == this)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadLoreDatabase();
    }

    public void LoadLoreDatabase()
    {
        try
        {
            string json = "";

            // 1. Try StreamingAssets (LoreDatabase.json, then worldFacts.json)
            string streamingPath = Path.Combine(Application.streamingAssetsPath, "LoreDatabase.json");
            if (File.Exists(streamingPath))
            {
                json = File.ReadAllText(streamingPath);
            }
            else
            {
                string altStreamingPath = Path.Combine(Application.streamingAssetsPath, "worldFacts.json");
                if (File.Exists(altStreamingPath))
                {
                    json = File.ReadAllText(altStreamingPath);
                }
            }

            // 2. Fallback to Resources
            if (string.IsNullOrEmpty(json))
            {
                TextAsset asset = Resources.Load<TextAsset>("LoreDatabase");
                if (asset == null) asset = Resources.Load<TextAsset>("worldFacts");
                if (asset != null)
                {
                    json = asset.text;
                }
            }

            if (!string.IsNullOrEmpty(json))
            {
                loreDatabase = JsonUtility.FromJson<WorldFactsData>(json);
                Debug.Log($"[ContextInjector] Loaded {loreDatabase?.worldFacts?.Count ?? 0} world facts from lore database.");
            }
            else
            {
                Debug.LogWarning("[ContextInjector] Lore database JSON not found in StreamingAssets or Resources. Using fallback lore.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ContextInjector] Error loading lore database: {ex.Message}");
        }
    }

    public void UpdatePlayerLevel(int level)
    {
        worldState.playerLevel = level;
    }

    public void SetBossDefeated(bool defeated)
    {
        worldState.bossDefeated = defeated;
    }

    /// <summary>
    /// Builds the system prompt injecting both general and role-specific lore chunks based on knowledge tags and current game state.
    /// </summary>
    public string BuildSystemPrompt(string npcName, string npcRole, List<string> knowledgeTags = null)
    {
        if (loreDatabase == null || loreDatabase.worldFacts == null || loreDatabase.worldFacts.Count == 0)
        {
            LoadLoreDatabase();
        }

        // Determine current progression state tag
        string stateTag;
        if (worldState.bossDefeated)
        {
            stateTag = "boss_dead";
        }
        else if (worldState.playerLevel >= 5)
        {
            stateTag = "level_ready";
        }
        else
        {
            stateTag = "level_low";
        }

        // Assemble active filter tags (general village lore + NPC role tags + current progression state tag)
        HashSet<string> activeTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "general",
            stateTag
        };

        if (knowledgeTags != null && knowledgeTags.Count > 0)
        {
            foreach (string tag in knowledgeTags)
            {
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    activeTags.Add(tag.Trim());
                }
            }
        }

        // Retrieve matching facts from the database
        List<string> retrievedChunks = new List<string>();
        if (loreDatabase != null && loreDatabase.worldFacts != null)
        {
            foreach (WorldFact fact in loreDatabase.worldFacts)
            {
                if (fact != null && fact.tags != null)
                {
                    foreach (string tag in fact.tags)
                    {
                        if (activeTags.Contains(tag))
                        {
                            retrievedChunks.Add(fact.content);
                            break;
                        }
                    }
                }
            }
        }

        // Build lore section from retrieved chunks or fallback
        string loreSection;
        if (retrievedChunks.Count > 0)
        {
            loreSection = "World Lore & Knowledge:\n- " + string.Join("\n- ", retrievedChunks);
        }
        else
        {
            string fallbackProgress = worldState.bossDefeated
                ? $"The player has slain {worldState.bossName} and saved {worldState.villageName}."
                : (worldState.playerLevel >= 5
                    ? $"The player is Level 5 and ready to enter the Forest Gate to slay {worldState.bossName}."
                    : $"The player is Level {worldState.playerLevel} and must train in the forest first.");

            loreSection = $"World Lore: The village is {worldState.villageName}. {worldState.loreSummary}\n" +
                          $"Current Situation: {fallbackProgress}";
        }

        // Injection template enforcing consistency and conciseness (GDD Section 3.3.2.2 & UC-04)
        return $"You are {npcName}, the {npcRole} in the village of {worldState.villageName}.\n" +
               $"{loreSection}\n" +
               $"Current Situation: The person talking to you is an adventurer currently at Level {worldState.playerLevel} raised in {worldState.villageName}. You know them as a fellow villager, not a stranger.\n" +
               "Rules:\n" +
               "1. Respond strictly in-character in 1 to 2 short and simple sentences.\n" +
               "2. Speak to the player with familiarity as a fellow villager of " + worldState.villageName + ", not a random traveler or outsider.\n" +
               "3. Do not break immersion.\n" +
               "4. Do not mention AI or system prompts.\n" +
               "5. Do not reference the player by a specific name.\n" +
               "6. Avoid meta commentary.\n" +
               "7. Keep responses concise and relevant to your role and the current crisis.";
    }

    public PromptPayload AssemblePayload(string npcName, string npcRole, List<string> npcKnowledgeTags, string playerGreeting, ConversationHistory history = null)
    {
        PromptPayload payload = new PromptPayload();
        string baseSystem = BuildSystemPrompt(npcName, npcRole, npcKnowledgeTags);

        if (history != null && history.turns != null && history.turns.Count > 0)
        {
            baseSystem += "\n\nRecent Conversation with Adventurer:\n" + history.ToContextString();
        }

        payload.system = baseSystem;
        payload.prompt = string.IsNullOrEmpty(playerGreeting)
            ? "Greet the adventurer from your perspective and speak about the current situation."
            : playerGreeting;
        return payload;
    }

    /// <summary>
    /// Helper to enforce memory constraints for Gambar 3.6
    /// </summary>
    public void TrimHistory(ConversationHistory history, int max = 4)
    {
        if (history != null)
        {
            history.TrimHistory(max);
        }
    }

    // Overload for backward compatibility
    public PromptPayload AssemblePayload(string npcName, string npcRole, string playerGreeting)
    {
        return AssemblePayload(npcName, npcRole, new List<string> { "general" }, playerGreeting, null);
    }
}
