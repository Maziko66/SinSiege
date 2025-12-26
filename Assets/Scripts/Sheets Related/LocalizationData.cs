using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LocalizationData", menuName = "Localization/Data Asset")]
public class LocalizationData : ScriptableObject
{
    // Stores "en", "tr", "fr" based on your columns
    public List<string> languageCodes = new List<string>();

    // Stores the actual rows
    public List<LocalizationEntry> entries = new List<LocalizationEntry>();

    [System.Serializable]
    public class LocalizationEntry
    {
        public string key;
        public List<string> values; // values[0] is 'en', values[1] is 'tr'...
    }
}