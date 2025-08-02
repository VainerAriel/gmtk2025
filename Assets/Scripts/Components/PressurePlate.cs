using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    [Header("Pressure Plate Settings")]
    [SerializeField] private GameObject doorObject; // The door that will move
    [SerializeField] private Vector2 moveDirection = Vector2.right; // Direction the door moves
    [SerializeField] private float moveDistance = 1f; // How far the door moves (in grid units)
    [SerializeField] private float moveSpeed = 2f; // Speed of door movement
    [SerializeField] private LayerMask playerLayer = 1; // Layer mask for player detection
    
    [Header("Plate Movement")]
    [SerializeField] private bool movePlateDown = true; // Whether the plate moves down when activated
    [SerializeField] private float plateMoveDistance = 1f; // How far the plate moves down
    [SerializeField] private float plateMoveSpeed = 3f; // Speed of plate movement
    
    [Header("Collision Setup")]
    [SerializeField] private bool addSolidCollider = true; // Add a solid collider for the player to stand on
    [SerializeField] private bool addTriggerCollider = true; // Add a trigger collider for detection
    
    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer plateRenderer;
    [SerializeField] private Color activatedColor = Color.green;
    [SerializeField] private Color deactivatedColor = Color.red;
    
    private bool isActivated = false;
    private bool playerOnPlate = false;
    private bool ghostOnPlate = false;
    private bool playerApproachingFromSide = false;
    private bool ghostApproachingFromSide = false;
    private DoorController doorController;
    
    // Plate movement variables
    private Vector3 originalPlatePosition;
    private Vector3 targetPlatePosition;
    private Coroutine plateMoveCoroutine;
    
    // Debouncing variables to prevent rapid toggling
    private float lastToggleTime = 0f;
    private float toggleCooldown = 0.2f; // Minimum time between toggles (reduced for better responsiveness)
    
    private void Start()
    {
        // Store the original position of the plate
        originalPlatePosition = transform.position;
        targetPlatePosition = originalPlatePosition + Vector3.down * plateMoveDistance;
        
        // Setup colliders for the pressure plate
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
        if (plateRenderer != null)
        {
            plateRenderer.color = deactivatedColor;
        }
    }
    
    private void Update()
    {
        // Periodically check if plate should be deactivated (fallback)
        if (isActivated && !playerOnPlate && !ghostOnPlate && Time.time - lastToggleTime > toggleCooldown)
        {
            // Check if we're at the target position (plate has moved down)
            if (Vector3.Distance(transform.position, targetPlatePosition) < 0.1f)
            {
                Debug.Log("Plate is down but no entities detected - forcing deactivation check");
                ForceCheckDeactivation();
            }
        }
    }
    
    private void SetupColliders()
    {
        // Add solid collider for the player to stand on
        if (addSolidCollider)
        {
            BoxCollider2D solidCollider = GetComponent<BoxCollider2D>();
            if (solidCollider == null)
            {
                solidCollider = gameObject.AddComponent<BoxCollider2D>();
            }
            solidCollider.isTrigger = false; // Solid collider for standing
        }
        
        // Add trigger colliders for detection
        if (addTriggerCollider)
        {
            // Create a child GameObject for the top trigger that stays at the original position
            GameObject topTriggerObject = new GameObject("PressurePlateTopTrigger");
            topTriggerObject.transform.SetParent(transform.parent); // Parent to the same parent as the plate
            topTriggerObject.transform.position = originalPlatePosition; // Set to original plate position
            
            BoxCollider2D topTriggerCollider = topTriggerObject.AddComponent<BoxCollider2D>();
            topTriggerCollider.isTrigger = true;
            topTriggerCollider.size = new Vector2(0.8f, 0.5f); // Taller trigger to account for movement
            topTriggerCollider.offset = new Vector2(0, 0.4f); // Position at the top surface
            
            // Add the trigger detection script to the top trigger object
            PressurePlateTrigger topTriggerScript = topTriggerObject.AddComponent<PressurePlateTrigger>();
            topTriggerScript.Initialize(this, true); // true = isTopTrigger
            
            // Create left side trigger for approaching from left
            GameObject leftTriggerObject = new GameObject("PressurePlateLeftTrigger");
            leftTriggerObject.transform.SetParent(transform.parent);
            leftTriggerObject.transform.position = originalPlatePosition + Vector3.left * 0.8f; // One tile to the left
            
            BoxCollider2D leftTriggerCollider = leftTriggerObject.AddComponent<BoxCollider2D>();
            leftTriggerCollider.isTrigger = true;
            leftTriggerCollider.size = new Vector2(0.3f, 0.8f); // Thin trigger for side detection
            leftTriggerCollider.offset = new Vector2(0, 0.4f);
            
            PressurePlateTrigger leftTriggerScript = leftTriggerObject.AddComponent<PressurePlateTrigger>();
            leftTriggerScript.Initialize(this, false, true); // false = not top trigger, true = isLeftTrigger
            
            // Create right side trigger for approaching from right
            GameObject rightTriggerObject = new GameObject("PressurePlateRightTrigger");
            rightTriggerObject.transform.SetParent(transform.parent);
            rightTriggerObject.transform.position = originalPlatePosition + Vector3.right * 0.8f; // One tile to the right
            
            BoxCollider2D rightTriggerCollider = rightTriggerObject.AddComponent<BoxCollider2D>();
            rightTriggerCollider.isTrigger = true;
            rightTriggerCollider.size = new Vector2(0.3f, 0.8f); // Thin trigger for side detection
            rightTriggerCollider.offset = new Vector2(0, 0.4f);
            
            PressurePlateTrigger rightTriggerScript = rightTriggerObject.AddComponent<PressurePlateTrigger>();
            rightTriggerScript.Initialize(this, false, false); // false = not top trigger, false = not left trigger (so it's right)
        }
    }
    
    // Public methods called by the trigger script
    public void OnEntityEnter(bool isPlayer)
    {
        string entityType = isPlayer ? "Player" : "Ghost";
        
        // Track which entity is on the plate
        if (isPlayer)
        {
            playerOnPlate = true;
        }
        else
        {
            ghostOnPlate = true;
        }
        
        Debug.Log($"{entityType} entered plate. Current state - Activated: {isActivated}, Player: {playerOnPlate}, Ghost: {ghostOnPlate}, Time since last toggle: {Time.time - lastToggleTime:F2}s");
        
        // Check if any entity (player or ghost) is on the plate and enough time has passed
        if (!isActivated && Time.time - lastToggleTime > toggleCooldown)
        {
            // First entity to step on the plate
            ActivatePlate();
        }
        else if (isActivated)
        {
            Debug.Log("Plate already activated, ignoring enter event");
        }
        else if (Time.time - lastToggleTime <= toggleCooldown)
        {
            Debug.Log($"Toggle cooldown active ({toggleCooldown - (Time.time - lastToggleTime):F2}s remaining)");
        }
    }
    
    public void OnEntityExit(bool isPlayer)
    {
        string entityType = isPlayer ? "Player" : "Ghost";
        
        // Track which entity left the plate
        if (isPlayer)
        {
            playerOnPlate = false;
        }
        else
        {
            ghostOnPlate = false;
        }
        
        Debug.Log($"{entityType} exited plate. Current state - Activated: {isActivated}, Player: {playerOnPlate}, Ghost: {ghostOnPlate}, Time since last toggle: {Time.time - lastToggleTime:F2}s");
        
        // If no entities are on the plate and enough time has passed, deactivate it
        if (!playerOnPlate && !ghostOnPlate && Time.time - lastToggleTime > toggleCooldown)
        {
            DeactivatePlate();
        }
        else if (playerOnPlate || ghostOnPlate)
        {
            Debug.Log("Other entities still on plate, not deactivating");
        }
        else if (Time.time - lastToggleTime <= toggleCooldown)
        {
            Debug.Log($"Toggle cooldown active ({toggleCooldown - (Time.time - lastToggleTime):F2}s remaining)");
        }
    }
    
    private void ActivatePlate()
    {
        if (!isActivated)
        {
            isActivated = true;
            lastToggleTime = Time.time; // Update toggle time
            
            // Change color to indicate activation
            if (plateRenderer != null)
            {
                plateRenderer.color = activatedColor;
            }
            
            // Move the plate down
            if (movePlateDown)
            {
                StartPlateMovement(targetPlatePosition);
            }
            
            // Activate the door
            if (doorController != null)
            {
                Debug.Log($"Activating door: {doorController.name}");
                doorController.Activate();
            }
            else
            {
                Debug.LogWarning("No door controller assigned to pressure plate!");
            }
            
            Debug.Log($"Pressure plate activated! Player on plate: {playerOnPlate}, Ghost on plate: {ghostOnPlate}");
        }
    }
    
    private void DeactivatePlate()
    {
        if (isActivated)
        {
            isActivated = false;
            lastToggleTime = Time.time; // Update toggle time
            
            // Change color to indicate deactivation
            if (plateRenderer != null)
            {
                plateRenderer.color = deactivatedColor;
            }
            
            // Move the plate back up
            if (movePlateDown)
            {
                StartPlateMovement(originalPlatePosition);
            }
            
            // Deactivate the door
            if (doorController != null)
            {
                doorController.Deactivate();
            }
            
            Debug.Log("Pressure plate deactivated!");
        }
    }
    
    // Public method to check if plate is currently activated
    public bool IsActivated()
    {
        return isActivated;
    }
    
    // Public method to check if player is on plate
    public bool IsPlayerOnPlate()
    {
        return playerOnPlate;
    }
    
    // Public method to check if ghost is on plate
    public bool IsGhostOnPlate()
    {
        return ghostOnPlate;
    }
    
    // Methods for side approach detection (smooth walking)
    public void OnSideApproach(bool isPlayer)
    {
        string entityType = isPlayer ? "Player" : "Ghost";
        Debug.Log($"{entityType} approaching from side - moving plate down for smooth walking");
        
        // Move plate down immediately for smooth walking
        if (movePlateDown && Vector3.Distance(transform.position, targetPlatePosition) > 0.1f)
        {
            StartPlateMovement(targetPlatePosition);
        }
        
        // Track which entity is approaching
        if (isPlayer)
        {
            playerApproachingFromSide = true;
        }
        else
        {
            ghostApproachingFromSide = true;
        }
    }
    
    public void OnSideDeparture(bool isPlayer)
    {
        string entityType = isPlayer ? "Player" : "Ghost";
        Debug.Log($"{entityType} departed from side");
        
        // Track which entity stopped approaching
        if (isPlayer)
        {
            playerApproachingFromSide = false;
        }
        else
        {
            ghostApproachingFromSide = false;
        }
        
        // If no entities are approaching and no entities are on the plate, move plate back up
        if (!playerApproachingFromSide && !ghostApproachingFromSide && !playerOnPlate && !ghostOnPlate)
        {
            Debug.Log("No entities approaching or on plate - moving plate back up");
            StartPlateMovement(originalPlatePosition);
        }
    }
    
    // Method to force check if plate should be deactivated
    public void ForceCheckDeactivation()
    {
        if (isActivated && !playerOnPlate && !ghostOnPlate && Time.time - lastToggleTime > toggleCooldown)
        {
            Debug.Log("Force checking deactivation - no entities on plate");
            DeactivatePlate();
        }
    }
    
    private void StartPlateMovement(Vector3 destination)
    {
        // Stop any existing movement
        if (plateMoveCoroutine != null)
        {
            StopCoroutine(plateMoveCoroutine);
        }
        
        // Start new movement
        plateMoveCoroutine = StartCoroutine(MovePlateToPosition(destination));
    }
    
    private System.Collections.IEnumerator MovePlateToPosition(Vector3 destination)
    {
        while (Vector3.Distance(transform.position, destination) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, destination, plateMoveSpeed * Time.deltaTime);
            yield return null;
        }
        
        // Snap to exact position to avoid floating point imprecision
        transform.position = destination;
        
        plateMoveCoroutine = null;
        
        Debug.Log($"Pressure plate moved to {destination}");
    }
    
    // Method to reset the pressure plate (useful for respawn systems)
    public void ResetPlate()
    {
        isActivated = false;
        playerOnPlate = false;
        ghostOnPlate = false;
        playerApproachingFromSide = false;
        ghostApproachingFromSide = false;
        
        // Stop any ongoing movement
        if (plateMoveCoroutine != null)
        {
            StopCoroutine(plateMoveCoroutine);
            plateMoveCoroutine = null;
        }
        
        // Reset position
        transform.position = originalPlatePosition;
        
        if (plateRenderer != null)
        {
            plateRenderer.color = deactivatedColor;
        }
        
        if (doorController != null)
        {
            doorController.Reset();
        }
    }
} 