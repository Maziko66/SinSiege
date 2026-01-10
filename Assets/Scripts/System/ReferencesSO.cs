using UnityEngine;

[CreateAssetMenu(fileName = "ReferencesSO", menuName = "Scriptable Objects/ReferencesSO")]
public class ReferencesSO : ScriptableObject
{
    [SerializeField] private CharacterData[] characters;
    public CharacterData[] Characters => characters;
}
