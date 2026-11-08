using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private static UIManager instance;
    public static UIManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<UIManager>(true);
            }
            return instance;
        }
        private set => instance = value;
    }

    [Header("Player Reference")]
    [SerializeField]
    private PlayerController player;

    [Header("Player HUD Elements")]
    [SerializeField]
    private Image hpFillImage;

    [SerializeField]
    private Image expFillImage;

    [SerializeField]
    private TextMeshProUGUI levelText;

    [SerializeField]
    private TextMeshProUGUI hpText; // numeric display for hp(optional)

    private bool isBound = false;

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

        AutoFindUIReferences();
    }

    private void Start()
    {
        if (player == null)
        {
            player = FindObjectOfType<PlayerController>();
        }

        if (player != null)
        {
            BindPlayer(player);
        }
    }

    private void OnDestroy()
    {
        UnbindPlayer();
    }

    private void AutoFindUIReferences()
    {
        if (hpFillImage == null)
        {
            GameObject hpBarObj = GameObject.Find("Hp_Bar_Fill") ?? GameObject.Find("HP_Bar_Fill") ?? GameObject.Find("HP_Bar");
            if (hpBarObj != null)
                hpFillImage = hpBarObj.GetComponent<Image>();
        }

        if (expFillImage == null)
        {
            GameObject expBarObj = GameObject.Find("Exp_Bar_Fill") ?? GameObject.Find("EXP_Bar_Fill") ?? GameObject.Find("EXP_Bar");
            if (expBarObj != null)
                expFillImage = expBarObj.GetComponent<Image>();
        }

        if (levelText == null)
        {
            GameObject levelTextObj = GameObject.Find("LevelText");
            if (levelTextObj != null)
                levelText = levelTextObj.GetComponent<TextMeshProUGUI>();
        }
    }

    public void BindPlayer(PlayerController targetPlayer)
    {
        if (targetPlayer == null)
            return;

        UnbindPlayer();

        player = targetPlayer;
        player.OnHealthChanged += UpdateHealth;
        player.OnExpChanged += UpdateExperience;
        player.OnLevelUpEvent += UpdateLevel;
        isBound = true;

        InitializeHUD(
            player.MaxHP,
            player.CurrentHP,
            player.ExpToNextLevel,
            player.Experience,
            player.Level
        );
    }

    private void UnbindPlayer()
    {
        if (player != null && isBound)
        {
            player.OnHealthChanged -= UpdateHealth;
            player.OnExpChanged -= UpdateExperience;
            player.OnLevelUpEvent -= UpdateLevel;
            isBound = false;
        }
    }

    public void InitializeHUD(int maxHP, int currentHP, int maxEXP, int currentEXP, int level)
    {
        UpdateHealth(currentHP, maxHP);
        UpdateExperience(currentEXP, maxEXP);
        UpdateLevel(level);
    }

    public void UpdateHealth(int currentHP, int maxHP)
    {
        if (hpFillImage != null)
        {
            hpFillImage.fillAmount = maxHP > 0 ? Mathf.Clamp01((float)currentHP / maxHP) : 0f;
        }

        if (hpText != null)
        {
            hpText.text = $"{Mathf.Max(0, currentHP)} / {maxHP}";
        }
    }

    public void UpdateExperience(int currentEXP, int maxEXP)
    {
        if (expFillImage != null)
        {
            expFillImage.fillAmount = maxEXP > 0 ? Mathf.Clamp01((float)currentEXP / maxEXP) : 0f;
        }
    }

    public void UpdateLevel(int level)
    {
        if (levelText != null)
        {
            levelText.text = $"LVL {level}";
        }
    }
}
