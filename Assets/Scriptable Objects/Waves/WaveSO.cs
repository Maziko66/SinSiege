using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Wave")]
public class WaveSO : ScriptableObject
{
    [Header("Tower Defense Waves")]
    public Vector3 spawnPoint;
    public int routeIndex;
    public float spawnInterval = -1;
    public float waveCooldown;
    public List<Enemy> enemyList;
    public List<Enemy> enemyListHard;
    
    
    [Header("Horde")]
    public bool hasHorde;
    public List<Enemy> hordeList;
    public List<Enemy> hordeListHard;
    public float hordeInterval;
    
    [Header("General")]
    public int totalGoldValue;
}
