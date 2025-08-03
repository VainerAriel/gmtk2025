using UnityEngine;

public class PressurePlateTrigger : MonoBehaviour
{
    private PressurePlate parentPlate;
    private LayerMask playerLayer;
    private LayerMask ghostLayer;
    private bool isTopTrigger;
    private bool isLeftTrigger;
    
    public void Initialize(PressurePlate plate, bool topTrigger = false, bool leftTrigger = false)
    {
        parentPlate = plate;
        isTopTrigger = topTrigger;
        isLeftTrigger = leftTrigger;
        // Get the player and ghost layers
        playerLayer = 1 << LayerMask.NameToLayer("Player");
        ghostLayer = 1 << LayerMask.NameToLayer("Ghost");
        
        // Debug layer setup
        string triggerType = topTrigger ? "Top" : (leftTrigger ? "Left" : "Right");
        Debug.Log($"Initialized {triggerType} trigger - Player layer: {LayerMask.NameToLayer("Player")}, Ghost layer: {LayerMask.NameToLayer("Ghost")}");
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the entering object is the player or ghost
        bool isPlayer = ((1 << other.gameObject.layer) & playerLayer) != 0;
        bool isGhost = ((1 << other.gameObject.layer) & ghostLayer) != 0;
        
        // Only respond to players and ghosts
        if (!isPlayer && !isGhost)
        {
            return; // Ignore other entities
        }
        
        // Debug layer checks
        Debug.Log($"Layer check - Player: {isPlayer}, Ghost: {isGhost}");
        Debug.Log($"Object layer: {other.gameObject.layer} ({LayerMask.LayerToName(other.gameObject.layer)})");
        
        // Debug all collisions to see what's happening
        string triggerType = isTopTrigger ? "Top" : (isLeftTrigger ? "Left" : "Right");
        Debug.Log($"Trigger {triggerType} detected collision with: {other.gameObject.name} (Layer: {LayerMask.LayerToName(other.gameObject.layer)})");
        
        if (isPlayer || isGhost)
        {
            string entityType = isPlayer ? "Player" : "Ghost";
            Debug.Log($"{entityType} entered {triggerType} pressure plate trigger: {other.gameObject.name}");
            
            if (isTopTrigger)
            {
                // Top trigger handles door activation/deactivation
                parentPlate.OnEntityEnter(isPlayer, isGhost, false);
            }
            else
            {
                // Side triggers only handle plate movement (smooth walking)
                parentPlate.OnSideApproach(isPlayer, isGhost, false);
            }
        }
        else
        {
            Debug.Log($"Ignoring collision with {other.gameObject.name} - not a valid entity type");
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        // Check if the exiting object is the player or ghost
        bool isPlayer = ((1 << other.gameObject.layer) & playerLayer) != 0;
        bool isGhost = ((1 << other.gameObject.layer) & ghostLayer) != 0;
        
        // Only respond to players and ghosts
        if (!isPlayer && !isGhost)
        {
            return; // Ignore other entities
        }
        
        if (isPlayer || isGhost)
        {
            string entityType = isPlayer ? "Player" : "Ghost";
            string triggerType = isTopTrigger ? "Top" : (isLeftTrigger ? "Left" : "Right");
            Debug.Log($"{entityType} exited {triggerType} pressure plate trigger: {other.gameObject.name}");
            
            if (isTopTrigger)
            {
                // Top trigger handles door activation/deactivation
                parentPlate.OnEntityExit(isPlayer, isGhost, false);
            }
            else
            {
                // Side triggers only handle plate movement (smooth walking)
                parentPlate.OnSideDeparture(isPlayer, isGhost, false);
            }
        }
    }
} 