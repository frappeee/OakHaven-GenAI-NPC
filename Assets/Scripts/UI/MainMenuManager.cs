using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Manages the Main Menu UI, Options Panel, and smooth scene navigation
/// from MainMenu to Village_Hub with configurable delay and easing.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    public const string PREF_MASTER_VOLUME = "MasterVolume";
    public const string PREF_TEXT_SPEED = "DialogueTextScrollSpeed";

    [Header("Scene Navigation Settings")]
    [SerializeField] private string villageHubSceneName = "Village_Hub";
    [Tooltip("Delay in seconds before the scene transition fade begins after clicking Start.")]
    [SerializeField] private float transitionDelay = 0.2f;
    [Tooltip("Duration in seconds to ease out of the current scene.")]
    [SerializeField] private float easeOutDuration = 0.6f;
    [Tooltip("Duration in seconds to ease in when the new scene loads.")]
    [SerializeField] private float easeInDuration = 0.75f;
    [SerializeField] private Color fadeColor = Color.black;
    [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Main Menu Panels")]
    [SerializeField] private GameObject mainButtonPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private float panelEaseDuration = 0.25f;

    [Header("Main Menu Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button backButton;

    [Header("Options Controls")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Slider textSpeedSlider;

    [Header("Ollama Auto-Start Configuration")]
    [Tooltip("Automatically starts the Ollama server and loads the model when clicking Start Game.")]
    [SerializeField] private bool autoStartOllamaOnStartGame = true;
    [SerializeField] private string ollamaModelName = "llama3.2:1b-instruct-q4_K_M";
    [SerializeField] private string ollamaEndpointURL = "http://localhost:11434/api/generate";

    private bool isTransitioning = false;
    private Coroutine panelEaseCoroutine;

    private void Awake()
    {
        AutoFindUIReferences();
        BindUIEvents();
        LoadSavedOptions();
    }

    private void Start()
    {
        if (mainButtonPanel != null)
            mainButtonPanel.SetActive(true);

        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        // Ease in the Main Menu when the scene starts
        StartCoroutine(EaseInSceneOverlay(easeInDuration));
    }

    private void AutoFindUIReferences()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        Transform root = canvas != null ? canvas.transform : null;

        if (mainButtonPanel == null)
        {
            mainButtonPanel = FindGameObject(root, "MainButtonPanel");
        }

        if (optionsPanel == null)
        {
            optionsPanel = FindGameObject(root, "OptionsPanel");
        }

        if (startButton == null)
        {
            GameObject obj = FindGameObject(root, "StartButton");
            if (obj != null) startButton = obj.GetComponent<Button>();
        }

        if (optionsButton == null)
        {
            GameObject obj = FindGameObject(root, "OptionsButton");
            if (obj != null) optionsButton = obj.GetComponent<Button>();
        }

        if (exitButton == null)
        {
            GameObject obj = FindGameObject(root, "ExitButton");
            if (obj != null) exitButton = obj.GetComponent<Button>();
        }

        if (backButton == null)
        {
            GameObject obj = FindGameObject(root, "BackButton");
            if (obj != null) backButton = obj.GetComponent<Button>();
        }

        if (volumeSlider == null)
        {
            GameObject obj = FindGameObject(root, "VolumeSlider");
            if (obj != null) volumeSlider = obj.GetComponent<Slider>();
        }

        if (textSpeedSlider == null)
        {
            GameObject obj = FindGameObject(root, "TextSpeedSlider");
            if (obj != null) textSpeedSlider = obj.GetComponent<Slider>();
        }
    }

    private GameObject FindGameObject(Transform root, string targetName)
    {
        if (root != null)
        {
            Transform found = FindDeepChild(root, targetName);
            if (found != null) return found.gameObject;
        }
        return GameObject.Find(targetName);
    }

    private Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform result = FindDeepChild(child, childName);
            if (result != null)
                return result;
        }
        return null;
    }

    private void BindUIEvents()
    {
        if (startButton != null && startButton.onClick.GetPersistentEventCount() == 0)
        {
            startButton.onClick.RemoveListener(StartGame);
            startButton.onClick.AddListener(StartGame);
        }

        if (optionsButton != null && optionsButton.onClick.GetPersistentEventCount() == 0)
        {
            optionsButton.onClick.RemoveListener(OpenOptions);
            optionsButton.onClick.AddListener(OpenOptions);
        }

        if (backButton != null && backButton.onClick.GetPersistentEventCount() == 0)
        {
            backButton.onClick.RemoveListener(CloseOptions);
            backButton.onClick.AddListener(CloseOptions);
        }

        if (exitButton != null && exitButton.onClick.GetPersistentEventCount() == 0)
        {
            exitButton.onClick.RemoveListener(ExitGame);
            exitButton.onClick.AddListener(ExitGame);
        }

        if (volumeSlider != null && volumeSlider.onValueChanged.GetPersistentEventCount() == 0)
        {
            volumeSlider.onValueChanged.RemoveListener(SetMasterVolume);
            volumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        if (textSpeedSlider != null && textSpeedSlider.onValueChanged.GetPersistentEventCount() == 0)
        {
            textSpeedSlider.onValueChanged.RemoveListener(SetTextScrollSpeed);
            textSpeedSlider.onValueChanged.AddListener(SetTextScrollSpeed);
        }
    }

    private void LoadSavedOptions()
    {
        float savedVolume = PlayerPrefs.GetFloat(PREF_MASTER_VOLUME, 1f);
        AudioListener.volume = savedVolume;
        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(savedVolume);
        }

        float savedTextSpeed = PlayerPrefs.GetFloat(PREF_TEXT_SPEED, 0.03f);
        if (textSpeedSlider != null)
        {
            textSpeedSlider.SetValueWithoutNotify(savedTextSpeed);
        }
    }

    /// <summary>
    /// Navigates from MainMenu to Village_Hub with a delay and smooth easing transition.
    /// </summary>
    public void StartGame()
    {
        NavigateToScene(villageHubSceneName);
    }

    /// <summary>
    /// Navigates to any target scene by name with delay and ease-out / ease-in transition.
    /// </summary>
    public void NavigateToScene(string sceneName)
    {
        if (isTransitioning || string.IsNullOrEmpty(sceneName))
            return;

        isTransitioning = true;
        SetMenuButtonsInteractable(false);
        StartCoroutine(TransitionToSceneRoutine(sceneName));
    }

    /// <summary>
    /// Opens the Options Panel and hides the Main Button Panel with an ease-in animation.
    /// </summary>
    public void OpenOptions()
    {
        if (isTransitioning) return;

        if (mainButtonPanel != null)
            mainButtonPanel.SetActive(false);

        if (optionsPanel != null)
        {
            if (panelEaseCoroutine != null) StopCoroutine(panelEaseCoroutine);
            panelEaseCoroutine = StartCoroutine(EaseInPanelRoutine(optionsPanel));
        }
    }

    /// <summary>
    /// Closes the Options Panel and returns to the Main Button Panel with an ease-in animation.
    /// </summary>
    public void CloseOptions()
    {
        if (isTransitioning) return;

        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        if (mainButtonPanel != null)
        {
            if (panelEaseCoroutine != null) StopCoroutine(panelEaseCoroutine);
            panelEaseCoroutine = StartCoroutine(EaseInPanelRoutine(mainButtonPanel));
        }
    }

    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(PREF_MASTER_VOLUME, AudioListener.volume);
        PlayerPrefs.Save();
    }

    public void SetTextScrollSpeed(float speed)
    {
        PlayerPrefs.SetFloat(PREF_TEXT_SPEED, speed);
        PlayerPrefs.Save();

        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.SetTextScrollSpeed(speed);
        }
    }

    public void ExitGame()
    {
        if (isTransitioning) return;

        Debug.Log("[MainMenuManager] Exiting game...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetMenuButtonsInteractable(bool interactable)
    {
        if (startButton != null) startButton.interactable = interactable;
        if (optionsButton != null) optionsButton.interactable = interactable;
        if (exitButton != null) exitButton.interactable = interactable;
        if (backButton != null) backButton.interactable = interactable;
    }

    private IEnumerator EaseInPanelRoutine(GameObject panel)
    {
        panel.SetActive(true);

        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = panel.AddComponent<CanvasGroup>();
        }

        RectTransform rt = panel.GetComponent<RectTransform>();
        Vector3 startScale = new Vector3(0.92f, 0.92f, 1f);
        Vector3 endScale = Vector3.one;

        float elapsed = 0f;
        cg.alpha = 0f;
        if (rt != null) rt.localScale = startScale;

        while (elapsed < panelEaseDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, panelEaseDuration));
            float eased = EvaluateEaseOutCubic(normalized);

            cg.alpha = eased;
            if (rt != null) rt.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);
            yield return null;
        }

        cg.alpha = 1f;
        if (rt != null) rt.localScale = endScale;
        panelEaseCoroutine = null;
    }

    private IEnumerator EaseInSceneOverlay(float duration)
    {
        if (duration <= 0f) yield break;

        GameObject overlayRoot = CreateTransitionOverlay(out CanvasGroup overlayGroup);
        overlayGroup.alpha = 1f;
        overlayGroup.blocksRaycasts = true;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EvaluateEase(t);
            overlayGroup.alpha = 1f - eased;
            yield return null;
        }

        overlayGroup.alpha = 0f;
        overlayGroup.blocksRaycasts = false;
        Destroy(overlayRoot);
    }

    private IEnumerator TransitionToSceneRoutine(string sceneName)
    {
        // Keep MainMenuManager alive across the scene change so this coroutine and the
        // Ollama warm-up coroutine continue running until both finish.
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        GameObject overlayRoot = CreateTransitionOverlay(out CanvasGroup overlayGroup);
        DontDestroyOnLoad(overlayRoot);
        overlayGroup.alpha = 0f;
        overlayGroup.blocksRaycasts = true;

        // 1. Start Ollama server & preload model immediately in parallel with the scene transition
        Coroutine ollamaWarmupCoroutine = null;
        if (autoStartOllamaOnStartGame)
        {
            ollamaWarmupCoroutine = StartCoroutine(
                OllamaAPIClient.EnsureOllamaAndModelRunningRoutine(ollamaModelName, ollamaEndpointURL)
            );
        }

        // 2. Optional delay before fade-out begins
        if (transitionDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(transitionDelay);
        }

        // 3. Ease out (fade to fadeColor)
        if (easeOutDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < easeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / easeOutDuration);
                overlayGroup.alpha = EvaluateEase(t);
                yield return null;
            }
        }
        overlayGroup.alpha = 1f;

        // 4. Load the target scene (Village_Hub)
        Debug.Log($"[MainMenuManager] Navigating to scene: {sceneName}");
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        if (asyncLoad != null)
        {
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }
        else
        {
            SceneManager.LoadScene(sceneName);
            yield return null;
        }

        // Wait one frame after scene activation so the new scene finishes initializing
        yield return null;

        // 5. Ease in (fade from fadeColor to transparent in the newly loaded scene)
        if (easeInDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < easeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / easeInDuration);
                overlayGroup.alpha = 1f - EvaluateEase(t);
                yield return null;
            }
        }

        overlayGroup.alpha = 0f;
        overlayGroup.blocksRaycasts = false;
        Destroy(overlayRoot);

        // 6. Wait for Ollama warm-up to finish in the background before cleaning up MainMenuManager
        if (ollamaWarmupCoroutine != null)
        {
            yield return ollamaWarmupCoroutine;
        }

        Destroy(gameObject);
    }

    private float EvaluateEase(float t)
    {
        if (easeCurve != null && easeCurve.length >= 2)
        {
            return Mathf.Clamp01(easeCurve.Evaluate(t));
        }
        // Fallback smoothstep ease-in-out
        return t * t * (3f - 2f * t);
    }

    private float EvaluateEaseOutCubic(float t)
    {
        float inv = 1f - Mathf.Clamp01(t);
        return 1f - (inv * inv * inv);
    }

    private GameObject CreateTransitionOverlay(out CanvasGroup canvasGroup)
    {
        GameObject canvasObj = new GameObject("SceneTransitionOverlay");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObj.AddComponent<GraphicRaycaster>();
        canvasGroup = canvasObj.AddComponent<CanvasGroup>();

        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform, false);

        RectTransform rt = imageObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = imageObj.AddComponent<Image>();
        img.color = fadeColor;
        img.raycastTarget = true;

        return canvasObj;
    }
}
