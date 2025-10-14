using System;
using System.Collections;
using UnityEngine;
using FMODUnity;

public class FMODBootstrap : MonoBehaviour
{
    bool loaded;

    private void Awake()
    {
        StartCoroutine(Start());
    }

    IEnumerator Start()
    {
        // Load core banks (loadSamples: true if you want sample data now)
        RuntimeManager.LoadBank("Master");
        RuntimeManager.LoadBank("Master.strings");
        // Wait until the banks report as loaded
        while (!(RuntimeManager.HasBankLoaded("Master") && RuntimeManager.HasBankLoaded("Master.strings")))
        {
            yield return null;
        }

        loaded = true;
        Debug.Log("FMOD banks loaded.");
        // Now it's safe to create / start events
    }
}
