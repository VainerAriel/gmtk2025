using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;

public class AcidProjectile : MonoBehaviour
{
    [Header("Acid Projectile Settings")]
    [SerializeField] private float speed = 6f;
    [SerializeField] private float maxDistance = 50f;
    [SerializeField] private LayerMask collisionLayers = -1;
    
    [Header("Damage Over Time Settings")]
    [SerializeField] private float totalDamage = 100f;
    [SerializeField] private int numberOfTicks = 10;
    [SerializeField] private float tickInterval = 0.5f;
    [SerializeField] private float acidDuration = 5f;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject acidSplashEffect;
    [SerializeField] private GameObject acidPoolEffect;
    [SerializeField] private Color acidColor = Color.green;
    
    [Header("Tilemap Settings")]
    [SerializeField] private Tilemap tilemap;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    private Vector2 direction;
    private Vector2 startPosition;
    private float distanceTraveled;
    private bool hasHit = false;
    private bool canCollide = false; // New flag to prevent immediate collisions
    
    private GameObject acidPool;
    
    public void Initialize(Vector2 direction, float speed = -1f)
    {
        this.direction = direction.normalized;
        if (speed > 0)
        {
            this.speed = speed;
        }
        
        startPosition = transform.position;
        distanceTraveled = 0f;
        hasHit = false;
        canCollide = false;
        
        if (debugMode)
        {
            Debug.Log($"[AcidProjectile] Initialized with direction: {this.direction}, speed: {this.speed}, angle: {Mathf.Atan2(this.direction.y, this.direction.x) * Mathf.Rad2Deg}°");
        }
        
        // Start collision detection after a short delay
        StartCoroutine(EnableCollisionDetection());
    }
    
    private IEnumerator EnableCollisionDetection()
    {
        yield return new WaitForSeconds(0.1f); // Small delay to prevent immediate collisions
        canCollide = true;
        
        if (debugMode)
        {
            Debug.Log($"[AcidProjectile] Collision detection enabled");
        }
    }
    
    private void Update()
    {
        if (hasHit) return;
        
        // Move projectile
        Vector2 movement = direction * speed * Time.deltaTime;
        transform.Translate(movement);
        
        // Track distance
        distanceTraveled = Vector2.Distance(startPosition, transform.position);
        
        // Check if we've traveled too far
        if (distanceTraveled > maxDistance)
        {
            if (debugMode)
            {
                Debug.Log($"[AcidProjectile] Max distance reached, destroying projectile");
            }
            Destroy(gameObject);
        }
    }
    
    private void CheckCollisions()
    {
        if (hasHit || !canCollide) return;
        
        // Cast a ray in the direction of movement
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, speed * Time.deltaTime, collisionLayers);
        
        if (hit.collider != null)
        {
            if (debugMode)
            {
                Debug.Log($"[AcidProjectile] Hit detected: {hit.collider.name} at {hit.point}, layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
            }
            
            HandleCollision(hit);
        }
    }
    
    private void HandleCollision(RaycastHit2D hit)
    {
        if (hasHit) return; // Prevent multiple collisions
        
        hasHit = true;
        
        // Hide the projectile and disable collider immediately
        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<Collider2D>().enabled = false;
        
        // Create acid splash effect
        if (acidSplashEffect != null)
        {
            Instantiate(acidSplashEffect, hit.point, Quaternion.identity);
        }
        
        // Check if hit player
        PlayerController player = hit.collider.GetComponent<PlayerController>();
        if (player != null)
        {
            // Check if player is already poisoned - if so, don't start ANY damage
            if (player.IsPoisoned())
            {
                if (debugMode)
                {
                    Debug.Log($"[AcidProjectile] Player already poisoned, skipping damage");
                }
                
                // Destroy projectile after damage duration
                float projectileDestroyDelay = (numberOfTicks * tickInterval) + 1f;
                Destroy(gameObject, projectileDestroyDelay);
                return;
            }
            
            // Apply acid damage to player
            StartCoroutine(ApplyAcidDamage(player));
            
            // Destroy projectile after damage duration
            float playerHitDestroyDelay = (numberOfTicks * tickInterval) + 1f;
            Destroy(gameObject, playerHitDestroyDelay);
        }
        else
        {
            // Hit a wall/ground - create acid pool
            CreateAcidPoolOnTilemap(hit.point);
            
            // Destroy projectile after damage duration
            float wallHitDestroyDelay = (numberOfTicks * tickInterval) + 1f;
            Destroy(gameObject, wallHitDestroyDelay);
        }
    }
    
    private void CreateAcidPoolOnTilemap(Vector2 hitPoint)
    {
        if (acidPoolEffect == null || tilemap == null)
        {
            if (debugMode)
            {
                Debug.Log($"[AcidProjectile] No acid pool effect or tilemap assigned");
            }
            return;
        }
        
        // Convert world position to tilemap position
        Vector3Int tilePos = tilemap.WorldToCell(hitPoint);
        
        // Get the center of the tile at the hit point (instead of above)
        Vector3 poolSpawnPosition = tilemap.GetCellCenterWorld(tilePos);
        
        // Move the spawn position down by half a tile
        poolSpawnPosition.y -= tilemap.cellSize.y * 0.5f;
        
        // Check if there's already an acid pool at this location
        Collider2D[] existingPools = Physics2D.OverlapCircleAll(poolSpawnPosition, 0.1f);
        bool poolExists = false;
        
        foreach (Collider2D collider in existingPools)
        {
            AcidPool existingPool = collider.GetComponent<AcidPool>();
            if (existingPool != null)
            {
                poolExists = true;
                // Extend the life of the existing pool
                existingPool.ExtendLife(acidDuration);
                
                if (debugMode)
                {
                    Debug.Log($"[AcidProjectile] Extended life of existing acid pool at {poolSpawnPosition}");
                }
                break;
            }
        }
        
        if (!poolExists)
        {
            // Create new acid pool
            GameObject newPool = Instantiate(acidPoolEffect, poolSpawnPosition, Quaternion.identity);
            
            if (debugMode)
            {
                Debug.Log($"[AcidProjectile] Created new acid pool at {poolSpawnPosition}");
            }
        }
    }
    
    private IEnumerator ApplyAcidDamage(PlayerController player)
    {
        if (debugMode)
        {
            Debug.Log($"[AcidProjectile] Starting acid damage: {totalDamage} damage over {numberOfTicks} ticks");
        }
        
        // Mark player as poisoned
        player.SetPoisoned(true);
        
        float damagePerTick = totalDamage / numberOfTicks;
        float previousHealth = player.GetHealth();
        
        for (int i = 0; i < numberOfTicks; i++)
        {
            if (player != null)
            {
                // Check if player is no longer poisoned (indicating respawn)
                if (!player.IsPoisoned())
                {
                    if (debugMode)
                    {
                        Debug.Log($"[AcidProjectile] Player respawned, stopping acid damage");
                    }
                    break; // Player respawned, stop the damage
                }
                
                // Check if player died since last tick
                float currentHealth = player.GetHealth();
                if (currentHealth <= 0 && previousHealth > 0)
                {
                    if (debugMode)
                    {
                        Debug.Log($"[AcidProjectile] Player died during acid damage, stopping ticks");
                    }
                    break; // Player died, stop the damage
                }
                
                // Apply damage
                player.TakeDamage(damagePerTick);
                
                // Flash player with acid color
                StartCoroutine(FlashPlayer(player));
                
                if (debugMode)
                {
                    Debug.Log($"[AcidProjectile] Applied tick {i + 1}/{numberOfTicks}: {damagePerTick} damage. Player health: {player.GetHealth()}");
                }
                
                previousHealth = currentHealth;
            }
            else
            {
                // Player reference lost, stop acid damage
                if (debugMode)
                {
                    Debug.Log($"[AcidProjectile] Player reference lost, stopping acid damage");
                }
                break;
            }
            
            yield return new WaitForSeconds(tickInterval);
        }
        
        // Clear poison status when damage is complete (only if we set it)
        if (player != null && player.GetHealth() > 0 && player.IsPoisoned())
        {
            player.SetPoisoned(false);
        }
    }
    
    private IEnumerator FlashPlayer(PlayerController player)
    {
        SpriteRenderer playerSprite = player.GetComponent<SpriteRenderer>();
        if (playerSprite != null)
        {
            Color originalColor = playerSprite.color;
            playerSprite.color = acidColor;
            yield return new WaitForSeconds(0.1f);
            playerSprite.color = originalColor;
        }
    }
    
    private void DestroyAcidProjectile()
    {
        if (debugMode)
        {
            Debug.Log($"[AcidProjectile] Destroying acid projectile");
        }
        
        Destroy(gameObject);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit || !canCollide) return; // Don't process collisions if we can't collide yet
        
        if (debugMode)
        {
            Debug.Log($"[AcidProjectile] Trigger entered with: {other.name}");
        }
        
        // Check if it's a player directly
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            // Check if player is already poisoned - if so, don't start ANY damage
            if (player.IsPoisoned())
            {
                if (debugMode)
                {
                    Debug.Log($"[AcidProjectile] Player is already poisoned, ignoring trigger hit - NO damage started");
                }
                
                hasHit = true;
                GetComponent<SpriteRenderer>().enabled = false;
                GetComponent<Collider2D>().enabled = false;
                
                // Destroy after damage duration
                float triggerPoisonedDestroyDelay = (numberOfTicks * tickInterval) + 1f;
                Invoke(nameof(DestroyAcidProjectile), triggerPoisonedDestroyDelay);
                return;
            }
            
            if (debugMode)
            {
                Debug.Log($"[AcidProjectile] Direct player hit via trigger, starting acid damage - NO acid pool");
            }
            
            hasHit = true;
            
            // Start acid damage over time (NO acid pool for player hits)
            StartCoroutine(ApplyAcidDamage(player));
            
            // Hide the projectile immediately for player hits
            GetComponent<SpriteRenderer>().enabled = false;
            GetComponent<Collider2D>().enabled = false;
            
            // Destroy after damage duration
            float triggerPlayerHitDestroyDelay = (numberOfTicks * tickInterval) + 1f;
            Invoke(nameof(DestroyAcidProjectile), triggerPlayerHitDestroyDelay);
            
            return; // Exit early to prevent acid pool creation
        }
        
        // If it's not a player, handle it as a regular collision
        Vector2 hitPoint = other.ClosestPoint(transform.position);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, (hitPoint - (Vector2)transform.position).normalized, 0.1f);
        
        if (hit.collider == null)
        {
            // If raycast didn't work, create a simple collision point
            hit.point = hitPoint;
        }
        
        HandleCollision(hit);
    }
}
