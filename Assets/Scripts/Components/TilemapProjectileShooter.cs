using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapProjectileShooter : MonoBehaviour
{
    [Header("Tilemap Settings")]
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float shootInterval = 2f;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private bool shootOnStart = true;
    
    [Header("Shooting Direction")]
    [SerializeField] private Vector2 shootDirection = Vector2.left; // Set this based on the direction (Left, Right, Up, Down)
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject shootEffect;
    [SerializeField] private AudioClip shootSound;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    
    private AudioSource audioSource;
    private float nextShootTime;
    
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Ensure the shooter has collision components
        SetupCollision();
        
        // If no tilemap assigned, try to get it from this GameObject
        if (tilemap == null)
        {
            tilemap = GetComponent<Tilemap>();
        }
        
        if (shootOnStart)
        {
            nextShootTime = Time.time + shootInterval;
        }
        else
        {
            nextShootTime = Time.time + shootInterval;
        }
        
        if (debugMode)
        {
            Debug.Log($"[TilemapProjectileShooter] {gameObject.name} initialized with direction: {shootDirection}");
        }
    }
    
    /// <summary>
    /// Ensures the shooter has proper collision components to act as a solid block
    /// </summary>
    private void SetupCollision()
    {
        // Add BoxCollider2D if it doesn't exist
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider2D>();
            // Set default size to 1x1 tile
            boxCollider.size = Vector2.one;
            boxCollider.offset = Vector2.zero;
        }
        
        // Ensure it's not a trigger (should be solid)
        boxCollider.isTrigger = false;
        
        // Add Rigidbody2D if it doesn't exist (for static collision)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static; // Static so it doesn't move
            rb.simulated = true; // Enable physics simulation
        }
        
        // Ensure it's on the ProjectileShooter layer (layer 12) for proper collision
        if (gameObject.layer != 12)
        {
            gameObject.layer = 12;
        }
    }
    
    /// <summary>
    /// Reset the shooting timer to start counting from now
    /// </summary>
    public void ResetTimer()
    {
        nextShootTime = Time.time + shootInterval;
        Debug.Log($"[TilemapProjectileShooter] Timer reset for {gameObject.name}");
    }
    
    private void Update()
    {
        if (Time.time >= nextShootTime)
        {
            ShootFromAllTiles();
            nextShootTime = Time.time + shootInterval;
        }
    }
    
    private void ShootFromAllTiles()
    {
        if (tilemap == null || projectilePrefab == null) return;
        
        // Get all tiles in the tilemap
        BoundsInt bounds = tilemap.cellBounds;
        
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int tilePosition = new Vector3Int(x, y, 0);
                TileBase tile = tilemap.GetTile(tilePosition);
                
                if (tile != null)
                {
                    // Convert tile position to world position
                    Vector3 worldPosition = tilemap.GetCellCenterWorld(tilePosition);
                    
                    // Shoot projectile from this tile position
                    ShootProjectile(worldPosition);
                }
            }
        }
    }
    
    private void ShootProjectile(Vector3 spawnPosition)
    {
        // Create projectile
        GameObject projectileObj = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        
        if (projectile != null)
        {
            projectile.Initialize(shootDirection, projectileSpeed, gameObject);
        }
        
        // Visual and audio effects
        if (shootEffect != null)
        {
            Instantiate(shootEffect, spawnPosition, Quaternion.identity);
        }
        
        if (shootSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
        
        if (debugMode)
        {
            Debug.Log($"[TilemapProjectileShooter] {gameObject.name} shot projectile from {spawnPosition} in direction {shootDirection}");
        }
    }
    
    // Public methods for external control
    
    /// <summary>
    /// Set the shooting direction
    /// </summary>
    /// <param name="direction">Direction vector</param>
    public void SetShootDirection(Vector2 direction)
    {
        shootDirection = direction.normalized;
    }
    
    /// <summary>
    /// Set the projectile speed
    /// </summary>
    /// <param name="speed">New projectile speed</param>
    public void SetProjectileSpeed(float speed)
    {
        projectileSpeed = speed;
    }
    
    /// <summary>
    /// Change the shoot interval
    /// </summary>
    /// <param name="newInterval">New interval in seconds</param>
    public void SetShootInterval(float newInterval)
    {
        shootInterval = newInterval;
    }
    
    // Gizmos for visual debugging
    private void OnDrawGizmosSelected()
    {
        if (tilemap != null)
        {
            Gizmos.color = Color.red;
            BoundsInt bounds = tilemap.cellBounds;
            
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector3Int tilePosition = new Vector3Int(x, y, 0);
                    TileBase tile = tilemap.GetTile(tilePosition);
                    
                    if (tile != null)
                    {
                        Vector3 worldPosition = tilemap.GetCellCenterWorld(tilePosition);
                        Gizmos.DrawWireCube(worldPosition, Vector3.one * 0.8f);
                        
                        // Draw shooting direction
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawRay(worldPosition, shootDirection * 1f);
                        Gizmos.color = Color.red;
                    }
                }
            }
        }
    }
} 