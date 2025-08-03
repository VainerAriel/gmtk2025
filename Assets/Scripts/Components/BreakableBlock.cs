using UnityEngine;

public class BreakableBlock : MonoBehaviour
{
    [Header("Breakable Block Settings")]
    [SerializeField] private bool isDestroyed = false;
    [SerializeField] private Color normalColor = Color.gray;
    [SerializeField] private Color damagedColor = Color.red;
    [SerializeField] private float destructionDelay = 0.1f;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject destructionEffect;
    [SerializeField] private AudioClip destructionSound;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    private SpriteRenderer spriteRenderer;
    private Collider2D blockCollider;
    private AudioSource audioSource;
    
    private void Start()
    {
        // Set up components
        spriteRenderer = GetComponent<SpriteRenderer>();
        blockCollider = GetComponent<Collider2D>();
        audioSource = GetComponent<AudioSource>();
        
        // Set to Ground layer (layer 8)
        gameObject.layer = 8;
        
        // Set up visual appearance
        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }
        
        // Set up collider
        if (blockCollider != null)
        {
            blockCollider.isTrigger = false;
        }
        
        if (debugMode)
        {
            Debug.Log($"[BreakableBlock] {gameObject.name} initialized at position: {transform.position}");
        }
    }
    
    /// <summary>
    /// Destroys the breakable block
    /// </summary>
    public void DestroyBlock()
    {
        if (isDestroyed) return;
        
        isDestroyed = true;
        
        if (debugMode)
        {
            Debug.Log($"[BreakableBlock] {gameObject.name} being destroyed");
        }
        
        // Play destruction effect
        if (destructionEffect != null)
        {
            Instantiate(destructionEffect, transform.position, Quaternion.identity);
        }
        
        // Play destruction sound
        if (destructionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(destructionSound);
        }
        
        // Destroy the block after a short delay
        Destroy(gameObject, destructionDelay);
    }
    
    /// <summary>
    /// Check if the block is destroyed
    /// </summary>
    /// <returns>True if the block is destroyed</returns>
    public bool IsDestroyed()
    {
        return isDestroyed;
    }
    
    /// <summary>
    /// Show visual feedback that the block is about to be destroyed
    /// </summary>
    public void ShowDestructionWarning()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = damagedColor;
        }
    }
    
    /// <summary>
    /// Reset the block to normal state
    /// </summary>
    public void ResetBlock()
    {
        isDestroyed = false;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }
    }
} 