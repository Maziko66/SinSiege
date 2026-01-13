using UnityEngine;

public class GameState : MonoBehaviour
{
    public static GameState Instance => PersistentManager.Instance.GameState;
    
    [SerializeField] private MasterDictionary.Characters selectedCharacter;
    public MasterDictionary.Characters SelectedCharacter => selectedCharacter;

    public void SelectCharacter(MasterDictionary.Characters character)
    {
        selectedCharacter = character;
    }
}
