using UnityEngine;

public class SimpleDamage : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private bool damageOnCollision = true;
    [SerializeField] private bool damageOnTrigger = false;
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!damageOnCollision) return;
        
        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        if (player != null)
        {
            player.TakeDamage(damage);
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!damageOnTrigger) return;
        
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            player.TakeDamage(damage);
        }
    }
} 