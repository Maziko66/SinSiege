using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "EnemyDatabase", menuName = "TowerDefense/EnemyDatabase")]
public class EnemyDatabase : ScriptableObject
{
    public List<Enemy> allEnemies = new List<Enemy>(); // Initialize directly here

#if UNITY_EDITOR
    [ContextMenu("Auto-Find All Enemies")]
    public void FindAllEnemies()
    {
        // Safety check: if list is somehow still null, create it
        if (allEnemies == null) allEnemies = new List<Enemy>();
        
        allEnemies.Clear();
        
        // Search the Assets folder for all Prefabs with the Enemy component
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Enemy enemy = AssetDatabase.LoadAssetAtPath<Enemy>(path);
            
            if (enemy != null)
            {
                allEnemies.Add(enemy);
            }
        }
        
        Debug.Log($"Found {allEnemies.Count} enemies.");
        EditorUtility.SetDirty(this);
    }
#endif
}