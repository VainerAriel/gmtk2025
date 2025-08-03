using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class BreakableTilemap : MonoBehaviour
{
    [Header("Breakable Tilemap Settings")]
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private Color normalColor = Color.gray;
    [SerializeField] private Color damagedColor = Color.red;
    [SerializeField] private float destructionDelay = 0.1f;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject destructionEffect;
    [SerializeField] private AudioClip destructionSound;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    private AudioSource audioSource;
    private Dictionary<Vector3Int, TileBase> originalTiles = new Dictionary<Vector3Int, TileBase>();
    
    private void Start()
    {
        // Get or add audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Set to Ground layer (layer 8)
        gameObject.layer = 8;
        
        // If no tilemap assigned, try to get it from this GameObject
        if (tilemap == null)
        {
            tilemap = GetComponent<Tilemap>();
        }
        
        if (tilemap == null)
        {
            Debug.LogError("[BreakableTilemap] No Tilemap component found!");
        }
        
        // Note: This component should be assigned to the BreakableTilemapManager in the inspector
        
        if (debugMode)
        {
            Debug.Log($"[BreakableTilemap] {gameObject.name} initialized with tilemap: {(tilemap != null ? tilemap.name : "NULL")}");
        }
        
        // Store all original tiles for restoration
        StoreOriginalTiles();
    }
    
    /// <summary>
    /// Destroys a tile at the specified world position
    /// </summary>
    /// <param name="worldPosition">World position to destroy tile at</param>
    /// <returns>True if a tile was destroyed</returns>
    public bool DestroyTileAtPosition(Vector3 worldPosition)
    {
        if (tilemap == null)
        {
            Debug.LogError("[BreakableTilemap] No tilemap assigned!");
            return false;
        }
        
        // Convert world position to tilemap cell position
        Vector3Int cellPosition = tilemap.WorldToCell(worldPosition);
        
        // Check if there's a tile at this position
        if (tilemap.HasTile(cellPosition))
        {
            if (debugMode)
            {
                Debug.Log($"[BreakableTilemap] Destroying tile at cell position: {cellPosition} (world: {worldPosition})");
            }
            
            // Remove the tile
            tilemap.SetTile(cellPosition, null);
            
            // Play destruction effect
            if (destructionEffect != null)
            {
                Vector3 effectPosition = tilemap.GetCellCenterWorld(cellPosition);
                Instantiate(destructionEffect, effectPosition, Quaternion.identity);
            }
            
            // Play destruction sound
            if (destructionSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(destructionSound);
            }
            
            return true;
        }
        else
        {
            if (debugMode)
            {
                Debug.Log($"[BreakableTilemap] No tile found at cell position: {cellPosition} (world: {worldPosition})");
            }
            return false;
        }
    }
    
    /// <summary>
    /// Check if there's a tile at the specified world position
    /// </summary>
    /// <param name="worldPosition">World position to check</param>
    /// <returns>True if there's a tile at the position</returns>
    public bool HasTileAtPosition(Vector3 worldPosition)
    {
        if (tilemap == null) return false;
        
        Vector3Int cellPosition = tilemap.WorldToCell(worldPosition);
        return tilemap.HasTile(cellPosition);
    }
    
    /// <summary>
    /// Get the tilemap component
    /// </summary>
    /// <returns>The tilemap component</returns>
    public Tilemap GetTilemap()
    {
        return tilemap;
    }
    
    /// <summary>
    /// Show visual feedback that tiles are about to be destroyed
    /// </summary>
    public void ShowDestructionWarning(Vector3 worldPosition)
    {
        if (tilemap == null) return;
        
        Vector3Int cellPosition = tilemap.WorldToCell(worldPosition);
        if (tilemap.HasTile(cellPosition))
        {
            // Change the tile color to show warning
            tilemap.SetTileFlags(cellPosition, TileFlags.None);
            tilemap.SetColor(cellPosition, damagedColor);
        }
    }
    
    /// <summary>
    /// Store all original tiles for later restoration
    /// </summary>
    private void StoreOriginalTiles()
    {
        if (tilemap == null) return;
        
        originalTiles.Clear();
        
        // Get all tiles in the tilemap
        BoundsInt bounds = tilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cellPos = new Vector3Int(x, y, 0);
                TileBase tile = tilemap.GetTile(cellPos);
                if (tile != null)
                {
                    originalTiles[cellPos] = tile;
                }
            }
        }
        
        if (debugMode)
        {
            Debug.Log($"[BreakableTilemap] Stored {originalTiles.Count} original tiles");
        }
    }
    
    /// <summary>
    /// Restore all tiles to their original state
    /// </summary>
    public void RestoreAllTiles()
    {
        if (tilemap == null) return;
        
        int restoredCount = 0;
        
        // Restore all original tiles
        foreach (KeyValuePair<Vector3Int, TileBase> kvp in originalTiles)
        {
            tilemap.SetTile(kvp.Key, kvp.Value);
            restoredCount++;
        }
        
        if (debugMode)
        {
            Debug.Log($"[BreakableTilemap] Restored {restoredCount} tiles to original state");
        }
    }
} 