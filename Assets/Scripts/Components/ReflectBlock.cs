using UnityEngine;

public class ReflectBlock : MonoBehaviour
{
    public enum ReflectDirection
    {
        UpRight,     // Can input/output from top or right
        UpLeft,      // Can input/output from top or left
        DownRight,   // Can input/output from bottom or right
        DownLeft,    // Can input/output from bottom or left
        UpDown,      // Can input/output from top or bottom
        LeftRight    // Can input/output from left or right
    }
    
    [Header("Reflection Settings")]
    [SerializeField] private ReflectDirection reflectDirection = ReflectDirection.UpRight;
    [SerializeField] private bool useFirstDirection = true; // Which of the two directions to use
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject reflectEffect;
    [SerializeField] private AudioClip reflectSound;
    [SerializeField] private Color reflectColor = Color.cyan;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true; // Toggle debug messages
    
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private Color originalColor;
    
    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        
        // Make sure the collider has this script too
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null && collider.gameObject != gameObject)
        {
            // If collider is on a different GameObject, add this script there too
            if (collider.gameObject.GetComponent<ReflectBlock>() == null)
            {
                collider.gameObject.AddComponent<ReflectBlock>();
            }
        }
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            spriteRenderer.color = reflectColor;
        }
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        if (debugMode)
        {
            Debug.Log($"[ReflectBlock] {gameObject.name} initialized with type: {reflectDirection}, useFirst: {useFirstDirection}, layer: {LayerMask.LayerToName(gameObject.layer)}");
        }
    }
    
    public Vector2 GetReflectionDirection(Vector2 incomingDirection, Vector2 surfaceNormal)
    {
        if (debugMode)
        {
            Debug.Log($"[ReflectBlock] {gameObject.name} calculating reflection for incoming direction: {incomingDirection}");
        }
        
        // Get the two valid sides for this reflect block
        Vector2[] validSides = GetValidSides();
        
        // Determine which side the projectile is hitting
        Vector2 hitSide = GetHitSide(incomingDirection);
        
        // Check if this side is valid for this reflect block
        bool isValidSide = false;
        foreach (Vector2 validSide in validSides)
        {
            if (hitSide == validSide)
            {
                isValidSide = true;
                break;
            }
        }
        
        if (!isValidSide)
        {
            if (debugMode)
            {
                Debug.Log($"[ReflectBlock] {gameObject.name} invalid input side: {hitSide}. Valid sides: {validSides[0]}, {validSides[1]}");
            }
            return Vector2.zero; // Return zero to indicate projectile should be destroyed
        }
        
        // Get the reflection direction (the other valid side)
        Vector2 reflectionDirection = Vector2.zero;
        if (hitSide == validSides[0])
        {
            reflectionDirection = validSides[1];
        }
        else
        {
            reflectionDirection = validSides[0];
        }
        
        if (debugMode)
        {
            Debug.Log($"[ReflectBlock] {gameObject.name} valid input from {hitSide}, reflecting to {reflectionDirection}");
        }
        
        return reflectionDirection.normalized;
    }
    
    private Vector2 GetHitSide(Vector2 incomingDirection)
    {
        // Determine which side is being hit based on the incoming direction
        float absX = Mathf.Abs(incomingDirection.x);
        float absY = Mathf.Abs(incomingDirection.y);
        
        if (absX > absY)
        {
            // Horizontal hit
            return incomingDirection.x > 0 ? Vector2.left : Vector2.right; // Opposite of incoming
        }
        else
        {
            // Vertical hit
            return incomingDirection.y > 0 ? Vector2.down : Vector2.up; // Opposite of incoming
        }
    }
    
    private Vector2[] GetValidSides()
    {
        Vector2[] sides = new Vector2[2];
        
        switch (reflectDirection)
        {
            case ReflectDirection.UpRight:
                sides[0] = Vector2.up;
                sides[1] = Vector2.right;
                break;
                
            case ReflectDirection.UpLeft:
                sides[0] = Vector2.up;
                sides[1] = Vector2.left;
                break;
                
            case ReflectDirection.DownRight:
                sides[0] = Vector2.down;
                sides[1] = Vector2.right;
                break;
                
            case ReflectDirection.DownLeft:
                sides[0] = Vector2.down;
                sides[1] = Vector2.left;
                break;
                
            case ReflectDirection.UpDown:
                sides[0] = Vector2.up;
                sides[1] = Vector2.down;
                break;
                
            case ReflectDirection.LeftRight:
                sides[0] = Vector2.left;
                sides[1] = Vector2.right;
                break;
        }
        
        return sides;
    }
    
    public void PlayReflectSound()
    {
        if (reflectSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(reflectSound);
        }
    }
    
    // Visual feedback when reflecting
    public void ShowReflectEffect(Vector3 position)
    {
        if (reflectEffect != null)
        {
            Instantiate(reflectEffect, position, Quaternion.identity);
        }
        
        // Flash effect
        if (spriteRenderer != null)
        {
            StartCoroutine(FlashEffect());
        }
    }
    
    private System.Collections.IEnumerator FlashEffect()
    {
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = reflectColor;
    }
    
    // Public method to toggle between the two reflection directions
    public void ToggleReflectionDirection()
    {
        useFirstDirection = !useFirstDirection;
        
        if (debugMode)
        {
            Debug.Log($"[ReflectBlock] {gameObject.name} toggled reflection direction to: {useFirstDirection}");
        }
    }
    
    // Public method to set specific reflection direction
    public void SetReflectionDirection(bool useFirst)
    {
        useFirstDirection = useFirst;
        
        if (debugMode)
        {
            Debug.Log($"[ReflectBlock] {gameObject.name} set reflection direction to: {useFirstDirection}");
        }
    }
} 