using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class NPCController : MonoBehaviour
{
    [Header("NPC Persona (UC-04)")]
    [SerializeField] private string npcName = "Village Elder";
    [SerializeField] private string npcRole = "weary leader of Oakhaven who watches over the villagers and worries about the starving wolf Ashen Fenrir";
    [SerializeField] private Sprite npcPortrait;
    [SerializeField] private string defaultPrompt = "Greet your fellow villager and speak about the crisis facing Oakhaven.";

    private bool isPlayerInRange = false;
    private bool isGenerating = false;
    private bool isDialogueOpenForThisNPC = false;
    private int lastInteractFrame = -1;
    private PlayerController nearbyPlayer;
    private PlayerController lockedPlayer;

    private void Awake()
    {
        // Ensure the CircleCollider2D acts as an interaction trigger zone
        CircleCollider2D triggerZone = GetComponent<CircleCollider2D>();
        triggerZone.isTrigger = true;
    }

    private void Update()
    {
        if (!isPlayerInRange && !isDialogueOpenForThisNPC) return;

        // Trigger interaction or close dialogue when pressing 'E'
        if (Input.GetKeyDown(KeyCode.E) && !isGenerating)
        {
            OnInteractKeyPressed();
        }
    }

    public void OnInteractKeyPressed()
    {
        // Prevent double-triggering on the same frame and ignore input while generating
        if (isGenerating || lastInteractFrame == Time.frameCount) return;
        lastInteractFrame = Time.frameCount;

        if (DialogueSystem.Instance == null)
        {
            Debug.LogError("[NPCController] DialogueSystem.Instance is missing in the scene.");
            return;
        }

        // Step 8 of UC-03: If dialogue is already open and finished generating, close it and unlock player
        if (DialogueSystem.Instance.IsActive)
        {
            if (isDialogueOpenForThisNPC)
            {
                CloseDialogue();
            }
            return;
        }

        if (ContextInjector.Instance == null || OllamaAPIClient.Instance == null)
        {
            Debug.LogError("[NPCController] ContextInjector or OllamaAPIClient instance is missing in the scene.");
            return;
        }

        if (OllamaAPIClient.Instance.IsBusy)
        {
            return;
        }

        // Step 1-5 of UC-03: Start new AI dialogue interaction
        StartCoroutine(StartInteraction());
    }

    private IEnumerator StartInteraction()
    {
        isGenerating = true;
        isDialogueOpenForThisNPC = true;

        // Step 2 of UC-03: Lock player movement
        if (nearbyPlayer != null)
        {
            lockedPlayer = nearbyPlayer;
            lockedPlayer.SetMovementLock(true);
        }

        // Step 3 of UC-03: Show UI Dialogue Box & Thinking Indicator
        DialogueSystem.Instance.ShowThinking(npcName, npcPortrait);

        // Step 4 of UC-03: Assemble Context Injection Payload (UC-04)
        PromptPayload payload = ContextInjector.Instance.AssemblePayload(
            npcName,
            npcRole,
            defaultPrompt
        );

        // Step 5 & 6 of UC-03: Send HTTP POST to Local Ollama Server
        yield return StartCoroutine(OllamaAPIClient.Instance.PostAsync(
            payload,
            onComplete: (response, tps) =>
            {
                // Step 7 of UC-03: Display generated response
                OnResponseReceived(response);
            },
            onTimeout: () =>
            {
                // Exception E-1 of UC-03: 5-second timeout fallback
                DialogueSystem.Instance.ShowFallback("Forgive me... my thoughts are clouded right now.");
                isGenerating = false;
            }
        ));

        // Failsafe in case PostAsync exited early
        isGenerating = false;
    }

    public void OnResponseReceived(string responseText)
    {
        string cleanedResponse = string.IsNullOrWhiteSpace(responseText) ? "..." : responseText.Trim();
        DialogueSystem.Instance.DisplayResponse(cleanedResponse);
        isGenerating = false;
    }

    private void CloseDialogue()
    {
        isDialogueOpenForThisNPC = false;
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
        if (isDialogueOpenForThisNPC)
        {
            CloseDialogue();
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
            Debug.Log($"[NPCController] Player entered range of {npcName}. Press 'E' to talk.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null && player == nearbyPlayer)
        {
            isPlayerInRange = false;
            nearbyPlayer = null;
            player.ClearNearbyNPC(this);
        }
    }
}
