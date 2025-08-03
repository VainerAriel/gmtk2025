using UnityEngine;
using UnityEngine.Tilemaps;

public class AcidTransformationTest : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private BreakableTilemap breakableTilemap;
    [SerializeField] private Transform testArea;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    private void Start()
    {
        if (debugMode)
        {
            Debug.Log("[AcidTransformationTest] Starting acid transformation test");
        }
    }
    
    private void Update()
    {
        // Test key: Create the 6 breakable blocks that acid transformation affects
        if (Input.GetKeyDown(KeyCode.B))
        {
            CreateAcidTransformationBlocks();
        }
        
        // Test key: Trigger acid death manually (requires holding Ctrl to prevent accidental triggering)
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.A))
        {
            TriggerAcidDeath();
        }
        
        // Test key: Test acid transformation on existing ghosts (Ctrl + T)
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.T))
        {
            TestAcidTransformationOnGhosts();
        }
        
        // Test key: Test grid snapping (Ctrl + G)
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.G))
        {
            TestGridSnapping();
        }
    }
    
    /// <summary>
    /// Creates the 6 breakable tiles that acid transformation affects
    /// </summary>
    private void CreateAcidTransformationBlocks()
    {
        if (breakableTilemap == null)
        {
            Debug.LogError("[AcidTransformationTest] No breakable tilemap assigned!");
            return;
        }
        
        Vector3 centerPos = testArea != null ? testArea.position : Vector3.zero;
        
        // The 6 positions that acid transformation affects:
        // 2 left, 2 right, 1 up, 1 down
        Vector3[] positions = new Vector3[]
        {
            centerPos + Vector3.left * 2,    // 2 left
            centerPos + Vector3.left,        // 1 left
            centerPos + Vector3.right,       // 1 right
            centerPos + Vector3.right * 2,   // 2 right
            centerPos + Vector3.up,          // 1 up
            centerPos + Vector3.down         // 1 down
        };
        
        string[] positionNames = new string[]
        {
            "2_Left",
            "1_Left", 
            "1_Right",
            "2_Right",
            "1_Up",
            "1_Down"
        };
        
        // Get the tilemap component
        Tilemap tilemap = breakableTilemap.GetTilemap();
        if (tilemap == null)
        {
            Debug.LogError("[AcidTransformationTest] No tilemap found in breakable tilemap!");
            return;
        }
        
        // Create a simple tile for testing (you can replace this with your actual tile)
        Tile testTile = ScriptableObject.CreateInstance<Tile>();
        
        for (int i = 0; i < positions.Length; i++)
        {
            Vector3 position = positions[i];
            Vector3Int cellPosition = tilemap.WorldToCell(position);
            
            // Place a tile at this position
            tilemap.SetTile(cellPosition, testTile);
            
            if (debugMode)
            {
                Debug.Log($"[AcidTransformationTest] Created breakable tile at {position} ({positionNames[i]}) -> cell: {cellPosition}");
            }
        }
        
        Debug.Log("[AcidTransformationTest] Created 6 breakable tiles for acid transformation testing");
    }
    
    /// <summary>
    /// Manually triggers an acid death for testing
    /// </summary>
    private void TriggerAcidDeath()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            // Set player as poisoned and then kill them
            player.SetPoisoned(true);
            player.TakeDamage(100f); // This should trigger acid death
            
            Debug.Log("[AcidTransformationTest] Triggered acid death for testing");
        }
        else
        {
            Debug.LogError("[AcidTransformationTest] No PlayerController found in scene!");
        }
    }
    
    /// <summary>
    /// Clears all breakable tiles from the scene
    /// </summary>
    public void ClearAllBreakableBlocks()
    {
        if (BreakableTilemapManager.Instance != null)
        {
            BreakableTilemapManager.Instance.ClearAllTiles();
            Debug.Log($"[AcidTransformationTest] Cleared all tiles using BreakableTilemapManager");
        }
        else
        {
            Debug.LogWarning("[AcidTransformationTest] No BreakableTilemapManager found to clear tiles!");
        }
    }
    
    /// <summary>
    /// Test acid transformation on existing ghosts
    /// </summary>
    private void TestAcidTransformationOnGhosts()
    {
        GhostController[] ghosts = FindObjectsOfType<GhostController>();
        Debug.Log($"[AcidTransformationTest] Found {ghosts.Length} ghosts to test");
        
        // Check for breakable tilemap manager
        if (BreakableTilemapManager.Instance != null)
        {
            Debug.Log($"[AcidTransformationTest] Found BreakableTilemapManager with {BreakableTilemapManager.Instance.GetTilemapCount()} registered tilemaps");
        }
        else
        {
            Debug.LogWarning("[AcidTransformationTest] No BreakableTilemapManager found in scene!");
        }
        
        foreach (GhostController ghost in ghosts)
        {
            if (ghost != null)
            {
                Debug.Log($"[AcidTransformationTest] Testing acid transformation on ghost: {ghost.name} at position {ghost.transform.position}");
                ghost.TestAcidTransformation();
            }
        }
    }
    
    /// <summary>
    /// Test grid snapping functionality
    /// </summary>
    private void TestGridSnapping()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            Vector3 playerPos = player.transform.position;
            Debug.Log($"[AcidTransformationTest] Player position: {playerPos}");
            
            // Test the 6 adjacent positions
            Vector3[] testPositions = new Vector3[]
            {
                playerPos + Vector3.left * 2,
                playerPos + Vector3.left,
                playerPos + Vector3.right,
                playerPos + Vector3.right * 2,
                playerPos + Vector3.up,
                playerPos + Vector3.down
            };
            
            for (int i = 0; i < testPositions.Length; i++)
            {
                Vector3 originalPos = testPositions[i];
                Vector3 snappedPos = SnapToGrid(originalPos);
                Debug.Log($"[AcidTransformationTest] Position {i}: {originalPos} -> {snappedPos}");
            }
        }
    }
    
    /// <summary>
    /// Snap a position to the grid (same as GhostController)
    /// </summary>
    private Vector3 SnapToGrid(Vector3 worldPosition, float gridSize = 1.0f)
    {
        float snappedX = Mathf.Round(worldPosition.x + 0.5f) - 0.5f;
        float snappedY = Mathf.Round(worldPosition.y + 0.5f) - 0.5f;
        return new Vector3(snappedX, snappedY, worldPosition.z);
    }
} 