using System;
using UnityEngine;

public class Clickable : MonoBehaviour
{
    public event Action OnClicked; // Event that anyone can subscribe to

    public void HandleClick()
    {
        OnClicked?.Invoke();
    }
}
