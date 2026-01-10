using UnityEngine;

public class PersistentManager : MonoBehaviour
{
    public static PersistentManager Instance { get; private set; }
    
    [Header("Managers")]
    [SerializeField] private LocalizationManager localizationManager;
    public LocalizationManager LocalizationManager => localizationManager;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
