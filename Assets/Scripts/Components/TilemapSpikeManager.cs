using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class TilemapSpikeManager : MonoBehaviour
{
    [Header("Tilemap Settings")]
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase spikeTile; // The tile that represents spikes
    [SerializeField] private float damage = 30f;
    [SerializeField] private float activeTime = 2f;
    [SerializeField] private float inactiveTime = 1f;
    [SerializeField] private bool startActive = true;
    [SerializeField] private Vector2 collisionSize = new Vector2(0.6f, 0.6f); // Size of the collision area for each spike
    // Removed randomizeStart - spikes will always start in the configured state
    
    [Header("Visual Effects")]
    [SerializeField] private Color activeColor = Color.red;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private GameObject spikeEffect;
    [SerializeField] private AudioClip spikeSound;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    
    private AudioSource audioSource;
    private bool isActive = false;
    private Coroutine spikeCycle;
    private List<Vector3Int> spikePositions = new List<Vector3Int>();
    private Collider2D tilemapCollider; // Add collider reference
    
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Get tilemap if not assigned
        if (tilemap == null)
        {
            tilemap = GetComponent<Tilemap>();
        }
        
        // Get collider for enabling/disabling
        tilemapCollider = GetComponent<Collider2D>();
        
        // Apply collision size to the collider
        ApplyCollisionSize();
        
        // Find all spike tiles in the tilemap
        FindSpikeTiles();
        
        // Initialize spike state - no randomization, just use startActive
        isActive = startActive;
        UpdateSpikeVisuals();
        
        // Start the spike cycle
        spikeCycle = StartCoroutine(SpikeCycle());
        
        if (debugMode)
        {
            Debug.Log($"[TilemapSpikeManager] {gameObject.name} initialized - Active: {isActive}, ActiveTime: {activeTime}s, InactiveTime: {inactiveTime}s, SpikeTiles: {spikePositions.Count}");
        }
    }
    
    private void ApplyCollisionSize()
    {
        if (tilemapCollider == null) return;
        
        // Apply size to different collider types
        if (tilemapCollider is BoxCollider2D boxCollider)
        {
            boxCollider.size = collisionSize;
        }
        else if (tilemapCollider is CircleCollider2D circleCollider)
        {
            circleCollider.radius = Mathf.Min(collisionSize.x, collisionSize.y) * 0.5f;
        }
        else if (tilemapCollider is CapsuleCollider2D capsuleCollider)
        {
            capsuleCollider.size = collisionSize;
        }
        
        if (debugMode)
        {
            Debug.Log($"[TilemapSpikeManager] {gameObject.name} collision size set to: {collisionSize}");
        }
    }
    
    /// <summary>
    /// Check if the player is protected by falling ground from this spike
    /// </summary>
    /// <param name="player">The player controller</param>
    /// <returns>True if the player is protected by falling ground</returns>
    private bool IsPlayerProtectedByFallingGround(PlayerController player)
    {
        // For tilemap spikes, we need to check from the player's position downward
        Vector2 rayStart = player.transform.position;
        Vector2 rayDirection = Vector2.down;
        float rayDistance = 3f; // Check down to 3 units below the player
        
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
                // Check if there's a spike tile below this falling ground
                Vector3Int tilePosition = tilemap.WorldToCell(hit.collider.transform.position);
                TileBase tile = tilemap.GetTile(tilePosition);
                
                if (tile == spikeTile)
                {
                    if (debugMode)
                    {
                        Debug.Log($"[TilemapSpikeManager] {gameObject.name} found falling ground protection: {hit.collider.name}");
                    }
                    return true;
                }
            }
        }
        
        return false;
    }
    
    private void FindSpikeTiles()
    {
        spikePositions.Clear();
        
        if (tilemap == null || spikeTile == null) return;
        
        BoundsInt bounds = tilemap.cellBounds;
        
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int tilePosition = new Vector3Int(x, y, 0);
                TileBase tile = tilemap.GetTile(tilePosition);
                
                if (tile == spikeTile)
                {
                    spikePositions.Add(tilePosition);
                }
            }
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
                    Debug.Log($"[TilemapSpikeManager] {gameObject.name} is active for {activeTime} seconds");
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
                    Debug.Log($"[TilemapSpikeManager] {gameObject.name} is inactive for {inactiveTime} seconds");
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
            Debug.Log($"[TilemapSpikeManager] {gameObject.name} set to {(active ? "ACTIVE" : "INACTIVE")}");
        }
    }
    
    private void UpdateSpikeVisuals()
    {
        if (tilemap == null) return;
        
        foreach (Vector3Int position in spikePositions)
        {
            // Update tile color with transparency
            Color tileColor = isActive ? activeColor : inactiveColor;
            tileColor.a = isActive ? 1f : 0.5f; // Full opacity when active, half transparent when inactive
            
            tilemap.SetTileFlags(position, TileFlags.None); // Remove all flags to allow color changes
            tilemap.SetColor(position, tileColor);
        }
        
        // Update collider - disable when inactive, enable when active
        if (tilemapCollider != null)
        {
            tilemapCollider.enabled = isActive; // Disable collider when inactive
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return; // Only damage when active
        
        if (debugMode)
        {
            Debug.Log($"[TilemapSpikeManager] {gameObject.name} OnTriggerEnter2D with: {other.name}");
        }
        
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            // Check if there's falling ground protecting the player from the spike
            if (IsPlayerProtectedByFallingGround(player))
            {
                if (debugMode)
                {
                    Debug.Log($"[TilemapSpikeManager] {gameObject.name} player is protected by falling ground, no damage dealt");
                }
                return; // Don't damage the player
            }
            
            if (debugMode)
            {
                Debug.Log($"[TilemapSpikeManager] {gameObject.name} hit player, dealing {damage} damage");
            }
            
            player.TakeDamage(damage);
            
            // Visual and audio effects
            if (spikeEffect != null)
            {
                Instantiate(spikeEffect, other.transform.position, Quaternion.identity);
            }
            
            if (spikeSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(spikeSound);
            }
        }
        
        // Check for ghost collision
        GhostController ghost = other.GetComponent<GhostController>();
        if (ghost != null)
        {
            if (debugMode)
            {
                Debug.Log($"[TilemapSpikeManager] {gameObject.name} hit ghost, triggering transformation");
            }
            
            // The ghost will handle its own transformation
        }
    }
    
    private void CheckForOverlappingObjects()
    {
        if (debugMode)
        {
            Debug.Log($"[TilemapSpikeManager] {gameObject.name} checking for overlapping objects...");
        }
        
        // Get all colliders overlapping with this tilemap spike manager
        Collider2D[] overlappingColliders = new Collider2D[10];
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true;
        filter.useLayerMask = false;
        
        // Use the tilemap's collider or create a temporary one for overlap detection
        Collider2D tilemapCollider = GetComponent<Collider2D>();
        if (tilemapCollider == null)
        {
            // If no collider, create a temporary one for overlap detection
            BoxCollider2D tempCollider = gameObject.AddComponent<BoxCollider2D>();
            tempCollider.isTrigger = true;
            tempCollider.size = new Vector2(10f, 10f); // Large enough to cover the tilemap area
            
            int count = tempCollider.OverlapCollider(filter, overlappingColliders);
            
            if (debugMode)
            {
                Debug.Log($"[TilemapSpikeManager] {gameObject.name} found {count} overlapping colliders");
            }
            
            for (int i = 0; i < count; i++)
            {
                Collider2D other = overlappingColliders[i];
                if (other != null && other != tempCollider)
                {
                    if (debugMode)
                    {
                        Debug.Log($"[TilemapSpikeManager] {gameObject.name} overlapping with: {other.name}");
                    }
                    // Simulate trigger enter for overlapping objects
                    OnTriggerEnter2D(other);
                }
            }
            
            // Remove the temporary collider
            DestroyImmediate(tempCollider);
        }
        else
        {
            int count = tilemapCollider.OverlapCollider(filter, overlappingColliders);
            
            if (debugMode)
            {
                Debug.Log($"[TilemapSpikeManager] {gameObject.name} found {count} overlapping colliders");
            }
            
            for (int i = 0; i < count; i++)
            {
                Collider2D other = overlappingColliders[i];
                if (other != null && other != tilemapCollider)
                {
                    if (debugMode)
                    {
                        Debug.Log($"[TilemapSpikeManager] {gameObject.name} overlapping with: {other.name}");
                    }
                    // Simulate trigger enter for overlapping objects
                    OnTriggerEnter2D(other);
                }
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
            Debug.Log($"[TilemapSpikeManager] {gameObject.name} timing updated - Active: {activeTime}s, Inactive: {inactiveTime}s");
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
        StopCycle();
        
        // Reset to initial state
        isActive = startActive;
        UpdateSpikeVisuals();
        
        // Restart the cycle
        spikeCycle = StartCoroutine(SpikeCycle());
        
        if (debugMode)
        {
            Debug.Log($"[TilemapSpikeManager] {gameObject.name} cycle restarted - Reset to initial state: {(isActive ? "ACTIVE" : "INACTIVE")}");
        }
    }
    
    /// <summary>
    /// Refresh the spike tile positions (call this if you modify the tilemap)
    /// </summary>
    public void RefreshSpikeTiles()
    {
        FindSpikeTiles();
        UpdateSpikeVisuals();
        
        if (debugMode)
        {
            Debug.Log($"[TilemapSpikeManager] {gameObject.name} refreshed - Found {spikePositions.Count} spike tiles");
        }
    }
    
    /// <summary>
    /// Set the collision size for the tilemap spikes
    /// </summary>
    /// <param name="size">New collision size</param>
    public void SetCollisionSize(Vector2 size)
    {
        collisionSize = size;
        ApplyCollisionSize();
        
        if (debugMode)
        {
            Debug.Log($"[TilemapSpikeManager] {gameObject.name} collision size changed to: {size}");
        }
    }
    
    // Gizmos for visual debugging
    private void OnDrawGizmosSelected()
    {
        if (tilemap != null && spikePositions.Count > 0)
        {
            Gizmos.color = isActive ? Color.red : Color.gray;
            
            foreach (Vector3Int position in spikePositions)
            {
                Vector3 worldPosition = tilemap.GetCellCenterWorld(position);
                Gizmos.DrawWireCube(worldPosition, Vector3.one * 0.8f);
                
                // Draw collision area
                Gizmos.color = isActive ? Color.yellow : Color.cyan;
                Gizmos.DrawWireCube(worldPosition, new Vector3(collisionSize.x, collisionSize.y, 0.1f));
            }
        }
    }
} 