using UnityEngine;

public class TowerGeneric : MonoBehaviour
{
    public string towerName;
    public Sprite towerSprite;
    public TowerZone attachedZone;
    public int bulletsFired;
    public int bulletsHit;

    public void IncreaseTowerZoneVet(float exp)
    {
        attachedZone.IncreaseVet(exp);
    }
}
