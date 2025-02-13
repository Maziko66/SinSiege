using UnityEngine;

[CreateAssetMenu(fileName = "WaveElement")]
public class WaveElementSO : ScriptableObject
{
    public Enemy enemy;
    public Vector2 spawnPosition;
}
