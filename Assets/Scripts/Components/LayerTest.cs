using UnityEngine;

public class LayerTest : MonoBehaviour
{
    [Header("Layer Test")]
    [SerializeField] private bool runLayerTest = true;
    
    private void Start()
    {
        if (runLayerTest)
        {
            TestLayerSetup();
        }
    }
    
    private void TestLayerSetup()
    {
        Debug.Log("=== LAYER COLLISION TEST ===");
        
        // Test layer assignments
        Debug.Log($"Player layer: {LayerMask.NameToLayer("Player")} (should be 6)");
        Debug.Log($"Ghost layer: {LayerMask.NameToLayer("Ghost")} (should be 7)");
        Debug.Log($"Ground layer: {LayerMask.NameToLayer("Ground")} (should be 8)");
        Debug.Log($"BG layer: {LayerMask.NameToLayer("BG")} (should be 9)");
        Debug.Log($"Projectile layer: {LayerMask.NameToLayer("Projectile")} (should be 10)");
        Debug.Log($"Reflect layer: {LayerMask.NameToLayer("Reflect")} (should be 11)");
        Debug.Log($"ProjectileShooter layer: {LayerMask.NameToLayer("ProjectileShooter")} (should be 12)");
        Debug.Log($"Spike layer: {LayerMask.NameToLayer("Spike")} (should be 13)");
        
        // Test collision matrix
        int playerLayer = 6;
        int ghostLayer = 7;
        int groundLayer = 8;
        int bgLayer = 9;
        int projectileLayer = 10;
        int reflectLayer = 11;
        int projectileShooterLayer = 12;
        int spikeLayer = 13;
        
        Debug.Log("=== COLLISION MATRIX TEST ===");
        
        // Test Player collisions
        Debug.Log($"Player-Ground collision: {!Physics2D.GetIgnoreLayerCollision(playerLayer, groundLayer)} (should be true)");
        Debug.Log($"Player-Projectile collision: {!Physics2D.GetIgnoreLayerCollision(playerLayer, projectileLayer)} (should be true)");
        Debug.Log($"Player-Spike collision: {!Physics2D.GetIgnoreLayerCollision(playerLayer, spikeLayer)} (should be true)");
        Debug.Log($"Player-Ghost collision: {!Physics2D.GetIgnoreLayerCollision(playerLayer, ghostLayer)} (should be false - they should pass through each other)");
        
        // Test Ghost collisions
        Debug.Log($"Ghost-Ground collision: {!Physics2D.GetIgnoreLayerCollision(ghostLayer, groundLayer)} (should be true)");
        Debug.Log($"Ghost-Projectile collision: {!Physics2D.GetIgnoreLayerCollision(ghostLayer, projectileLayer)} (should be true)");
        Debug.Log($"Ghost-Spike collision: {!Physics2D.GetIgnoreLayerCollision(ghostLayer, spikeLayer)} (should be true)");
        
        // Test Projectile collisions
        Debug.Log($"Projectile-Ground collision: {!Physics2D.GetIgnoreLayerCollision(projectileLayer, groundLayer)} (should be true)");
        Debug.Log($"Projectile-Player collision: {!Physics2D.GetIgnoreLayerCollision(projectileLayer, playerLayer)} (should be true)");
        Debug.Log($"Projectile-Ghost collision: {!Physics2D.GetIgnoreLayerCollision(projectileLayer, ghostLayer)} (should be true)");
        Debug.Log($"Projectile-Reflect collision: {!Physics2D.GetIgnoreLayerCollision(projectileLayer, reflectLayer)} (should be true)");
        
        // Test BG collisions (should be false with everything)
        Debug.Log($"BG-Player collision: {!Physics2D.GetIgnoreLayerCollision(bgLayer, playerLayer)} (should be false)");
        Debug.Log($"BG-Ground collision: {!Physics2D.GetIgnoreLayerCollision(bgLayer, groundLayer)} (should be false)");
        
        Debug.Log("=== LAYER TEST COMPLETE ===");
    }
} 