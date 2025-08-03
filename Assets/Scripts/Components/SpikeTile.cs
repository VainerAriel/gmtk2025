using UnityEngine;
using System.Collections;

public class SpikeTile : MonoBehaviour
{
    [Header("Spike Settings")]
    [SerializeField] private float damage = 30f;
    [SerializeField] private float activeTime = 2f;
    [SerializeField] private float inactiveTime = 1f;
    [SerializeField] private bool startActive = true;
    [SerializeField] private bool randomizeStart = false;
    [SerializeField] private Vector2 collisionSize = new Vector2(0.6f, 0.6f); // Size of the collision area
    
    [Header("Visual Effects")]
    [SerializeField] private Sprite activeSprite;
    [SerializeField] private Sprite inactiveSprite;
    [SerializeField] private Color activeColor = Color.red;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private GameObject spikeEffect;
    [SerializeField] private AudioClip spikeSound;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    
    private SpriteRenderer spriteRenderer;
    private Collider2D spikeCollider;
    private AudioSource audioSource;
    private bool isActive = false;
    private Coroutine spikeCycle;
    
    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spikeCollider = GetComponent<Collider2D>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Set spike to layer 13
        gameObject.layer = 13;
        
        // Apply collision size to the collider
        ApplyCollisionSize();
        
        // Initialize spike state
        if (randomizeStart)
        {
            startActive = Random.value > 0.5f;
        }
        
        isActive = startActive;
        UpdateSpikeVisuals();
        
        // Start the spike cycle
        spikeCycle = StartCoroutine(SpikeCycle());
        
        if (debugMode)
        {
            Debug.Log($"[SpikeTile] {gameObject.name} initialized - Active: {isActive}, ActiveTime: {activeTime}s, InactiveTime: {inactiveTime}s, Layer: {gameObject.layer}");
        }
    }
    
    private IEnumerator SpikeCycle()
    {
        while (true)
        {
            if (isActive)
            {
                // Spike is active - wait for active time
                if (debugMode)
                {
                    Debug.Log($"[SpikeTile] {gameObject.name} is active for {activeTime} seconds");
                }
                yield return new WaitForSeconds(activeTime);
                
                // Deactivate spike
                SetSpikeActive(false);
            }
            else
            {
                // Spike is inactive - wait for inactive time
                if (debugMode)
                {
                    Debug.Log($"[SpikeTile] {gameObject.name} is inactive for {inactiveTime} seconds");
                }
                yield return new WaitForSeconds(inactiveTime);
                
                // Activate spike
                SetSpikeActive(true);
            }
        }
    }
    
    private void SetSpikeActive(bool active)
    {
        isActive = active;
        UpdateSpikeVisuals();
        
        // If becoming active, check for overlapping objects
        if (active)
        {
            CheckForOverlappingObjects();
        }
        
        if (debugMode)
        {
            Debug.Log($"[SpikeTile] {gameObject.name} set to {(active ? "ACTIVE" : "INACTIVE")}");
        }
    }
    
    private void ApplyCollisionSize()
    {
        if (spikeCollider == null) return;
        
        // Apply size to different collider types
        if (spikeCollider is BoxCollider2D boxCollider)
        {
            boxCollider.size = collisionSize;
        }
        else if (spikeCollider is CircleCollider2D circleCollider)
        {
            circleCollider.radius = Mathf.Min(collisionSize.x, collisionSize.y) * 0.5f;
        }
        else if (spikeCollider is CapsuleCollider2D capsuleCollider)
        {
            capsuleCollider.size = collisionSize;
        }
        
        if (debugMode)
        {
            Debug.Log($"[SpikeTile] {gameObject.name} collision size set to: {collisionSize}");
        }
    }
    
    /// <summary>
    /// Check if the player is protected by falling ground from this spike
    /// </summary>
    /// <param name="player">The player controller</param>
    /// <returns>True if the player is protected by falling ground</returns>
    private bool IsPlayerProtectedByFallingGround(PlayerController player)
    {
        // Cast a ray from the spike position upward to check for falling ground
        Vector2 rayStart = transform.position;
        Vector2 rayDirection = Vector2.up;
        float rayDistance = 2f; // Check up to 2 units above the spike
        
        RaycastHit2D[] hits = Physics2D.RaycastAll(rayStart, rayDirection, rayDistance);
        
        if (debugMode)
        {
            Debug.DrawRay(rayStart, rayDirection * rayDistance, Color.yellow, 1f);
        }
        
        foreach (RaycastHit2D hit in hits)
        {
            // Check if we hit falling ground
            if (hit.collider.CompareTag("FallingGround") || hit.collider.gameObject.layer == 8) // Layer 8 is Ground
            {
                // Check if the player is above this falling ground
                if (player.transform.position.y > hit.collider.transform.position.y)
                {
                    if (debugMode)
                    {
                        Debug.Log($"[SpikeTile] {gameObject.name} found falling ground protection: {hit.collider.name}");
                    }
                    return true;
                }
            }
        }
        
        return false;
    }
    
    private void UpdateSpikeVisuals()
    {
        if (spriteRenderer != null)
        {
            // Update sprite
            if (isActive && activeSprite != null)
            {
                spriteRenderer.sprite = activeSprite;
            }
            else if (!isActive && inactiveSprite != null)
            {
                spriteRenderer.sprite = inactiveSprite;
            }
            
            // Update color and transparency
            Color currentColor = isActive ? activeColor : inactiveColor;
            currentColor.a = isActive ? 1f : 0.5f; // Full opacity when active, half transparent when inactive
            spriteRenderer.color = currentColor;
        }
        
        // Update collider - disable when inactive, enable when active
        if (spikeCollider != null)
        {
            spikeCollider.enabled = isActive; // Disable collider when inactive
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return; // Only process collisions when active
        
        if (debugMode)
        {
            Debug.Log($"[SpikeTile] {gameObject.name} OnTriggerEnter2D with: {other.name}");
        }
        
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            // Check if there's falling ground protecting the player from the spike
            if (IsPlayerProtectedByFallingGround(player))
            {
                if (debugMode)
                {
                    Debug.Log($"[SpikeTile] {gameObject.name} player is protected by falling ground, no damage dealt");
                }
                return; // Don't damage the player
            }
            
            if (debugMode)
            {
                Debug.Log($"[SpikeTile] {gameObject.name} hit player, dealing {damage} damage");
            }
            
            player.TakeDamage(damage);
            
            // Visual and audio effects
            if (spikeEffect != null)
            {
                Instantiate(spikeEffect, transform.position, Quaternion.identity);
            }
            
            if (spikeSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(spikeSound);
            }
        }
        
        // Check for ghost collision - ONLY when active
        GhostController ghost = other.GetComponent<GhostController>();
        if (ghost != null && isActive)
        {
            if (debugMode)
            {
                Debug.Log($"[SpikeTile] {gameObject.name} hit ghost, triggering transformation");
            }
            
            // The ghost will handle its own transformation
        }
    }
    
    private void CheckForOverlappingObjects()
    {
        if (spikeCollider == null) return;
        
        if (debugMode)
        {
            Debug.Log($"[SpikeTile] {gameObject.name} checking for overlapping objects...");
        }
        
        // Get all colliders overlapping with this spike (collider is always enabled now)
        Collider2D[] overlappingColliders = new Collider2D[10];
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true;
        filter.useLayerMask = false;
        
        int count = spikeCollider.OverlapCollider(filter, overlappingColliders);
        
        if (debugMode)
        {
            Debug.Log($"[SpikeTile] {gameObject.name} found {count} overlapping colliders");
        }
        
        for (int i = 0; i < count; i++)
        {
            Collider2D other = overlappingColliders[i];
            if (other != null && other != spikeCollider)
            {
                if (debugMode)
                {
                    Debug.Log($"[SpikeTile] {gameObject.name} overlapping with: {other.name}");
                }
                // Simulate trigger enter for overlapping objects
                OnTriggerEnter2D(other);
            }
        }
    }
    
    // Public methods for external control
    
    /// <summary>
    /// Manually activate the spike
    /// </summary>
    public void ActivateSpike()
    {
        SetSpikeActive(true);
    }
    
    /// <summary>
    /// Manually deactivate the spike
    /// </summary>
    public void DeactivateSpike()
    {
        SetSpikeActive(false);
    }
    
    /// <summary>
    /// Toggle the spike state
    /// </summary>
    public void ToggleSpike()
    {
        SetSpikeActive(!isActive);
    }
    
    /// <summary>
    /// Set the timing for the spike cycle
    /// </summary>
    /// <param name="activeTime">How long the spike stays active</param>
    /// <param name="inactiveTime">How long the spike stays inactive</param>
    public void SetTiming(float activeTime, float inactiveTime)
    {
        this.activeTime = activeTime;
        this.inactiveTime = inactiveTime;
        
        if (debugMode)
        {
            Debug.Log($"[SpikeTile] {gameObject.name} timing updated - Active: {activeTime}s, Inactive: {inactiveTime}s");
        }
    }
    
    /// <summary>
    /// Stop the automatic cycle
    /// </summary>
    public void StopCycle()
    {
        if (spikeCycle != null)
        {
            StopCoroutine(spikeCycle);
            spikeCycle = null;
        }
    }
    
    /// <summary>
    /// Restart the automatic cycle and reset to initial state
    /// </summary>
    public void RestartCycle()
    {
        if (spikeCycle != null)
        {
            StopCoroutine(spikeCycle);
        }
        
        isActive = startActive;
        UpdateSpikeVisuals();
        spikeCycle = StartCoroutine(SpikeCycle());
        
        if (debugMode)
        {
            Debug.Log($"[SpikeTile] {gameObject.name} cycle restarted - Reset to initial state: {(isActive ? "ACTIVE" : "INACTIVE")}");
        }
    }
    
    /// <summary>
    /// Check if the spike is currently active and dangerous
    /// </summary>
    /// <returns>True if the spike is active and can damage the player</returns>
    public bool IsActive()
    {
        return isActive;
    }
    
    /// <summary>
    /// Set the collision size for this spike
    /// </summary>
    /// <param name="size">New collision size</param>
    public void SetCollisionSize(Vector2 size)
    {
        collisionSize = size;
        ApplyCollisionSize();
        
        if (debugMode)
        {
            Debug.Log($"[SpikeTile] {gameObject.name} collision size changed to: {size}");
        }
    }
    
    // Gizmos for visual debugging
    private void OnDrawGizmosSelected()
    {
        // Draw spike state in scene view
        if (debugMode)
        {
            Gizmos.color = isActive ? Color.red : Color.gray;
            Gizmos.DrawWireCube(transform.position, Vector3.one);
            
            // Draw collision area
            Gizmos.color = isActive ? Color.yellow : Color.cyan;
            Gizmos.DrawWireCube(transform.position, new Vector3(collisionSize.x, collisionSize.y, 0.1f));
        }
    }
} 