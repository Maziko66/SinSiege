using UnityEngine;

public class ButtonSceneSelect : ButtonGeneral
{
    [Header("Scene Select")]
    public string sceneToLoad = "Lust1";

    public int levelIndex;

    protected override void Start()
    {
        base.Start();

        if (sceneToLoad != string.Empty)
        {
            onClick.AddListener(() => SceneManager.Instance.StartLevelWithData(sceneToLoad, levelIndex));
        }
    }
}
