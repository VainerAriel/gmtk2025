using UnityEngine;

public class LayerCollisionSetup : MonoBehaviour
{
    [Header("Layer Setup")]
    [SerializeField] private bool setupLayerCollisions = true;
    
    private void Start()
    {
        if (setupLayerCollisions)
        {
            SetupLayerCollisions();
        }
    }
    
    private void SetupLayerCollisions()
    {
        // Get layer indices
        int playerLayer = LayerMask.NameToLayer("Player");
        int ghostLayer = LayerMask.NameToLayer("Ghost");
        int defaultLayer = LayerMask.NameToLayer("Default");
        int groundLayer = LayerMask.NameToLayer("Ground");
        int pressurePlateLayer = LayerMask.NameToLayer("PressurePlate");
        int moveableBlockLayer = LayerMask.NameToLayer("Moveable Blocks");
        
        // Disable collision between Player and Ghost layers
        if (playerLayer != -1 && ghostLayer != -1)
        {
            Physics2D.IgnoreLayerCollision(playerLayer, ghostLayer, true);
            Debug.Log($"Disabled collision between Player layer ({playerLayer}) and Ghost layer ({ghostLayer})");
        }
        
        // Disable collision between Ghost and Ghost layers (ghosts don't collide with each other)
        if (ghostLayer != -1)
        {
            Physics2D.IgnoreLayerCollision(ghostLayer, ghostLayer, true);
            Debug.Log($"Disabled collision between Ghost layer ({ghostLayer}) and itself");
        }
        
        // Ensure Player and Ghost still collide with Default and Ground layers
        if (playerLayer != -1 && defaultLayer != -1)
        {
            Physics2D.IgnoreLayerCollision(playerLayer, defaultLayer, false);
        }
        
        if (playerLayer != -1 && groundLayer != -1)
        {
            Physics2D.IgnoreLayerCollision(playerLayer, groundLayer, false);
        }
        
        if (ghostLayer != -1 && defaultLayer != -1)
        {
            Physics2D.IgnoreLayerCollision(ghostLayer, defaultLayer, false);
        }
        
        if (ghostLayer != -1 && groundLayer != -1)
        {
            Physics2D.IgnoreLayerCollision(ghostLayer, groundLayer, false);
        }
        
        // Setup pressure plate layer collisions
        if (pressurePlateLayer != -1)
        {
            // Player can collide with pressure plates (both solid and trigger)
            if (playerLayer != -1)
            {
                Physics2D.IgnoreLayerCollision(playerLayer, pressurePlateLayer, false);
            }
            
            // Ghosts can collide with pressure plates
            if (ghostLayer != -1)
            {
                Physics2D.IgnoreLayerCollision(ghostLayer, pressurePlateLayer, false);
            }
            
            // Moveable blocks can collide with pressure plates
            if (moveableBlockLayer != -1)
            {
                Physics2D.IgnoreLayerCollision(moveableBlockLayer, pressurePlateLayer, false);
            }
            
            // Pressure plates can collide with ground
            if (groundLayer != -1)
            {
                Physics2D.IgnoreLayerCollision(pressurePlateLayer, groundLayer, false);
            }
        }
        
        Debug.Log("Layer collision setup complete!");
    }
    
    // Method to manually setup collisions (can be called from other scripts)
    public static void SetupCollisions()
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        int ghostLayer = LayerMask.NameToLayer("Ghost");
        
        if (playerLayer != -1 && ghostLayer != -1)
        {
            Physics2D.IgnoreLayerCollision(playerLayer, ghostLayer, true);
        }
        
        if (ghostLayer != -1)
        {
            Physics2D.IgnoreLayerCollision(ghostLayer, ghostLayer, true);
        }
    }
} 