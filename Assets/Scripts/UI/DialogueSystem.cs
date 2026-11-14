using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueSystem : MonoBehaviour
{
#pragma warning disable 0649
    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TextMeshProUGUI npcNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject thinkingIndicator;
#pragma warning restore 0649

    [Header("Dialogue Settings")]
    [SerializeField] private float textScrollSpeed = 0.03f;

    private static DialogueSystem instance;
    public static DialogueSystem Instance
    {
        get
        {
            if (instance == null)
            {
                // Finds DialogueSystem even if DialogueCanvas starts inactive in the Hierarchy
                instance = FindObjectOfType<DialogueSystem>(true);
            }
            return instance;
        }
    }

    public bool IsActive => dialoguePanel != null && dialoguePanel.activeInHierarchy;

    private Coroutine typewriterCoroutine;

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

        if (PlayerPrefs.HasKey(MainMenuManager.PREF_TEXT_SPEED))
        {
            textScrollSpeed = PlayerPrefs.GetFloat(MainMenuManager.PREF_TEXT_SPEED, textScrollSpeed);
        }
    }

    public void SetTextScrollSpeed(float speed)
    {
        textScrollSpeed = Mathf.Clamp(speed, 0.005f, 0.2f);
    }

    public void ShowThinking(string npcName, Sprite portrait = null)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        StopTypewriter();
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (npcNameText != null) npcNameText.text = npcName;
        if (portraitImage != null)
        {
            portraitImage.sprite = portrait;
            portraitImage.gameObject.SetActive(portrait != null);
        }
        if (dialogueText != null) dialogueText.text = "";
        if (thinkingIndicator != null) thinkingIndicator.SetActive(true);
    }

    public void DisplayResponse(string text)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (thinkingIndicator != null) thinkingIndicator.SetActive(false);
        StopTypewriter();
        typewriterCoroutine = StartCoroutine(StreamText(text));
    }

    public void ShowFallback(string fallbackMessage = "Forgive me, adventurer. My mind is elsewhere... (Connection timeout)")
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        StopTypewriter();
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (thinkingIndicator != null) thinkingIndicator.SetActive(false);
        if (dialogueText != null) dialogueText.text = fallbackMessage;
    }

    public void Hide()
    {
        StopTypewriter();
        if (thinkingIndicator != null) thinkingIndicator.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (dialogueText != null) dialogueText.text = "";
    }

    private void StopTypewriter()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
    }

    private IEnumerator StreamText(string text)
    {
        if (dialogueText != null) dialogueText.text = "";
        if (string.IsNullOrEmpty(text))
        {
            typewriterCoroutine = null;
            yield break;
        }

        foreach (char c in text.ToCharArray())
        {
            if (dialogueText != null) dialogueText.text += c;
            yield return new WaitForSeconds(textScrollSpeed);
        }

        typewriterCoroutine = null;
    }
}
