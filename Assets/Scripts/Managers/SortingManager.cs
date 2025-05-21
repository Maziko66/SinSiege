using UnityEngine;

public class SortingManager : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] spriteRenderers;
    void Start()
    {
        foreach (SpriteRenderer sr in spriteRenderers)
        {
            sr.sortingOrder = Mathf.RoundToInt(-sr.gameObject.transform.position.y * 100);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
