using UnityEngine;

public class SceneManager : MonoBehaviour
{
    public static SceneManager Instance => PersistentManager.Instance.SceneManager;
    
    [SerializeField] private bool openDemoFeedbackForm;
    [SerializeField] private string linkDemoFeedbackForm;
    
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
}
