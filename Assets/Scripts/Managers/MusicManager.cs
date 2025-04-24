using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }
    
    private EventInstance _musicInstance;
    
    [Header("Tracks")]
    [SerializeField] private EventReference musicEvent;
    [SerializeField] private EventReference eventLustMusic;
    public EventReference EventLustMusic => eventLustMusic;
    
    [Header("Tags")]
    [SerializeField] private string tagCombatMode = "CombatMode";
    public string TagCombatMode => tagCombatMode;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // optional if you want it to persist
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // _musicInstance = RuntimeManager.CreateInstance(eventLustMusic);
        // _musicInstance.start();
        // _musicInstance.release(); // releases the handle after playback starts
        StartMusic(eventLustMusic);
    }

    [ContextMenu("Stop Current Music")]
    public void StopCurrentMusic()
    {
        _musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
    }
    
    [ContextMenu("SetCombatToTrue")]
    public void SetCombatToTrue()
    {
        //_musicInstance.setParameterByName("CombatMode", 1); 
        SetParameterValue(tagCombatMode, 1);
    }
    
    [ContextMenu("SetCombatToFalse")]
    public void SetCombatToFalse()
    {
        //_musicInstance.setParameterByName("CombatMode", 0); 
        SetParameterValue(tagCombatMode, 0);
    }

    public void SetParameterValue(string paramTag, float value)
    {
        _musicInstance.setParameterByName(paramTag, value);
    }

    public void StartMusic(EventReference musicEvent)
    {
        StopCurrentMusic();
        _musicInstance = RuntimeManager.CreateInstance(musicEvent);
        _musicInstance.start();
        _musicInstance.release();
    }
}
