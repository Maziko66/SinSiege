using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUpgradesManager : MonoBehaviour
{
    [Header("Character Info UI")]
    [SerializeField] private LocalizedText textCharacterName;
    [SerializeField] private LocalizedText textCharacterFullName;
    [SerializeField] private LocalizedText textCharacterDescription;
    [SerializeField] private Image charPortrait;
    
    [SerializeField] List<CharacterData> characters = new List<CharacterData>();

    private int characterIndex;

    [Header("Buttons")]
    [SerializeField] private Button buttonNext;
    [SerializeField] private Button buttonPrev;

    private void Awake()
    {
        buttonNext.onClick.AddListener(() => SwapCharacter(true));
        buttonPrev.onClick.AddListener(() => SwapCharacter(false));
        
        CharacterData character = characters[characterIndex];
        
        textCharacterName.UpdateKeyAndText(character.Name);
        textCharacterFullName.UpdateKeyAndText(character.FullName);
        textCharacterDescription.UpdateKeyAndText(character.Desc);
        
        charPortrait.sprite = character.UpgradeMenuPortrait;
    }

    private void SwapCharacter(bool next = true)
    {
        if (characters == null || characters.Count == 0)
        {
            Debug.Log("Character list is empty.");
            return;
        }

        if (next)
        {
            characterIndex++;
            if (characterIndex >= characters.Count)
            {
                characterIndex = 0;
            }
        }
        else
        {
            characterIndex--;
            if (characterIndex < 0)
            {
                characterIndex = characters.Count - 1;
            }
        }
        CharacterData character = characters[characterIndex];
        
        textCharacterName.UpdateKeyAndText(character.Name);
        textCharacterFullName.UpdateKeyAndText(character.FullName);
        textCharacterDescription.UpdateKeyAndText(character.Desc);

        charPortrait.sprite = character.UpgradeMenuPortrait;

        // TODO: Call your update method here
        // UpdateCharacterUI(character);
        Debug.Log($"Selected Character: {characterIndex}");
    }
}
