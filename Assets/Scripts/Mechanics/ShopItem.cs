using UnityEngine;

public class ShopItem : MonoBehaviour
{
    private Clickable _clickable;

    private void OnEnable()
    {
        _clickable = GetComponent<Clickable>();
        if (_clickable != null)
        {
            _clickable.OnClicked += HandleClick;
            Debug.Log("added ");
        }
    }

    private void OnDisable()
    {
        if (_clickable != null)
        {
            _clickable.OnClicked -= HandleClick;
        }
    }
    
    public void HandleClick()
    {
        Debug.Log($"{gameObject.name} shop item was clicked (via event)!");
    }
}
