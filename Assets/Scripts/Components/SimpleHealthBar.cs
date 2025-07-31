using UnityEngine;
using UnityEngine.UI;

public class SimpleHealthBar : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private Image fillImage;
    
    private void Start()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();
            
        if (fillImage == null)
            fillImage = GetComponent<Image>();
    }
    
    private void Update()
    {
        if (player != null && fillImage != null)
        {
            float healthPercent = player.GetHealth() / player.GetMaxHealth();
            fillImage.fillAmount = healthPercent;
        }
    }
} 