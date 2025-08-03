using UnityEngine;
using System.Collections;

public class AcidPool : MonoBehaviour
{
    [Header("Acid Pool Settings")]
    [SerializeField] private float totalDamage = 100f;
    [SerializeField] private int numberOfTicks = 10;
    [SerializeField] private float tickInterval = 0.5f;
    [SerializeField] private float poolDuration = 5f;
    
    [Header("Visual Effects")]
    [SerializeField] private Color acidColor = Color.green;
    [SerializeField] private float pulseSpeed = 1f;
    [SerializeField] private AcidPoolParticles particleController; // Add this line
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    private SpriteRenderer spriteRenderer;
    private bool isActive = true;
    private bool isThisPoolDamagingPlayer = false; // Track if THIS specific pool is damaging the player
    private Coroutine lifetimeCoroutine;
    
    public void Initialize(float damage, int ticks, float interval, float duration)
    {
        totalDamage = damage;
        numberOfTicks = ticks;
        tickInterval = interval;
        poolDuration = duration;
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = acidColor;
        }
        
        if (debugMode)
        {
            Debug.Log($"[AcidPool] Initialized with {totalDamage} damage over {numberOfTicks} ticks");
        }
        
        StartLifetime();
    }
    
    public void ExtendLife(float additionalDuration)
    {
        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
        }
        
        poolDuration += additionalDuration;
        
        if (debugMode)
        {
            Debug.Log($"[AcidPool] Life extended by {additionalDuration}s, new total duration: {poolDuration}s");
        }
        
        StartLifetime();
    }
    
    private void StartLifetime()
    {
        lifetimeCoroutine = StartCoroutine(PoolLifetime());
    }
    
    private IEnumerator PoolLifetime()
    {
        // Wait for pool duration
        yield return new WaitForSeconds(poolDuration);
        
        if (debugMode)
        {
            Debug.Log($"[AcidPool] Pool duration expired, destroying");
        }
        
        // Fade out effect
        StartCoroutine(FadeOut());
    }
    
    private IEnumerator FadeOut()
    {
        // Stop particles when fading out
        if (particleController != null)
        {
            particleController.StopParticles();
        }
        
        if (spriteRenderer != null)
        {
            float fadeTime = 1f;
            float elapsed = 0f;
            Color startColor = spriteRenderer.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
            
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                spriteRenderer.color = Color.Lerp(startColor, endColor, elapsed / fadeTime);
                yield return null;
            }
        }
        
        Destroy(gameObject);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive || isThisPoolDamagingPlayer) return;
        
        // Check if it's an acid projectile - if so, ignore it
        AcidProjectile acidProjectile = other.GetComponent<AcidProjectile>();
        if (acidProjectile != null)
        {
            if (debugMode)
            {
                Debug.Log($"[AcidPool] Acid projectile entered acid pool, ignoring collision");
            }
            return;
        }
        
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            // Check if player is already poisoned - if so, don't start ANY damage
            if (player.IsPoisoned())
            {
                if (debugMode)
                {
                    Debug.Log($"[AcidPool] Player is already poisoned, ignoring acid pool - NO damage started");
                }
                return;
            }
            
            if (debugMode)
            {
                Debug.Log($"[AcidPool] Player entered acid pool, starting damage over time");
            }
            
            isThisPoolDamagingPlayer = true; // This specific pool is now damaging the player
            StartCoroutine(ApplyAcidDamage(player));
        }
    }
    
    private IEnumerator ApplyAcidDamage(PlayerController player)
    {
        if (debugMode)
        {
            Debug.Log($"[AcidPool] Starting acid pool damage: {totalDamage} damage over {numberOfTicks} ticks");
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
                        Debug.Log($"[AcidPool] Player respawned, stopping acid damage");
                    }
                    break; // Player respawned, stop the damage
                }
                
                // Check if player died since last tick
                float currentHealth = player.GetHealth();
                if (currentHealth <= 0 && previousHealth > 0)
                {
                    if (debugMode)
                    {
                        Debug.Log($"[AcidPool] Player died since last tick, stopping acid damage at tick {i + 1}");
                    }
                    
                    // Clear poison status when player dies
                    player.SetPoisoned(false);
                    break;
                }
                
                // Additional safety check: if player health is 0, stop damage
                if (currentHealth <= 0)
                {
                    if (debugMode)
                    {
                        Debug.Log($"[AcidPool] Player health is 0, stopping acid damage at tick {i + 1}");
                    }
                    break;
                }
                
                // Apply damage
                player.TakeDamage(damagePerTick);
                
                if (debugMode)
                {
                    Debug.Log($"[AcidPool] Tick {i + 1}/{numberOfTicks}: Dealt {damagePerTick} damage (Health: {previousHealth} -> {player.GetHealth()})");
                }
                
                // Visual feedback
                StartCoroutine(FlashPlayer(player));
                
                // Update previous health for next tick
                previousHealth = player.GetHealth();
            }
            else
            {
                // Player reference lost, stop acid damage
                if (debugMode)
                {
                    Debug.Log($"[AcidPool] Player reference lost at tick {i + 1}, stopping acid damage");
                }
                break;
            }
            
            yield return new WaitForSeconds(tickInterval);
        }
        
        if (debugMode)
        {
            Debug.Log($"[AcidPool] Acid pool damage complete");
        }
        
        // Only clear poison if this was the effect that set it (player is still alive and this coroutine set the poison)
        if (player != null && player.GetHealth() > 0 && player.IsPoisoned())
        {
            player.SetPoisoned(false);
        }
        
        // Reset flag for this specific pool
        isThisPoolDamagingPlayer = false;
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
    
    private void Update()
    {
        // Pulse effect for visual feedback
        if (spriteRenderer != null && isActive)
        {
            float alpha = 0.5f + 0.3f * Mathf.Sin(Time.time * pulseSpeed);
            Color currentColor = spriteRenderer.color;
            spriteRenderer.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
        }
    }
}