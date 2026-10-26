using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueSystem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TextMeshProUGUI npcNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject thinkingIndicator;

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
    }

    public void ShowThinking(string npcName, Sprite portrait = null)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        StopTypewriter();
        dialoguePanel.SetActive(true);
        npcNameText.text = npcName;
        if (portraitImage != null)
        {
            portraitImage.sprite = portrait;
            portraitImage.gameObject.SetActive(portrait != null);
        }
        dialogueText.text = "";
        thinkingIndicator.SetActive(true);
    }

    public void DisplayResponse(string text)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        thinkingIndicator.SetActive(false);
        StopTypewriter();
        typewriterCoroutine = StartCoroutine(StreamText(text));
    }

    public void ShowFallback(string fallbackMessage = "Forgive me, adventurer. My mind is elsewhere... (Connection timeout)")
    {
        StopTypewriter();
        thinkingIndicator.SetActive(false);
        dialogueText.text = fallbackMessage;
    }

    public void Hide()
    {
        StopTypewriter();
        dialoguePanel.SetActive(false);
        dialogueText.text = "";
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
        dialogueText.text = "";
        if (string.IsNullOrEmpty(text))
        {
            typewriterCoroutine = null;
            yield break;
        }

        foreach (char c in text.ToCharArray())
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(textScrollSpeed);
        }

        typewriterCoroutine = null;
    }
}
