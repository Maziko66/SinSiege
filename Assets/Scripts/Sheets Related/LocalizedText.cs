using UnityEngine;
using TMPro; // Use UnityEngine.UI if using legacy Text

public class LocalizedText : MonoBehaviour
{
    public string key; // e.g., "menu_start"

    private TMP_Text textComponent;

    void Start()
    {
        textComponent = GetComponent<TMP_Text>();
        
        // Subscribe to the event
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += UpdateText;
            
            // Try to update immediately in case data is already there
            UpdateText();
        }
    }

    void OnDestroy()
    {
        // Always unsubscribe to prevent memory leaks
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= UpdateText;
        }
    }

    public void UpdateText()
    {
        if (LocalizationManager.Instance != null && textComponent != null)
        {
            textComponent.text = LocalizationManager.Instance.GetLocalizedValue(key);
        }
    }
}