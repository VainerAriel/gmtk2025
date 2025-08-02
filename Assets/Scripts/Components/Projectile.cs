using UnityEngine;
using System.Collections.Generic;

public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float defaultSpeed = 8f; // Default speed if not set by shooter
    [SerializeField] private float damage = 999f; // High damage for instant death
    [SerializeField] private float maxDistance = 50f;
    [SerializeField] private LayerMask collisionLayers = -1; // Will be set to exclude Ground layer
    [SerializeField] private LayerMask reflectLayers = -1;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private GameObject reflectEffect;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true; // Toggle debug messages
    
    private Vector2 direction;
    private Vector2 startPosition;
    private float distanceTraveled;
    private int maxBounces = 10; // Prevent infinite bouncing
    private int currentBounces = 0;
    private float speed; // Current speed of this projectile
    private HashSet<ReflectBlock> hitReflectBlocks = new HashSet<ReflectBlock>(); // Track which reflect blocks we've hit
    private GameObject shooter; // Reference to the shooter that created this projectile
    
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        startPosition = transform.position;
        speed = defaultSpeed; // Set default speed
        
        // Set up collision layers to exclude Ground layer (where shooters are)
        SetupCollisionLayers();
    }
    
    /// <summary>
    /// Sets up collision layers to prevent projectiles from hitting their own shooters but allow ground collision
    /// </summary>
    private void SetupCollisionLayers()
    {
        // Include Ground layer for collision (so bullets can hit ground and get destroyed)
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer != -1)
        {
            collisionLayers = collisionLayers | (1 << groundLayer);
        }
        
        // Set the projectile to the Projectile layer if it exists
        int projectileLayer = LayerMask.NameToLayer("Projectile");
        if (projectileLayer != -1)
        {
            gameObject.layer = projectileLayer;
        }
        
        if (debugMode)
        {
            Debug.Log($"[Projectile] Collision layers set to: {collisionLayers.value}, Layer: {LayerMask.LayerToName(gameObject.layer)}");
        }
    }
    
    /// <summary>
    /// Get the current direction of the projectile
    /// </summary>
    /// <returns>The normalized direction vector</returns>
    public Vector2 GetDirection()
    {
        return direction;
    }
    
    public void Initialize(Vector2 shootDirection, float projectileSpeed = -1f, GameObject shooter = null)
    {
        direction = shootDirection.normalized;
        distanceTraveled = 0f;
        currentBounces = 0;
        hitReflectBlocks.Clear(); // Clear the list of hit reflect blocks
        this.shooter = shooter; // Store reference to the shooter
        
        // Use provided speed or default
        if (projectileSpeed > 0)
        {
            speed = projectileSpeed;
        }
        
        // Set velocity for straight-line movement
        rb.velocity = direction * speed;
        
        // Rotate sprite to face direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        if (debugMode)
        {
            Debug.Log($"[Projectile] Initialized with direction: {direction}, speed: {speed}, shooter: {(shooter != null ? shooter.name : "null")}");
        }
    }
    
    private void FixedUpdate()
    {
        // Ensure projectile maintains straight-line movement
        rb.velocity = direction * speed;
        
        // Check if projectile has traveled too far
        distanceTraveled = Vector2.Distance(startPosition, transform.position);
        if (distanceTraveled > maxDistance)
        {
            DestroyProjectile();
            return;
        }
        
        // Check for collisions using raycast
        CheckCollisions();
    }
    
    private void CheckCollisions()
    {
        Vector2 currentPos = transform.position;
        Vector2 nextPos = currentPos + direction * speed * Time.fixedDeltaTime;
        
        // Cast ray from current position to next position
        RaycastHit2D hit = Physics2D.Raycast(currentPos, direction, speed * Time.fixedDeltaTime, collisionLayers);
        
        if (hit.collider != null)
        {
            if (debugMode)
            {
                Debug.Log($"[Projectile] Hit detected: {hit.collider.name} at {hit.point}");
            }
            HandleCollision(hit);
        }
    }
    
    private void HandleCollision(RaycastHit2D hit)
    {
        if (debugMode)
        {
            Debug.Log($"[Projectile] Handling collision with: {hit.collider.name}");
        }
        
        // Ignore collision with the shooter that created this projectile
        if (shooter != null && hit.collider.gameObject == shooter)
        {
            if (debugMode)
            {
                Debug.Log($"[Projectile] Ignoring collision with own shooter: {shooter.name}");
            }
            return;
        }
        
        // Ignore collision with any shooter (ProjectileShooter or TilemapProjectileShooter components)
        ProjectileShooter projectileShooter = hit.collider.GetComponent<ProjectileShooter>();
        TilemapProjectileShooter tilemapShooter = hit.collider.GetComponent<TilemapProjectileShooter>();
        if (projectileShooter != null || tilemapShooter != null)
        {
            if (debugMode)
            {
                Debug.Log($"[Projectile] Ignoring collision with shooter: {hit.collider.name}");
            }
            return;
        }
        
        // Check if it's a reflect block
        ReflectBlock reflectBlock = hit.collider.GetComponent<ReflectBlock>();
        if (reflectBlock != null)
        {
            if (debugMode)
            {
                Debug.Log($"[Projectile] Found ReflectBlock component on {hit.collider.name}");
            }
            
            if (!hitReflectBlocks.Contains(reflectBlock))
            {
                if (debugMode)
                {
                    Debug.Log($"[Projectile] Adding {reflectBlock.name} to hit list and reflecting");
                }
                
                // Add this reflect block to our hit list
                hitReflectBlocks.Add(reflectBlock);
                
                // Handle the reflection
                ReflectProjectile(hit, reflectBlock);
                return;
            }
            else
            {
                if (debugMode)
                {
                    Debug.Log($"[Projectile] Already hit {reflectBlock.name}, ignoring collision");
                }
                // Already hit this reflect block - ignore the collision
                return;
            }
        }

        // Check if it's the player
        PlayerController player = hit.collider.GetComponent<PlayerController>();
        if (player != null)
        {
            if (debugMode)
            {
                Debug.Log($"[Projectile] Hit player, dealing {damage} damage");
            }
            
            player.TakeDamage(damage);
            
            if (hitEffect != null)
            {
                Instantiate(hitEffect, hit.point, Quaternion.identity);
            }
            DestroyProjectile();
            return;
        }
        
        // Check if it's ground collision
        if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            if (debugMode)
            {
                Debug.Log($"[Projectile] Hit ground at {hit.point}, destroying projectile");
            }
            
            if (hitEffect != null)
            {
                Instantiate(hitEffect, hit.point, Quaternion.identity);
            }
            DestroyProjectile();
            return;
        }
        
        // Regular collision with other objects (walls, etc.)
        if (debugMode)
        {
            Debug.Log($"[Projectile] Regular collision with {hit.collider.name}, destroying projectile");
        }
        
        if (hitEffect != null)
        {
            Instantiate(hitEffect, hit.point, Quaternion.identity);
        }
        DestroyProjectile();
    }
    
    private void ReflectProjectile(RaycastHit2D hit, ReflectBlock reflectBlock)
    {
        currentBounces++;
        
        if (debugMode)
        {
            Debug.Log($"[Projectile] Reflecting projectile! Bounce {currentBounces}/{maxBounces}");
        }
        
        if (currentBounces >= maxBounces)
        {
            if (debugMode)
            {
                Debug.Log($"[Projectile] Max bounces reached, destroying projectile");
            }
            DestroyProjectile();
            return;
        }
        
        // Get reflection direction based on reflect block type
        Vector2 reflectionDirection = reflectBlock.GetReflectionDirection(direction, hit.normal);
        
        // Check if reflection direction is zero (invalid input)
        if (reflectionDirection == Vector2.zero)
        {
            if (debugMode)
            {
                Debug.Log($"[Projectile] Invalid input direction, destroying projectile");
            }
            // Invalid input direction - destroy the projectile
            if (hitEffect != null)
            {
                Instantiate(hitEffect, hit.point, Quaternion.identity);
            }
            DestroyProjectile();
            return;
        }
        
        if (debugMode)
        {
            Debug.Log($"[Projectile] Old direction: {direction} -> New direction: {reflectionDirection}");
        }
        
        // Get the center position of the reflect block's collider
        Vector2 reflectBlockCenter;
        Collider2D blockCollider = reflectBlock.GetComponent<Collider2D>();
        if (blockCollider != null)
        {
            reflectBlockCenter = blockCollider.bounds.center;
        }
        else
        {
            reflectBlockCenter = reflectBlock.transform.position;
        }
        
        // Move projectile to the center of the reflect block
        transform.position = reflectBlockCenter;
        
        // Update direction and velocity for straight-line movement
        direction = reflectionDirection;
        rb.velocity = direction * speed;
        
        // Update sprite rotation
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        // Spawn reflect effect at the center
        if (reflectEffect != null)
        {
            Instantiate(reflectEffect, reflectBlockCenter, Quaternion.identity);
        }
        
        // Play reflect sound
        reflectBlock.PlayReflectSound();
    }
    
    private void DestroyProjectile()
    {
        if (debugMode)
        {
            Debug.Log($"[Projectile] Destroying projectile at {transform.position}");
        }
        Destroy(gameObject);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (debugMode)
        {
            Debug.Log($"[Projectile] Trigger entered with: {other.name}");
        }
        
        // Only handle player damage in trigger, ignore reflect blocks
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            if (debugMode)
            {
                Debug.Log($"[Projectile] Trigger hit player, dealing {damage} damage");
            }
            
            player.TakeDamage(damage);
            
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }
            DestroyProjectile();
        }
    }
    
    // Gizmos for visual debugging
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, direction * 2f);
    }
} 