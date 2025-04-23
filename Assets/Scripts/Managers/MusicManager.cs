using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class MusicManager : MonoBehaviour
{
    [SerializeField] private EventReference musicEvent; // Drag your FMOD event here in the inspector
    private EventInstance musicInstance;

    private void Start()
    {
        musicInstance = RuntimeManager.CreateInstance(musicEvent);
        musicInstance.start();
        musicInstance.release(); // Optional: releases the handle after playback starts
    }
    
    [ContextMenu("SetCombatToTrue")]
    public void SetCombatToTrue()
    {
        musicInstance.setParameterByName("CombatMode", 1); // Replace with your parameter name
    }
    
    [ContextMenu("SetCombatToFalse")]
    public void SetCombatToFalse()
    {
        musicInstance.setParameterByName("CombatMode", 0); // Replace with your parameter name
    }
}
