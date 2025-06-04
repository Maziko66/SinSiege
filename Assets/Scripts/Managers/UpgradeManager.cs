using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] GameObject upgradePanel;
    [SerializeField] private Button btnUpgradePanelClose;
    
    
    [Header("Mechanical")]
    [SerializeField] List<UpgradeCard> upgradeCards = new List<UpgradeCard>();


    private void Start()
    {
        btnUpgradePanelClose.onClick.AddListener(() => SetUpgradePanelState(false));
    }

    public void TimeToUpgrade()
    {
        SetUpgradePanelState(true);
    }

    public void SetUpgradePanelState(bool state)
    {
        upgradePanel.SetActive(state);
    }
}