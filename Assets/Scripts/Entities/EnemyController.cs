using System.Collections;
using UnityEngine;

/// <summary>
/// Enemy class based on Class Diagram Gambar 3.5.
/// Handles enemy health, player detection, melee attacks, and EXP distribution upon death.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EnemyController : MonoBehaviour
{
    [Header("Enemy Attributes (Gambar 3.5)")]
    [SerializeField] protected int maxHP = 50;
    [SerializeField] protected int currentHP;
    [SerializeField] protected float attackDamage = 10f;
    [SerializeField] protected int expReward = 30;

    [Header("Combat & Movement Settings")]
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected float detectionRange = 5f;
    [SerializeField] protected float attackRange = 1f;
    [SerializeField] protected float attackCooldown = 1.2f;

    [Header("Mob Respawn (GDD Scene 2)")]
    [SerializeField] protected bool respawns = true;
    [SerializeField] protected float respawnTime = 5f;

    protected Transform playerTransform;
    protected PlayerController playerController;
    protected float lastAttackTime = -99f;
    protected bool isDead = false;
    protected Vector3 initialPosition;

    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;
    public bool IsDead => isDead;

    protected virtual void Awake()
    {
        currentHP = maxHP;
        initialPosition = transform.position;
    }

    protected virtual void Start()
    {
        FindPlayer();
    }

    protected void FindPlayer()
    {
        PlayerController p = FindObjectOfType<PlayerController>();
        if (p != null)
        {
            playerController = p;
            playerTransform = p.transform;
        }
    }

    protected virtual void Update()
    {
        if (isDead) return;

        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        float distance = Vector2.Distance(transform.position, playerTransform.position);

        if (distance <= detectionRange)
        {
            if (distance > attackRange)
            {
                // Chase player
                Vector2 dir = (playerTransform.position - transform.position).normalized;
                transform.position += (Vector3)(dir * moveSpeed * Time.deltaTime);
            }
            else
            {
                // In attack range
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    Attack(playerController);
                }
            }
        }
    }

    public virtual void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHP -= amount;
        Debug.Log($"[{gameObject.name}] Took {amount} damage. HP: {currentHP}/{maxHP}");

        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
        }
    }

    public virtual void Attack(PlayerController target)
    {
        if (target == null || target.IsDead) return;

        lastAttackTime = Time.time;
        Debug.Log($"[{gameObject.name}] Attacking Player for {attackDamage} damage.");
        target.TakeDamage((int)attackDamage);
    }

    public virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"[{gameObject.name}] Defeated! Rewarding {expReward} EXP.");

        if (playerController != null)
        {
            playerController.GainExp(expReward);
        }

        if (respawns)
        {
            StartCoroutine(RespawnRoutine());
        }
        else
        {
            Destroy(gameObject, 0.2f);
        }
    }

    protected virtual IEnumerator RespawnRoutine()
    {
        // Hide and disable collider during respawn cooldown
        Collider2D col = GetComponent<Collider2D>();
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (col != null) col.enabled = false;
        if (sr != null) sr.enabled = false;

        yield return new WaitForSeconds(respawnTime);

        // Reset state & respawn
        transform.position = initialPosition;
        currentHP = maxHP;
        isDead = false;

        if (sr != null) sr.enabled = true;
        if (col != null) col.enabled = true;

        Debug.Log($"[{gameObject.name}] Respawned at initial position.");
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}

