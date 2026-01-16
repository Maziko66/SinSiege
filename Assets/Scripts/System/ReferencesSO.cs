using UnityEngine;

[CreateAssetMenu(fileName = "ReferencesSO", menuName = "Scriptable Objects/ReferencesSO")]
public class ReferencesSO : ScriptableObject
{
    [SerializeField] private CharacterData[] characters;
    public CharacterData[] Characters => characters;
    
    [SerializeField] private Color32[] colors;
    public Color32[] Colors => colors;
}
