using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeData", menuName = "Scriptable Objects/UpgradeData")]
public class UpgradeData : ScriptableObject
{
    [SerializeField] private string upgradeName;
    public string UpgradeName => upgradeName;
    
    [SerializeField] private StatType statType;
    public StatType StatType => statType;

    [SerializeField] private float value;
    public float Value => value;

    [SerializeField] private bool isMultiplier;
    public bool IsMultiplier => isMultiplier;
}

public enum StatType { Damage, Speed, MaxHealth }
