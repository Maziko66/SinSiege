using UnityEngine;

public class ButtonSceneSelect : ButtonGeneral
{
    [Header("Scene Select")]
    public string sceneToLoad;

    protected override void Start()
    {
        base.Start();

        if (sceneToLoad != string.Empty)
        {
            onClick.AddListener(() => SceneManager.Instance.StartLevelWithData(sceneToLoad));
        }
    }
}
