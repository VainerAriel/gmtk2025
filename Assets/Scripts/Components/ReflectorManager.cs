using UnityEngine;

public class ReflectorManager : MonoBehaviour
{
    [Header("Reflector Prefabs")]
    [SerializeField] private GameObject leftUpReflectPrefab;
    [SerializeField] private GameObject leftDownReflectPrefab;
    [SerializeField] private GameObject rightUpReflectPrefab;
    [SerializeField] private GameObject rightDownReflectPrefab;
    
    // Public properties to access prefabs
    public GameObject LeftUpReflectPrefab => leftUpReflectPrefab;
    public GameObject LeftDownReflectPrefab => leftDownReflectPrefab;
    public GameObject RightUpReflectPrefab => rightUpReflectPrefab;
    public GameObject RightDownReflectPrefab => rightDownReflectPrefab;
    
    private static ReflectorManager instance;
    
    public static ReflectorManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ReflectorManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("ReflectorManager");
                    instance = go.AddComponent<ReflectorManager>();
                }
            }
            return instance;
        }
    }
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    public GameObject GetReflectorPrefab(bool isFacingRight, bool isGrounded)
    {
        if (isFacingRight)
        {
            return isGrounded ? rightDownReflectPrefab : rightUpReflectPrefab;
        }
        else
        {
            return isGrounded ? leftDownReflectPrefab : leftUpReflectPrefab;
        }
    }
    
    /// <summary>
    /// Clears all reflectors in the scene
    /// </summary>
    public void ClearAllReflectors()
    {
        // Find all reflectors in the scene and destroy them
        ReflectBlock[] reflectBlocks = FindObjectsOfType<ReflectBlock>();
        foreach (ReflectBlock reflector in reflectBlocks)
        {
            if (reflector != null)
            {
                Destroy(reflector.gameObject);
            }
        }
        
        Debug.Log($"[ReflectorManager] Cleared {reflectBlocks.Length} reflectors from the scene");
    }
} 