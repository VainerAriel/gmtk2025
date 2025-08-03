using UnityEngine;

public class ProjectileShooter : MonoBehaviour
{
    [Header("Shooting Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float shootInterval = 2f;
    [SerializeField] private float shootAngle = 0f; // Angle in degrees (0 = right, 90 = up, 180 = left, 270 = down)
    [SerializeField] private float projectileSpeed = 8f; // Speed of the projectile
    [SerializeField] private Transform shootPoint;
    [SerializeField] private bool shootOnStart = true;
    [SerializeField] private bool rotateEmitter = true; // Whether the emitter sprite rotates with the shoot angle
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject shootEffect;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private SpriteRenderer emitterSprite; // Reference to the emitter's sprite renderer
    
    private float nextShootTime;
    private AudioSource audioSource;
    
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Ensure the shooter has collision components
        SetupCollision();
        
        // Set initial rotation based on shoot angle
        if (rotateEmitter)
        {
            UpdateEmitterRotation();
        }
        
        if (shootOnStart)
        {
            nextShootTime = Time.time + shootInterval;
        }
        else
        {
            nextShootTime = Time.time + shootInterval;
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
    
    private void Update()
    {
        if (Time.time >= nextShootTime)
        {
            ShootProjectile();
            nextShootTime = Time.time + shootInterval;
        }
    }
    
    private void ShootProjectile()
    {
        if (projectilePrefab == null) return;
        
        Vector3 spawnPosition = shootPoint != null ? shootPoint.position : transform.position;
        Vector2 direction = GetShootDirection();
        
        // Create projectile
        GameObject projectileObj = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        
        if (projectile != null)
        {
            projectile.Initialize(direction, projectileSpeed, gameObject);
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
    }
    
    private Vector2 GetShootDirection()
    {
        // Convert angle to radians and calculate direction
        float angleRadians = shootAngle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians));
    }
    
    private void UpdateEmitterRotation()
    {
        if (emitterSprite != null)
        {
            emitterSprite.transform.rotation = Quaternion.Euler(0, 0, shootAngle);
        }
        else
        {
            // If no specific sprite renderer assigned, rotate the main transform
            transform.rotation = Quaternion.Euler(0, 0, shootAngle);
        }
    }
    
    // Public methods for external control
    
    /// <summary>
    /// Set the shooting angle in degrees
    /// </summary>
    /// <param name="angle">Angle in degrees (0 = right, 90 = up, 180 = left, 270 = down)</param>
    public void SetShootAngle(float angle)
    {
        shootAngle = angle;
        if (rotateEmitter)
        {
            UpdateEmitterRotation();
        }
    }
    
    /// <summary>
    /// Reset the shooting timer to start counting from now
    /// </summary>
    public void ResetTimer()
    {
        nextShootTime = Time.time + shootInterval;
        Debug.Log($"[ProjectileShooter] Timer reset for {gameObject.name}");
    }
    
    /// <summary>
    /// Set the shooting direction using a Vector2
    /// </summary>
    /// <param name="direction">Normalized direction vector</param>
    public void SetShootDirection(Vector2 direction)
    {
        shootAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (rotateEmitter)
        {
            UpdateEmitterRotation();
        }
    }
    
    /// <summary>
    /// Rotate the emitter by a certain amount
    /// </summary>
    /// <param name="rotationAmount">Amount to rotate in degrees</param>
    public void RotateEmitter(float rotationAmount)
    {
        shootAngle += rotationAmount;
        // Keep angle between 0 and 360
        shootAngle = ((shootAngle % 360) + 360) % 360;
        if (rotateEmitter)
        {
            UpdateEmitterRotation();
        }
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
    /// Manually trigger a shot
    /// </summary>
    public void TriggerShoot()
    {
        ShootProjectile();
    }
    
    /// <summary>
    /// Change the shoot interval
    /// </summary>
    /// <param name="newInterval">New interval in seconds</param>
    public void SetShootInterval(float newInterval)
    {
        shootInterval = newInterval;
    }
    
    // Editor helper methods (can be called from other scripts or Unity events)
    
    /// <summary>
    /// Set emitter to shoot right (0 degrees)
    /// </summary>
    [ContextMenu("Set Direction: Right")]
    public void SetDirectionRight()
    {
        SetShootAngle(0f);
    }
    
    /// <summary>
    /// Set emitter to shoot up (90 degrees)
    /// </summary>
    [ContextMenu("Set Direction: Up")]
    public void SetDirectionUp()
    {
        SetShootAngle(90f);
    }
    
    /// <summary>
    /// Set emitter to shoot left (180 degrees)
    /// </summary>
    [ContextMenu("Set Direction: Left")]
    public void SetDirectionLeft()
    {
        SetShootAngle(180f);
    }
    
    /// <summary>
    /// Set emitter to shoot down (270 degrees)
    /// </summary>
    [ContextMenu("Set Direction: Down")]
    public void SetDirectionDown()
    {
        SetShootAngle(270f);
    }
    
    // Gizmos for visual debugging in the editor
    private void OnDrawGizmosSelected()
    {
        if (shootPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(shootPoint.position, 0.1f);
            
            // Draw shooting direction
            Vector2 direction = GetShootDirection();
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(shootPoint.position, direction * 2f);
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
            
            // Draw shooting direction
            Vector2 direction = GetShootDirection();
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, direction * 2f);
        }
    }
} 