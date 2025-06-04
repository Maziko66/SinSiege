using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeCard : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI textTitle;
    [SerializeField] private TextMeshProUGUI textDescription;
    [SerializeField] private Image upgradeImage;
    
    [Header("Values")]
    [SerializeField] private string upgradeName;
    [SerializeField] private string upgradeDescription;
    [SerializeField] private Sprite upgradeSprite;
    
    public string UpgradeName => upgradeName;
    public string UpgradeDescription => upgradeDescription;
    public Sprite UpgradeSprite => upgradeSprite;
}
