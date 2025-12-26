using System.Collections.Generic;
using UnityEngine;
using System;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance;

    [Header("Settings")]
    // Now uses your MasterDictionary enum
    public MasterDictionary.GameLanguage currentLanguage = MasterDictionary.GameLanguage.English;
    
    public event Action OnLanguageChanged;

    private Dictionary<string, string> currentLanguageMap;
    private LocalizationData dataAsset;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        DontDestroyOnLoad(gameObject);

        LoadData();
    }

    private void LoadData()
    {
        dataAsset = Resources.Load<LocalizationData>("LocalizationData");
        
        if (dataAsset == null)
        {
            Debug.LogError("LocalizationData missing! Run Tools > Localization > Fetch & Bake Data");
            return;
        }
        
        // Initialize with the default language
        SetLanguage(currentLanguage);
    }

    // --- MAIN FUNCTION: Takes your Enum ---
    public void SetLanguage(MasterDictionary.GameLanguage lang)
    {
        currentLanguage = lang;
        
        // 1. Convert Enum to CSV string code (e.g. Turkish -> "tr")
        string csvCode = GetLanguageCode(lang);
        
        // 2. Update the dictionary
        UpdateLanguageDictionary(csvCode);
    }

    private void UpdateLanguageDictionary(string langCode)
    {
        if (dataAsset == null) return;

        // Find which column in the CSV corresponds to "tr", "en", etc.
        int langIndex = dataAsset.languageCodes.IndexOf(langCode);

        if (langIndex == -1)
        {
            Debug.LogError($"CSV is missing column for '{langCode}'! Falling back to English.");
            langIndex = 0; 
        }

        // Build the fast lookup dictionary
        currentLanguageMap = new Dictionary<string, string>();
        
        foreach (var entry in dataAsset.entries)
        {
            if (entry.values.Count > langIndex)
            {
                currentLanguageMap[entry.key] = entry.values[langIndex];
            }
        }

        OnLanguageChanged?.Invoke();
    }

    public string GetLocalizedValue(string key)
    {
        if (currentLanguageMap != null && currentLanguageMap.TryGetValue(key, out string value))
        {
            return value;
        }
        return $"MISSING: {key}";
    }

    // --- HELPER: Maps your Enum to CSV Headers ---
    private string GetLanguageCode(MasterDictionary.GameLanguage lang)
    {
        switch (lang)
        {
            case MasterDictionary.GameLanguage.English:            return "en";
            case MasterDictionary.GameLanguage.Turkish:            return "tr";
            case MasterDictionary.GameLanguage.French:             return "fr";
            case MasterDictionary.GameLanguage.Italian:            return "it";
            case MasterDictionary.GameLanguage.German:             return "de";
            case MasterDictionary.GameLanguage.Portuguese:         return "pt";
            case MasterDictionary.GameLanguage.Russian:            return "ru";
            case MasterDictionary.GameLanguage.Polish:             return "pl";
            case MasterDictionary.GameLanguage.Korean:             return "kr";
            case MasterDictionary.GameLanguage.Japanese:           return "jp";
            case MasterDictionary.GameLanguage.ChineseSimplified:  return "zh";
            case MasterDictionary.GameLanguage.ChineseTraditional: return "tc";
            default:                                               return "en";
        }
    }
    
    [ContextMenu("Set Language to English")]
    private void SetLanguageToEnglish()
    {
        SetLanguage(MasterDictionary.GameLanguage.English);
    }

    [ContextMenu("Set Language to Turkish")]
    private void SetLanguageToTurkish()
    {
        SetLanguage(MasterDictionary.GameLanguage.Turkish);
    }
}

