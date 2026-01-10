using System;
using TMPro;
using UnityEngine;

public class CharacterSelectCard : MonoBehaviour
{
    [SerializeField] private MasterDictionary.Characters character;
    public MasterDictionary.Characters Character => character;
    
    [SerializeField] private CharacterData characterData;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI textName;
    [SerializeField] private TextMeshProUGUI textFullName;
    [SerializeField] private TextMeshProUGUI textDescription;

    private void Start()
    {
        characterData = Refs.R.Characters[(int)character];
        
        SetTexts();
    }

    private void SetTexts()
    {
        Debug.Log(characterData.Name);
        textName.SetText(LocalizationManager.Instance.GetLocalizedValue(characterData.Name));
        textFullName.SetText(LocalizationManager.Instance.GetLocalizedValue(characterData.FullName));
        textDescription.SetText(LocalizationManager.Instance.GetLocalizedValue(characterData.Desc));
    }
}
