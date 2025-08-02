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
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the entering object is the player or ghost
        bool isPlayer = ((1 << other.gameObject.layer) & playerLayer) != 0;
        bool isGhost = ((1 << other.gameObject.layer) & ghostLayer) != 0;
        
        if (isPlayer || isGhost)
        {
            string entityType = isPlayer ? "Player" : "Ghost";
            string triggerType = isTopTrigger ? "Top" : (isLeftTrigger ? "Left" : "Right");
            Debug.Log($"{entityType} entered {triggerType} pressure plate trigger: {other.gameObject.name}");
            
            if (isTopTrigger)
            {
                // Top trigger handles door activation/deactivation
                parentPlate.OnEntityEnter(isPlayer);
            }
            else
            {
                // Side triggers only handle plate movement (smooth walking)
                parentPlate.OnSideApproach(isPlayer);
            }
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        // Check if the exiting object is the player or ghost
        bool isPlayer = ((1 << other.gameObject.layer) & playerLayer) != 0;
        bool isGhost = ((1 << other.gameObject.layer) & ghostLayer) != 0;
        
        if (isPlayer || isGhost)
        {
            string entityType = isPlayer ? "Player" : "Ghost";
            string triggerType = isTopTrigger ? "Top" : (isLeftTrigger ? "Left" : "Right");
            Debug.Log($"{entityType} exited {triggerType} pressure plate trigger: {other.gameObject.name}");
            
            if (isTopTrigger)
            {
                // Top trigger handles door activation/deactivation
                parentPlate.OnEntityExit(isPlayer);
            }
            else
            {
                // Side triggers only handle plate movement (smooth walking)
                parentPlate.OnSideDeparture(isPlayer);
            }
        }
    }
} 