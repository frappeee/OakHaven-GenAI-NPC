using UnityEngine;

/// Implementation for Scene 3 or The Forest Gate/Level Check (GDD & Gambar 3.1 Flowchart)
/// Preventing the player from entering the boss arena until level 5
[RequireComponent(typeof(Collider2D))]
public class LevelGate : MonoBehaviour
{
    [Header("Gate Settings")]
    [SerializeField] private int requiredLevel = 5;
    [SerializeField] private string bossSceneName = "Boss_Arena";
    [SerializeField] private GameObject lockedBarrierVisual = null;

    public bool IsPlayerAtGate { get; private set; }

    private void Start()
    {
        UpdateGateStatus();
    }

    public void UpdateGateStatus()
    {
        bool isUnlocked = GameManager.Instance != null && GameManager.Instance.CheckLevelGate();
        if (lockedBarrierVisual != null)
        {
            lockedBarrierVisual.SetActive(!isUnlocked);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            IsPlayerAtGate = true;
            AttemptPassThrough(player);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() != null)
        {
            IsPlayerAtGate = false;
        }
    }

    public bool AttemptPassThrough(PlayerController player)
    {
        if (player == null) return false;

        if (player.Level >= requiredLevel)
        {
            Debug.Log($"[LevelGate] Access Granted: Player is Level {player.Level}. Transitioning to {bossSceneName}.");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadScene(bossSceneName);
            }
            return true;
        }
        else
        {
            Debug.LogWarning($"[LevelGate] Access Denied: Player is Level {player.Level}. Required: Level {requiredLevel}.");
            if (DialogueSystem.Instance != null)
            {
                DialogueSystem.Instance.ShowFallback($"The Forest Gate is sealed. You are Level {player.Level}. You must reach Level {requiredLevel} to enter!");
            }
            return false;
        }
    }
}

