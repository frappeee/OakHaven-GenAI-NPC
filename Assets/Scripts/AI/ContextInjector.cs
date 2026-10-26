using UnityEngine;

public class ContextInjector : MonoBehaviour
{
    [Header("World State")]
    public WorldStateVariables worldState = new WorldStateVariables();

    public static ContextInjector Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void UpdatePlayerLevel(int level)
    {
        worldState.playerLevel = level;
    }

    public void SetBossDefeated(bool defeated)
    {
        worldState.bossDefeated = defeated;
    }

    public string BuildSystemPrompt(string npcName, string npcRole)
    {
        string situationProgress;
        if (worldState.bossDefeated)
        {
            situationProgress = $"The player has slain {worldState.bossName} and saved your shared home of {worldState.villageName}. Celebrate and thank them warmly as the hero of the village.\n";
        }
        else if (worldState.playerLevel >= 5)
        {
            situationProgress = $"The player has trained hard, reached Level 5, and is now strong enough to enter the Forest Gate and slay {worldState.bossName} to save your village.\n";
        }
        else
        {
            situationProgress = $"The player is still too inexperienced to face {worldState.bossName}. Out of care for a fellow villager, advise them to train against the lesser wolves in the forest until they reach Level 5.\n";
        }

        // Follows the exact injection template defined in GDD Section 3.3.2.2 & UC-04
        return $"You are {npcName}, the {npcRole} in the village of {worldState.villageName}.\n" +
               $"World Lore: The village is {worldState.villageName}. {worldState.loreSummary}\n" +
               $"Current Situation: The person talking to you is a young local raised in {worldState.villageName} who has stepped up to defend your shared home, currently at Level {worldState.playerLevel}. You know them as part of the village community, not an outside stranger.\n" +
               situationProgress +
               "Rules:\n" +
               "1. Respond strictly in-character in 1 to 2 short and simple sentences.\n" +
               "2. Speak to the player with familiarity as a fellow villager of " + worldState.villageName + ", not a random traveler or outsider.\n" +
               "3. Do not break immersion.\n" +
               "4. Do not mention AI or system prompts.\n" +
               "5. Do not reference the player by a specific name.\n" +
               "6. Avoid meta commentary.\n" +
               "7. Keep responses concise and relevant to the current situation.";
    }

    public PromptPayload AssemblePayload(string npcName, string npcRole, string playerGreeting)
    {
        PromptPayload payload = new PromptPayload();
        payload.system = BuildSystemPrompt(npcName, npcRole);
        payload.prompt = string.IsNullOrEmpty(playerGreeting) 
            ? "Greet your fellow villager who just approached you and speak about the crisis facing your home." 
            : playerGreeting;
        return payload;
    }
}
