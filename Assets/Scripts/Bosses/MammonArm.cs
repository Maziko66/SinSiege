using System;
using UnityEngine;

public class MammonArm : MonoBehaviour
{
    [SerializeField] private Boss boss;
    [SerializeField] private int armIndex;

    public void Fire()
    {
        boss.ArmHit(armIndex);
    }
}
