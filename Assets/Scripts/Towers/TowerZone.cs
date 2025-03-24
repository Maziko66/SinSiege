using System;
using UnityEngine;
using UnityEngine.UI;

public class TowerZone : MonoBehaviour
{
    private GameManager _gameManager;
    
    [SerializeField] private UISliderHp expSlider;
    [SerializeField] private string strSlider = "Zone Veterancy: ";
    
    public bool isEmpty = true;
    public TowerGeneric occupyingTower;
    public int[] vetPoints = { 1000, 2000, 3000 };
    public float vet;
    public int rank;

    private void Awake()
    {
        _gameManager = FindFirstObjectByType<GameManager>();
    }

    private void Start()
    {
        expSlider = _gameManager.GetZoneXpSlider();
    }

    public void IncreaseVet(float exp)
    {
        vet += exp;
        
        IncreaseRank();
    }

    public void IncreaseRank()
    {
        if(rank == 3) {return;}

        if (vet < vetPoints[rank])
        {
            if (rank < 3)
            {
                rank++;
            }
        }
    }

    public void SetSliderText()
    {
        expSlider.SliderTextSet(strSlider + vet + "/" + vetPoints[rank]);
    }
}
