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
    [SerializeField] private float plateMoveSpeed = 8f; // Speed of plate movement (increased for smoother gliding)
    
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
    private bool pushableBlockOnPlate = false;
    private bool playerApproachingFromSide = false;
    private bool ghostApproachingFromSide = false;
    private bool pushableBlockApproachingFromSide = false;
    private DoorController doorController;
    
    // Plate movement variables
    private Vector3 originalPlatePosition;
    private Vector3 targetPlatePosition;
    private Coroutine plateMoveCoroutine;
    
    // Debouncing variables to prevent rapid toggling
    private float lastToggleTime = 0f;
    private float toggleCooldown = 0.1f; // Minimum time between toggles (reduced for better responsiveness)
    

    
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
        plateRenderer.color = deactivatedColor;
        // if (plateRenderer != null)
        // {
        //     plateRenderer.color = deactivatedColor;
        // }
    }
    
    private void Update()
    {
        // Periodically check if plate should be deactivated (fallback)
        if (isActivated && !AreAnyEntitiesPresent() && Time.time - lastToggleTime > toggleCooldown)
        {
            // Check if we're at the target position (plate has moved down)
            if (Vector3.Distance(transform.position, targetPlatePosition) < 0.1f)
            {
                Debug.Log("Plate is down but no entities detected - forcing deactivation check");
                ForceCheckDeactivation();
            }
        }
        
        // Additional fallback: if plate is down and no entities are present, force it back up
        if (!isActivated && Vector3.Distance(transform.position, originalPlatePosition) > 0.1f && !AreAnyEntitiesPresent())
        {
            Debug.Log("Plate is not activated but is down and no entities present - forcing plate back up");
            StartPlateMovement(originalPlatePosition);
        }
        
        // Force cleanup of stuck side approach flags if no actual entities are detected
        if (playerApproachingFromSide || ghostApproachingFromSide || pushableBlockApproachingFromSide)
        {
            // Check if there are actually any entities near the side triggers
            bool hasActualEntitiesNearby = CheckForActualEntitiesNearSideTriggers();
            if (!hasActualEntitiesNearby)
            {
                Debug.Log("No actual entities detected near side triggers - clearing stuck approach flags");
                playerApproachingFromSide = false;
                ghostApproachingFromSide = false;
                pushableBlockApproachingFromSide = false;
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
            topTriggerCollider.size = new Vector2(0.7f, 0.3f); // Taller trigger to account for movement
            topTriggerCollider.offset = new Vector2(0, 0.3f); // Position at the top surface
            
            // Add the trigger detection script to the top trigger object
            PressurePlateTrigger topTriggerScript = topTriggerObject.AddComponent<PressurePlateTrigger>();
            topTriggerScript.Initialize(this, true); // true = isTopTrigger
            
            // Ensure the top trigger can detect the right layers
            topTriggerObject.layer = LayerMask.NameToLayer("Default");
            
            Debug.Log($"Created top trigger at {topTriggerObject.transform.position} with size {topTriggerCollider.size} and offset {topTriggerCollider.offset}");
            Debug.Log($"Top trigger layer: {LayerMask.LayerToName(topTriggerObject.layer)}");
            
            // Create left side trigger for approaching from left
            GameObject leftTriggerObject = new GameObject("PressurePlateLeftTrigger");
            leftTriggerObject.transform.SetParent(transform.parent);
            leftTriggerObject.transform.position = originalPlatePosition + Vector3.left * 0.5f; // One tile to the left
            
            BoxCollider2D leftTriggerCollider = leftTriggerObject.AddComponent<BoxCollider2D>();
            leftTriggerCollider.isTrigger = true;
            leftTriggerCollider.size = new Vector2(0.5f, 0.7f); // Full height trigger for side detection
            leftTriggerCollider.offset = new Vector2(0, 0.2f); // Center at ground level
            
            // Ensure the trigger can detect the right layers
            leftTriggerObject.layer = LayerMask.NameToLayer("Default");
            
            PressurePlateTrigger leftTriggerScript = leftTriggerObject.AddComponent<PressurePlateTrigger>();
            leftTriggerScript.Initialize(this, false, true); // false = not top trigger, true = isLeftTrigger
            
            Debug.Log($"Created left side trigger at {leftTriggerObject.transform.position} with size {leftTriggerCollider.size}");
            
            // Create right side trigger for approaching from right
            GameObject rightTriggerObject = new GameObject("PressurePlateRightTrigger");
            rightTriggerObject.transform.SetParent(transform.parent);
            rightTriggerObject.transform.position = originalPlatePosition + Vector3.right * 0.8f; // One tile to the right
            
            BoxCollider2D rightTriggerCollider = rightTriggerObject.AddComponent<BoxCollider2D>();
            rightTriggerCollider.isTrigger = true;
            rightTriggerCollider.size = new Vector2(0.5f, 0.7f); // Full height trigger for side detection
            rightTriggerCollider.offset = new Vector2(-0.3f, 0.2f); // Center at ground level
            
            // Ensure the trigger can detect the right layers
            rightTriggerObject.layer = LayerMask.NameToLayer("Default");
            
            PressurePlateTrigger rightTriggerScript = rightTriggerObject.AddComponent<PressurePlateTrigger>();
            rightTriggerScript.Initialize(this, false, false); // false = not top trigger, false = not left trigger (so it's right)
            
            Debug.Log($"Created right side trigger at {rightTriggerObject.transform.position} with size {rightTriggerCollider.size}");
            
            // Create separate triggers specifically for movable blocks (closer to the plate)
            
            // Top trigger for movable blocks (closer)
            GameObject topBlockTriggerObject = new GameObject("PressurePlateTopBlockTrigger");
            topBlockTriggerObject.transform.SetParent(transform.parent);
            topBlockTriggerObject.transform.position = originalPlatePosition; // Same position as regular top trigger
            
            BoxCollider2D topBlockTriggerCollider = topBlockTriggerObject.AddComponent<BoxCollider2D>();
            topBlockTriggerCollider.isTrigger = true;
            topBlockTriggerCollider.size = new Vector2(0.4f, 0.2f); // Smaller trigger for blocks
            topBlockTriggerCollider.offset = new Vector2(0, 0.3f); // Position at the top surface
            
            // Add the trigger detection script to the top block trigger object
            PressurePlateTrigger topBlockTriggerScript = topBlockTriggerObject.AddComponent<PressurePlateTrigger>();
            topBlockTriggerScript.Initialize(this, true, false, true); // true = isTopTrigger, false = not left, true = isBlockTrigger
            
            // Ensure the trigger can detect the right layers
            topBlockTriggerObject.layer = LayerMask.NameToLayer("Default");
            
            Debug.Log($"Created top block trigger at {topBlockTriggerObject.transform.position} with size {topBlockTriggerCollider.size}");
            
            // Left side trigger for movable blocks (closer)
            GameObject leftBlockTriggerObject = new GameObject("PressurePlateLeftBlockTrigger");
            leftBlockTriggerObject.transform.SetParent(transform.parent);
            leftBlockTriggerObject.transform.position = originalPlatePosition + Vector3.left * 0.3f; // Closer than regular left trigger
            
            BoxCollider2D leftBlockTriggerCollider = leftBlockTriggerObject.AddComponent<BoxCollider2D>();
            leftBlockTriggerCollider.isTrigger = true;
            leftBlockTriggerCollider.size = new Vector2(0.3f, 0.8f); // Smaller trigger for blocks
            leftBlockTriggerCollider.offset = new Vector2(0, 0.3f); // Center at ground level
            
            // Ensure the trigger can detect the right layers
            leftBlockTriggerObject.layer = LayerMask.NameToLayer("Default");
            
            PressurePlateTrigger leftBlockTriggerScript = leftBlockTriggerObject.AddComponent<PressurePlateTrigger>();
            leftBlockTriggerScript.Initialize(this, false, true, true); // false = not top trigger, true = isLeftTrigger, true = isBlockTrigger
            
            Debug.Log($"Created left block trigger at {leftBlockTriggerObject.transform.position} with size {leftBlockTriggerCollider.size}");
            
            // Right side trigger for movable blocks (closer)
            GameObject rightBlockTriggerObject = new GameObject("PressurePlateRightBlockTrigger");
            rightBlockTriggerObject.transform.SetParent(transform.parent);
            rightBlockTriggerObject.transform.position = originalPlatePosition + Vector3.right * 0.5f; // Closer than regular right trigger
            
            BoxCollider2D rightBlockTriggerCollider = rightBlockTriggerObject.AddComponent<BoxCollider2D>();
            rightBlockTriggerCollider.isTrigger = true;
            rightBlockTriggerCollider.size = new Vector2(0.3f, 0.8f); // Smaller trigger for blocks
            rightBlockTriggerCollider.offset = new Vector2(-0.2f, 0.3f); // Center at ground level
            
            // Ensure the trigger can detect the right layers
            rightBlockTriggerObject.layer = LayerMask.NameToLayer("Default");
            
            PressurePlateTrigger rightBlockTriggerScript = rightBlockTriggerObject.AddComponent<PressurePlateTrigger>();
            rightBlockTriggerScript.Initialize(this, false, false, true); // false = not top trigger, false = not left trigger, true = isBlockTrigger
            
            Debug.Log($"Created right block trigger at {rightBlockTriggerObject.transform.position} with size {rightBlockTriggerCollider.size}");
        }
    }
    
    // Public methods called by the trigger script
    public void OnEntityEnter(bool isPlayer, bool isGhost, bool isPushableBlock)
    {
        string entityType = isPlayer ? "Player" : (isGhost ? "Ghost" : "PushableBlock");
        
        // Track which entity is on the plate
        if (isPlayer)
        {
            playerOnPlate = true;
        }
        else if (isGhost)
        {
            ghostOnPlate = true;
        }
        else if (isPushableBlock)
        {
            pushableBlockOnPlate = true;
        }
        
        Debug.Log($"{entityType} entered plate. Current state - Activated: {isActivated}, Player: {playerOnPlate}, Ghost: {ghostOnPlate}, PushableBlock: {pushableBlockOnPlate}, Time since last toggle: {Time.time - lastToggleTime:F2}s");
        
        // Check if any entity (player, ghost, or pushable block) is on the plate and enough time has passed
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
    
    public void OnEntityExit(bool isPlayer, bool isGhost, bool isPushableBlock)
    {
        string entityType = isPlayer ? "Player" : (isGhost ? "Ghost" : "PushableBlock");
        
        // Track which entity left the plate
        if (isPlayer)
        {
            playerOnPlate = false;
        }
        else if (isGhost)
        {
            ghostOnPlate = false;
        }
        else if (isPushableBlock)
        {
            pushableBlockOnPlate = false;
        }
        
        Debug.Log($"{entityType} exited plate. Current state - Activated: {isActivated}, Player: {playerOnPlate}, Ghost: {ghostOnPlate}, PushableBlock: {pushableBlockOnPlate}, Time since last toggle: {Time.time - lastToggleTime:F2}s");
        
        // If no entities are on the plate AND no entities are approaching, and enough time has passed, deactivate it
        if (!AreAnyEntitiesPresent() && Time.time - lastToggleTime > toggleCooldown)
        {
            DeactivatePlate();
        }
        else if (AreAnyEntitiesPresent())
        {
            Debug.Log("Other entities still present (on plate or approaching), not deactivating");
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
            
            Debug.Log($"Pressure plate activated! Player on plate: {playerOnPlate}, Ghost on plate: {ghostOnPlate}, PushableBlock on plate: {pushableBlockOnPlate}");
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
    public void OnSideApproach(bool isPlayer, bool isGhost, bool isPushableBlock)
    {
        string entityType = isPlayer ? "Player" : (isGhost ? "Ghost" : "PushableBlock");
        Debug.Log($"{entityType} approaching from side - activating plate for smooth walking");
        
        // Track which entity is approaching
        if (isPlayer)
        {
            playerApproachingFromSide = true;
        }
        else if (isGhost)
        {
            ghostApproachingFromSide = true;
        }
        else if (isPushableBlock)
        {
            pushableBlockApproachingFromSide = true;
        }
        
        // Activate the plate if it's not already activated and enough time has passed
        if (!isActivated && Time.time - lastToggleTime > toggleCooldown)
        {
            Debug.Log($"Activating plate due to {entityType} approaching from side");
            ActivatePlate();
        }
        else if (isActivated)
        {
            Debug.Log($"Plate already activated, {entityType} approaching from side");
        }
        else if (Time.time - lastToggleTime <= toggleCooldown)
        {
            Debug.Log($"Toggle cooldown active while {entityType} approaching ({toggleCooldown - (Time.time - lastToggleTime):F2}s remaining)");
        }
    }
    
    public void OnSideDeparture(bool isPlayer, bool isGhost, bool isPushableBlock)
    {
        string entityType = isPlayer ? "Player" : (isGhost ? "Ghost" : "PushableBlock");
        Debug.Log($"{entityType} departed from side");
        
        // Track which entity stopped approaching
        if (isPlayer)
        {
            playerApproachingFromSide = false;
        }
        else if (isGhost)
        {
            ghostApproachingFromSide = false;
        }
        else if (isPushableBlock)
        {
            pushableBlockApproachingFromSide = false;
        }
        
        // Debug current state
        Debug.Log($"Side departure - Player approaching: {playerApproachingFromSide}, Ghost approaching: {ghostApproachingFromSide}, Block approaching: {pushableBlockApproachingFromSide}");
        Debug.Log($"Entities on plate - Player: {playerOnPlate}, Ghost: {ghostOnPlate}, Block: {pushableBlockOnPlate}");
        
        // If no entities are approaching and no entities are on the plate, deactivate the plate
        if (!AreAnyEntitiesPresent() && Time.time - lastToggleTime > toggleCooldown)
        {
            Debug.Log("No entities approaching or on plate - deactivating plate");
            DeactivatePlate();
        }
        else if (AreAnyEntitiesPresent())
        {
            Debug.Log($"Still have entities present - not deactivating plate. AreAnyEntitiesPresent: {AreAnyEntitiesPresent()}");
        }
        else if (Time.time - lastToggleTime <= toggleCooldown)
        {
            Debug.Log($"Toggle cooldown active during departure ({toggleCooldown - (Time.time - lastToggleTime):F2}s remaining)");
        }
    }
    
    // Method to force check if plate should be deactivated
    public void ForceCheckDeactivation()
    {
        if (isActivated && !AreAnyEntitiesPresent() && Time.time - lastToggleTime > toggleCooldown)
        {
            Debug.Log("Force checking deactivation - no entities present (on plate or approaching)");
            DeactivatePlate();
        }
    }
    
    // Helper method to check if any entities are present
    private bool AreAnyEntitiesPresent()
    {
        bool hasEntities = playerOnPlate || ghostOnPlate || pushableBlockOnPlate || 
                          playerApproachingFromSide || ghostApproachingFromSide || pushableBlockApproachingFromSide;
        
        // Debug this check when it's called frequently
        if (hasEntities)
        {
            Debug.Log($"AreAnyEntitiesPresent: {hasEntities} - Player on: {playerOnPlate}, Ghost on: {ghostOnPlate}, Block on: {pushableBlockOnPlate}, Player approaching: {playerApproachingFromSide}, Ghost approaching: {ghostApproachingFromSide}, Block approaching: {pushableBlockApproachingFromSide}");
        }
        
        return hasEntities;
    }
    
    // Helper method to check if there are actual entities near the side triggers
    private bool CheckForActualEntitiesNearSideTriggers()
    {
        // Check left side trigger area
        Vector2 leftCheckCenter = originalPlatePosition + Vector3.left * 0.8f;
        Collider2D[] leftHits = Physics2D.OverlapBoxAll(leftCheckCenter, new Vector2(0.3f, 1.0f), 0f);
        
        // Check right side trigger area
        Vector2 rightCheckCenter = originalPlatePosition + Vector3.right * 0.8f;
        Collider2D[] rightHits = Physics2D.OverlapBoxAll(rightCheckCenter, new Vector2(0.3f, 1.0f), 0f);
        
        // Check for valid entities in both areas
        foreach (Collider2D hit in leftHits)
        {
            if (IsValidEntity(hit.gameObject))
            {
                return true;
            }
        }
        
        foreach (Collider2D hit in rightHits)
        {
            if (IsValidEntity(hit.gameObject))
            {
                return true;
            }
        }
        
        return false;
    }
    
    // Helper method to check if a GameObject is a valid entity
    private bool IsValidEntity(GameObject obj)
    {
        // Ignore the pressure plate itself and its children
        if (obj.transform.IsChildOf(transform) || obj == gameObject)
        {
            return false;
        }
        
        // Check if it's a player, ghost, or pushable block
        int playerLayer = LayerMask.NameToLayer("Player");
        int ghostLayer = LayerMask.NameToLayer("Ghost");
        int moveableBlockLayer = LayerMask.NameToLayer("Moveable Blocks");
        
        bool isPlayer = obj.layer == playerLayer;
        bool isGhost = obj.layer == ghostLayer;
        bool isPushableBlock = obj.layer == moveableBlockLayer && obj.GetComponent<PushableBlock>() != null;
        
        return isPlayer || isGhost || isPushableBlock;
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
        pushableBlockOnPlate = false;
        playerApproachingFromSide = false;
        ghostApproachingFromSide = false;
        pushableBlockApproachingFromSide = false;
        
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