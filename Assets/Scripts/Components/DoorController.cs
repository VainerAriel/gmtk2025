using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Door State")]
    [SerializeField] private bool isOpen = false;
    
    [Header("Door Components")]
    [SerializeField] private SpriteRenderer doorSprite;
    [SerializeField] private Collider2D doorCollider;
    [SerializeField] private Rigidbody2D doorRigidbody;
    
    private Vector3 originalPosition;
    Animator animator;
    
    private void Start()
    {
        // Store the original position
        originalPosition = transform.position;
        
        // Auto-find components if not assigned
        if (doorSprite == null)
        {
            doorSprite = GetComponent<SpriteRenderer>();
        }
        
        if (doorCollider == null)
        {
            doorCollider = GetComponent<Collider2D>();
        }
        
        if (doorRigidbody == null)
        {
            doorRigidbody = GetComponent<Rigidbody2D>();
        }
        animator = GetComponent<Animator>();
    }
    
    public void Initialize(Vector2 direction, float distance, float speed)
    {
        // Movement settings are no longer used, but kept for compatibility
        Debug.Log("Door initialized - movement disabled, only visibility changes");
    }
    
    public void Activate()
    {
        if (!isOpen)
        {
            isOpen = true;
            
            Debug.Log($"Door activation - Sprite: {doorSprite != null}, Collider: {doorCollider != null}, Rigidbody: {doorRigidbody != null}");
            
            // Hide the door (disable sprite and collider)
            HideDoor();
            animator.SetBool("isOpen", true);
        }
        else
        {
            Debug.Log($"Door already open - Open: {isOpen}");
        }
    }
    
    public void Deactivate()
    {
        if (isOpen)
        {
            isOpen = false;
            
            // Show the door (enable sprite and collider)
            ShowDoor();
            animator.SetBool("isOpen", false);
        }
    }
    

    
    private void HideDoor()
    {
        // Keep sprite rendering enabled so animations are visible
        // if (doorSprite != null)
        // {
        //     doorSprite.enabled = false;
        // }
        
        // Disable collider so entities can pass through
        if (doorCollider != null)
        {
            doorCollider.enabled = false;
        }
        
        // Disable rigidbody (if it exists)
        if (doorRigidbody != null)
        {
            doorRigidbody.simulated = false;
        }
        
        Debug.Log("Door collision disabled - entities can now pass through while door remains visible");
    }
    
    private void ShowDoor()
    {
        // Sprite rendering should already be enabled for animations
        // if (doorSprite != null)
        // {
        //     doorSprite.enabled = true;
        // }
        
        // Enable collider so entities cannot pass through
        if (doorCollider != null)
        {
            doorCollider.enabled = true;
        }
        
        // Enable rigidbody (if it exists)
        if (doorRigidbody != null)
        {
            doorRigidbody.simulated = true;
        }
        
        Debug.Log("Door collision enabled - entities can no longer pass through");
    }
    
    public void Reset()
    {
        // Reset to original state
        isOpen = false;
        transform.position = originalPosition;
        
        // Make sure door is visible and solid
        ShowDoor();
    }
    
    // Public getters for state checking
    public bool IsOpen()
    {
        return isOpen;
    }
    
    public Vector3 GetOriginalPosition()
    {
        return originalPosition;
    }
    
    // Method to set a new original position (useful for level design)
    public void SetOriginalPosition(Vector3 newPosition)
    {
        originalPosition = newPosition;
    }
} 