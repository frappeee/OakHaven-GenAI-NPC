using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField]
    private float moveSpeed = 4f;
    private bool isMovementLocked = false;

    [Header("Interaction Settings")]
    [SerializeField]
    private KeyCode interactKey = KeyCode.E;
    private NPCController nearbyNPC;

    [Header("Player Stats (Gambar 3.5 & GDD Section 3.3.2.3)")]
    [SerializeField]
    private int maxHP = 100;

    [SerializeField]
    private int currentHP;

    [SerializeField]
    private int level = 1;

    [SerializeField]
    private int experience = 0;

    [SerializeField]
    private float attackDamage = 25f;

    [Header("Combat Settings")]
    [SerializeField]
    private float attackCooldown = 0.35f;

    [SerializeField]
    private float attackRange = 0.8f;

    [SerializeField]
    private Transform attackPoint = null;

    [SerializeField]
    private LayerMask enemyLayers = default;
    private float lastAttackTime = -99f;
    private bool isDead = false;

    [Header("Level Up Feedback (GDD Section 3.3.2.3)")]
    [SerializeField]
    private ParticleSystem levelUpParticle = null;

    [SerializeField]
    private AudioSource audioSource = null;

    [SerializeField]
    private AudioClip levelUpAudio = null;

    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;
    public int Level => level;
    public int Experience => experience;
    public float AttackDamage => attackDamage;
    public int ExpToNextLevel => level * 100;
    public bool IsDead => isDead;

    public event Action<int, int> OnHealthChanged;
    public event Action<int, int> OnExpChanged;
    public event Action<int> OnLevelUpEvent;

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;
    private Vector2 lastFacingDirection = Vector2.down;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        currentHP = maxHP;

        // auto finds attackPoint if child exists and not assigned in inspector
        if (attackPoint == null)
        {
            Transform foundPoint = transform.Find("AttackPoint");
            if (foundPoint != null)
                attackPoint = foundPoint;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void Start()
    {
        // Sync initial state with ContextInjector & GameManager
        if (ContextInjector.Instance != null)
        {
            ContextInjector.Instance.UpdatePlayerLevel(level);
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPlayerLevel(level);
        }
        if (UIManager.Instance != null)
        {
            UIManager.Instance.BindPlayer(this);
        }

        OnHealthChanged?.Invoke(currentHP, maxHP);
        OnExpChanged?.Invoke(experience, ExpToNextLevel);
    }

    private void Update()
    {
        if (isDead)
            return;

        // NPC Interaction Trigger
        if (Input.GetKeyDown(interactKey) && nearbyNPC != null)
        {
            nearbyNPC.OnInteractKeyPressed();
        }

        // Combat Swing (Left Mouse Click per GDD Section 3.3.2.3)
        if (
            Input.GetMouseButtonDown(0)
            && !isMovementLocked
            && Time.time >= lastAttackTime + attackCooldown
        )
        {
            Attack();
        }

        if (isMovementLocked)
        {
            moveInput = Vector2.zero;
            animator.SetFloat("Speed", 0f);
            return;
        }

        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        Move(new Vector2(moveX, moveY));
    }

    private void FixedUpdate()
    {
        if (isDead)
        {
            rb.velocity = Vector2.zero;
            return;
        }
        rb.velocity = moveInput * moveSpeed;
    }

    /// <summary>
    /// Processes movement input (Gambar 3.5 Move method)
    /// </summary>
    public void Move(Vector2 direction)
    {
        moveInput = direction.normalized;

        if (moveInput != Vector2.zero)
        {
            lastFacingDirection = moveInput;
        }

        animator.SetFloat("MoveX", lastFacingDirection.x);
        animator.SetFloat("MoveY", lastFacingDirection.y);
        animator.SetFloat("Speed", moveInput.sqrMagnitude);
    }

    /// <summary>
    /// Performs melee attack swing using sword hitbox (Gambar 3.5 & GDD Section 3.3.2.3)
    /// </summary>
    public void Attack()
    {
        lastAttackTime = Time.time;

        // Trigger attack animation if available in controller
        animator.SetTrigger("Attack");

        // Determine center of attack hitbox
        Vector2 attackOrigin =
            attackPoint != null
                ? (Vector2)attackPoint.position
                : (Vector2)transform.position + (lastFacingDirection * 0.6f);

        // Detect all enemies within attack range
        Collider2D[] hits =
            enemyLayers != 0
                ? Physics2D.OverlapCircleAll(attackOrigin, attackRange, enemyLayers)
                : Physics2D.OverlapCircleAll(attackOrigin, attackRange);

        foreach (Collider2D hit in hits)
        {
            EnemyController enemy = hit.GetComponentInParent<EnemyController>();
            if (enemy != null && !enemy.IsDead)
            {
                Debug.Log($"[PlayerController] Hit {enemy.name} for {attackDamage} damage.");
                enemy.TakeDamage((int)attackDamage);
            }
        }
    }

    /// <summary>
    /// Reduces player health and evaluates death condition (Gambar 3.5)
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (isDead)
            return;

        currentHP = Mathf.Max(0, currentHP - amount);
        Debug.Log($"[PlayerController] Took {amount} damage. Current HP: {currentHP}/{maxHP}");
        OnHealthChanged?.Invoke(currentHP, maxHP);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealth(currentHP, maxHP);
        }

        if (currentHP <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Adds Experience points and triggers LevelUp when threshold is reached (Gambar 3.5)
    /// </summary>
    public void GainExp(int amount)
    {
        if (isDead)
            return;

        experience += amount;
        Debug.Log($"[PlayerController] Gained {amount} EXP. Total: {experience}/{ExpToNextLevel}");

        while (experience >= ExpToNextLevel)
        {
            LevelUp();
        }

        OnExpChanged?.Invoke(experience, ExpToNextLevel);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateExperience(experience, ExpToNextLevel);
        }
    }

    /// <summary>
    /// Levels up the character: increases max HP (+20), damage (+10), updates ContextInjector and GameManager.
    /// </summary>
    public void LevelUp()
    {
        experience -= ExpToNextLevel;
        level++;
        maxHP += 20; // GDD Section 3.3.2.3 (+20 HP per level)
        currentHP = maxHP; // Full heal upon level up
        attackDamage += 10f; // Attacks become visibly stronger

        Debug.Log(
            $"[PlayerController] LEVEL UP! Now Level {level}! Max HP: {maxHP}, Attack: {attackDamage}"
        );

        // Feedback: Bright particle effect & audio chime (GDD Section 3.3.2.3)
        if (levelUpParticle != null)
        {
            levelUpParticle.Play();
        }
        if (audioSource != null && levelUpAudio != null)
        {
            audioSource.PlayOneShot(levelUpAudio);
        }

        // Sync with AI ContextInjector & GameManager
        if (ContextInjector.Instance != null)
        {
            ContextInjector.Instance.UpdatePlayerLevel(level);
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPlayerLevel(level);
        }

        OnHealthChanged?.Invoke(currentHP, maxHP);
        OnLevelUpEvent?.Invoke(level);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealth(currentHP, maxHP);
            UIManager.Instance.UpdateLevel(level);
            UIManager.Instance.UpdateExperience(experience, ExpToNextLevel);
        }
    }

    public void Die()
    {
        if (isDead)
            return;
        isDead = true;

        Debug.LogWarning("[PlayerController] Player has died!");
        SetMovementLock(true);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver();
        }
    }

    // Public method for NPCController / GameManager to lock movement during dialogue
    public void SetMovementLock(bool locked)
    {
        isMovementLocked = locked;
        if (locked)
        {
            moveInput = Vector2.zero;
            if (rb != null)
                rb.velocity = Vector2.zero;
            if (animator != null)
                animator.SetFloat("Speed", 0f);
        }
    }

    public void SetNearbyNPC(NPCController npc)
    {
        nearbyNPC = npc;
    }

    public void ClearNearbyNPC(NPCController npc)
    {
        if (nearbyNPC == npc)
        {
            nearbyNPC = null;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        NPCController npc = other.GetComponentInParent<NPCController>();
        if (npc != null)
        {
            nearbyNPC = npc;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        NPCController npc = other.GetComponentInParent<NPCController>();
        if (npc != null && nearbyNPC == npc)
        {
            nearbyNPC = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 origin =
            attackPoint != null
                ? (Vector2)attackPoint.position
                : (Vector2)transform.position + (lastFacingDirection * 0.6f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin, attackRange);
    }
}
