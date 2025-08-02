using UnityEngine;

public class GhostController : MonoBehaviour
{
    private PlayerController.PlayerAction[] recordedActions;
    private int currentActionIndex = 0;
    private float ghostStartTime;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private bool isReplaying = true;
    private bool allowPhysicsAfterFreeze = false;
    private bool isGrounded = false;
    private bool hasJumped = false;
    private float moveSpeed = 5f;
    private float jumpForce = 14f;

    // Block-pushing variables (copied from PlayerController)
    [SerializeField] private LayerMask pushableLayer;
    [SerializeField] private float pushDistance = 0.51f;

    private float blockPushCooldown = 0.2f;
    private float blockPushTimer = 0f;

    private Animator animator;


    [Header("Ghost Settings")]
    [SerializeField] private float ghostAlpha = 0.5f;
    [SerializeField] private Color ghostColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private LayerMask groundLayer = 1 | (1 << 8); // Default layer + Ground layer
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private string sortingLayerName = "Player";
    [SerializeField] private int sortingOrder = 0;

    private PhysicsMaterial2D originalMaterial;

    public void Initialize(PlayerController.PlayerAction[] actions, bool allowPhysicsAfterFreeze, float moveSpeed, float jumpForce, PhysicsMaterial2D playerMaterial)
    {
        recordedActions = actions;
        this.allowPhysicsAfterFreeze = allowPhysicsAfterFreeze;
        this.moveSpeed = moveSpeed;
        this.jumpForce = jumpForce;
        ghostStartTime = Time.time;
        currentActionIndex = 0;
        isReplaying = true;
        hasJumped = false;

        BoxCollider2D ghostCollider = GetComponent<BoxCollider2D>();
        BoxCollider2D playerCollider = FindObjectOfType<PlayerController>().GetComponent<BoxCollider2D>();

        if (ghostCollider != null && playerCollider != null)
        {
            ghostCollider.size = playerCollider.size;
            ghostCollider.offset = playerCollider.offset;
        }

        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Ghost prefab is missing an Animator component!");
        }

        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.drag = 0f;
        rb.mass = 1f;
        rb.angularDrag = 0.05f;
        rb.interpolation = RigidbodyInterpolation2D.None;
        rb.sleepMode = RigidbodySleepMode2D.StartAwake;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (playerMaterial != null)
        {
            rb.sharedMaterial = playerMaterial;
            originalMaterial = playerMaterial;
        }
        else
        {
            PhysicsMaterial2D zeroFrictionMaterial = Resources.Load<PhysicsMaterial2D>("ZeroFriction");
            if (zeroFrictionMaterial == null)
            {
                zeroFrictionMaterial = Resources.Load<PhysicsMaterial2D>("Scripts/Components/ZeroFriction");
            }
            if (zeroFrictionMaterial != null)
            {
                rb.sharedMaterial = zeroFrictionMaterial;
                originalMaterial = zeroFrictionMaterial;
            }
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = ghostColor;
            spriteRenderer.sortingLayerName = sortingLayerName;
            spriteRenderer.sortingOrder = sortingOrder;
        }

        if (ghostCollider != null)
        {
            ghostCollider.isTrigger = false;
            ghostCollider.density = 1f;
            ghostCollider.usedByEffector = false;
            ghostCollider.usedByComposite = false;
        }

        gameObject.layer = LayerMask.NameToLayer("Ghost");
        groundLayer = LayerMask.GetMask("Default", "Ground");
        pushableLayer = LayerMask.GetMask("Moveable Blocks");

        if (recordedActions.Length > 0)
        {
            transform.position = recordedActions[0].position;
        }
    }

    public void RestartReplay()
    {
        ghostStartTime = Time.time;
        currentActionIndex = 0;
        isReplaying = true;
        hasJumped = false;
        blockPushTimer = 0f; // Reset the timer

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.velocity = Vector2.zero;
        }

        BoxCollider2D ghostCollider = GetComponent<BoxCollider2D>();
        if (ghostCollider != null)
        {
            ghostCollider.isTrigger = false;
        }

        if (rb != null && originalMaterial != null)
        {
            rb.sharedMaterial = originalMaterial;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = ghostColor;
        }

        if (recordedActions.Length > 0)
        {
            transform.position = recordedActions[0].position;
        }
    }

    private void Update()
    {
        if (!isReplaying)
        {
            return;
        }

        CheckGrounded();

        if (isGrounded)
        {
            hasJumped = false;
        }

        if (recordedActions == null || recordedActions.Length == 0 || currentActionIndex >= recordedActions.Length)
        {
            FreezeGhost();
            return;
        }

        float currentTime = Time.time - ghostStartTime;
        PlayerController.PlayerAction currentAction = recordedActions[currentActionIndex];

        if (currentTime >= currentAction.timestamp)
        {
            ApplyAction(currentAction);
            currentActionIndex++;
        }

        UpdateVisualAppearance();

        // Always count down the timer
        if (blockPushTimer > 0f)
        {
            blockPushTimer -= Time.deltaTime;
        }
    }

    private void ApplyAction(PlayerController.PlayerAction action)
    {
        if (rb != null)
        {
            Vector2 velocity = rb.velocity;
            velocity.x = action.horizontalInput * moveSpeed;
            rb.velocity = velocity;

            if (action.jumpPressed && isGrounded && !hasJumped)
            {
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                hasJumped = true;
            }

            // --- GHOST BLOCK PUSHING LOGIC ---
            // Replicate the block pushing logic from PlayerController
            if (action.horizontalInput != 0 && blockPushTimer <= 0f)
            {
                Vector2 direction = new Vector2(Mathf.Sign(action.horizontalInput), 0);
                Vector2 boxOrigin = (Vector2)transform.position;
                Vector2 boxSize = new Vector2(0.3f, 0.9f);

                RaycastHit2D hit = Physics2D.BoxCast(
                    boxOrigin,
                    boxSize,
                    0f,
                    direction,
                    pushDistance,
                    pushableLayer
                );

                if (hit.collider != null)
                {
                    PushableBlock block = hit.collider.GetComponent<PushableBlock>();
                    if (block != null)
                    {
                        // A ghost should be able to push a block
                        if (block.TryMove(direction))
                        {
                            blockPushTimer = blockPushCooldown;
                        }
                    }
                }
            }

            if (animator != null)
            {
                animator.SetFloat("xVelocity", action.xVelocityAnim);
                animator.SetFloat("yVelocity", action.yVelocityAnim);
                animator.SetBool("isJumping", action.isJumpingAnim);
                animator.SetBool("isPushing", action.isPushingAnim);
            }
        }

        // --- GHOST SPRITE FLIPPING LOGIC ---
        // The player's sprite flipping logic checks horizontalInput. We do the same.
        if (action.horizontalInput > 0)
        {
            transform.localScale = new Vector3(1, transform.localScale.y, transform.localScale.z); // Face right
        }
        else if (action.horizontalInput < 0)
        {
            transform.localScale = new Vector3(-1, transform.localScale.y, transform.localScale.z); // Face left
        }
    }

    private void CheckGrounded()
    {
        // Get the ghost's collider to find the bottom position
        Collider2D ghostCollider = GetComponent<Collider2D>();
        if (ghostCollider == null) return;
        
        // Calculate the bottom center of the ghost
        Vector2 bottomCenter = (Vector2)transform.position + ghostCollider.offset;
        bottomCenter.y -= ghostCollider.bounds.extents.y;
        
        // Cast ray from bottom center downward
        RaycastHit2D hit = Physics2D.Raycast(bottomCenter, Vector2.down, groundCheckDistance, groundLayer);
        isGrounded = hit.collider != null;
        
        // Debug visualization
        Debug.DrawRay(bottomCenter, Vector2.down * groundCheckDistance, isGrounded ? Color.green : Color.red);
    }
    
    private void FreezeGhost()
    {
        isReplaying = false;
        
        // IMMEDIATELY stop all movement to prevent sliding
        if (rb != null)
        {
            rb.velocity = Vector2.zero; // Stop all movement immediately
            rb.isKinematic = true; // Make it kinematic so it doesn't move at all
        }
        
        if (allowPhysicsAfterFreeze)
        {
            // Let physics continue naturally (ghost will fall, bounce, etc.)
            // But first stop the current movement
            if (rb != null)
            {
                rb.isKinematic = false; // Make it dynamic again for physics
                rb.velocity = Vector2.zero; // But stop current movement
            }
            
            if (spriteRenderer != null)
            {
                Color frozenColor = spriteRenderer.color;
                frozenColor.a = ghostAlpha * 0.8f; // Slightly more transparent when frozen
                spriteRenderer.color = frozenColor;
            }
        }
        else
        {
            // Keep it kinematic to prevent any movement
            if (rb != null)
            {
                rb.isKinematic = true; // Make it kinematic so it doesn't fall
            }
        }
        
        // Always change to high friction material when frozen to prevent sliding
        if (rb != null)
        {
            // Create a high friction material for frozen ghosts
            PhysicsMaterial2D frozenMaterial = new PhysicsMaterial2D();
            frozenMaterial.name = "FrozenGhostMaterial";
            frozenMaterial.friction = 1f; // High friction to prevent sliding
            frozenMaterial.bounciness = 0f; // No bouncing
            rb.sharedMaterial = frozenMaterial;
        }
        
        // Make the ghost slightly more transparent when frozen
        if (spriteRenderer != null)
        {
            Color frozenColor = spriteRenderer.color;
            frozenColor.a = ghostAlpha * 0.7f; // Make it more transparent when frozen
            spriteRenderer.color = frozenColor;
        }
        
        // Optional: Add a subtle visual effect to indicate it's frozen
        // You could add a particle effect or change the color here
    }
    
    private void UpdateVisualAppearance()
    {
        if (spriteRenderer != null)
        {
            // Make ghost slightly transparent and add a subtle glow effect
            Color currentColor = spriteRenderer.color;
            currentColor.a = ghostAlpha;
            spriteRenderer.color = currentColor;
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ghosts can pass through objects but we can add visual effects here if needed
        // For example, we could add a particle effect when ghost passes through walls
    }
    
    private void OnDestroy()
    {
        // Clean up any resources if needed
    }
} 