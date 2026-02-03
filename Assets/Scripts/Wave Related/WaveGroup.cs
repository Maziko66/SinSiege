using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A group of WaveSOs that spawn simultaneously.
/// WaveSOs on the same route will spawn in list order (left to right).
/// </summary>
[System.Serializable]
public class WaveGroup
{
    [Tooltip("All waves in this group spawn together. Same-route waves spawn left-to-right.")]
    public List<WaveSO> waveSet = new List<WaveSO>();
    
    /// <summary>
    /// Gets the cooldown timer for this wave group (uses the first wave's cooldown)
    /// </summary>
    public float GetWaveCooldown()
    {
        if (waveSet == null || waveSet.Count == 0) return 30f;
        
        foreach (var wave in waveSet)
        {
            if (wave != null) return wave.waveCooldown;
        }
        return 30f;
    }
    
    /// <summary>
    /// Calculates total gold value for all waves in the group
    /// </summary>
    public int GetTotalGold()
    {
        int total = 0;
        if (waveSet == null) return total;
        
        foreach (var wave in waveSet)
        {
            if (wave != null) total += wave.totalGoldValue;
        }
        return total;
    }
    
    /// <summary>
    /// Calculates total exp value for all waves in the group
    /// </summary>
    public float GetTotalExp()
    {
        float total = 0f;
        if (waveSet == null) return total;
        
        foreach (var wave in waveSet)
        {
            if (wave != null) total += wave.totalExpValue;
        }
        return total;
    }
}