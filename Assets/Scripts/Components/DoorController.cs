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
        }
    }
    

    
    private void HideDoor()
    {
        // Disable sprite rendering
        if (doorSprite != null)
        {
            doorSprite.enabled = false;
        }
        
        // Disable collider
        if (doorCollider != null)
        {
            doorCollider.enabled = false;
        }
        
        // Disable rigidbody (if it exists)
        if (doorRigidbody != null)
        {
            doorRigidbody.simulated = false;
        }
        
        Debug.Log("Door hidden - entities can now pass through");
    }
    
    private void ShowDoor()
    {
        // Enable sprite rendering
        if (doorSprite != null)
        {
            doorSprite.enabled = true;
        }
        
        // Enable collider
        if (doorCollider != null)
        {
            doorCollider.enabled = true;
        }
        
        // Enable rigidbody (if it exists)
        if (doorRigidbody != null)
        {
            doorRigidbody.simulated = true;
        }
        
        Debug.Log("Door shown - entities can no longer pass through");
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