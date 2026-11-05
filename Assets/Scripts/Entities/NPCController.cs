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

    [Header("Conversation Memory (Gambar 3.6)")]
    [SerializeField] private string lastNPCResponse = ""; // Optional per-NPC memory

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

        // Pass this specific NPC's name, role, and knowledge tags to ContextInjector
        PromptPayload payload = ContextInjector.Instance.AssemblePayload(
            npcName,
            npcRole,
            npcKnowledgeTags,
            "Greet the adventurer from your perspective and speak about the current situation."
        );

        yield return StartCoroutine(OllamaAPIClient.Instance.PostAsync(
            payload,
            onComplete: (response, tps) =>
            {
                lastNPCResponse = !string.IsNullOrEmpty(response) ? response.Trim() : "...";
                DialogueSystem.Instance.DisplayResponse(lastNPCResponse);
                isGenerating = false;
            },
            onTimeout: () =>
            {
                DialogueSystem.Instance.ShowFallback("Maaf, aku sedang sibuk... (Server Timeout)");
                isGenerating = false;
            }
        ));

        // Failsafe in case PostAsync exited early (e.g. client was busy)
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
