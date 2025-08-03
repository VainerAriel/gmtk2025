using UnityEngine;
using UnityEngine.Tilemaps;

public class BreakableTilemapManager : MonoBehaviour
{
    [Header("Breakable Tilemap Manager")]
    [SerializeField] private BreakableTilemap breakableTilemap;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    // Singleton instance
    public static BreakableTilemapManager Instance { get; private set; }
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        if (debugMode)
        {
            if (breakableTilemap != null)
            {
                Debug.Log($"[BreakableTilemapManager] Initialized with tilemap: {breakableTilemap.name}");
            }
            else
            {
                Debug.LogWarning("[BreakableTilemapManager] No breakable tilemap assigned!");
            }
        }
    }
    
    /// <summary>
    /// Set the breakable tilemap reference
    /// </summary>
    /// <param name="tilemap">The breakable tilemap to manage</param>
    public void SetTilemap(BreakableTilemap tilemap)
    {
        breakableTilemap = tilemap;
        if (debugMode)
        {
            Debug.Log($"[BreakableTilemapManager] Set tilemap: {tilemap?.name ?? "null"}");
        }
    }
    
    /// <summary>
    /// Get the current breakable tilemap
    /// </summary>
    /// <returns>The current breakable tilemap</returns>
    public BreakableTilemap GetTilemap()
    {
        return breakableTilemap;
    }
    
    /// <summary>
    /// Check if there's a breakable tile at the specified world position
    /// </summary>
    /// <param name="worldPosition">World position to check</param>
    /// <returns>True if the tilemap has a tile at the position</returns>
    public bool HasTileAtPosition(Vector3 worldPosition)
    {
        if (breakableTilemap != null)
        {
            return breakableTilemap.HasTileAtPosition(worldPosition);
        }
        return false;
    }
    
    /// <summary>
    /// Destroy a tile at the specified world position
    /// </summary>
    /// <param name="worldPosition">World position to destroy tile at</param>
    /// <returns>True if a tile was destroyed</returns>
    public bool DestroyTileAtPosition(Vector3 worldPosition)
    {
        if (breakableTilemap != null)
        {
            bool destroyed = breakableTilemap.DestroyTileAtPosition(worldPosition);
            if (debugMode && destroyed)
            {
                Debug.Log($"[BreakableTilemapManager] Destroyed tile at {worldPosition}");
            }
            return destroyed;
        }
        return false;
    }
    
    /// <summary>
    /// Clear all tiles from the tilemap
    /// </summary>
    public void ClearAllTiles()
    {
        if (breakableTilemap != null)
        {
            Tilemap tm = breakableTilemap.GetTilemap();
            if (tm != null)
            {
                tm.ClearAllTiles();
                if (debugMode)
                {
                    Debug.Log($"[BreakableTilemapManager] Cleared all tiles from tilemap: {breakableTilemap.name}");
                }
            }
        }
    }
    
    /// <summary>
    /// Restore all tiles to their original state
    /// </summary>
    public void RestoreAllTiles()
    {
        if (breakableTilemap != null)
        {
            breakableTilemap.RestoreAllTiles();
            if (debugMode)
            {
                Debug.Log($"[BreakableTilemapManager] Restored all tiles in tilemap: {breakableTilemap.name}");
            }
        }
    }
    
    /// <summary>
    /// Get the number of registered tilemaps (always 1 or 0 for simplified version)
    /// </summary>
    /// <returns>Number of registered tilemaps</returns>
    public int GetTilemapCount()
    {
        return breakableTilemap != null ? 1 : 0;
    }
} 