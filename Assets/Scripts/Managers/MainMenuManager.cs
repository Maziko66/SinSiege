using System;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject[] panels;
    [SerializeField] private GameObject panelMainMenu;
    [SerializeField] private GameObject panelStartGame;
    [SerializeField] private GameObject panelLevelSelect;
    [SerializeField] private GameObject panelUpgradeMenu;
    
    [Header("Buttons")]
    [SerializeField] private ButtonGeneral buttonStartGame;
    [SerializeField] private ButtonGeneral buttonUpgradeMenu;
    [SerializeField] private ButtonGeneral buttonSettings;
    [SerializeField] private ButtonGeneral buttonExtras;
    [SerializeField] private ButtonGeneral buttonExit;

    [SerializeField] private ButtonGeneral buttonBackToMainMenuFromUpgradeMenu;

    [SerializeField] private Button buttonSelectCharacter;

    [SerializeField] private InfiniteScroll infiniteScrollCharacterSelect;

    private void Start()
    {
        panelMainMenu.SetActive(true);
        panelStartGame.SetActive(false);
        panelUpgradeMenu.SetActive(false);
        panelLevelSelect.SetActive(false);
        
        buttonStartGame.onClick.AddListener(StartGame);
        buttonUpgradeMenu.onClick.AddListener(UpgradeMenu);
        buttonSettings.onClick.AddListener(Settings);
        buttonExtras.onClick.AddListener(Extras);
        buttonExtras.onClick.AddListener(Exit);
        
        buttonBackToMainMenuFromUpgradeMenu.onClick.AddListener(BackToMainMenuFromUpgradeMenu);
        
        buttonSelectCharacter.onClick.AddListener(LevelSelect);
    }

    public void EnterGame()
    {
        SceneManager.Instance.StartGame();
    }
    
    #region MAIN_MENU_BUTTON_METHODS

    private void StartGame()
    {
        panelMainMenu.SetActive(false);
        panelStartGame.SetActive(true);
    }

    public void LevelSelect()
    {
        GameState.Instance.SelectCharacter((MasterDictionary.Characters)infiniteScrollCharacterSelect.GetSelectedIndex());
        panelStartGame.SetActive(false);
        panelLevelSelect.SetActive(true);
    }
    private void UpgradeMenu()
    {
        panelMainMenu.SetActive(false);
        panelUpgradeMenu.SetActive(true);
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

    private void BackToMainMenuFromUpgradeMenu()
    {
        panelUpgradeMenu.SetActive(false);
        panelMainMenu.SetActive(true);
    }

    #endregion
    
}