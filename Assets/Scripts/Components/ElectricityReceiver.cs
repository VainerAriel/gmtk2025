using UnityEngine;

public class ElectricityReceiver : MonoBehaviour
{
    [Header("Receiver Settings")]
    [SerializeField] private GameObject doorObject; // The door that will be controlled
    [SerializeField] private Vector2 moveDirection = Vector2.right; // Direction the door moves
    [SerializeField] private float moveDistance = 1f; // How far the door moves (in grid units)
    [SerializeField] private float moveSpeed = 2f; // Speed of door movement
    
    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer receiverRenderer;
    [SerializeField] private Color activatedColor = Color.yellow;
    [SerializeField] private Color deactivatedColor = Color.gray;
    
    [Header("Collision Setup")]
    [SerializeField] private bool addTriggerCollider = true; // Add a trigger collider for detection
    
    private bool isActivated = false;
    private DoorController doorController;
    
    // Debouncing variables to prevent rapid toggling
    private float lastToggleTime = 0f;
    private float toggleCooldown = 0.1f; // Minimum time between toggles
    
    private void Start()
    {
        // Setup colliders for the receiver
        SetupColliders();
        
        // Find the door controller if not assigned
        if (doorObject != null)
        {
            doorController = doorObject.GetComponent<DoorController>();
            if (doorController == null)
            {
                // Add door controller if it doesn't exist
                doorController = doorObject.AddComponent<DoorController>();
            }
        }
        
        // Set up the door controller
        if (doorController != null)
        {
            doorController.Initialize(moveDirection, moveDistance, moveSpeed);
        }
        
        // Set initial color
        if (receiverRenderer != null)
        {
            receiverRenderer.color = deactivatedColor;
        }
    }
    
    private void SetupColliders()
    {
        // Add trigger colliders for detection
        if (addTriggerCollider)
        {
            BoxCollider2D triggerCollider = GetComponent<BoxCollider2D>();
            if (triggerCollider == null)
            {
                triggerCollider = gameObject.AddComponent<BoxCollider2D>();
            }
            triggerCollider.isTrigger = true;
            triggerCollider.size = new Vector2(0.8f, 0.8f); // Slightly smaller than full tile
            triggerCollider.offset = Vector2.zero;
            
            // Ensure the trigger can detect the right layers
            gameObject.layer = LayerMask.NameToLayer("Default");
            
            Debug.Log($"Created electricity receiver trigger with size {triggerCollider.size}");
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if hit by electricity projectile
        Projectile projectile = other.GetComponent<Projectile>();
        if (projectile != null && Time.time - lastToggleTime > toggleCooldown)
        {
            Debug.Log($"[ElectricityReceiver] Hit by electricity projectile from {other.name}");
            ActivateReceiver();
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if hit by electricity projectile via collision
        Projectile projectile = collision.gameObject.GetComponent<Projectile>();
        if (projectile != null && Time.time - lastToggleTime > toggleCooldown)
        {
            Debug.Log($"[ElectricityReceiver] Hit by electricity projectile via collision from {collision.gameObject.name}");
            ActivateReceiver();
        }
    }
    
    private void ActivateReceiver()
    {
        if (!isActivated)
        {
            isActivated = true;
            lastToggleTime = Time.time; // Update toggle time
            
            // Change color to indicate activation
            if (receiverRenderer != null)
            {
                receiverRenderer.color = activatedColor;
            }
            
            // Activate the door
            if (doorController != null)
            {
                Debug.Log($"Activating door: {doorController.name}");
                doorController.Activate();
            }
            else
            {
                Debug.LogWarning("No door controller assigned to electricity receiver!");
            }
            
            Debug.Log("Electricity receiver activated!");
        }
        else
        {
            Debug.Log("Electricity receiver already activated, ignoring hit");
        }
    }
    
    private void DeactivateReceiver()
    {
        if (isActivated)
        {
            isActivated = false;
            lastToggleTime = Time.time; // Update toggle time
            
            // Change color to indicate deactivation
            if (receiverRenderer != null)
            {
                receiverRenderer.color = deactivatedColor;
            }
            
            // Deactivate the door
            if (doorController != null)
            {
                doorController.Deactivate();
            }
            
            Debug.Log("Electricity receiver deactivated!");
        }
    }
    
    // Public method to check if receiver is currently activated
    public bool IsActivated()
    {
        return isActivated;
    }
    
    // Method to reset the electricity receiver (useful for respawn systems)
    public void ResetReceiver()
    {
        isActivated = false;
        
        if (receiverRenderer != null)
        {
            receiverRenderer.color = deactivatedColor;
        }
        
        if (doorController != null)
        {
            doorController.Reset();
        }
    }
    
    // Method to manually activate the receiver (for testing or external control)
    public void ManualActivate()
    {
        if (!isActivated)
        {
            ActivateReceiver();
        }
    }
    
    // Method to manually deactivate the receiver (for testing or external control)
    public void ManualDeactivate()
    {
        if (isActivated)
        {
            DeactivateReceiver();
        }
    }
} 