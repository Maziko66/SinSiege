using System;
using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject[] panels;
    [SerializeField] private GameObject panelMainMenu;
    [SerializeField] private GameObject panelStartGame;
    
    [Header("Buttons")]
    [SerializeField] private ButtonGeneral buttonStartGame;
    [SerializeField] private ButtonGeneral buttonSettings;
    [SerializeField] private ButtonGeneral buttonExtras;
    [SerializeField] private ButtonGeneral buttonExit;

    private void Start()
    {
        panelMainMenu.SetActive(true);
        panelStartGame.SetActive(false);
        
        buttonStartGame.onClick.AddListener(StartGame);
        buttonSettings.onClick.AddListener(Settings);
        buttonExtras.onClick.AddListener(Extras);
        buttonExtras.onClick.AddListener(Exit);
    }

    #region MAIN_MENU_BUTTON_METHODS

    private void StartGame()
    {
        panelMainMenu.SetActive(false);
        panelStartGame.SetActive(true);
    }

    private void Settings()
    {
        throw new NotImplementedException();
    }

    private void Extras()
    {
        throw new NotImplementedException();
    }

    private void Exit()
    {
        throw new NotImplementedException();
    }

    #endregion
    
    
}
