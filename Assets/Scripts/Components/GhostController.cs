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
    
    [Header("Ghost Settings")]
    [SerializeField] private float ghostAlpha = 0.5f;
    [SerializeField] private Color ghostColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private LayerMask groundLayer = 1 | (1 << 8); // Default layer + Ground layer
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private string sortingLayerName = "Player"; // Sorting layer for visual rendering
    [SerializeField] private int sortingOrder = 0; // Order within the sorting layer
    
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
        
        // Get or add required components
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        // Configure rigidbody for ghost behavior - EXACTLY like the player
        rb.gravityScale = 3f; // Same as player
        rb.freezeRotation = true;
        rb.drag = 0f; // No drag to match player
        rb.mass = 1f; // Same mass as player
        rb.angularDrag = 0.05f; // Same as player
        rb.interpolation = RigidbodyInterpolation2D.None; // Same as player
        rb.sleepMode = RigidbodySleepMode2D.StartAwake; // Same as player
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Same as player
        
        // Use the same physics material as the player
        if (playerMaterial != null)
        {
            rb.sharedMaterial = playerMaterial;
            originalMaterial = playerMaterial; // Store the original material
        }
        else
        {
            // Fallback: Try to load the ZeroFriction material directly
            PhysicsMaterial2D zeroFrictionMaterial = Resources.Load<PhysicsMaterial2D>("ZeroFriction");
            if (zeroFrictionMaterial == null)
            {
                // If not in Resources, try to load from the Components folder
                zeroFrictionMaterial = Resources.Load<PhysicsMaterial2D>("Scripts/Components/ZeroFriction");
            }
            if (zeroFrictionMaterial != null)
            {
                rb.sharedMaterial = zeroFrictionMaterial;
                originalMaterial = zeroFrictionMaterial;
            }
        }
        
        // Set up visual appearance
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = ghostColor;
            spriteRenderer.sortingLayerName = sortingLayerName;
            spriteRenderer.sortingOrder = sortingOrder;
        }
        
        // Set up collider for physics mode - EXACTLY like the player
        BoxCollider2D ghostCollider = GetComponent<BoxCollider2D>();
        if (ghostCollider != null)
        {
            // Make it solid so it can interact with the real world
            ghostCollider.isTrigger = false;
            ghostCollider.density = 1f; // Same as player
            ghostCollider.usedByEffector = false; // Same as player
            ghostCollider.usedByComposite = false; // Same as player
            ghostCollider.offset = Vector2.zero; // Same as player
        }
        
        // Ensure ghost is on Ghost layer
        gameObject.layer = LayerMask.NameToLayer("Ghost");
        
        // Set ground layer mask to detect Default and Ground layers
        groundLayer = LayerMask.GetMask("Default", "Ground");
        
        // Set initial position
        if (recordedActions.Length > 0)
        {
            transform.position = recordedActions[0].position;
        }
    }
    
    public void RestartReplay()
    {
        // Reset replay state
        ghostStartTime = Time.time;
        currentActionIndex = 0;
        isReplaying = true;
        hasJumped = false;
        
        // Reset rigidbody
        if (rb != null)
        {
            rb.isKinematic = false; // Make it dynamic again
            rb.velocity = Vector2.zero;
        }
        
        // Reset collider to trigger mode for replay
        BoxCollider2D ghostCollider = GetComponent<BoxCollider2D>();
        if (ghostCollider != null)
        {
            ghostCollider.isTrigger = false; // Keep solid for real world interaction
        }
        
        // Restore original physics material
        if (rb != null && originalMaterial != null)
        {
            rb.sharedMaterial = originalMaterial;
        }
        
        // Reset visual appearance
        if (spriteRenderer != null)
        {
            spriteRenderer.color = ghostColor;
        }
        
        // Set to initial position
        if (recordedActions.Length > 0)
        {
            transform.position = recordedActions[0].position;
        }
    }
    
    private void Update()
    {
        if (!isReplaying)
        {
            // Ghost has finished replaying, just stay frozen
            return;
        }
        
        // Check if grounded
        CheckGrounded();
        
        // Reset jump when grounded
        if (isGrounded)
        {
            hasJumped = false;
        }
        
        if (recordedActions == null || recordedActions.Length == 0 || currentActionIndex >= recordedActions.Length)
        {
            // Ghost has finished replaying, freeze in place
            FreezeGhost();
            return;
        }
        
        float currentTime = Time.time - ghostStartTime;
        PlayerController.PlayerAction currentAction = recordedActions[currentActionIndex];
        
        // Check if it's time for the next action
        if (currentTime >= currentAction.timestamp)
        {
            // Apply the recorded action
            ApplyAction(currentAction);
            currentActionIndex++;
        }
        
        // Update visual appearance
        UpdateVisualAppearance();
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
    
    private void ApplyAction(PlayerController.PlayerAction action)
    {
        // Use velocity-based movement instead of forces for better control
        if (rb != null)
        {
            // Set horizontal velocity directly (like the player does)
            Vector2 velocity = rb.velocity;
            velocity.x = action.horizontalInput * moveSpeed; // Same speed as player
            rb.velocity = velocity;
            
            // Handle jump if it was pressed and ghost is actually grounded (not just recorded as grounded)
            if (action.jumpPressed && isGrounded && !hasJumped)
            {
                // Apply jump force (same as player)
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                hasJumped = true;
            }
        }
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