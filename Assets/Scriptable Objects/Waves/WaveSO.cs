using System.Collections.Generic;
using UnityEngine;

public enum SpawnModMode
{
    NoModification,
    Multiplier,
    CustomValue
}

[System.Serializable]
public class WaveSpawnData
{
    [Tooltip("The enemy prefab to spawn")]
    public Enemy enemyPrefab;

    [Tooltip("How to modify this enemy's stats")]
    public SpawnModMode modificationMode = SpawnModMode.NoModification;

    // -- Multipliers (Default 1.0 means no change) --
    public float hpMultiplier = 1.0f;
    public float speedMultiplier = 1.0f;
    public float damageMultiplier = 1.0f;
    public float goldMultiplier = 1.0f;
    public float expMultiplier = 1.0f;

    // -- Custom Values --
    public float customHealth = 10f;
    public float customSpeed = 2f;
    public int customDamage = 1;
    public int customGold = 5;
    public float customExp = 1f;
}


[CreateAssetMenu(fileName = "Wave")]
// public class WaveSO : ScriptableObject
// {
//     [Header("Tower Defense Waves")]
//     public Vector3 spawnPoint;
//     public int routeIndex;
//     public float spawnInterval = -1;
//     public float waveCooldown;
//     public List<Enemy> enemyList;
//     public List<Enemy> enemyListHard;
//     
//     
//     [Header("Horde")]
//     public bool hasHorde;
//     public List<Enemy> hordeList;
//     public List<Enemy> hordeListHard;
//     public float hordeInterval;
//     
//     [Header("General")]
//     public int totalGoldValue;
// }

public class WaveSO : ScriptableObject
{
    [Header("Tower Defense Waves")]
    public int routeIndex;
    public float waveCooldown;
    
    public List<WaveSpawnData> enemySpawns; 
    
    [Header("Horde")]
    public bool hasHorde;
    public List<WaveSpawnData> hordeSpawns; 
    public float hordeInterval;
    
    [Header("General (Calculated)")]
    public int totalGoldValue;
    public float totalExpValue;
    
    public void CalculateTotalStats()
    {
        totalGoldValue = 0;
        totalExpValue = 0;

        CalculateList(enemySpawns);

        if (hasHorde)
        {
            CalculateList(hordeSpawns);
        }
    }
    
    private void CalculateList(List<WaveSpawnData> list)
    {
        if (list == null) return;

        foreach (WaveSpawnData data in list)
        {
            if (data.enemyPrefab == null) continue;

            // Get Base Values
            // Ensure your Enemy script has public int coinValue and public float GetExp()
            int currentGold = data.enemyPrefab.coinValue; 
            float currentExp = data.enemyPrefab.GetExp(); 

            // Apply Modifications
            if (data.modificationMode == SpawnModMode.Multiplier)
            {
                currentGold = Mathf.RoundToInt(currentGold * data.goldMultiplier);
                currentExp *= data.expMultiplier;
            }
            else if (data.modificationMode == SpawnModMode.CustomValue)
            {
                currentGold = Mathf.RoundToInt(data.customGold);
                currentExp = data.customExp;
            }

            totalGoldValue += currentGold;
            totalExpValue += currentExp;
        }
    }
}


