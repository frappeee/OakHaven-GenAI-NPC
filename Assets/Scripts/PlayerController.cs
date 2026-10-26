using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 4f;
    private bool isMovementLocked = false;

    [Header("Interaction Settings")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    private NPCController nearbyNPC;

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
    }

    private void Update()
    {
        if (Input.GetKeyDown(interactKey) && nearbyNPC != null)
        {
            nearbyNPC.OnInteractKeyPressed();
        }

        if (isMovementLocked)
        {
            moveInput = Vector2.zero;
            animator.SetFloat("Speed", 0f);
            return;
        }

        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        moveInput = new Vector2(moveX, moveY).normalized;

        if (moveInput != Vector2.zero)
        {
            lastFacingDirection = moveInput;
        }

        animator.SetFloat("MoveX", lastFacingDirection.x);
        animator.SetFloat("MoveY", lastFacingDirection.y);
        animator.SetFloat("Speed", moveInput.sqrMagnitude);
    }

    private void FixedUpdate()
    {
        rb.velocity = moveInput * moveSpeed;
    }

    // Public method for NPCController / GameManager to lock movement during dialogue
    public void SetMovementLock(bool locked)
    {
        isMovementLocked = locked;
        if (locked)
        {
            moveInput = Vector2.zero;
            if (rb != null) rb.velocity = Vector2.zero;
            if (animator != null) animator.SetFloat("Speed", 0f);
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
}
