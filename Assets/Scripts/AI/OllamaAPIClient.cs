using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class OllamaAPIClient : MonoBehaviour
{
    [Header("Ollama Server Configuration")]
    [SerializeField] private string endpointURL = "http://localhost:11434/api/generate";
    [SerializeField] private float timeoutSeconds = 5f; // Exception E-1 per UC-03
    [SerializeField] private float coldStartTimeoutSeconds = 15f;
    [SerializeField] private string modelName = "llama3.2:1b";

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

    private bool isModelWarm = false;

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

    public IEnumerator PostAsync(PromptPayload payload, Action<string, float> onComplete, Action onTimeout)
    {
        if (IsBusy)
        {
            Debug.LogWarning("[OllamaAPIClient] A request is already in progress. Ignoring duplicate request.");
            yield break;
        }

        IsBusy = true;
        payload.model = modelName;
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
                    Debug.LogError($"[OllamaAPIClient] HTTP Error: {request.error}");
                    onTimeout?.Invoke();
                }
            }
            else
            {
                isModelWarm = true;
                APIResponse responseData = ParseResponse(request.downloadHandler.text);
                float tps = responseData != null ? responseData.GetTPS() : 0f;
                Debug.Log($"[Ollama] Generation Complete. TPS: {tps:F2} | Latency: {(Time.realtimeSinceStartup - startTime):F2}s");
                string replyText = !string.IsNullOrEmpty(responseData?.response) ? responseData.response : "...";
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

