using System;
using UnityEngine;
using UnityEngine.UI;

public class GameState : MonoBehaviour
{
    public static GameState Instance => PersistentManager.Instance.GameState;
    
    [SerializeField] private MasterDictionary.Characters selectedCharacter;
    public MasterDictionary.Characters SelectedCharacter => selectedCharacter;

    [Header("Menu")]
    [SerializeField] private Button buttonSelectCharacter;

    public void SelectCharacter(MasterDictionary.Characters character)
    {
        if (SceneManager.Instance.GetCurrentSceneName().Equals(MasterDictionary.SceneMainMenuName))
        {
            selectedCharacter = character;
            Debug.Log($"Selected character: {character}");
        }
    }
}
