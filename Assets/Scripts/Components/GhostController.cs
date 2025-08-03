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
    [SerializeField] private LayerMask groundLayer = 256; // Same as player - Layer 8 (Ground)
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private string sortingLayerName = "Player"; // Sorting layer for visual rendering
    [SerializeField] private int sortingOrder = 0; // Order within the sorting layer
    
    private PhysicsMaterial2D originalMaterial;
    
    [Header("Transformation")]
    [SerializeField] private bool hasBeenHitByElectricity = false;
    [SerializeField] private bool hasBeenHitBySpike = false;
    [SerializeField] private GameObject fallingGroundPrefab; // Fallback reference to the FallingGround prefab
    private Vector2 lastBulletDirection = Vector2.right; // Store the last bullet direction for reflector logic
    
    public void Initialize(PlayerController.PlayerAction[] actions, bool allowPhysicsAfterFreeze, float moveSpeed, float jumpForce, PhysicsMaterial2D playerMaterial)
    {
        recordedActions = actions;
        this.allowPhysicsAfterFreeze = allowPhysicsAfterFreeze;
        this.moveSpeed = moveSpeed;
        this.jumpForce = jumpForce;
        // FIXED: Use the same time reference as the player's original recording session
        // The ghost should start replaying at the same time the player started recording
        // So we calculate the original game start time by subtracting the last action's timestamp from current time
        if (actions != null && actions.Length > 0)
        {
            float lastActionTime = actions[actions.Length - 1].timestamp;
            ghostStartTime = Time.time - lastActionTime;
            Debug.Log($"[GhostController] Time sync: current time={Time.time}, last action time={lastActionTime}, ghost start time={ghostStartTime}");
        }
        else
        {
            ghostStartTime = Time.time;
        }
        currentActionIndex = 0;
        isReplaying = true;
        hasJumped = false;
        hasBeenHitByElectricity = false; // Reset electricity hit state
        hasBeenHitBySpike = false; // Reset spike hit state
        
        // Debug initialization
        Debug.Log($"[GhostController] Initialized with {actions?.Length ?? 0} actions, moveSpeed={moveSpeed}, jumpForce={jumpForce}");
        if (actions != null && actions.Length > 0)
        {
            int jumpCount = 0;
            foreach (var action in actions)
            {
                if (action.jumpPressed) jumpCount++;
            }
            Debug.Log($"[GhostController] Recorded actions contain {jumpCount} jump actions");
        }
        BoxCollider2D ghostCollider = GetComponent<BoxCollider2D>();
        BoxCollider2D playerCollider = FindObjectOfType<PlayerController>().GetComponent<BoxCollider2D>();

        if (ghostCollider != null && playerCollider != null)
        {
            ghostCollider.size = playerCollider.size;
            ghostCollider.offset = playerCollider.offset;
        }

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
        rb.bodyType = RigidbodyType2D.Dynamic; // Ensure same body type as player
        
        // Debug rigidbody setup
        Debug.Log($"[GhostController] Rigidbody setup: gravityScale={rb.gravityScale}, mass={rb.mass}, bodyType={rb.bodyType}, isKinematic={rb.isKinematic}");
        
        // Check if rigidbody is kinematic (this would prevent jumping)
        if (rb.isKinematic)
        {
            Debug.LogError($"[GhostController] WARNING: Rigidbody is kinematic! This will prevent jumping!");
        }
        
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
        
        if (ghostCollider != null)
        {
            // Make it solid so it can interact with the real world
            ghostCollider.isTrigger = false;
            ghostCollider.density = 1f; // Same as player
            ghostCollider.usedByEffector = false; // Same as player
            ghostCollider.usedByComposite = false; // Same as player
            ghostCollider.sharedMaterial = null; // Same as player (no shared material on collider)
            
            // Debug collider setup
            Debug.Log($"[GhostController] Collider setup: isTrigger={ghostCollider.isTrigger}, size={ghostCollider.size}, offset={ghostCollider.offset}");
        }
        else
        {
            Debug.LogError($"[GhostController] No BoxCollider2D found on ghost!");
        }
        
        // Add a separate trigger collider for spike detection
        CircleCollider2D triggerCollider = gameObject.GetComponent<CircleCollider2D>();
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        triggerCollider.isTrigger = true;
        triggerCollider.radius = 0.6f; // Slightly larger than the ghost
        triggerCollider.offset = Vector2.zero;
        
        // Ensure ghost is on Ghost layer (layer 7)
        gameObject.layer = 7;
        Debug.Log($"[GhostController] Set to Ghost layer: {gameObject.layer}");
        
        // Ground layer is now set correctly in the inspector field
        
        // Set initial position
        if (recordedActions.Length > 0)
        {
            transform.position = recordedActions[0].position;
            Debug.Log($"[GhostController] Set initial position to: {transform.position}");
            
            // Compare with player position
            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                Debug.Log($"[GhostController] Player position: {player.transform.position}, Ghost position: {transform.position}");
            }
        }
    }
    
    public void RestartReplay()
    {
        // Reset replay state
        // FIXED: Use the same time reference as the original recording session
        if (recordedActions != null && recordedActions.Length > 0)
        {
            float lastActionTime = recordedActions[recordedActions.Length - 1].timestamp;
            ghostStartTime = Time.time - lastActionTime;
            Debug.Log($"[GhostController] Restart replay: current time={Time.time}, last action time={lastActionTime}, ghost start time={ghostStartTime}");
        }
        else
        {
            ghostStartTime = Time.time;
        }
        currentActionIndex = 0;
        isReplaying = true;
        hasJumped = false;
        hasBeenHitByElectricity = false; // Reset electricity hit state
        hasBeenHitBySpike = false; // Reset spike hit state
        
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
            Debug.Log($"[GhostController] Reset jump - now grounded");
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
            Debug.Log($"[GhostController] Applying action at time {currentTime}: jumpPressed={currentAction.jumpPressed}, horizontalInput={currentAction.horizontalInput}");
            ApplyAction(currentAction);
            currentActionIndex++;
        }
        
        // Update visual appearance
        UpdateVisualAppearance();
        
        // Debug: Track velocity to see if jump force is being applied
        if (rb != null && rb.velocity.y > 0)
        {
            Debug.Log($"[GhostController] Current velocity: {rb.velocity}");
        }
        
        // Debug: Press T to test transformation
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log($"[GhostController] Manual test transformation triggered by T key");
            TransformIntoReflector(transform.position, Vector2.right); // Default to right direction for testing
        }
        
        // Debug: Press Y to test spike transformation
        if (Input.GetKeyDown(KeyCode.Y))
        {
            Debug.Log($"[GhostController] Manual test spike transformation triggered by Y key");
            TransformIntoFallingGround();
        }
        
        // Debug: Press U to test spike transformation with state check
        if (Input.GetKeyDown(KeyCode.U))
        {
            Debug.Log($"[GhostController] Manual test spike transformation with state check triggered by U key");
            if (!hasBeenHitBySpike)
            {
                hasBeenHitBySpike = true;
                TransformIntoFallingGround();
            }
            else
            {
                Debug.Log($"[GhostController] Already hit by spike, resetting state and trying again");
                hasBeenHitBySpike = false;
                TransformIntoFallingGround();
            }
        }
    }
    
    private void CheckGrounded()
    {
        // Get the ghost's collider to find the bottom position - EXACTLY like the player
        Collider2D ghostCollider = GetComponent<Collider2D>();
        if (ghostCollider == null) return;
        
        // Calculate the bottom center of the ghost - EXACTLY like the player
        Vector2 bottomCenter = (Vector2)transform.position + ghostCollider.offset;
        bottomCenter.y -= ghostCollider.bounds.extents.y;
        
        // Cast ray from bottom center downward - EXACTLY like the player
        RaycastHit2D hit = Physics2D.Raycast(bottomCenter, Vector2.down, groundCheckDistance, groundLayer);
        isGrounded = hit.collider != null;
        

        
        // Debug visualization - EXACTLY like the player
        Debug.DrawRay(bottomCenter, Vector2.down * groundCheckDistance, isGrounded ? Color.green : Color.red);
        
        // Debug ground detection
        Debug.Log($"[GhostController] Ground check: bottomCenter={bottomCenter}, isGrounded={isGrounded}, hit.collider={(hit.collider != null ? hit.collider.name : "NULL")}, layer={hit.collider?.gameObject.layer ?? 0}, groundLayer.value={groundLayer.value}, groundCheckDistance={groundCheckDistance}");
        
        // If not grounded, let's check what's actually there
        if (!isGrounded)
        {
            // Cast a longer ray to see what's below
            RaycastHit2D[] allHits = Physics2D.RaycastAll(bottomCenter, Vector2.down, 2f);
            Debug.Log($"[GhostController] No ground detected. All objects below: {allHits.Length} hits");
            foreach (var rayHit in allHits)
            {
                Debug.Log($"[GhostController] Hit: {rayHit.collider.name} on layer {rayHit.collider.gameObject.layer} at distance {rayHit.distance}");
            }
        }
    }
    
    private void ApplyAction(PlayerController.PlayerAction action)
    {
        // Use velocity-based movement - EXACTLY like the player does
        if (rb != null)
        {
            // Set horizontal velocity directly - EXACTLY like the player does
            Vector2 velocity = rb.velocity;
            velocity.x = action.horizontalInput * moveSpeed; // Same speed as player
            rb.velocity = velocity;
            
            // Handle jump if it was pressed and ghost is actually grounded - EXACTLY like the player
            Debug.Log($"[GhostController] Checking jump: action.jumpPressed={action.jumpPressed}, isGrounded={isGrounded}, hasJumped={hasJumped}, rb.isKinematic={rb.isKinematic}");
            if (action.jumpPressed && isGrounded && !hasJumped)
            {
                // Apply jump force - EXACTLY like the player
                Debug.Log($"[GhostController] JUMPING! action.jumpPressed: {action.jumpPressed}, isGrounded: {isGrounded}, hasJumped: {hasJumped}");
                if (rb.isKinematic)
                {
                    Debug.LogError($"[GhostController] ERROR: Cannot jump - rigidbody is kinematic!");
                }
                else
                {
                    Debug.Log($"[GhostController] Applying jump force: {jumpForce} to rigidbody");
                    rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                    hasJumped = true;
                    Debug.Log($"[GhostController] Jump force applied. New velocity: {rb.velocity}, hasJumped: {hasJumped}");
                }
            }
            else if (action.jumpPressed)
            {
                Debug.Log($"[GhostController] Jump conditions not met: action.jumpPressed: {action.jumpPressed}, isGrounded: {isGrounded}, hasJumped: {hasJumped}");
            }
        }
        
        // Flip character sprite based on movement direction - EXACTLY like the player does
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
        Debug.Log($"[GhostController] Trigger entered with: {other.name}, layer: {other.gameObject.layer}");
        
        // Check if hit by electricity projectile
        Projectile projectile = other.GetComponent<Projectile>();
        if (projectile != null && !hasBeenHitByElectricity)
        {
            Debug.Log($"[GhostController] Hit by projectile! Transforming into reflector...");
            hasBeenHitByElectricity = true;
            lastBulletDirection = projectile.GetDirection(); // Store the bullet direction
            TransformIntoReflector(projectile.transform.position, projectile.GetDirection());
        }
        // Check if hit by spike
        else if ((other.GetComponent<SpikeTile>() != null || other.GetComponent<TilemapSpikeManager>() != null) && !hasBeenHitBySpike)
        {
            Debug.Log($"[GhostController] Hit by spike! Transforming into falling ground...");
            hasBeenHitBySpike = true;
            TransformIntoFallingGround();
        }
        else if (projectile == null && other.GetComponent<SpikeTile>() == null && other.GetComponent<TilemapSpikeManager>() == null)
        {
            Debug.Log($"[GhostController] Hit by something without Projectile or SpikeTile/TilemapSpikeManager component: {other.name}");
        }
        else if (hasBeenHitByElectricity)
        {
            Debug.Log($"[GhostController] Already been hit by electricity, ignoring");
        }
        else if (hasBeenHitBySpike)
        {
            Debug.Log($"[GhostController] Already been hit by spike, ignoring");
        }
    }
    
    // Test method to manually trigger spike transformation
    public void TestSpikeTransformation()
    {
        Debug.Log($"[GhostController] Test spike transformation called");
        if (!hasBeenHitBySpike)
        {
            hasBeenHitBySpike = true;
            TransformIntoFallingGround();
        }
        else
        {
            Debug.Log($"[GhostController] Already hit by spike, resetting and trying again");
            hasBeenHitBySpike = false;
            TransformIntoFallingGround();
        }
    }
    
    private void OnTriggerStay2D(Collider2D other)
    {
        // Also check for spikes while staying in trigger
        if ((other.GetComponent<SpikeTile>() != null || other.GetComponent<TilemapSpikeManager>() != null) && !hasBeenHitBySpike)
        {
            Debug.Log($"[GhostController] Staying in spike trigger! Transforming into falling ground...");
            hasBeenHitBySpike = true;
            TransformIntoFallingGround();
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"[GhostController] Collision with: {collision.gameObject.name}, layer: {collision.gameObject.layer}");
        
        // Check if hit by electricity projectile
        Projectile projectile = collision.gameObject.GetComponent<Projectile>();
        if (projectile != null && !hasBeenHitByElectricity)
        {
            Debug.Log($"[GhostController] Hit by projectile via collision! Transforming into reflector...");
            hasBeenHitByElectricity = true;
            lastBulletDirection = projectile.GetDirection(); // Store the bullet direction
            TransformIntoReflector(projectile.transform.position, projectile.GetDirection());
        }
        // Check if hit by spike
        else if ((collision.gameObject.GetComponent<SpikeTile>() != null || collision.gameObject.GetComponent<TilemapSpikeManager>() != null) && !hasBeenHitBySpike)
        {
            Debug.Log($"[GhostController] Hit by spike via collision! Transforming into falling ground...");
            hasBeenHitBySpike = true;
            TransformIntoFallingGround();
        }
    }
    
    private void TransformIntoReflector(Vector3 bulletPosition, Vector2 bulletDirection)
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
            // Calculate the spawn position based on bullet direction and position
            Vector3 spawnPosition = CalculateReflectorSpawnPosition(bulletPosition, bulletDirection);
            
            // Snap to nearest grid tile (assuming 1 unit grid size)
            Vector3 snappedPosition = SnapToGrid(spawnPosition);
            
            Debug.Log($"[GhostController] Creating reflector: {reflectorPrefab.name} at spawn position {spawnPosition} -> snapped to {snappedPosition}");
            
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
    
    private void TransformIntoFallingGround()
    {
        Debug.Log($"[GhostController] TransformIntoFallingGround called");
        
        // Try to get the prefab from FallingBlockManager first
        GameObject prefabToUse = null;
        
        if (FallingBlockManager.Instance != null)
        {
            prefabToUse = FallingBlockManager.Instance.GetFallingGroundPrefab();
            if (prefabToUse != null)
            {
                Debug.Log($"[GhostController] Using FallingGround prefab from FallingBlockManager: {prefabToUse.name}");
            }
        }
        
        // Fallback to the inspector-assigned prefab
        if (prefabToUse == null && fallingGroundPrefab != null)
        {
            prefabToUse = fallingGroundPrefab;
            Debug.Log($"[GhostController] Using fallback FallingGround prefab from inspector: {prefabToUse.name}");
        }
        
        if (prefabToUse != null)
        {
            // Calculate the spawn position at the ghost's position
            Vector3 spawnPosition = transform.position;
            
            // Snap to nearest grid tile (assuming 1 unit grid size)
            Vector3 snappedPosition = SnapToGrid(spawnPosition);
            
            Debug.Log($"[GhostController] Creating falling ground at spawn position {spawnPosition} -> snapped to {snappedPosition}");
            
            // Create the falling ground at the snapped grid position
            GameObject fallingGround = Instantiate(prefabToUse, snappedPosition, transform.rotation);
            
            // Destroy the ghost
            Destroy(gameObject);
            
            Debug.Log($"[GhostController] Ghost transformed into falling ground at grid position {snappedPosition}");
        }
        else
        {
            Debug.LogError($"[GhostController] FallingGround prefab is null! Please assign the FallingGround prefab in FallingBlockManager or the inspector.");
            
            // Create a simple falling ground as fallback
            Debug.Log($"[GhostController] Creating fallback falling ground");
            GameObject fallbackGround = new GameObject("FallbackFallingGround");
            fallbackGround.transform.position = transform.position;
            
            // Add components
            Rigidbody2D rb = fallbackGround.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            
            BoxCollider2D collider = fallbackGround.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1f, 2f);
            
            SpriteRenderer renderer = fallbackGround.AddComponent<SpriteRenderer>();
            renderer.color = new Color(0.5f, 0.3f, 0.2f, 1f);
            renderer.sprite = GetComponent<SpriteRenderer>()?.sprite;
            
            // Set layer to Ground (layer 8)
            fallbackGround.layer = 8;
            
            // Destroy the ghost
            Destroy(gameObject);
            
            Debug.Log($"[GhostController] Created fallback falling ground");
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
    /// Calculates the spawn position for reflector based on bullet direction and position
    /// </summary>
    /// <param name="bulletPosition">Position of the bullet</param>
    /// <param name="bulletDirection">Direction of the bullet</param>
    /// <returns>The spawn position for the reflector</returns>
    private Vector3 CalculateReflectorSpawnPosition(Vector3 bulletPosition, Vector2 bulletDirection)
    {
        // Get the ghost's collider to find player dimensions
        Collider2D ghostCollider = GetComponent<Collider2D>();
        if (ghostCollider == null)
        {
            Debug.LogError($"[GhostController] No collider found on ghost, using bullet position");
            return bulletPosition;
        }
        
        // Calculate player center and bounds
        Vector2 playerCenter = (Vector2)transform.position + ghostCollider.offset;
        Vector2 playerExtents = ghostCollider.bounds.extents;
        
        // Determine if bullet is horizontal or vertical
        bool isHorizontal = Mathf.Abs(bulletDirection.x) > Mathf.Abs(bulletDirection.y);
        
        Vector3 spawnPosition;
        
        if (isHorizontal)
        {
            // Horizontal bullet: use centerX of player, centerY of bullet
            spawnPosition = new Vector3(playerCenter.x, bulletPosition.y, transform.position.z);
            Debug.Log($"[GhostController] Horizontal bullet - Player center X: {playerCenter.x}, Bullet Y: {bulletPosition.y}");
        }
        else
        {
            // Vertical bullet: use centerX of bullet, and determine Y based on bullet direction
            bool bulletComingFromAbove = bulletDirection.y < 0; // Negative Y means bullet is moving downward
            
            if (bulletComingFromAbove)
            {
                // Bullet hit from above: use top tile of player
                float topTileCenter = playerCenter.y + playerExtents.y - 0.5f; // Top of player minus half a tile
                spawnPosition = new Vector3(bulletPosition.x, topTileCenter, transform.position.z);
                Debug.Log($"[GhostController] Vertical bullet from above - Bullet X: {bulletPosition.x}, Player top tile: {topTileCenter}");
            }
            else
            {
                // Bullet hit from below: use bottom tile of player
                float bottomTileCenter = playerCenter.y - playerExtents.y + 0.5f; // Bottom of player plus half a tile
                spawnPosition = new Vector3(bulletPosition.x, bottomTileCenter, transform.position.z);
                Debug.Log($"[GhostController] Vertical bullet from below - Bullet X: {bulletPosition.x}, Player bottom tile: {bottomTileCenter}");
            }
        }
        
        Debug.Log($"[GhostController] Bullet direction: {bulletDirection}, Is horizontal: {isHorizontal}, Spawn position: {spawnPosition}");
        
        return spawnPosition;
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
        
        // Determine reflector type based on bullet direction and player state
        // For horizontal bullets: face the bullet, then up/down based on grounded state
        // For vertical bullets: face the bullet, then left/right based on player facing direction
        
        // Get the bullet direction from the last collision (we'll need to store this)
        Vector2 bulletDirection = GetLastBulletDirection();
        bool isHorizontalBullet = Mathf.Abs(bulletDirection.x) > Mathf.Abs(bulletDirection.y);
        
        Debug.Log($"[GhostController] Bullet direction: {bulletDirection}, Is horizontal: {isHorizontalBullet}");
        
        if (isHorizontalBullet)
        {
            // Horizontal bullet: face the bullet, then up/down based on grounded state
            bool bulletComingFromRight = bulletDirection.x < 0;
            
            if (bulletComingFromRight)
            {
                // Bullet coming from right, reflector faces right
                GameObject prefab = isGrounded ? ReflectorManager.Instance.RightUpReflectPrefab : ReflectorManager.Instance.RightDownReflectPrefab;
                Debug.Log($"[GhostController] Horizontal bullet from right, grounded: {isGrounded}, selected prefab: {(prefab != null ? prefab.name : "NULL")}");
                return prefab;
            }
            else
            {
                // Bullet coming from left, reflector faces left
                GameObject prefab = isGrounded ? ReflectorManager.Instance.LeftUpReflectPrefab : ReflectorManager.Instance.LeftDownReflectPrefab;
                Debug.Log($"[GhostController] Horizontal bullet from left, grounded: {isGrounded}, selected prefab: {(prefab != null ? prefab.name : "NULL")}");
                return prefab;
            }
        }
        else
        {
            // Vertical bullet: face the bullet, then left/right based on player facing direction
            bool bulletComingFromAbove = bulletDirection.y < 0;
            
            if (bulletComingFromAbove)
            {
                // Bullet coming from above, reflector faces up
                GameObject prefab = isFacingRight ? ReflectorManager.Instance.RightUpReflectPrefab : ReflectorManager.Instance.LeftUpReflectPrefab;
                Debug.Log($"[GhostController] Vertical bullet from above, facing right: {isFacingRight}, selected prefab: {(prefab != null ? prefab.name : "NULL")}");
                return prefab;
            }
            else
            {
                // Bullet coming from below, reflector faces down
                GameObject prefab = isFacingRight ? ReflectorManager.Instance.RightDownReflectPrefab : ReflectorManager.Instance.LeftDownReflectPrefab;
                Debug.Log($"[GhostController] Vertical bullet from below, facing right: {isFacingRight}, selected prefab: {(prefab != null ? prefab.name : "NULL")}");
                return prefab;
            }
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
            TransformIntoReflector(transform.position, Vector2.right); // Default to right direction for testing
        }
    }
    
    /// <summary>
    /// Manually trigger transformation into falling ground (for testing)
    /// </summary>
    public void ManualTransformIntoFallingGround()
    {
        if (!hasBeenHitBySpike)
        {
            hasBeenHitBySpike = true;
            TransformIntoFallingGround();
        }
    }
    
    /// <summary>
    /// Reset the electricity hit state so the ghost can transform again
    /// </summary>
    public void ResetElectricityHitState()
    {
        hasBeenHitByElectricity = false;
        Debug.Log($"[GhostController] Electricity hit state reset for {gameObject.name}");
    }
    
    /// <summary>
    /// Reset the spike hit state so the ghost can transform again
    /// </summary>
    public void ResetSpikeHitState()
    {
        hasBeenHitBySpike = false;
        Debug.Log($"[GhostController] Spike hit state reset for {gameObject.name}");
    }
    
    /// <summary>
    /// Get the last bullet direction that hit this ghost
    /// </summary>
    /// <returns>The direction of the last bullet that hit this ghost</returns>
    private Vector2 GetLastBulletDirection()
    {
        return lastBulletDirection;
    }
} 