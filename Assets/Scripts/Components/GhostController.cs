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
    
    [Header("Ghost Settings")]
    [SerializeField] private float ghostAlpha = 0.5f;
    [SerializeField] private Color ghostColor = new Color(1f, 1f, 1f, 0.5f);
    
    public void Initialize(PlayerController.PlayerAction[] actions, bool allowPhysicsAfterFreeze)
    {
        recordedActions = actions;
        this.allowPhysicsAfterFreeze = allowPhysicsAfterFreeze;
        ghostStartTime = Time.time;
        currentActionIndex = 0;
        isReplaying = true;
        
        // Get or add required components
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        // Configure rigidbody for ghost behavior
        rb.gravityScale = 3f; // Same as player
        rb.freezeRotation = true;
        rb.drag = 0f;
        
        // Set up visual appearance
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = ghostColor;
        }
        
        // Set up collider for physics mode
        BoxCollider2D ghostCollider = GetComponent<BoxCollider2D>();
        if (ghostCollider != null)
        {
            // Start as trigger during replay, will be made solid if physics mode is enabled
            ghostCollider.isTrigger = true;
        }
        
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
            ghostCollider.isTrigger = true; // Back to trigger mode for replay
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
    
    private void ApplyAction(PlayerController.PlayerAction action)
    {
        // Set position
        transform.position = action.position;
        
        // Set velocity
        if (rb != null)
        {
            rb.velocity = action.velocity;
        }
        
        // Handle jump if it was pressed
        if (action.jumpPressed && action.isGrounded && !action.hasJumped)
        {
            // Apply jump force (same as player)
            if (rb != null)
            {
                rb.AddForce(Vector2.up * 14f, ForceMode2D.Impulse);
            }
        }
    }
    
    private void FreezeGhost()
    {
        isReplaying = false;
        
        if (allowPhysicsAfterFreeze)
        {
            // Let physics continue naturally (ghost will fall, bounce, etc.)
            // Make the collider solid so it can collide with ground
            BoxCollider2D ghostCollider = GetComponent<BoxCollider2D>();
            if (ghostCollider != null)
            {
                ghostCollider.isTrigger = false; // Make it solid so it can collide with ground
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
            // Stop all movement immediately
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.isKinematic = true; // Make it kinematic so it doesn't fall
            }
            
            // Keep collider as trigger for non-physics mode
            BoxCollider2D ghostCollider = GetComponent<BoxCollider2D>();
            if (ghostCollider != null)
            {
                ghostCollider.isTrigger = true; // Keep it as trigger
            }
            
            // Make the ghost slightly more transparent when frozen
            if (spriteRenderer != null)
            {
                Color frozenColor = spriteRenderer.color;
                frozenColor.a = ghostAlpha * 0.7f; // Make it more transparent when frozen
                spriteRenderer.color = frozenColor;
            }
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