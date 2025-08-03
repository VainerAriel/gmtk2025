using UnityEngine;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 14f;
    
    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer = 256; // Layer 8 (Ground)
    [SerializeField] private float groundCheckDistance = 0.1f;
    
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    
    [Header("Death Boundary")]
    [SerializeField] private float deathYThreshold = -10f;
    [SerializeField] private bool useTileBasedDeath = true;
    [SerializeField] private float tileSize = 1f; // Size of one tile in world units
    [SerializeField] private int tilesBelowPlatform = 10; // Number of tiles below platform to trigger death
    
    [Header("Ghost System")]
    [SerializeField] private int maxGhosts = 3;
    [SerializeField] private Vector3 startPosition = new Vector3(-3.38f, -2.55f, 0f);
    [SerializeField] private GameObject ghostPrefab;
    [SerializeField] private bool allowGhostPhysicsAfterFreeze = false;
    
    [Header("Death Types")]
    [SerializeField] private bool instantDeathFromElectricity = true;
    [SerializeField] private bool instantDeathFromSpike = true;
    [SerializeField] private bool instantDeathFromAcid = true;
    
    private Rigidbody2D rb;
    private bool isGrounded;
    private bool hasJumped = false;

    Animator animator;
    
    // Ghost system variables
    private List<PlayerAction> recordedActions = new List<PlayerAction>();
    private List<GhostController> activeGhosts = new List<GhostController>();
    private List<PlayerAction[]> ghostMemories = new List<PlayerAction[]>(); // Store memories for each ghost
    private float gameStartTime;
    private bool isRecording = true;
    
    private bool isPoisoned = false;
    private bool diedFromAcid = false;
    
    [System.Serializable]
    public class PlayerAction
    {
        public float timestamp;
        public Vector3 position;
        public Vector2 velocity;
        public bool isGrounded;
        public bool hasJumped;
        public float horizontalInput;
        public bool jumpPressed;
        
        public PlayerAction(float time, Vector3 pos, Vector2 vel, bool grounded, bool jumped, float horizontal, bool jump)
        {
            timestamp = time;
            position = pos;
            velocity = vel;
            isGrounded = grounded;
            hasJumped = jumped;
            horizontalInput = horizontal;
            jumpPressed = jump;
        }
    }
    
    private void Start()
    {
        animator = GetComponent<Animator>();

        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
        gameStartTime = Time.time;
        
        // Set player to Player layer (layer 6)
        gameObject.layer = 6;
        
        // Prevent rotation
        if (rb != null)
        {
            rb.freezeRotation = true;
        }
    }
    
    private void Update()
    {
        // Check for respawn input
        if (Input.GetKeyDown(KeyCode.X))
        {
            RespawnPlayer();
        }
        
        // Movement input
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        Vector2 velocity = rb.velocity;
        velocity.x = horizontalInput * moveSpeed;
        rb.velocity = velocity;

        // Animation inputs
        animator.SetFloat("xVelocity", System.Math.Abs(rb.velocity.x));

        // Check if grounded
        CheckGrounded();
        
        // Reset jump when grounded
        if (isGrounded)
        {
            hasJumped = false;
        }
        
        // Jump input - only one jump allowed
        bool jumpPressed = Input.GetButtonDown("Jump");
        if (jumpPressed && isGrounded && !hasJumped)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            hasJumped = true;
        }
        
        // Record action if recording
        if (isRecording)
        {
            RecordAction(horizontalInput, jumpPressed);
        }
        
        // Check for death boundary
        CheckDeathBoundary();

        // Flip character sprite based on movement direction
        if (horizontalInput > 0)
        {
            transform.localScale = new Vector3(1, transform.localScale.y, transform.localScale.z); // Face right
        }
        else if (horizontalInput < 0)
        {
            transform.localScale = new Vector3(-1, transform.localScale.y, transform.localScale.z); // Face left
        }
    }
    
    private void RecordAction(float horizontalInput, bool jumpPressed)
    {
        float currentTime = Time.time - gameStartTime;
        PlayerAction action = new PlayerAction(
            currentTime,
            transform.position,
            rb.velocity,
            isGrounded,
            hasJumped,
            horizontalInput,
            jumpPressed
        );
        recordedActions.Add(action);
    }
    
    private void RespawnPlayer()
    {
        // Stop recording current session
        isRecording = false;
        
        // Reset everything when player dies
        ResetGameState();
        
        // Create ghost from recorded actions
        if (recordedActions.Count > 0)
        {
            CreateGhost();
        }
        
        // Restart all active ghosts
        RestartAllGhosts();
        
        // Clear poison status on respawn
        SetPoisoned(false);
        
        // Reset player position and state
        transform.position = startPosition;
        rb.velocity = Vector2.zero;
        hasJumped = false;
        isGrounded = false;
        
        // Start new recording session
        recordedActions.Clear();
        gameStartTime = Time.time;
        isRecording = true;
    }
    
    /// <summary>
    /// Resets the entire game state when player dies
    /// </summary>
    private void ResetGameState()
    {
        // Clear all reflectors
        if (ReflectorManager.Instance != null)
        {
            ReflectorManager.Instance.ClearAllReflectors();
        }
        
        // Clear all falling grounds
        if (FallingBlockManager.Instance != null)
        {
            FallingBlockManager.Instance.ClearAllFallingGrounds();
        }
        
        // Clear all bullets from the screen
        Projectile[] allProjectiles = FindObjectsOfType<Projectile>();
        foreach (Projectile projectile in allProjectiles)
        {
            if (projectile != null)
            {
                Destroy(projectile.gameObject);
            }
        }
        
        // Reset all projectile shooters
        ProjectileShooter[] projectileShooters = FindObjectsOfType<ProjectileShooter>();
        foreach (ProjectileShooter shooter in projectileShooters)
        {
            if (shooter != null)
            {
                shooter.ResetTimer();
            }
        }
        
        // Reset all tilemap projectile shooters
        TilemapProjectileShooter[] tilemapShooters = FindObjectsOfType<TilemapProjectileShooter>();
        foreach (TilemapProjectileShooter shooter in tilemapShooters)
        {
            if (shooter != null)
            {
                shooter.ResetTimer();
            }
        }
        
        // Reset all tilemap spike managers
        TilemapSpikeManager[] tilemapSpikeManagers = FindObjectsOfType<TilemapSpikeManager>();
        foreach (TilemapSpikeManager spikeManager in tilemapSpikeManagers)
        {
            if (spikeManager != null)
            {
                spikeManager.RestartCycle();
            }
        }
        
        // Reset all ghosts' electricity, spike, and acid hit states
        foreach (GhostController ghost in activeGhosts)
        {
            if (ghost != null)
            {
                ghost.ResetElectricityHitState();
                ghost.ResetSpikeHitState();
                ghost.ResetAcidHitState();
            }
        }
        
    }
    
    private void RestartAllGhosts()
    {
        // Clear existing ghosts
        foreach (GhostController ghost in activeGhosts)
        {
            if (ghost != null)
            {
                Destroy(ghost.gameObject);
            }
        }
        activeGhosts.Clear();
        
        // Recreate ghosts with their stored memories
        for (int i = 0; i < ghostMemories.Count; i++)
        {
            if (i < ghostMemories.Count)
            {
                CreateGhostFromMemory(ghostMemories[i], i);
            }
        }
    }
    
    private void CreateGhostFromMemory(PlayerAction[] memory, int memoryIndex)
    {
        // Create ghost object
        GameObject ghostObject;
        if (ghostPrefab != null)
        {
            ghostObject = Instantiate(ghostPrefab, startPosition, Quaternion.identity);
        }
        else
        {
            // Create a simple ghost if no prefab is assigned
            ghostObject = new GameObject("Ghost");
            ghostObject.transform.position = startPosition;
            
            // Add sprite renderer with ghost appearance
            SpriteRenderer ghostRenderer = ghostObject.AddComponent<SpriteRenderer>();
            SpriteRenderer playerRenderer = GetComponent<SpriteRenderer>();
            if (playerRenderer != null && playerRenderer.sprite != null)
            {
                ghostRenderer.sprite = playerRenderer.sprite;
                ghostRenderer.color = new Color(1f, 1f, 1f, 0.5f); // Semi-transparent
            }
            
            // Add collider (non-trigger for visual purposes)
            BoxCollider2D ghostCollider = ghostObject.AddComponent<BoxCollider2D>();
            BoxCollider2D playerCollider = GetComponent<BoxCollider2D>();
            if (playerCollider != null)
            {
                ghostCollider.size = playerCollider.size;
                ghostCollider.offset = playerCollider.offset;
            }
            ghostCollider.isTrigger = true; // Make it non-solid
        }
        
        // Set ghost to Ghost layer (layer 7)
        ghostObject.layer = 7;
        
        // Add ghost controller with the stored memory
        GhostController ghostController = ghostObject.AddComponent<GhostController>();
        ghostController.Initialize(memory, allowGhostPhysicsAfterFreeze, moveSpeed, jumpForce, rb.sharedMaterial, false); // Recreated ghosts don't have acid death
        activeGhosts.Add(ghostController);
        
        Debug.Log($"[PlayerController] Recreated ghost {memoryIndex} with {memory.Length} recorded actions");
    }
    
    private void CreateGhost()
    {
        // Store the current recorded actions as a memory for this ghost
        PlayerAction[] ghostMemory = recordedActions.ToArray();
        ghostMemories.Add(ghostMemory);
        
        // Remove oldest ghost if at max capacity (FIFO)
        if (activeGhosts.Count >= maxGhosts)
        {
            GhostController oldestGhost = activeGhosts[0];
            activeGhosts.RemoveAt(0);
            if (oldestGhost != null)
            {
                Destroy(oldestGhost.gameObject);
            }
            
            // Also remove the oldest memory
            if (ghostMemories.Count > 0)
            {
                ghostMemories.RemoveAt(0);
            }
        }
        
        // Create ghost object
        GameObject ghostObject;
        if (ghostPrefab != null)
        {
            ghostObject = Instantiate(ghostPrefab, startPosition, Quaternion.identity);
        }
        else
        {
            // Create a simple ghost if no prefab is assigned
            ghostObject = new GameObject("Ghost");
            ghostObject.transform.position = startPosition;
            
            // Add sprite renderer with ghost appearance
            SpriteRenderer ghostRenderer = ghostObject.AddComponent<SpriteRenderer>();
            SpriteRenderer playerRenderer = GetComponent<SpriteRenderer>();
            if (playerRenderer != null && playerRenderer.sprite != null)
            {
                ghostRenderer.sprite = playerRenderer.sprite;
                ghostRenderer.color = new Color(1f, 1f, 1f, 0.5f); // Semi-transparent
            }
            
            // Add collider (non-trigger for visual purposes)
            BoxCollider2D ghostCollider = ghostObject.AddComponent<BoxCollider2D>();
            BoxCollider2D playerCollider = GetComponent<BoxCollider2D>();
            if (playerCollider != null)
            {
                ghostCollider.size = playerCollider.size;
                ghostCollider.offset = playerCollider.offset;
            }
            ghostCollider.isTrigger = true; // Make it non-solid
        }
        
        // Set ghost to Ghost layer (layer 7)
        ghostObject.layer = 7;
        
        // Add ghost controller
        GhostController ghostController = ghostObject.AddComponent<GhostController>();
        ghostController.Initialize(ghostMemory, allowGhostPhysicsAfterFreeze, moveSpeed, jumpForce, rb.sharedMaterial, diedFromAcid);
        activeGhosts.Add(ghostController);
        
        Debug.Log($"[PlayerController] Created ghost with diedFromAcid={diedFromAcid}");
        
        // Reset acid death state after passing it to the ghost
        ResetAcidDeathState();
    }
    
    private void CheckGrounded()
    {
        // Get the player's collider to find the bottom position
        Collider2D playerCollider = GetComponent<Collider2D>();
        if (playerCollider == null) return;
        
        // Calculate the bottom center of the player
        Vector2 bottomCenter = (Vector2)transform.position + playerCollider.offset;
        bottomCenter.y -= playerCollider.bounds.extents.y;
        
        // Cast ray from bottom center downward
        RaycastHit2D hit = Physics2D.Raycast(bottomCenter, Vector2.down, groundCheckDistance, groundLayer);
        isGrounded = hit.collider != null;
        
        // Debug visualization
        Debug.DrawRay(bottomCenter, Vector2.down * groundCheckDistance, isGrounded ? Color.green : Color.red);
        
        // Debug ground detection for player
        Debug.Log($"[PlayerController] Ground check: bottomCenter={bottomCenter}, isGrounded={isGrounded}, hit.collider={hit.collider?.name}, layer={hit.collider?.gameObject.layer ?? 0}, groundLayer={groundLayer}");
    }
    
    public void TakeDamage(float damage)
    {
        // Prevent damage if player is already dead/respawning
        if (currentHealth <= 0)
        {
            Debug.LogWarning("[PlayerController] TakeDamage called while player is already dead, ignoring damage");
            return;
        }
        
        currentHealth -= damage;
        
        // Check for instant death from electricity (very high damage)
        if (instantDeathFromElectricity && damage >= 999f)
        {
            // Instant death - respawn immediately
            RespawnPlayer();
            currentHealth = maxHealth;
            return;
        }
        
        // Check for instant death from spike (30 damage)
        if (instantDeathFromSpike && damage >= 30f)
        {
            // Instant death from spike - respawn immediately
            RespawnPlayer();
            currentHealth = maxHealth;
            return;
        }
        
        if (currentHealth <= 0)
        {
            // Check if player died from acid (was poisoned)
            diedFromAcid = isPoisoned;
            
            Debug.Log($"[PlayerController] Player died! isPoisoned={isPoisoned}, diedFromAcid={diedFromAcid}");
            
            // Clear poison when player dies
            SetPoisoned(false);
            
            // Automatic respawn when health reaches zero
            RespawnPlayer();
            currentHealth = maxHealth;
        }
    }
    
    public float GetHealth()
    {
        return currentHealth;
    }
    
    public float GetMaxHealth()
    {
        return maxHealth;
    }
    
    private void CheckDeathBoundary()
    {
        float deathThreshold;
        
        if (useTileBasedDeath)
        {
            // Calculate death threshold based on tile size and number of tiles below platform
            deathThreshold = -(tilesBelowPlatform * tileSize);
        }
        else
        {
            // Use the original world unit threshold
            deathThreshold = deathYThreshold;
        }
        
        // Check if player has fallen below the death threshold
        if (transform.position.y < deathThreshold)
        {
            // Trigger automatic respawn when falling off the platform
            RespawnPlayer();
            currentHealth = maxHealth;
        }
    }

    public bool IsPoisoned()
    {
        return isPoisoned;
    }

    public void SetPoisoned(bool poisoned)
    {
        isPoisoned = poisoned;
        
        Debug.Log($"[PlayerController] Poison status: {poisoned}");
    }
    
    public bool DiedFromAcid()
    {
        return diedFromAcid;
    }
    
    public void ResetAcidDeathState()
    {
        diedFromAcid = false;
    }
} 