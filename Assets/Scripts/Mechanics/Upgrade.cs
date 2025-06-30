using UnityEngine;

public class Upgrade : MonoBehaviour
{
    [Header("Values")]
    [SerializeField] private string upgradeName;
    [SerializeField] private string upgradeDescription;
    [SerializeField] private Sprite upgradeSprite;
    [SerializeField] private MasterDictionary.UpgradeRarity rarity;
    
    public string UpgradeName => upgradeName;
    public string UpgradeDescription => upgradeDescription;
    public Sprite UpgradeSprite => upgradeSprite;
    public MasterDictionary.UpgradeRarity Rarity => rarity;
    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
