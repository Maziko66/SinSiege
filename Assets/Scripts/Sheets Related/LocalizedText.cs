using UnityEngine;
using TMPro;

public class LocalizedText : MonoBehaviour
{
    public string key;

    private TMP_Text textComponent;

    void Start()
    {
        textComponent = GetComponent<TMP_Text>();
        
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += UpdateText;
            
            UpdateText();
        }
        else
        {
            Debug.Log("LocalizationManager is null.");
        }
    }

    void OnDestroy()
    {
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