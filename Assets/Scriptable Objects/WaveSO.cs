using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Wave")]
public class WaveSO : ScriptableObject
{
    public Vector3 spawnPoint;
    public List<Enemy> enemyList;
    public List<Enemy> enemyListHard;
}
