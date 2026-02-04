using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A slot that pairs a WaveSO with a specific route
/// </summary>
[System.Serializable]
public class WaveSlot
{
    public WaveSO wave;
    public int routeIndex;
}

/// <summary>
/// A group of WaveSlots that spawn simultaneously.
/// Same-route waves spawn in list order (left to right).
/// </summary>
[System.Serializable]
public class WaveGroup
{
    [Tooltip("All wave slots in this group spawn together. Same-route waves spawn left-to-right.")]
    public List<WaveSlot> waveSlots = new List<WaveSlot>();
    
    /// <summary>
    /// Gets the cooldown timer for this wave group (uses the first wave's cooldown)
    /// </summary>
    public float GetWaveCooldown()
    {
        if (waveSlots == null || waveSlots.Count == 0) return 30f;
        
        foreach (var slot in waveSlots)
        {
            if (slot.wave != null) return slot.wave.waveCooldown;
        }
        return 30f;
    }
    
    /// <summary>
    /// Calculates total gold value for all waves in the group
    /// </summary>
    public int GetTotalGold()
    {
        int total = 0;
        if (waveSlots == null) return total;
        
        foreach (var slot in waveSlots)
        {
            if (slot.wave != null) total += slot.wave.totalGoldValue;
        }
        return total;
    }
    
    /// <summary>
    /// Calculates total exp value for all waves in the group
    /// </summary>
    public float GetTotalExp()
    {
        float total = 0f;
        if (waveSlots == null) return total;
        
        foreach (var slot in waveSlots)
        {
            if (slot.wave != null) total += slot.wave.totalExpValue;
        }
        return total;
    }
}