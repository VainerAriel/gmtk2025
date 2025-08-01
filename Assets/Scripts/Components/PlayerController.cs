using UnityEngine;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 14f;
    
    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer = 1;
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
    
    private Rigidbody2D rb;
    private bool isGrounded;
    private bool hasJumped = false;

    Animator animator;
    
    // Ghost system variables
    private List<PlayerAction> recordedActions = new List<PlayerAction>();
    private List<GhostController> activeGhosts = new List<GhostController>();
    private float gameStartTime;
    private bool isRecording = true;
    
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
        
        // Set player to Player layer
        gameObject.layer = LayerMask.NameToLayer("Player");
        
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
        
        // Create ghost from recorded actions
        if (recordedActions.Count > 0)
        {
            CreateGhost();
        }
        
        // Restart all active ghosts
        RestartAllGhosts();
        
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
    
    private void RestartAllGhosts()
    {
        foreach (GhostController ghost in activeGhosts)
        {
            if (ghost != null)
            {
                ghost.RestartReplay();
            }
        }
    }
    
    private void CreateGhost()
    {
        // Remove oldest ghost if at max capacity (FIFO)
        if (activeGhosts.Count >= maxGhosts)
        {
            GhostController oldestGhost = activeGhosts[0];
            activeGhosts.RemoveAt(0);
            if (oldestGhost != null)
            {
                Destroy(oldestGhost.gameObject);
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
        
        // Set ghost to Ghost layer
        ghostObject.layer = LayerMask.NameToLayer("Ghost");
        
        // Add ghost controller
        GhostController ghostController = ghostObject.AddComponent<GhostController>();
        ghostController.Initialize(recordedActions.ToArray(), allowGhostPhysicsAfterFreeze, moveSpeed, jumpForce, rb.sharedMaterial);
        activeGhosts.Add(ghostController);
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
    }
    
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
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
} 