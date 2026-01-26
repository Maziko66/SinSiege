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
    public Vector3 spawnPoint;
    public int routeIndex;
    public float waveCooldown;
    
    // CHANGED: Using the new Data Class instead of List<Enemy>
    public List<WaveSpawnData> enemySpawns; 
    
    // Keeping hard/horde separate? You can update them to use WaveSpawnData too if desired.
    // For now, I'll update Horde to use it as well so you have full control.
    [Header("Horde")]
    public bool hasHorde;
    public List<WaveSpawnData> hordeSpawns; // Updated to new system
    public float hordeInterval;
    
    [Header("General")]
    public int totalGoldValue;
}
