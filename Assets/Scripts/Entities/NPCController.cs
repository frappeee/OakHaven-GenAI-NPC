using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class NPCController : MonoBehaviour
{
    [Header("NPC Persona (UC-04)")]
    [SerializeField] private string npcName = "Village Elder";
    [SerializeField] private string npcRole = "weary leader of Oakhaven";
    
    [Tooltip("Tags to retrieve from LoreDatabase.json (e.g., 'general', 'elder', 'blacksmith', 'baker', 'fisherman')")]
    [SerializeField] private List<string> npcKnowledgeTags = new List<string> { "general", "elder" };

    [Header("Conversation Memory & Multi-Turn (Gambar 3.6)")]
    [SerializeField] private ConversationHistory conversationHistory = new ConversationHistory();
    [SerializeField] private string lastNPCResponse = "";
    [Tooltip("If enabled, the NPC will ask the player a question to trigger a multi-turn interaction")]
    [SerializeField] private bool asksQuestionToPlayer = false;
    [SerializeField] private string questionPrompt = "Ask the adventurer what weapon style or approach they prefer against the wolves.";

    private bool isPlayerInRange = false;
    private bool isGenerating = false;
    private int lastInteractFrame = -1;
    private PlayerController nearbyPlayer;
    private PlayerController lockedPlayer;

    private void Awake()
    {
        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        if (circle != null)
        {
            circle.isTrigger = true;
        }
    }

    private void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E) && !isGenerating)
        {
            OnInteractKeyPressed();
        }
    }

    public void OnInteractKeyPressed()
    {
        // Guard against double invocation in the same frame (e.g., from both PlayerController and NPCController)
        // or input while Ollama generation is active
        if (isGenerating || lastInteractFrame == Time.frameCount) return;
        lastInteractFrame = Time.frameCount;

        if (DialogueSystem.Instance == null)
        {
            Debug.LogError("[NPCController] DialogueSystem.Instance is missing in the scene.");
            return;
        }

        // If dialogue is currently open and finished generating, close it and unlock the player
        if (DialogueSystem.Instance.IsActive)
        {
            CloseDialogue();
            return;
        }

        StartCoroutine(StartInteraction());
    }

    private IEnumerator StartInteraction()
    {
        isGenerating = true;

        if (nearbyPlayer != null)
        {
            lockedPlayer = nearbyPlayer;
            lockedPlayer.SetMovementLock(true);
        }

        DialogueSystem.Instance.ShowThinking(npcName);

        // Fallback check for ContextInjector and OllamaAPIClient
        if (ContextInjector.Instance == null || OllamaAPIClient.Instance == null)
        {
            Debug.LogError("[NPCController] ContextInjector or OllamaAPIClient is missing from the scene.");
            DialogueSystem.Instance.ShowFallback("Error: System middleware missing.");
            CloseDialogue();
            yield break;
        }

        // Determine prompt based on whether NPC asks a question or greets
        string promptToSend = asksQuestionToPlayer
            ? questionPrompt
            : "Greet the adventurer from your perspective and speak about the current situation.";

        // Pass this specific NPC's name, role, and knowledge tags to ContextInjector
        PromptPayload payload = ContextInjector.Instance.AssemblePayload(
            npcName,
            npcRole,
            npcKnowledgeTags,
            promptToSend,
            conversationHistory
        );

        yield return StartCoroutine(OllamaAPIClient.Instance.PostAsync(
            payload,
            onComplete: (response, tps) =>
            {
                lastNPCResponse = !string.IsNullOrEmpty(response) ? response.Trim() : "...";
                conversationHistory.Append("assistant", lastNPCResponse);
                DialogueSystem.Instance.DisplayResponse(lastNPCResponse);
                isGenerating = false;
            },
            onTimeout: () =>
            {
                DialogueSystem.Instance.ShowFallback("Sorry. I'm Busy (Server Timeout)");
                isGenerating = false;
            }
        ));

        // Failsafe in case PostAsync exited early (e.g. client was busy)
        isGenerating = false;
    }

    /// <summary>
    /// Multi-turn questioning handler: allows player to reply to the NPC question.
    /// Used for advisor's requested interactive feature and Gambar 3.6 ConversationHistory.
    /// </summary>
    public void SubmitPlayerReply(string playerReply)
    {
        if (isGenerating || string.IsNullOrWhiteSpace(playerReply)) return;
        StartCoroutine(ProcessPlayerReply(playerReply));
    }

    private IEnumerator ProcessPlayerReply(string playerReply)
    {
        isGenerating = true;
        conversationHistory.Append("user", playerReply.Trim());

        DialogueSystem.Instance.ShowThinking(npcName);

        PromptPayload payload = ContextInjector.Instance.AssemblePayload(
            npcName,
            npcRole,
            npcKnowledgeTags,
            "The adventurer answered your question. Respond in-character in 1 or 2 concise sentences acknowledging their answer.",
            conversationHistory
        );

        yield return StartCoroutine(OllamaAPIClient.Instance.PostAsync(
            payload,
            onComplete: (response, tps) =>
            {
                lastNPCResponse = !string.IsNullOrEmpty(response) ? response.Trim() : "...";
                conversationHistory.Append("assistant", lastNPCResponse);
                DialogueSystem.Instance.DisplayResponse(lastNPCResponse);
                isGenerating = false;
            },
            onTimeout: () =>
            {
                DialogueSystem.Instance.ShowFallback("Sorry, I'm Busy (Server Timeout)");
                isGenerating = false;
            }
        ));

        isGenerating = false;
    }

    private void CloseDialogue()
    {
        isGenerating = false;

        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.Hide();
        }

        UnlockPlayer();
    }

    private void UnlockPlayer()
    {
        if (lockedPlayer != null)
        {
            lockedPlayer.SetMovementLock(false);
            lockedPlayer = null;
        }
        else if (nearbyPlayer != null)
        {
            nearbyPlayer.SetMovementLock(false);
        }
    }

    private void OnDisable()
    {
        if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsActive)
        {
            CloseDialogue();
        }
        else
        {
            UnlockPlayer();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            isPlayerInRange = true;
            nearbyPlayer = player;
            player.SetNearbyNPC(this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null && player == nearbyPlayer)
        {
            isPlayerInRange = false;
            player.ClearNearbyNPC(this);
            nearbyPlayer = null;
        }
    }
}
