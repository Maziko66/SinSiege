using UnityEngine;

public class TowerGeneric : MonoBehaviour
{
    public string towerName;
    public Sprite towerSprite;
    public TowerZone attachedZone;
    public int bulletsFired;
    public int bulletsHit;

    [SerializeField] private float towerCost = 100;

    [SerializeField] private float[] _RankBonusDamage = {0.0f, 0.1f, 0.2f, 0.3f};
    [SerializeField] private float[] _RankBonusInterval = {0.0f, 0.1f, 0.2f, 0.3f};
    [SerializeField] private float[] _RankBonusRange = {0.0f, 0.1f, 0.2f, 0.3f};
    
    public void IncreaseTowerZoneVet(float exp)
    {
        attachedZone.IncreaseVet(exp);
    }
    
    
}
