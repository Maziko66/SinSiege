using UnityEngine;
using TMPro; // TextMeshPro namespace
using System;
using System.Collections.Generic;
using System.Linq;

public class LanguageDropdown : MonoBehaviour
{
    private TMP_Dropdown dropdown;

    void Start()
    {
        dropdown = GetComponent<TMP_Dropdown>();
        dropdown.ClearOptions();

        // 1. Get all Enum values from MasterDictionary
        var enumValues = Enum.GetValues(typeof(MasterDictionary.GameLanguage))
            .Cast<MasterDictionary.GameLanguage>()
            .ToList();
        
        // 2. Convert them to a list of strings for the Dropdown
        List<string> options = new List<string>();
        foreach(var lang in enumValues)
        {
            options.Add(lang.ToString()); // e.g., "English", "Turkish"
        }
        
        dropdown.AddOptions(options);

        // 3. Set the dropdown to the currently active language
        int currentIndex = enumValues.IndexOf(LocalizationManager.Instance.currentLanguage);
        dropdown.value = currentIndex;

        // 4. Listener
        dropdown.onValueChanged.AddListener((index) => 
        {
            MasterDictionary.GameLanguage selectedLang = enumValues[index];
            LocalizationManager.Instance.SetLanguage(selectedLang);
        });
    }
}