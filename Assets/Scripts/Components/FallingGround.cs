using UnityEngine;

public class FallingGround : MonoBehaviour
{
    [Header("Falling Ground Settings")]
    [SerializeField] private float gravityScale = 1f;
    [SerializeField] private bool freezeRotation = true;
    [SerializeField] private Vector2 colliderSize = new Vector2(1f, 2f);
    [SerializeField] private Color groundColor = new Color(0.5f, 0.3f, 0.2f, 1f);
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    
    private Rigidbody2D rb;
    private BoxCollider2D groundCollider;
    private SpriteRenderer spriteRenderer;
    
    private void Start()
    {
        // Get or add required components
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        groundCollider = GetComponent<BoxCollider2D>();
        if (groundCollider == null)
        {
            groundCollider = gameObject.AddComponent<BoxCollider2D>();
        }
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        // Configure the falling ground
        SetupFallingGround();
        
        if (debugMode)
        {
            Debug.Log($"[FallingGround] {gameObject.name} initialized at position: {transform.position}");
        }
    }
    
    private void SetupFallingGround()
    {
        // Configure Rigidbody2D
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = gravityScale;
        rb.mass = 5f; // Heavier than player
        rb.drag = 0f;
        rb.angularDrag = 0.05f;
        
        if (freezeRotation)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        
        // Configure BoxCollider2D
        groundCollider.size = colliderSize;
        groundCollider.isTrigger = false; // Solid collider
        groundCollider.usedByEffector = false;
        groundCollider.usedByComposite = false;
        
        // Configure SpriteRenderer
        spriteRenderer.color = groundColor;
        spriteRenderer.sortingLayerName = "Ground";
        spriteRenderer.sortingOrder = 1;
        
        // Set layer to Ground (layer 8)
        gameObject.layer = 8;
        
        // Add tag for easy identification
        gameObject.tag = "FallingGround";
    }
    
    /// <summary>
    /// Initialize the falling ground with custom settings
    /// </summary>
    /// <param name="position">The world position to place the falling ground</param>
    /// <param name="customGravityScale">Custom gravity scale (optional)</param>
    /// <param name="customColor">Custom color (optional)</param>
    public void Initialize(Vector3 position, float customGravityScale = -1f, Color? customColor = null)
    {
        transform.position = position;
        
        if (customGravityScale >= 0f)
        {
            gravityScale = customGravityScale;
            if (rb != null)
            {
                rb.gravityScale = gravityScale;
            }
        }
        
        if (customColor.HasValue)
        {
            groundColor = customColor.Value;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = groundColor;
            }
        }
        
        if (debugMode)
        {
            Debug.Log($"[FallingGround] {gameObject.name} initialized at {position} with gravity: {gravityScale}");
        }
    }
    
    /// <summary>
    /// Snap the falling ground to the nearest grid position
    /// </summary>
    /// <param name="gridSize">The size of each grid tile (default 1.0)</param>
    public void SnapToGrid(float gridSize = 1.0f)
    {
        Vector3 snappedPosition = SnapToGridPosition(transform.position, gridSize);
        transform.position = snappedPosition;
        
        if (debugMode)
        {
            Debug.Log($"[FallingGround] {gameObject.name} snapped to grid position: {snappedPosition}");
        }
    }
    
    /// <summary>
    /// Snaps a world position to the nearest grid tile center
    /// </summary>
    /// <param name="worldPosition">The world position to snap</param>
    /// <param name="gridSize">The size of each grid tile (default 1.0)</param>
    /// <returns>The snapped grid position at tile center</returns>
    private Vector3 SnapToGridPosition(Vector3 worldPosition, float gridSize = 1.0f)
    {
        // Round to the nearest grid tile center (not corner)
        float snappedX = Mathf.Round(worldPosition.x + 0.5f) - 0.5f;
        float snappedY = Mathf.Round(worldPosition.y + 0.5f) - 0.5f;
        
        return new Vector3(snappedX, snappedY, worldPosition.z);
    }
    
    /// <summary>
    /// Set the falling ground to be static (no physics)
    /// </summary>
    public void SetStatic()
    {
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Static;
        }
        
        if (debugMode)
        {
            Debug.Log($"[FallingGround] {gameObject.name} set to static");
        }
    }
    
    /// <summary>
    /// Set the falling ground to be dynamic (affected by physics)
    /// </summary>
    public void SetDynamic()
    {
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
        
        if (debugMode)
        {
            Debug.Log($"[FallingGround] {gameObject.name} set to dynamic");
        }
    }
    
    /// <summary>
    /// Change the color of the falling ground
    /// </summary>
    /// <param name="newColor">The new color to apply</param>
    public void SetColor(Color newColor)
    {
        groundColor = newColor;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = groundColor;
        }
        
        if (debugMode)
        {
            Debug.Log($"[FallingGround] {gameObject.name} color changed to: {newColor}");
        }
    }
    
    /// <summary>
    /// Get the current velocity of the falling ground
    /// </summary>
    /// <returns>The current velocity vector</returns>
    public Vector2 GetVelocity()
    {
        return rb != null ? rb.velocity : Vector2.zero;
    }
    
    /// <summary>
    /// Set the velocity of the falling ground
    /// </summary>
    /// <param name="velocity">The velocity to set</param>
    public void SetVelocity(Vector2 velocity)
    {
        if (rb != null)
        {
            rb.velocity = velocity;
        }
        
        if (debugMode)
        {
            Debug.Log($"[FallingGround] {gameObject.name} velocity set to: {velocity}");
        }
    }
    
    /// <summary>
    /// Check if the falling ground is currently moving
    /// </summary>
    /// <returns>True if the falling ground is moving</returns>
    public bool IsMoving()
    {
        if (rb == null) return false;
        return rb.velocity.magnitude > 0.1f;
    }
    
    /// <summary>
    /// Stop all movement of the falling ground
    /// </summary>
    public void StopMovement()
    {
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        
        if (debugMode)
        {
            Debug.Log($"[FallingGround] {gameObject.name} movement stopped");
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (debugMode)
        {
            Debug.Log($"[FallingGround] {gameObject.name} collided with: {collision.gameObject.name}");
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (debugMode)
        {
            Debug.Log($"[FallingGround] {gameObject.name} triggered by: {other.name}");
        }
    }
} 