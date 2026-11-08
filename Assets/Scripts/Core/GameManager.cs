using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Playing,
    Dialogue,
    GameOver,
    Victory
}

/// This is the core Game Manager based on Class Diagram Gambar 3.5
/// which will track the current scene, player level, level-based gating and global game state
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State (Gambar 3.5)")]
    public string currentScene = "Village_Hub";
    public int playerLevel = 1;
    public bool bossUnlocked = false;
    public GameState gameState = GameState.Playing;

    [Header("Progression Rules (GDD)")]
    public const int REQUIRED_LEVEL_FOR_BOSS = 5;

    public event Action<int> OnPlayerLevelChanged;
    public event Action<GameState> OnGameStateChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        currentScene = SceneManager.GetActiveScene().name;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentScene = scene.name;
        Debug.Log($"[GameManager] Loaded scene: {currentScene}");
    }

    public void SetPlayerLevel(int newLevel)
    {
        playerLevel = newLevel;
        CheckLevelGate();

        if (ContextInjector.Instance != null)
        {
            ContextInjector.Instance.UpdatePlayerLevel(playerLevel);
        }

        OnPlayerLevelChanged?.Invoke(playerLevel);
    }

    /// checks whether the player meets the Level 5 gate requirement (GDD Section 3.3.3 & Gambar 3.5).
    public bool CheckLevelGate()
    {
        if (playerLevel >= REQUIRED_LEVEL_FOR_BOSS)
        {
            bossUnlocked = true;
            return true;
        }
        bossUnlocked = false;
        return false;
    }

    public void LoadScene(string sceneName)
    {
        Debug.Log($"[GameManager] Loading scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    public void TriggerGameOver()
    {
        gameState = GameState.GameOver;
        Debug.Log("[GameManager] GAME OVER: Player HP reached zero.");
        OnGameStateChanged?.Invoke(gameState);
    }

    public void TriggerVictory()
    {
        gameState = GameState.Victory;
        Debug.Log("[GameManager] VICTORY: Boss Ashen Fenrir has been defeated!");
        if (ContextInjector.Instance != null)
        {
            ContextInjector.Instance.SetBossDefeated(true);
        }
        OnGameStateChanged?.Invoke(gameState);
    }

    public void SetGameState(GameState newState)
    {
        gameState = newState;
        OnGameStateChanged?.Invoke(gameState);
    }
}

