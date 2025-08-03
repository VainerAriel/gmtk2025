using UnityEngine;

public class FallingBlockManager : MonoBehaviour
{
    [Header("Falling Ground Prefab")]
    [SerializeField] private GameObject fallingGroundPrefab;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    
    // Singleton pattern
    public static FallingBlockManager Instance { get; private set; }
    
    private void Awake()
    {
        // Singleton pattern - ensure only one instance exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (debugMode)
            {
                Debug.Log($"[FallingBlockManager] Instance created: {gameObject.name}");
            }
        }
        else
        {
            // If another instance already exists, destroy this one
            if (debugMode)
            {
                Debug.Log($"[FallingBlockManager] Another instance already exists, destroying: {gameObject.name}");
            }
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        if (debugMode)
        {
            Debug.Log($"[FallingBlockManager] FallingGround prefab assigned: {(fallingGroundPrefab != null ? fallingGroundPrefab.name : "NULL")}");
        }
    }
    
    /// <summary>
    /// Get the FallingGround prefab
    /// </summary>
    /// <returns>The FallingGround prefab GameObject</returns>
    public GameObject GetFallingGroundPrefab()
    {
        if (fallingGroundPrefab == null)
        {
            Debug.LogError($"[FallingBlockManager] FallingGround prefab is null! Please assign it in the inspector.");
        }
        return fallingGroundPrefab;
    }
    
    /// <summary>
    /// Set the FallingGround prefab
    /// </summary>
    /// <param name="prefab">The FallingGround prefab to set</param>
    public void SetFallingGroundPrefab(GameObject prefab)
    {
        fallingGroundPrefab = prefab;
        
        if (debugMode)
        {
            Debug.Log($"[FallingBlockManager] FallingGround prefab set to: {(prefab != null ? prefab.name : "NULL")}");
        }
    }
    
    /// <summary>
    /// Create a falling ground at the specified position
    /// </summary>
    /// <param name="position">The world position to create the falling ground</param>
    /// <returns>The created falling ground GameObject</returns>
    public GameObject CreateFallingGround(Vector3 position)
    {
        if (fallingGroundPrefab == null)
        {
            Debug.LogError($"[FallingBlockManager] Cannot create falling ground - prefab is null!");
            return null;
        }
        
        GameObject fallingGround = Instantiate(fallingGroundPrefab, position, Quaternion.identity);
        
        if (debugMode)
        {
            Debug.Log($"[FallingBlockManager] Created falling ground at position: {position}");
        }
        
        return fallingGround;
    }
    
    /// <summary>
    /// Create a falling ground at the specified position and snap it to the grid
    /// </summary>
    /// <param name="position">The world position to create the falling ground</param>
    /// <param name="gridSize">The size of each grid tile (default 1.0)</param>
    /// <returns>The created falling ground GameObject</returns>
    public GameObject CreateFallingGroundSnappedToGrid(Vector3 position, float gridSize = 1.0f)
    {
        Vector3 snappedPosition = SnapToGrid(position, gridSize);
        return CreateFallingGround(snappedPosition);
    }
    
    /// <summary>
    /// Snaps a world position to the nearest grid tile center
    /// </summary>
    /// <param name="worldPosition">The world position to snap</param>
    /// <param name="gridSize">The size of each grid tile (default 1.0)</param>
    /// <returns>The snapped grid position at tile center</returns>
    private Vector3 SnapToGrid(Vector3 worldPosition, float gridSize = 1.0f)
    {
        // Round to the nearest grid tile center (not corner)
        float snappedX = Mathf.Round(worldPosition.x + 0.5f) - 0.5f;
        float snappedY = Mathf.Round(worldPosition.y + 0.5f) - 0.5f;
        
        return new Vector3(snappedX, snappedY, worldPosition.z);
    }
    
    /// <summary>
    /// Clear all falling ground objects in the scene
    /// </summary>
    public void ClearAllFallingGrounds()
    {
        // Find all objects with "FallingGround" in their name
        FallingGround[] fallingGrounds = FindObjectsOfType<FallingGround>();
        
        foreach (FallingGround fallingGround in fallingGrounds)
        {
            if (fallingGround != null)
            {
                Destroy(fallingGround.gameObject);
            }
        }
        
        if (debugMode)
        {
            Debug.Log($"[FallingBlockManager] Cleared all falling ground objects");
        }
    }
    
    /// <summary>
    /// Get the count of falling ground objects in the scene
    /// </summary>
    /// <returns>The number of falling ground objects</returns>
    public int GetFallingGroundCount()
    {
        GameObject[] fallingGrounds = GameObject.FindGameObjectsWithTag("FallingGround");
        return fallingGrounds.Length;
    }
} 