using UnityEngine;

public class SceneManager : MonoBehaviour
{
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
}
