using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeCard : MonoBehaviour
{
    public Upgrade upgrade;
    
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI textTitle;
    [SerializeField] private TextMeshProUGUI textDescription;
    [SerializeField] private Image upgradeImage;
    
    public void SetUI(string upgradeName, string upgradeDescription, Sprite upgradeSprite, int rarity)
    {
        textTitle.SetText(upgradeName);
        textDescription.SetText(upgradeDescription);
        upgradeImage.sprite = upgradeSprite;
        textTitle.color = Color.white;
    }
}
