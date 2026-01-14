using System.Collections.Generic;
using UnityEngine;

public class SceneManager : MonoBehaviour
{
    public static SceneManager Instance => PersistentManager.Instance.SceneManager;
    
    [SerializeField] private bool openDemoFeedbackForm;
    [SerializeField] private string linkDemoFeedbackForm;
    
    public List<string> sceneList = new List<string>();
    
    void OnApplicationQuit()
    {
        
        #if !UNITY_EDITOR
        if (openDemoFeedbackForm && !string.IsNullOrEmpty(linkDemoFeedbackForm))
        {
            Application.OpenURL(linkDemoFeedbackForm);
        }
        #endif
    }

    public void StartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Lust1");
    }

    public void StartLevelWithData(string sceneName)
    {
        Debug.Log($"Starting level '{sceneName}' with data.");
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    public string GetCurrentSceneName()
    {
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    }
}
