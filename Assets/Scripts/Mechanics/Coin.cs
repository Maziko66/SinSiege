using System;
using UnityEngine;

public class Coin : MonoBehaviour
{
    public int value;

    private void OnDestroy()
    {
        if (SoundManager.Instance != null && gameObject.scene.isLoaded)
        {
            //SoundManager.Instance.PlaySound(SoundManager.Instance.sfxCoinPickup, transform.position);
            SoundManager.Instance.PlaySound(SoundManager.Instance.sfxCoinPickup);
        }
    }
}
