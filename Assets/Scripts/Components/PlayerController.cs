using UnityEngine;

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
    
    private Rigidbody2D rb;
    private bool isGrounded;
    private bool hasJumped = false;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
        
        // Prevent rotation
        if (rb != null)
        {
            rb.freezeRotation = true;
        }
    }
    
    private void Update()
    {
        // Movement input
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        Vector2 velocity = rb.velocity;
        velocity.x = horizontalInput * moveSpeed;
        rb.velocity = velocity;

        // Check if grounded
        CheckGrounded();
        Debug.Log("isGrounded: " + isGrounded);
        
        // Reset jump when grounded
        if (isGrounded)
        {
            hasJumped = false;
        }
        
        // Jump input - only one jump allowed
        if (Input.GetButtonDown("Jump") && isGrounded && !hasJumped)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            hasJumped = true;
        }
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
            Destroy(gameObject);
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
} 