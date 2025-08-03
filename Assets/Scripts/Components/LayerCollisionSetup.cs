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
        // Get layer indices according to the requirements:
        // Player = layer 6, Ghost = layer 7, Ground = layer 8, BG = layer 9
        // Projectile = layer 10, Reflect = layer 11, ProjectileShooter = layer 12, Spike = layer 13
        int playerLayer = 6;
        int ghostLayer = 7;
        int groundLayer = 8;
        int bgLayer = 9;
        int projectileLayer = 10;
        int reflectLayer = 11;
        int projectileShooterLayer = 12;
        int spikeLayer = 13;
        
        // Clear all layer collisions first
        for (int i = 0; i < 32; i++)
        {
            for (int j = 0; j < 32; j++)
            {
                Physics2D.IgnoreLayerCollision(i, j, false);
            }
        }
        
        // Player (layer 6) collisions:
        // - Collides with Ground (layer 8)
        // - Collides with Projectile (layer 10) - gets killed
        // - Collides with Spike (layer 13) - gets killed
        // - Collides with Reflect (layer 11) - normal block
        // - Collides with ProjectileShooter (layer 12) - normal block
        // - DOES NOT collide with Ghost (layer 7) - they should pass through each other
        Physics2D.IgnoreLayerCollision(playerLayer, groundLayer, false);
        Physics2D.IgnoreLayerCollision(playerLayer, projectileLayer, false);
        Physics2D.IgnoreLayerCollision(playerLayer, spikeLayer, false);
        Physics2D.IgnoreLayerCollision(playerLayer, reflectLayer, false);
        Physics2D.IgnoreLayerCollision(playerLayer, projectileShooterLayer, false);
        Physics2D.IgnoreLayerCollision(playerLayer, ghostLayer, true); // Disable Player-Ghost collision
        
        // Ghost (layer 7) collisions:
        // - Collides with Ground (layer 8)
        // - Collides with Projectile (layer 10) - transforms into reflect
        // - Collides with Spike (layer 13) - transforms into falling ground
        // - Collides with Reflect (layer 11) - normal block
        // - Collides with ProjectileShooter (layer 12) - normal block
        Physics2D.IgnoreLayerCollision(ghostLayer, groundLayer, false);
        Physics2D.IgnoreLayerCollision(ghostLayer, ghostLayer, true);
        Physics2D.IgnoreLayerCollision(ghostLayer, projectileLayer, false);
        Physics2D.IgnoreLayerCollision(ghostLayer, spikeLayer, false);
        Physics2D.IgnoreLayerCollision(ghostLayer, reflectLayer, false);
        Physics2D.IgnoreLayerCollision(ghostLayer, projectileShooterLayer, false);
        
        // Ground (layer 8) collisions:
        // - Collides with Player (layer 6)
        // - Collides with Ghost (layer 7)
        // - Collides with Projectile (layer 10) - projectile disappears
        // - Collides with Reflect (layer 11) - normal block
        // - Collides with ProjectileShooter (layer 12) - normal block
        // - Collides with Spike (layer 13) - normal block
        Physics2D.IgnoreLayerCollision(groundLayer, playerLayer, false);
        Physics2D.IgnoreLayerCollision(groundLayer, ghostLayer, false);
        Physics2D.IgnoreLayerCollision(groundLayer, projectileLayer, false);
        Physics2D.IgnoreLayerCollision(groundLayer, reflectLayer, false);
        Physics2D.IgnoreLayerCollision(groundLayer, projectileShooterLayer, false);
        Physics2D.IgnoreLayerCollision(groundLayer, spikeLayer, false);
        
        // BG (layer 9) - no collisions with anything
        for (int i = 0; i < 32; i++)
        {
            Physics2D.IgnoreLayerCollision(bgLayer, i, true);
        }
        
        // Projectile (layer 10) collisions:
        // - Collides with Ground (layer 8) - disappears
        // - Collides with Player (layer 6) - kills player
        // - Collides with Ghost (layer 7) - transforms ghost
        // - Collides with Reflect (layer 11) - reflects
        Physics2D.IgnoreLayerCollision(projectileLayer, groundLayer, false);
        Physics2D.IgnoreLayerCollision(projectileLayer, playerLayer, false);
        Physics2D.IgnoreLayerCollision(projectileLayer, ghostLayer, false);
        Physics2D.IgnoreLayerCollision(projectileLayer, reflectLayer, false);
        
        // Reflect (layer 11) collisions:
        // - Collides with Player (layer 6) - normal block
        // - Collides with Ghost (layer 7) - normal block
        // - Collides with Ground (layer 8) - normal block
        // - Collides with Projectile (layer 10) - reflects
        // - Collides with ProjectileShooter (layer 12) - normal block
        // - Collides with Spike (layer 13) - normal block
        Physics2D.IgnoreLayerCollision(reflectLayer, playerLayer, false);
        Physics2D.IgnoreLayerCollision(reflectLayer, ghostLayer, false);
        Physics2D.IgnoreLayerCollision(reflectLayer, groundLayer, false);
        Physics2D.IgnoreLayerCollision(reflectLayer, projectileLayer, false);
        Physics2D.IgnoreLayerCollision(reflectLayer, projectileShooterLayer, false);
        Physics2D.IgnoreLayerCollision(reflectLayer, spikeLayer, false);
        
        // ProjectileShooter (layer 12) collisions:
        // - Collides with Player (layer 6) - normal block
        // - Collides with Ghost (layer 7) - normal block
        // - Collides with Ground (layer 8) - normal block
        // - Collides with Reflect (layer 11) - normal block
        // - Collides with Spike (layer 13) - normal block
        Physics2D.IgnoreLayerCollision(projectileShooterLayer, playerLayer, false);
        Physics2D.IgnoreLayerCollision(projectileShooterLayer, ghostLayer, false);
        Physics2D.IgnoreLayerCollision(projectileShooterLayer, groundLayer, false);
        Physics2D.IgnoreLayerCollision(projectileShooterLayer, reflectLayer, false);
        Physics2D.IgnoreLayerCollision(projectileShooterLayer, spikeLayer, false);
        
        // Spike (layer 13) collisions:
        // - Collides with Player (layer 6) - kills player
        // - Collides with Ghost (layer 7) - transforms ghost into falling ground
        // - Collides with Ground (layer 8) - normal block
        // - Collides with Reflect (layer 11) - normal block
        // - Collides with ProjectileShooter (layer 12) - normal block
        Physics2D.IgnoreLayerCollision(spikeLayer, playerLayer, false);
        Physics2D.IgnoreLayerCollision(spikeLayer, ghostLayer, false);
        Physics2D.IgnoreLayerCollision(spikeLayer, groundLayer, false);
        Physics2D.IgnoreLayerCollision(spikeLayer, reflectLayer, false);
        Physics2D.IgnoreLayerCollision(spikeLayer, projectileShooterLayer, false);
        
        Debug.Log("Layer collision setup complete!");
        Debug.Log($"Player layer: {playerLayer}, Ghost layer: {ghostLayer}, Ground layer: {groundLayer}");
        Debug.Log($"Projectile layer: {projectileLayer}, Reflect layer: {reflectLayer}, Spike layer: {spikeLayer}");
    }
    
    // Method to manually setup collisions (can be called from other scripts)
    public static void SetupCollisions()
    {
        int playerLayer = 6;
        int ghostLayer = 7;
        int groundLayer = 8;
        int projectileLayer = 10;
        int reflectLayer = 11;
        int spikeLayer = 13;
        
        // Set up the same collision matrix as above
        Physics2D.IgnoreLayerCollision(playerLayer, groundLayer, false);
        Physics2D.IgnoreLayerCollision(playerLayer, projectileLayer, false);
        Physics2D.IgnoreLayerCollision(playerLayer, spikeLayer, false);
        Physics2D.IgnoreLayerCollision(playerLayer, reflectLayer, false);
        Physics2D.IgnoreLayerCollision(playerLayer, ghostLayer, true); // Disable Player-Ghost collision
        
        Physics2D.IgnoreLayerCollision(ghostLayer, groundLayer, false);
        Physics2D.IgnoreLayerCollision(ghostLayer, projectileLayer, false);
        Physics2D.IgnoreLayerCollision(ghostLayer, spikeLayer, false);
        Physics2D.IgnoreLayerCollision(ghostLayer, reflectLayer, false);
        
        Physics2D.IgnoreLayerCollision(projectileLayer, groundLayer, false);
        Physics2D.IgnoreLayerCollision(projectileLayer, playerLayer, false);
        Physics2D.IgnoreLayerCollision(projectileLayer, ghostLayer, false);
        Physics2D.IgnoreLayerCollision(projectileLayer, reflectLayer, false);
        
        Physics2D.IgnoreLayerCollision(spikeLayer, playerLayer, false);
        Physics2D.IgnoreLayerCollision(spikeLayer, ghostLayer, false);
    }
} 