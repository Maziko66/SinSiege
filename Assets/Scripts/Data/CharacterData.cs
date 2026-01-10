using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Scriptable Objects/CharacterData")]
public class CharacterData : ScriptableObject
{
    [SerializeField] private MasterDictionary.Characters id;
    public MasterDictionary.Characters ID => id;
    
    [SerializeField] private string characterName;
    public string Name => characterName;

    [SerializeField] private string fullName;
    public string FullName => fullName;
    
    [SerializeField] private string desc;
    public string Desc => desc;

    [SerializeField] private int damage;
    public int Damage => damage;

    [SerializeField] private int movementSpeed;
    public int MovementSpeed => movementSpeed;

    [SerializeField] private int attackSpeed;
    public int AttackSpeed => attackSpeed;
}
