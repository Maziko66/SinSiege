using UnityEngine;
using FMODUnity;


public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }
    
    [Header("Audio")]
    public EventReference sfxCoinPickup;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void PlaySound(EventReference eventRef)
    {
        FMOD.Studio.EventInstance sound = RuntimeManager.CreateInstance(eventRef);
        //fire.setParameterByID(fullHealthParameterId, restoreAll ? 1.0f : 0.0f);
        //fire.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(gameObject));
        sound.start();
        sound.release();
    }
}
