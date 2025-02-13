using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class WaveManager : MonoBehaviour
{
    //LISTS TO CREATE:
    //listSpawnPoints, listWaveGroups

    public List<WaveSO> waves = new List<WaveSO>();
    [FormerlySerializedAs("_enemyList")] [SerializeField] private List<Enemy> enemyList = new List<Enemy>();
    [FormerlySerializedAs("_spawnPosition")] [SerializeField] private Vector3 spawnPosition;

    [Header("Spawner Variables")]
    [SerializeField] private float spawnInterval = 1.0f;
    [SerializeField] private float spawnCooldown = 1.0f;
    [SerializeField] private int wavesListIndex = 0;


    private void Start()
    {
        spawnCooldown *= spawnInterval;
        GetEnemyList();
    }

    private void Update()
    {
        if (enemyList.Count > 0)
        {
            spawnCooldown -= Time.deltaTime;
            SpawnFromList();
        }
        

    }


    private void SpawnCooldown()
    {
        
    }

    private void GetEnemyList()
    {
        enemyList.Clear();
        enemyList = waves[wavesListIndex].enemyList;
        spawnPosition = waves[wavesListIndex].spawnPoint;
    }

    private void SpawnFromList()
    {
        if(spawnCooldown <= 0.0f)
        {
            Instantiate(enemyList[0].gameObject, spawnPosition, Quaternion.identity);
            enemyList.RemoveAt(0);
            spawnCooldown = spawnInterval;
        }
        
    }
}
