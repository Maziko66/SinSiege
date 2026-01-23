using System;
using UnityEngine;
using UnityEngine.UI;

public class GameState : MonoBehaviour
{
    public static GameState Instance => PersistentManager.Instance.GameState;
    
    public event Action<int> OnLevelChanged;
    
    [SerializeField] private MasterDictionary.Characters selectedCharacter;
    public MasterDictionary.Characters SelectedCharacter => selectedCharacter;

    [Header("Menu")]
    [SerializeField] private Button buttonSelectCharacter;

    [SerializeField] private int levelIndex;
    public int LevelIndex => levelIndex;

    public void SelectCharacter(MasterDictionary.Characters character)
    {
        if (SceneManager.Instance.GetCurrentSceneName().Equals(MasterDictionary.SceneMainMenuName))
        {
            selectedCharacter = character;
            Debug.Log($"Selected character: {character}");
        }
    }

    public void SetLevelIndex(int newIndex)
    {
        this.levelIndex = newIndex;
        
        OnLevelChanged?.Invoke(this.levelIndex);
        
        Debug.Log($"GameState: Level index set to {newIndex}");
    }
}
