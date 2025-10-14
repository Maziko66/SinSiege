using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class UpgradeManager : MonoBehaviour
{
    public bool hasUpgraded;
    
    [Header("UI")]
    [SerializeField] GameObject upgradePanel;
    [SerializeField] private Button btnUpgradePanelClose;
    [SerializeField] private List<UpgradeCard> upgradeCardsUI;
    
    
    [Header("Mechanical")]
    [SerializeField] List<Upgrade> upgrades = new List<Upgrade>();
    [SerializeField] List<Upgrade> upgradesCommon = new List<Upgrade>();
    [SerializeField] List<Upgrade> upgradesUncommon = new List<Upgrade>();
    [SerializeField] List<Upgrade> upgradesRare = new List<Upgrade>();
    [SerializeField] List<Upgrade> upgradesLegendary = new List<Upgrade>();
    [SerializeField] List<Upgrade> upgradesUnique = new List<Upgrade>();
    


    private void Start()
    {
        LoadUpgradeCards();
        btnUpgradePanelClose.onClick.AddListener(() => SetUpgradePanelState(false, true));
        //upgradePanel.SetActive(false);
    }

    public void TimeToUpgrade()
    {
        if (!hasUpgraded)
        {
            SetUpgradePanelState(true);
        }
        
    }

    public void SetUpgradePanelState(bool state, bool isCloseButton = false)
    {
        upgradePanel.SetActive(state);
        if (!isCloseButton)
        {
            hasUpgraded = true;
            Debug.Log("Upgrade Panel Closed without Upgrading.");
        }
    }
    
    private void LoadUpgradeCards()
    {
        foreach (Upgrade upgrade in upgrades)
        {
            MasterDictionary.UpgradeRarity rarity = upgrade.Rarity;
            switch (rarity) // 0 = common, 1 = uncommon, 2 = rare, 3 = legendary, 4 = unique
            {
            case MasterDictionary.UpgradeRarity.Common:
                upgradesCommon.Add(upgrade);
                break;
            case MasterDictionary.UpgradeRarity.Uncommon:
                upgradesUncommon.Add(upgrade);
                break;
            case MasterDictionary.UpgradeRarity.Rare:
                upgradesRare.Add(upgrade);
                break;
            case MasterDictionary.UpgradeRarity.Legendary:
                upgradesLegendary.Add(upgrade);
                break;
            case MasterDictionary.UpgradeRarity.Unique:
                upgradesUnique.Add(upgrade);
                break;
            default:
                Debug.Log("card rarity unknown");
                break;
            }
        }
    }

    private void SelectRandomCards()
    {
        foreach (UpgradeCard card in upgradeCardsUI)
        {
            int random = Random.Range(0, 4);

            // switch (random)
            // {
            //     case 0:
            //     {
            //         
            //     }
            // }
            
        }
    }

    private void SetUpgradeCard(UpgradeCard card, Upgrade upgrade)
    {
        card.upgrade = upgrade;
        card.SetUIToUpgrade();
    }
}