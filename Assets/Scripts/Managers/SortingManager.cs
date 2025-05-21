using UnityEngine;

public class SortingManager : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] spriteRenderers;
    [SerializeField] private SpriteRenderer[] propSpriteRenderers;
    
    void Start()
    {
        foreach (SpriteRenderer sr in spriteRenderers)
        {
            sr.sortingOrder = Mathf.RoundToInt(-sr.gameObject.transform.position.y * 100);
        }
        
        foreach (SpriteRenderer sr in propSpriteRenderers)
        {
            sr.sortingOrder = Mathf.RoundToInt(-sr.gameObject.transform.position.y * 100);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
