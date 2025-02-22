using System;
using UnityEngine;
using UnityEngine.UI;

public class UIMergeMenuCombat : MonoBehaviour
{
    private BuildManager _buildManager;
    private void Awake()
    {
        _buildManager = FindFirstObjectByType<BuildManager>();
    }

    [SerializeField] private Button buttonClear;
    [SerializeField] private Button buttonMerge;

    [SerializeField] private Image[] slots = new Image[2];
    [SerializeField] private Sprite defaultSprite;
    
    
    [SerializeField] private Image slot1;
    [SerializeField] private Image slot2;
    
    public TowerGeneric[] towers = new TowerGeneric[2];
    
    void Start()
    {
        buttonClear.onClick.AddListener(() => _buildManager.ClearMerge());
        buttonMerge.onClick.AddListener(() => _buildManager.MergeTowers());
    }

    public void SetSlotImage(int slot, Sprite sprite)
    {
        slots[slot].sprite = sprite;
    }

    public void ResetSlotImage(int slot = -1)
    {
        if (slot == -1)
        {
            foreach (var img in slots)
            {
                img.sprite = defaultSprite;
            }
            return;
        }
        
        slots[slot].sprite = defaultSprite;
    }
}
