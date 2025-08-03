using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class AcidShooter : MonoBehaviour
{
    [Header("Tilemap Settings")]
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private GameObject acidProjectilePrefab;
    [SerializeField] private float shootInterval = 3f;
    [SerializeField] private float shootAngle = 0f; // Angle in degrees
    [SerializeField] private float projectileSpeed = 6f;
    
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    private float shootTimer;
    private Vector3Int[] acidShooterPositions;
    
    private void Start()
    {
        FindAcidShooterTiles();
    }
    
    private void FindAcidShooterTiles()
    {
        if (tilemap == null)
        {
            Debug.LogError("[AcidShooter] No tilemap assigned!");
            return;
        }
        
        // Get all tiles in the tilemap
        BoundsInt bounds = tilemap.cellBounds;
        List<Vector3Int> positions = new List<Vector3Int>();
        
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                if (tilemap.HasTile(pos))
                {
                    positions.Add(pos);
                }
            }
        }
        
        acidShooterPositions = positions.ToArray();
        
        if (debugMode)
        {
            Debug.Log($"[AcidShooter] Found {acidShooterPositions.Length} acid shooter tiles");
        }
    }
    
    private void Update()
    {
        shootTimer += Time.deltaTime;
        
        if (shootTimer >= shootInterval)
        {
            ShootAcidFromAllTiles();
            shootTimer = 0f;
        }
    }
    
    private void ShootAcidFromAllTiles()
    {
        if (acidProjectilePrefab == null)
        {
            Debug.LogError("[AcidShooter] No acid projectile prefab assigned!");
            return;
        }
        
        foreach (Vector3Int tilePos in acidShooterPositions)
        {
            Vector3 worldPos = tilemap.GetCellCenterWorld(tilePos);
            ShootAcidFromPosition(worldPos);
        }
    }
    
    private void ShootAcidFromPosition(Vector3 position)
    {
        // Calculate direction based on angle
        Vector2 direction = new Vector2(
            Mathf.Cos(shootAngle * Mathf.Deg2Rad),
            Mathf.Sin(shootAngle * Mathf.Deg2Rad)
        );
        
        // Create acid projectile
        GameObject acidProjectile = Instantiate(acidProjectilePrefab, position, Quaternion.identity);
        AcidProjectile acidComponent = acidProjectile.GetComponent<AcidProjectile>();
        
        if (acidComponent != null)
        {
            // Set acid properties
            acidComponent.Initialize(direction, projectileSpeed);
            
            if (debugMode)
            {
                Debug.Log($"[AcidShooter] Shot acid from {position} in direction {direction}");
            }
        }
        else
        {
            Debug.LogError("[AcidShooter] Acid projectile prefab doesn't have AcidProjectile component!");
        }
    }
} 