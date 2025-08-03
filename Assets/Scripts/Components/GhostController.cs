using UnityEngine;

public class GhostController : MonoBehaviour
{
    [Header("Movement")]
    private PlayerController.PlayerAction[] recordedActions;
    private int currentActionIndex = 0;
    private float ghostStartTime;
    private Rigidbody2D rb;
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
    [SerializeField] private string sortingLayerName = "Player";
    [SerializeField] private int sortingOrder = 0;
    
    private PhysicsMaterial2D originalMaterial;
    private SpriteRenderer spriteRenderer; // Add this missing field
    
    [Header("Transformation")]
    [SerializeField] private bool hasBeenHitByElectricity = false;
    [SerializeField] private bool hasBeenHitBySpike = false;
    [SerializeField] private GameObject fallingGroundPrefab;
    private Vector2 lastBulletDirection = Vector2.right;
    
    // POSITION-BASED REPLAY SYSTEM
    private bool usePositionBasedReplay = true; // Use position-based replay for perfect accuracy
    private float replaySpeed = 1f; // Can be adjusted for slow-motion effects
    
    public void Initialize(PlayerController.PlayerAction[] actions, bool allowPhysicsAfterFreeze, float moveSpeed, float jumpForce, PhysicsMaterial2D playerMaterial)
    {
        recordedActions = actions;
        this.allowPhysicsAfterFreeze = allowPhysicsAfterFreeze;
        this.moveSpeed = moveSpeed;
        this.jumpForce = jumpForce;
        
        // Set up timing for position-based replay
        if (actions != null && actions.Length > 0)
        {
            ghostStartTime = Time.time;
            Debug.Log($"[GhostController] Position-based replay initialized with {actions.Length} recorded positions");
        }
        
        currentActionIndex = 0;
        isReplaying = true;
        hasJumped = false;
        hasBeenHitByElectricity = false;
        hasBeenHitBySpike = false;
        
        // Set up components
        SetupComponents(playerMaterial);
        
        // Set initial position
        if (recordedActions.Length > 0)
        {
            transform.position = recordedActions[0].position;
            Debug.Log($"[GhostController] Set initial position to: {transform.position}");
        }
    }
    
    private void SetupComponents(PhysicsMaterial2D playerMaterial)
    {
        // Set up collider
        BoxCollider2D ghostCollider = GetComponent<BoxCollider2D>();
        BoxCollider2D playerCollider = FindObjectOfType<PlayerController>().GetComponent<BoxCollider2D>();
        
        if (ghostCollider != null && playerCollider != null)
        {
            ghostCollider.size = playerCollider.size;
            ghostCollider.offset = playerCollider.offset;
        }
        
        // Set up rigidbody
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        // Configure rigidbody for position-based replay
        rb.gravityScale = 0f; // No gravity for position-based replay
        rb.freezeRotation = true;
        rb.drag = 0f;
        rb.mass = 1f;
        rb.angularDrag = 0.05f;
        rb.interpolation = RigidbodyInterpolation2D.None;
        rb.sleepMode = RigidbodySleepMode2D.StartAwake;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.bodyType = RigidbodyType2D.Kinematic; // Use kinematic for position-based replay
        
        // Use physics material
        if (playerMaterial != null)
        {
            rb.sharedMaterial = playerMaterial;
            originalMaterial = playerMaterial;
        }
        
        // Set up visual
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = ghostColor;
            spriteRenderer.sortingLayerName = sortingLayerName;
            spriteRenderer.sortingOrder = sortingOrder;
        }
        
        // Set up collider
        if (ghostCollider != null)
        {
            ghostCollider.isTrigger = false;
            ghostCollider.density = 1f;
            ghostCollider.usedByEffector = false;
            ghostCollider.usedByComposite = false;
            ghostCollider.sharedMaterial = null;
        }
        
        // Add trigger collider for spike detection
        CircleCollider2D triggerCollider = gameObject.GetComponent<CircleCollider2D>();
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        triggerCollider.isTrigger = true;
        triggerCollider.radius = 0.6f;
        triggerCollider.offset = Vector2.zero;
        
        // Set layer
        gameObject.layer = 7;
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
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
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
        if (!isReplaying) return;
        
        if (recordedActions == null || recordedActions.Length == 0 || currentActionIndex >= recordedActions.Length)
        {
            FreezeGhost();
            return;
        }
        
        // POSITION-BASED REPLAY: Direct position interpolation
        if (usePositionBasedReplay)
        {
            UpdatePositionBasedReplay();
        }
        else
        {
            UpdateInputBasedReplay();
        }
        
        // Update visual appearance
        UpdateVisualAppearance();
        
        // Debug keys
        if (Input.GetKeyDown(KeyCode.T))
        {
            ManualTransformIntoReflector();
        }
        
        if (Input.GetKeyDown(KeyCode.Y))
        {
            ManualTransformIntoFallingGround();
        }
    }
    
    private void UpdatePositionBasedReplay()
    {
        float currentTime = (Time.time - ghostStartTime) * replaySpeed;
        
        // Find the two recorded positions to interpolate between
        int nextIndex = currentActionIndex + 1;
        
        if (nextIndex < recordedActions.Length)
        {
            PlayerController.PlayerAction currentAction = recordedActions[currentActionIndex];
            PlayerController.PlayerAction nextAction = recordedActions[nextIndex];
            
            // Calculate interpolation factor
            float timeDiff = nextAction.timestamp - currentAction.timestamp;
            float elapsedTime = currentTime - currentAction.timestamp;
            float t = timeDiff > 0 ? elapsedTime / timeDiff : 0f;
            
            // Clamp t to prevent overshooting
            t = Mathf.Clamp01(t);
            
            // Interpolate position
            Vector3 interpolatedPosition = Vector3.Lerp(currentAction.position, nextAction.position, t);
            
            // FIXED: Check for ground collision and prevent sinking
            Vector3 finalPosition = CheckGroundCollision(interpolatedPosition);
            transform.position = finalPosition;
            
            // Interpolate velocity for visual effects
            Vector2 interpolatedVelocity = Vector2.Lerp(currentAction.velocity, nextAction.velocity, t);
            rb.velocity = interpolatedVelocity;
            
            // Update sprite direction based on velocity
            if (interpolatedVelocity.x > 0.1f)
            {
                transform.localScale = new Vector3(1, transform.localScale.y, transform.localScale.z);
            }
            else if (interpolatedVelocity.x < -0.1f)
            {
                transform.localScale = new Vector3(-1, transform.localScale.y, transform.localScale.z);
            }
            
            // Move to next action when we've reached it
            if (currentTime >= nextAction.timestamp)
            {
                currentActionIndex = nextIndex;
            }
        }
        else
        {
            // We're at the last action, just stay there
            Vector3 finalPosition = CheckGroundCollision(recordedActions[currentActionIndex].position);
            transform.position = finalPosition;
        }
    }
    
    private Vector3 CheckGroundCollision(Vector3 targetPosition)
    {
        // Get the ghost's collider
        Collider2D ghostCollider = GetComponent<Collider2D>();
        if (ghostCollider == null) return targetPosition;
        
        // Calculate the bottom of the ghost
        Vector2 bottomCenter = (Vector2)targetPosition + ghostCollider.offset;
        bottomCenter.y -= ghostCollider.bounds.extents.y;
        
        // Cast a ray downward to check for ground
        RaycastHit2D hit = Physics2D.Raycast(bottomCenter, Vector2.down, groundCheckDistance + 0.1f, groundLayer);
        
        if (hit.collider != null)
        {
            // Calculate the correct Y position to sit on top of the ground
            float groundY = hit.point.y + ghostCollider.bounds.extents.y - ghostCollider.offset.y;
            
            // Only adjust if the ghost would be below the ground
            if (targetPosition.y < groundY)
            {
                return new Vector3(targetPosition.x, groundY, targetPosition.z);
            }
        }
        
        return targetPosition;
    }
    
    private void UpdateInputBasedReplay()
    {
        // Original input-based replay (fallback)
        isGrounded = CheckGroundedState();
        
        if (isGrounded)
        {
            hasJumped = false;
        }
        
        float currentTime = Time.time - ghostStartTime;
        
        if (currentActionIndex < recordedActions.Length && currentTime >= recordedActions[currentActionIndex].timestamp)
        {
            PlayerController.PlayerAction action = recordedActions[currentActionIndex];
            ApplyAction(action);
            currentActionIndex++;
        }
        
        StopMovementIfNoInput();
    }
    
    private void StopMovementIfNoInput()
    {
        if (rb == null || currentActionIndex >= recordedActions.Length) return;
        
        // Get the current action's input
        PlayerController.PlayerAction currentAction = recordedActions[currentActionIndex];
        
        // If there's no horizontal input, stop horizontal movement
        if (Mathf.Abs(currentAction.horizontalInput) < 0.1f)
        {
            Vector2 velocity = rb.velocity;
            velocity.x = 0f; // Stop horizontal movement
            rb.velocity = velocity;
        }
    }
    
    private void ApplyAction(PlayerController.PlayerAction action)
    {
        if (rb != null)
        {
            // Apply movement based on the recorded action
            Vector2 velocity = rb.velocity;
            velocity.x = action.horizontalInput * moveSpeed;
            rb.velocity = velocity;
            
            // Handle jump if it was pressed
            if (action.jumpPressed && isGrounded && !hasJumped)
            {
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                hasJumped = true;
            }
            
            // Flip sprite
            if (action.horizontalInput > 0)
            {
                transform.localScale = new Vector3(1, transform.localScale.y, transform.localScale.z);
            }
            else if (action.horizontalInput < 0)
            {
                transform.localScale = new Vector3(-1, transform.localScale.y, transform.localScale.z);
            }
        }
    }
    
    private void FreezeGhost()
    {
        isReplaying = false;
        
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
        }
        
        if (allowPhysicsAfterFreeze)
        {
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.velocity = Vector2.zero;
                rb.gravityScale = 3f; // Restore gravity
            }
            
            if (spriteRenderer != null)
            {
                Color frozenColor = spriteRenderer.color;
                frozenColor.a = ghostAlpha * 0.8f;
                spriteRenderer.color = frozenColor;
            }
        }
        else
        {
            if (rb != null)
            {
                rb.isKinematic = true;
            }
        }
        
        if (rb != null)
        {
            PhysicsMaterial2D frozenMaterial = new PhysicsMaterial2D();
            frozenMaterial.name = "FrozenGhostMaterial";
            frozenMaterial.friction = 1f;
            frozenMaterial.bounciness = 0f;
            rb.sharedMaterial = frozenMaterial;
        }
        
        if (spriteRenderer != null)
        {
            Color frozenColor = spriteRenderer.color;
            frozenColor.a = ghostAlpha * 0.7f;
            spriteRenderer.color = frozenColor;
        }
    }
    
    private void UpdateVisualAppearance()
    {
        if (spriteRenderer != null)
        {
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
        // Check if hit by ACTIVE spike
        else if (!hasBeenHitBySpike)
        {
            // Check for SpikeTile
            SpikeTile spikeTile = other.GetComponent<SpikeTile>();
            if (spikeTile != null && spikeTile.IsActive())
            {
                Debug.Log($"[GhostController] Hit by ACTIVE spike! Transforming into falling ground...");
                hasBeenHitBySpike = true;
                TransformIntoFallingGround();
            }
            // Check for TilemapSpikeManager
            else if (other.GetComponent<TilemapSpikeManager>() != null)
            {
                Debug.Log($"[GhostController] Hit by TilemapSpikeManager! Transforming into falling ground...");
                hasBeenHitBySpike = true;
                TransformIntoFallingGround();
            }
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
        // Also check for ACTIVE spikes while staying in trigger
        if (!hasBeenHitBySpike)
        {
            SpikeTile spikeTile = other.GetComponent<SpikeTile>();
            if (spikeTile != null && spikeTile.IsActive())
            {
                Debug.Log($"[GhostController] Staying in ACTIVE spike trigger! Transforming into falling ground...");
                hasBeenHitBySpike = true;
                TransformIntoFallingGround();
            }
            else if (other.GetComponent<TilemapSpikeManager>() != null)
            {
                Debug.Log($"[GhostController] Staying in TilemapSpikeManager trigger! Transforming into falling ground...");
                hasBeenHitBySpike = true;
                TransformIntoFallingGround();
            }
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
        // Check if hit by ACTIVE spike
        else if (!hasBeenHitBySpike)
        {
            SpikeTile spikeTile = collision.gameObject.GetComponent<SpikeTile>();
            if (spikeTile != null && spikeTile.IsActive())
            {
                Debug.Log($"[GhostController] Hit by ACTIVE spike via collision! Transforming into falling ground...");
                hasBeenHitBySpike = true;
                TransformIntoFallingGround();
            }
            else if (collision.gameObject.GetComponent<TilemapSpikeManager>() != null)
            {
                Debug.Log($"[GhostController] Hit by TilemapSpikeManager via collision! Transforming into falling ground...");
                hasBeenHitBySpike = true;
                TransformIntoFallingGround();
            }
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