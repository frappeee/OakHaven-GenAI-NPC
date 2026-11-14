using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

public class OllamaAPIClient : MonoBehaviour
{
    [Header("Ollama Server Configuration")]
    [SerializeField] private string endpointURL = "http://localhost:11434/api/generate";
    [SerializeField] private float timeoutSeconds = 5f; // Exception E-1 per UC-03
    [SerializeField] private float coldStartTimeoutSeconds = 15f;
    [SerializeField] private string modelName = "llama3.2:1b-instruct-q4_K_M";
    [SerializeField] private bool autoStartModelOnSceneLoad = true;

    private static OllamaAPIClient instance;
    public static OllamaAPIClient Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<OllamaAPIClient>();
            }
            return instance;
        }
        private set => instance = value;
    }
    public bool IsBusy { get; private set; }

    private static bool isModelWarm = false;
    private static bool isWarmingUp = false;
    private static string resolvedModelName = null;
    private static Process ollamaServerProcess;

    private void Awake()
    {
        if (instance == null || instance == this)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (autoStartModelOnSceneLoad && !isModelWarm && !isWarmingUp)
        {
            StartCoroutine(EnsureOllamaAndModelRunningRoutine(modelName, endpointURL, coldStartTimeoutSeconds));
        }
    }

    /// <summary>
    /// Ensures the Ollama server is running (starting `ollama serve` in the background if needed),
    /// verifies/pulls the model so `/api/generate` does not return 404 Not Found, and preloads it into memory.
    /// </summary>
    public static IEnumerator EnsureOllamaAndModelRunningRoutine(
        string targetModelName = "llama3.2:1b-instruct-q4_K_M",
        string generateEndpointUrl = "http://localhost:11434/api/generate",
        float warmupTimeoutSeconds = 30f)
    {
        if (isModelWarm || isWarmingUp)
        {
            yield break;
        }

        isWarmingUp = true;

        string baseUrl = GetBaseServerUrl(generateEndpointUrl);
        string tagsUrl = $"{baseUrl}/api/tags";
        string tagsJson = null;

        // 1. Check if the local Ollama server is already running
        bool serverRunning = false;
        using (UnityWebRequest pingRequest = UnityWebRequest.Get(tagsUrl))
        {
            pingRequest.timeout = 2;
            yield return pingRequest.SendWebRequest();
            if (pingRequest.result == UnityWebRequest.Result.Success)
            {
                serverRunning = true;
                tagsJson = pingRequest.downloadHandler.text;
            }
        }

        // 2. If not running, automatically launch `ollama serve` in the background
        if (!serverRunning)
        {
            Debug.Log("[OllamaAPIClient] Ollama server not detected on localhost. Starting Ollama service automatically...");
            bool processStarted = TryStartOllamaServerProcess();

            if (processStarted)
            {
                float waitElapsed = 0f;
                const float maxServerStartupWait = 10f;
                while (waitElapsed < maxServerStartupWait && !serverRunning)
                {
                    yield return new WaitForSecondsRealtime(0.5f);
                    waitElapsed += 0.5f;

                    using (UnityWebRequest checkReq = UnityWebRequest.Get(tagsUrl))
                    {
                        checkReq.timeout = 2;
                        yield return checkReq.SendWebRequest();
                        if (checkReq.result == UnityWebRequest.Result.Success)
                        {
                            serverRunning = true;
                            tagsJson = checkReq.downloadHandler.text;
                        }
                    }
                }
            }
        }

        if (!serverRunning)
        {
            Debug.LogWarning("[OllamaAPIClient] Could not reach or auto-start local Ollama server.");
            isWarmingUp = false;
            yield break;
        }

        // 3. Resolve which model is installed in Ollama (preventing 404 Not Found on /api/generate)
        string modelToUse = ResolveAvailableModel(tagsJson, targetModelName);

        if (string.IsNullOrEmpty(modelToUse))
        {
            // Target model is not installed and no fallback models exist -> auto-pull the target model
            Debug.Log($"[OllamaAPIClient] Model '{targetModelName}' not found locally. Pulling '{targetModelName}' via Ollama API...");
            string pullUrl = $"{baseUrl}/api/pull";
            string pullJson = $"{{\"name\":\"{targetModelName}\",\"stream\":false}}";

            using (UnityWebRequest pullReq = new UnityWebRequest(pullUrl, "POST"))
            {
                byte[] pullBody = Encoding.UTF8.GetBytes(pullJson);
                pullReq.uploadHandler = new UploadHandlerRaw(pullBody);
                pullReq.downloadHandler = new DownloadHandlerBuffer();
                pullReq.SetRequestHeader("Content-Type", "application/json");
                pullReq.timeout = 300; // allow time if downloading model weights
                yield return pullReq.SendWebRequest();

                if (pullReq.result == UnityWebRequest.Result.Success)
                {
                    modelToUse = targetModelName;
                    Debug.Log($"[OllamaAPIClient] Successfully pulled model '{targetModelName}'.");
                }
                else
                {
                    Debug.LogWarning($"[OllamaAPIClient] Failed to auto-pull '{targetModelName}': {pullReq.error}");
                    modelToUse = targetModelName;
                }
            }
        }

        resolvedModelName = modelToUse;

        // 4. Preload/run the model in memory via /api/generate with an empty prompt and keep_alive
        Debug.Log($"[OllamaAPIClient] Loading and warming up model '{resolvedModelName}'...");
        string preloadJson = $"{{\"model\":\"{resolvedModelName}\",\"prompt\":\"\",\"keep_alive\":\"30m\",\"stream\":false}}";

        using (UnityWebRequest warmupReq = new UnityWebRequest(generateEndpointUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(preloadJson);
            warmupReq.uploadHandler = new UploadHandlerRaw(bodyRaw);
            warmupReq.downloadHandler = new DownloadHandlerBuffer();
            warmupReq.SetRequestHeader("Content-Type", "application/json");
            warmupReq.timeout = Mathf.Max(15, Mathf.RoundToInt(warmupTimeoutSeconds));

            float startTime = Time.realtimeSinceStartup;
            yield return warmupReq.SendWebRequest();

            if (warmupReq.result == UnityWebRequest.Result.Success)
            {
                isModelWarm = true;
                Debug.Log($"[OllamaAPIClient] Model '{resolvedModelName}' is now running and ready in memory ({(Time.realtimeSinceStartup - startTime):F2}s).");
            }
            else
            {
                string errDetails = warmupReq.downloadHandler != null ? warmupReq.downloadHandler.text : "";
                Debug.LogWarning($"[OllamaAPIClient] Model warm-up request failed: {warmupReq.error} {errDetails}");
            }
        }

        isWarmingUp = false;
    }

    private static string ResolveAvailableModel(string tagsJson, string preferredModel)
    {
        if (string.IsNullOrEmpty(tagsJson))
            return null;

        // Fast direct check in the /api/tags JSON for the preferred model name
        if (!string.IsNullOrEmpty(preferredModel) &&
            tagsJson.IndexOf($"\"{preferredModel}\"", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return preferredModel;
        }

        try
        {
            OllamaTagsList tags = JsonUtility.FromJson<OllamaTagsList>(tagsJson);
            if (tags == null || tags.models == null || tags.models.Count == 0)
            {
                return null;
            }

            // 1. Exact match (e.g., "llama3.2:1b-instruct-q4_K_M")
            foreach (OllamaModelEntry entry in tags.models)
            {
                string candidate = !string.IsNullOrEmpty(entry.name) ? entry.name : entry.model;
                if (string.Equals(candidate, preferredModel, StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }

            // 2. Base family match (e.g., "llama3.2:latest" or "llama3.2:3b" when preferred is "llama3.2:1b-instruct-q4_K_M")
            string preferredBase = !string.IsNullOrEmpty(preferredModel) ? preferredModel.Split(':')[0] : "";
            if (!string.IsNullOrEmpty(preferredBase))
            {
                foreach (OllamaModelEntry entry in tags.models)
                {
                    string candidate = !string.IsNullOrEmpty(entry.name) ? entry.name : entry.model;
                    if (!string.IsNullOrEmpty(candidate) &&
                        candidate.StartsWith(preferredBase, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Log($"[OllamaAPIClient] '{preferredModel}' not found; using installed matching model '{candidate}'.");
                        return candidate;
                    }
                }
            }

            // 3. Fallback to the first installed model in Ollama so NPC dialogue works out-of-the-box
            string firstAvailable = !string.IsNullOrEmpty(tags.models[0].name) ? tags.models[0].name : tags.models[0].model;
            if (!string.IsNullOrEmpty(firstAvailable))
            {
                Debug.Log($"[OllamaAPIClient] '{preferredModel}' not found; falling back to installed Ollama model '{firstAvailable}'.");
                return firstAvailable;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[OllamaAPIClient] Could not parse /api/tags response: {ex.Message}");
        }

        return null;
    }

    private static string GetBaseServerUrl(string endpoint)
    {
        if (string.IsNullOrEmpty(endpoint))
            return "http://localhost:11434";

        try
        {
            Uri uri = new Uri(endpoint);
            return $"{uri.Scheme}://{uri.Authority}";
        }
        catch
        {
            return "http://localhost:11434";
        }
    }

    private static bool TryStartOllamaServerProcess()
    {
        try
        {
            string ollamaExecutable = ResolveOllamaExecutablePath();
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = ollamaExecutable,
                Arguments = "serve",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            ollamaServerProcess = Process.Start(startInfo);
            Debug.Log($"[OllamaAPIClient] Launched background process: {ollamaExecutable} serve");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[OllamaAPIClient] Failed to launch 'ollama serve' automatically: {ex.Message}");
            return false;
        }
    }

    private static string ResolveOllamaExecutablePath()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrEmpty(localAppData))
        {
            string defaultWinPath = Path.Combine(localAppData, "Programs", "Ollama", "ollama.exe");
            if (File.Exists(defaultWinPath))
            {
                return defaultWinPath;
            }
        }
        return "ollama";
    }

    public IEnumerator PostAsync(PromptPayload payload, Action<string, float> onComplete, Action onTimeout)
    {
        // If the model is currently being warmed up from Start Game, wait briefly for warm-up to finish
        if (isWarmingUp)
        {
            float waitStart = Time.realtimeSinceStartup;
            while (isWarmingUp && (Time.realtimeSinceStartup - waitStart) < coldStartTimeoutSeconds)
            {
                yield return null;
            }
        }

        if (IsBusy)
        {
            Debug.LogWarning("[OllamaAPIClient] A request is already in progress. Ignoring duplicate request.");
            yield break;
        }

        if (string.IsNullOrEmpty(resolvedModelName))
        {
            yield return StartCoroutine(EnsureOllamaAndModelRunningRoutine(modelName, endpointURL, coldStartTimeoutSeconds));
        }

        IsBusy = true;
        payload.model = !string.IsNullOrEmpty(resolvedModelName) ? resolvedModelName : modelName;
        payload.stream = false;
        string jsonBody = payload.ToJson();

        using (UnityWebRequest request = new UnityWebRequest(endpointURL, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            float activeTimeout = isModelWarm ? timeoutSeconds : Mathf.Max(timeoutSeconds, coldStartTimeoutSeconds);
            request.timeout = Mathf.RoundToInt(activeTimeout);

            float startTime = Time.realtimeSinceStartup;
            yield return request.SendWebRequest();
            IsBusy = false;

            if (request.result != UnityWebRequest.Result.Success)
            {
                bool isTimeout = request.responseCode == 0 ||
                                 (!string.IsNullOrEmpty(request.error) &&
                                  (request.error.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   request.error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0));

                if (isTimeout)
                {
                    Debug.LogWarning("[OllamaAPIClient] Request timed out. Executing UC-03 fallback.");
                    onTimeout?.Invoke();
                }
                else
                {
                    string errorBody = request.downloadHandler != null ? request.downloadHandler.text : "";
                    Debug.LogError($"[OllamaAPIClient] HTTP Error: {request.error} {errorBody}");
                    onTimeout?.Invoke();
                }
            }
            else
            {
                isModelWarm = true;
                APIResponse responseData = ParseResponse(request.downloadHandler.text);
                float tps = responseData != null ? responseData.GetTPS() : 0f;
                Debug.Log($"[Ollama] Generation Complete. TPS: {tps:F2} | Latency: {(Time.realtimeSinceStartup - startTime):F2}s");
                string replyText = responseData != null ? responseData.GetTextResponse() : "...";
                if (string.IsNullOrEmpty(replyText)) replyText = "...";
                onComplete?.Invoke(replyText, tps);
            }
        }
    }

    public APIResponse ParseResponse(string json)
    {
        try
        {
            return JsonUtility.FromJson<APIResponse>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OllamaAPIClient] JSON Parse Exception: {ex.Message}");
            return null;
        }
    }
}

