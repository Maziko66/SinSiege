using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    //LISTS TO CREATE:
    //listSpawnPoints, listWaveGroups

    public List<WaveSO> waves = new List<WaveSO>();
    [SerializeField] private List<Enemy> _enemyList = new List<Enemy>();
    [SerializeField] private Vector3 _spawnPosition;

    [Header("Spawner Variables")]
    [SerializeField] private float _spawnInterval = 1.0f;
    [SerializeField] private float _spawnCooldown = 1.0f;
    [SerializeField] private int _wavesListIndex = 0;


    private void Start()
    {
        _spawnCooldown *= _spawnInterval;
        GetEnemyList();
    }

    private void Update()
    {
        if (_enemyList.Count > 0)
        {
            _spawnCooldown -= Time.deltaTime;
            SpawnFromList();
        }
        

    }


    private void SpawnCooldown()
    {
        
    }

    private void GetEnemyList()
    {
        _enemyList.Clear();
        _enemyList = waves[_wavesListIndex].enemyList;
        _spawnPosition = waves[_wavesListIndex].spawnPoint;
    }

    private void SpawnFromList()
    {
        if(_spawnCooldown <= 0.0f)
        {
            Instantiate(_enemyList[0].gameObject, _spawnPosition, Quaternion.identity);
            _enemyList.RemoveAt(0);
            _spawnCooldown = _spawnInterval;
        }
        
    }
}
