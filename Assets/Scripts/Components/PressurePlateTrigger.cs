using UnityEngine;

public class PressurePlateTrigger : MonoBehaviour
{
    private PressurePlate parentPlate;
    private LayerMask playerLayer;
    private LayerMask ghostLayer;
    private LayerMask moveableBlockLayer;
    private bool isTopTrigger;
    private bool isLeftTrigger;
    private bool isBlockTrigger;
    
    public void Initialize(PressurePlate plate, bool topTrigger = false, bool leftTrigger = false, bool blockTrigger = false)
    {
        parentPlate = plate;
        isTopTrigger = topTrigger;
        isLeftTrigger = leftTrigger;
        isBlockTrigger = blockTrigger;
        // Get the player, ghost, and moveable block layers
        playerLayer = 1 << LayerMask.NameToLayer("Player");
        ghostLayer = 1 << LayerMask.NameToLayer("Ghost");
        moveableBlockLayer = 1 << LayerMask.NameToLayer("Moveable Blocks");
        
        // Debug layer setup
        string triggerType = topTrigger ? "Top" : (leftTrigger ? "Left" : "Right");
        string blockType = blockTrigger ? "Block" : "Regular";
        Debug.Log($"Initialized {blockType} {triggerType} trigger - Player layer: {LayerMask.NameToLayer("Player")}, Ghost layer: {LayerMask.NameToLayer("Ghost")}, MoveableBlock layer: {LayerMask.NameToLayer("Moveable Blocks")}");
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the entering object is the player, ghost, or pushable block
        bool isPlayer = ((1 << other.gameObject.layer) & playerLayer) != 0;
        bool isGhost = ((1 << other.gameObject.layer) & ghostLayer) != 0;
        bool isPushableBlock = ((1 << other.gameObject.layer) & moveableBlockLayer) != 0 && other.GetComponent<PushableBlock>() != null;
        
        // Block triggers only respond to pushable blocks
        if (isBlockTrigger && !isPushableBlock)
        {
            return; // Ignore non-block entities for block triggers
        }
        
        // Regular triggers only respond to players and ghosts
        if (!isBlockTrigger && !isPlayer && !isGhost)
        {
            return; // Ignore blocks for regular triggers
        }
        
        // Debug layer checks
        Debug.Log($"Layer check - Player: {isPlayer}, Ghost: {isGhost}, PushableBlock: {isPushableBlock}");
        Debug.Log($"Object layer: {other.gameObject.layer} ({LayerMask.LayerToName(other.gameObject.layer)})");
        Debug.Log($"Has PushableBlock component: {other.GetComponent<PushableBlock>() != null}");
        
        // Debug all collisions to see what's happening
        string triggerType = isTopTrigger ? "Top" : (isLeftTrigger ? "Left" : "Right");
        string blockType = isBlockTrigger ? "Block" : "Regular";
        Debug.Log($"{blockType} Trigger {triggerType} detected collision with: {other.gameObject.name} (Layer: {LayerMask.LayerToName(other.gameObject.layer)})");
        
        if (isPlayer || isGhost || isPushableBlock)
        {
            string entityType = isPlayer ? "Player" : (isGhost ? "Ghost" : "PushableBlock");
            Debug.Log($"{entityType} entered {blockType} {triggerType} pressure plate trigger: {other.gameObject.name}");
            
            if (isTopTrigger)
            {
                // Top trigger handles door activation/deactivation
                parentPlate.OnEntityEnter(isPlayer, isGhost, isPushableBlock);
            }
            else
            {
                // Side triggers only handle plate movement (smooth walking)
                parentPlate.OnSideApproach(isPlayer, isGhost, isPushableBlock);
            }
        }
        else
        {
            Debug.Log($"Ignoring collision with {other.gameObject.name} - not a valid entity type");
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        // Check if the exiting object is the player, ghost, or pushable block
        bool isPlayer = ((1 << other.gameObject.layer) & playerLayer) != 0;
        bool isGhost = ((1 << other.gameObject.layer) & ghostLayer) != 0;
        bool isPushableBlock = ((1 << other.gameObject.layer) & moveableBlockLayer) != 0 && other.GetComponent<PushableBlock>() != null;
        
        // Block triggers only respond to pushable blocks
        if (isBlockTrigger && !isPushableBlock)
        {
            return; // Ignore non-block entities for block triggers
        }
        
        // Regular triggers only respond to players and ghosts
        if (!isBlockTrigger && !isPlayer && !isGhost)
        {
            return; // Ignore blocks for regular triggers
        }
        
        if (isPlayer || isGhost || isPushableBlock)
        {
            string entityType = isPlayer ? "Player" : (isGhost ? "Ghost" : "PushableBlock");
            string triggerType = isTopTrigger ? "Top" : (isLeftTrigger ? "Left" : "Right");
            string blockType = isBlockTrigger ? "Block" : "Regular";
            Debug.Log($"{entityType} exited {blockType} {triggerType} pressure plate trigger: {other.gameObject.name}");
            
            if (isTopTrigger)
            {
                // Top trigger handles door activation/deactivation
                parentPlate.OnEntityExit(isPlayer, isGhost, isPushableBlock);
            }
            else
            {
                // Side triggers only handle plate movement (smooth walking)
                parentPlate.OnSideDeparture(isPlayer, isGhost, isPushableBlock);
            }
        }
    }
} 