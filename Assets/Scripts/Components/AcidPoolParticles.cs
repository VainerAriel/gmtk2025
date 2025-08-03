using UnityEngine;

public class AcidPoolParticles : MonoBehaviour
{
    [SerializeField] private ParticleSystem particleSystem;
    
    private void Start()
    {
        // Start the particle system when the acid pool is created
        if (particleSystem != null)
        {
            particleSystem.Play();
        }
    }
    
    public void StopParticles()
    {
        if (particleSystem != null)
        {
            particleSystem.Stop();
        }
    }
    
    public void StartParticles()
    {
        if (particleSystem != null)
        {
            particleSystem.Play();
        }
    }
} 