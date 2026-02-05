using UnityEngine;

[System.Serializable]
public class TowerReference
{
    public string name;
    public GameObject prefab;
}

[CreateAssetMenu(fileName = "ReferencesSO", menuName = "Scriptable Objects/ReferencesSO")]
public class ReferencesSO : ScriptableObject
{
    [SerializeField] private CharacterData[] characters;
    public CharacterData[] Characters => characters;
    
    [SerializeField] private Color32[] colors;
    public Color32[] Colors => colors;

    [field: SerializeField] public TowerReference[] TowerReferences { get; private set; }
}
