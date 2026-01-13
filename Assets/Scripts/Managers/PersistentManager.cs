using UnityEngine;

public class PersistentManager : MonoBehaviour
{
    public static PersistentManager Instance { get; private set; }
    
    [Header("Other")]
    [SerializeField] private GameState gameState;
    public GameState GameState => gameState;
    
    [Header("Managers")]
    [SerializeField] private LocalizationManager localizationManager;
    public LocalizationManager LocalizationManager => localizationManager;
    
    [SerializeField] private SaveManager saveManager;
    public SaveManager SaveManager => saveManager;
    
    [SerializeField] private SceneManager sceneManager;
    public SceneManager SceneManager => sceneManager;
    
    
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
