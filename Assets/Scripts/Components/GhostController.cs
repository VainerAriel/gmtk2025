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
    
    [Header("Electricity Transformation")]
    [SerializeField] private bool hasBeenHitByElectricity = false;
    
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
        hasBeenHitByElectricity = false; // Reset electricity hit state
        
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
        hasBeenHitByElectricity = false; // Reset electricity hit state
        
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
        
        // Debug: Press T to test transformation
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log($"[GhostController] Manual test transformation triggered by T key");
            TransformIntoReflector(transform.position);
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
        
        // Flip character sprite based on movement direction (like the player does)
        if (action.horizontalInput > 0)
        {
            transform.localScale = new Vector3(1, transform.localScale.y, transform.localScale.z); // Face right
        }
        else if (action.horizontalInput < 0)
        {
            transform.localScale = new Vector3(-1, transform.localScale.y, transform.localScale.z); // Face left
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
        Debug.Log($"[GhostController] Trigger entered with: {other.name}, layer: {LayerMask.LayerToName(other.gameObject.layer)}");
        
        // Check if hit by electricity projectile
        Projectile projectile = other.GetComponent<Projectile>();
        if (projectile != null && !hasBeenHitByElectricity)
        {
            Debug.Log($"[GhostController] Hit by projectile! Transforming into reflector...");
            hasBeenHitByElectricity = true;
            TransformIntoReflector(projectile.transform.position);
        }
        else if (projectile == null)
        {
            Debug.Log($"[GhostController] Hit by something without Projectile component: {other.name}");
        }
        else if (hasBeenHitByElectricity)
        {
            Debug.Log($"[GhostController] Already been hit by electricity, ignoring");
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"[GhostController] Collision with: {collision.gameObject.name}, layer: {LayerMask.LayerToName(collision.gameObject.layer)}");
        
        // Check if hit by electricity projectile
        Projectile projectile = collision.gameObject.GetComponent<Projectile>();
        if (projectile != null && !hasBeenHitByElectricity)
        {
            Debug.Log($"[GhostController] Hit by projectile via collision! Transforming into reflector...");
            hasBeenHitByElectricity = true;
            TransformIntoReflector(projectile.transform.position);
        }
    }
    
    private void TransformIntoReflector(Vector3 bulletPosition)
    {
        Debug.Log($"[GhostController] TransformIntoReflector called with bullet position: {bulletPosition}");
        
        // Check if ReflectorManager exists
        if (ReflectorManager.Instance == null)
        {
            Debug.LogError($"[GhostController] ReflectorManager.Instance is null! Make sure ReflectorManager exists in the scene.");
            return;
        }
        
        // Determine which reflector to spawn based on ghost state
        GameObject reflectorPrefab = DetermineReflectorType();
        
        if (reflectorPrefab != null)
        {
            // Calculate the center of the bottom tile of the ghost (player is 2 tiles high)
            Vector3 bottomTileCenter = CalculateBottomTileCenter();
            
            // Snap to nearest grid tile (assuming 1 unit grid size)
            Vector3 snappedPosition = SnapToGrid(bottomTileCenter);
            
            Debug.Log($"[GhostController] Creating reflector: {reflectorPrefab.name} at bottom tile center {bottomTileCenter} -> snapped to {snappedPosition}");
            
            // Create the reflector at the snapped grid position
            GameObject reflector = Instantiate(reflectorPrefab, snappedPosition, transform.rotation);
            
            // Destroy the ghost
            Destroy(gameObject);
            
            Debug.Log($"[GhostController] Ghost transformed into reflector: {reflectorPrefab.name} at grid position {snappedPosition}");
        }
        else
        {
            Debug.LogError($"[GhostController] reflectorPrefab is null! Check ReflectorManager prefab assignments.");
        }
    }
    
    /// <summary>
    /// Calculates the center of the bottom tile of the ghost (player is 2 tiles high)
    /// </summary>
    /// <returns>The center position of the bottom tile</returns>
    private Vector3 CalculateBottomTileCenter()
    {
        // Get the ghost's collider to find the bottom position
        Collider2D ghostCollider = GetComponent<Collider2D>();
        if (ghostCollider == null)
        {
            Debug.LogError($"[GhostController] No collider found on ghost, using transform position");
            return transform.position;
        }
        
        // Calculate the bottom center of the ghost (same logic as CheckGrounded)
        Vector2 bottomCenter = (Vector2)transform.position + ghostCollider.offset;
        bottomCenter.y -= ghostCollider.bounds.extents.y;
        
        // Since the player is 2 tiles high, the bottom tile center is at the bottom of the ghost
        // We need to move up by half a tile (0.5 units) to get to the center of the bottom tile
        Vector3 bottomTileCenter = new Vector3(bottomCenter.x, bottomCenter.y + 0.5f, transform.position.z);
        
        Debug.Log($"[GhostController] Ghost position: {transform.position}, Bottom center: {bottomCenter}, Bottom tile center: {bottomTileCenter}");
        
        return bottomTileCenter;
    }
    
    /// <summary>
    /// Snaps a world position to the nearest grid tile center
    /// </summary>
    /// <param name="worldPosition">The world position to snap</param>
    /// <param name="gridSize">The size of each grid tile (default 1.0)</param>
    /// <returns>The snapped grid position at tile center</returns>
    private Vector3 SnapToGrid(Vector3 worldPosition, float gridSize = 1.0f)
    {
        // Round to the nearest grid tile center (not corner)
        float snappedX = Mathf.Round(worldPosition.x + 0.5f) - 0.5f;
        float snappedY = Mathf.Round(worldPosition.y + 0.5f) - 0.5f;
        
        return new Vector3(snappedX, snappedY, worldPosition.z);
    }
    
    private GameObject DetermineReflectorType()
    {
        // Get the ghost's current state
        bool isFacingRight = transform.localScale.x > 0;
        bool isGrounded = CheckGroundedState();
        
        Debug.Log($"[GhostController] Determining reflector type - FacingRight: {isFacingRight}, IsGrounded: {isGrounded}");
        
        // Determine reflector type based on direction and ground state
        if (isFacingRight)
        {
            GameObject prefab = isGrounded ? ReflectorManager.Instance.RightUpReflectPrefab : ReflectorManager.Instance.RightDownReflectPrefab;
            Debug.Log($"[GhostController] Right-facing ghost, grounded: {isGrounded}, selected prefab: {(prefab != null ? prefab.name : "NULL")}");
            return prefab;
        }
        else
        {
            GameObject prefab = isGrounded ? ReflectorManager.Instance.LeftUpReflectPrefab : ReflectorManager.Instance.LeftDownReflectPrefab;
            Debug.Log($"[GhostController] Left-facing ghost, grounded: {isGrounded}, selected prefab: {(prefab != null ? prefab.name : "NULL")}");
            return prefab;
        }
    }
    
    private bool CheckGroundedState()
    {
        // Use the same ground check logic as the player
        Collider2D ghostCollider = GetComponent<Collider2D>();
        if (ghostCollider == null) return false;
        
        Vector2 bottomCenter = (Vector2)transform.position + ghostCollider.offset;
        bottomCenter.y -= ghostCollider.bounds.extents.y;
        
        RaycastHit2D hit = Physics2D.Raycast(bottomCenter, Vector2.down, groundCheckDistance, groundLayer);
        return hit.collider != null;
    }
    
    private void OnDestroy()
    {
        // Clean up any resources if needed
    }
    
    /// <summary>
    /// Manually trigger transformation into reflector (for testing)
    /// </summary>
    public void ManualTransformIntoReflector()
    {
        if (!hasBeenHitByElectricity)
        {
            hasBeenHitByElectricity = true;
            TransformIntoReflector(transform.position);
        }
    }
} 