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
        int projectileLayer = LayerMask.NameToLayer("Projectile");
        int reflectLayer = LayerMask.NameToLayer("Reflect");
        
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
        
        // Setup projectile layer collisions
        if (projectileLayer != -1)
        {
            // Projectiles collide with ground, reflect blocks, and player
            Physics2D.IgnoreLayerCollision(projectileLayer, defaultLayer, false);
            Physics2D.IgnoreLayerCollision(projectileLayer, groundLayer, false);
            Physics2D.IgnoreLayerCollision(projectileLayer, reflectLayer, false);
            Physics2D.IgnoreLayerCollision(projectileLayer, playerLayer, false);
            
            // Projectiles don't collide with ghosts
            Physics2D.IgnoreLayerCollision(projectileLayer, ghostLayer, true);
            
            Debug.Log($"Setup projectile layer ({projectileLayer}) collisions");
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