using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class UpgradeManager : MonoBehaviour
{
    public bool CharacterIndex;
    
    public bool hasUpgraded;
    
    public static UpgradeManager Instance;
    
    [Header("UI")]
    [SerializeField] GameObject upgradePanel;
    [SerializeField] private Button btnUpgradePanelClose;
    [SerializeField] private Button btnReroll;
    [SerializeField] private List<UpgradeCard> upgradeCardsUI;
    
    
    [Header("Mechanical")]
    [SerializeField] List<Upgrade> upgrades = new List<Upgrade>();
    
    [SerializeField] List<Upgrade> upgradesCommon = new List<Upgrade>();
    [SerializeField] List<Upgrade> upgradesUncommon = new List<Upgrade>();
    [SerializeField] List<Upgrade> upgradesRare = new List<Upgrade>();
    [SerializeField] List<Upgrade> upgradesLegendary = new List<Upgrade>();
    [SerializeField] List<Upgrade> upgradesUnique = new List<Upgrade>();
    
    private List<List<Upgrade>> rarityList = new List<List<Upgrade>>();

    public event Action OnRecalculateUpgrades;
    
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        LoadUpgradeCards();
        btnUpgradePanelClose.onClick.AddListener(() => SetUpgradePanelState(false, true));
        btnReroll.onClick.AddListener(Reroll);
        upgradePanel.SetActive(false);
        
        rarityList.Add(upgradesCommon);
        rarityList.Add(upgradesUncommon);
        rarityList.Add(upgradesRare);
        rarityList.Add(upgradesLegendary);
        // rarityList.Add(upgradesUnique);
        
        //SelectRandomCards();
        
        RecalculateUpgrades();
    }

    public void TimeToUpgrade()
    {
        if (!hasUpgraded)
        {
            SetUpgradePanelState(true);
        }
        
    }

    private void RecalculateUpgrades()
    {
        Debug.Log("RecalculateUpgrades");
        OnRecalculateUpgrades?.Invoke();
    }

    public void SetUpgradePanelState(bool state, bool isCloseButton = false)
    {
        upgradePanel.SetActive(state);
        SelectRandomCards();
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

            List<Upgrade> randomList = rarityList[random];
            
            SetUpgradeCard(card, randomList[Random.Range(0, randomList.Count)]);
        }
    }

    private void SetUpgradeCard(UpgradeCard card, Upgrade upgrade)
    {
        card.upgrade = upgrade;
        card.SetUIToUpgrade();
    }

    private void Reroll()
    {
        SelectRandomCards();
    }
}