using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "UpgradeData", menuName = "Scriptable Objects/UpgradeData")]
public class UpgradeData : ScriptableObject
{
    [SerializeField] private int upgradeID;
    public int UpgradeID => upgradeID;
    
    [SerializeField] private string upgradeName;
    public string UpgradeName => upgradeName;
    
    [SerializeField] private UpgradeType upgradeType;
    public UpgradeType UpgradeType => upgradeType;

    [SerializeField] private int upgradeLevel;
    public int UpgradeLevel => upgradeLevel;

    [SerializeField] private float value;
    public float Value => value;

    [SerializeField] private bool isMultiplier;
    public bool IsMultiplier => isMultiplier;

    [SerializeField] private string identifier;
    public string Identifier => identifier;

    [SerializeField] private float secondaryValue;
    public float SecondaryValue => secondaryValue;
    
    [SerializeField] private float ternaryValue;
    public float TernaryValue => ternaryValue;
}

public enum UpgradeType
{
    AttackInterval,
    Damage, 
    BulletSpeed,
    BulletCount,
    BulletHealth,
    MoveSpeed, 
    MaxHealth,
    Custom
}
