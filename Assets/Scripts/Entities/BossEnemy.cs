using UnityEngine;

/// Boss Enemy class based on Class Diagram Gambar 3.5.
/// Represents Ashen Fenrir with a two-phase fight and victory triggers.
public class BossEnemy : EnemyController
{
    [Header("Boss Specific Settings (Gambar 3.5)")]
    [SerializeField] private string bossName = "Ashen Fenrir";
    [SerializeField] private float phaseThreshold = 0.5f; // Enters phase 2 at 50% HP

    private bool isPhaseTwo = false;

    public string BossName => bossName;
    public bool IsPhaseTwo => isPhaseTwo;

    protected override void Awake()
    {
        base.Awake();
        // Default boss stats
        maxHP = 300;
        currentHP = maxHP;
        attackDamage = 25f;
        expReward = 500;
        respawns = false;
    }

    public override void TakeDamage(int amount)
    {
        if (isDead) return;

        base.TakeDamage(amount);

        if (!isDead && !isPhaseTwo && currentHP <= maxHP * phaseThreshold)
        {
            EnterPhaseTwo();
        }
    }

    public virtual void EnterPhaseTwo()
    {
        isPhaseTwo = true;
        moveSpeed *= 1.35f;
        attackDamage *= 1.5f;
        attackCooldown *= 0.75f;
        Debug.Log($"[BossEnemy] {bossName} has entered PHASE TWO! Increased rage, speed, and damage!");
    }

    public virtual void TriggerVictorySequence()
    {
        Debug.Log($"[BossEnemy] {bossName} has been slain! Triggering Victory Sequence.");

        // Notify ContextInjector of boss defeat for lore consistency
        if (ContextInjector.Instance != null)
        {
            ContextInjector.Instance.SetBossDefeated(true);
        }

        // Notify GameManager to trigger victory state and unlock village victory dialogue
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerVictory();
        }
    }

    public override void Die()
    {
        if (isDead) return;
        TriggerVictorySequence();
        base.Die();
    }
}

