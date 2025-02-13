using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UISliderHp : MonoBehaviour
{
    private Slider _slider;
    private TextMeshProUGUI _text;

    private void Awake()
    {
        _slider = GetComponent<Slider>();
        _text = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void SliderMinMaxValueSet(float val)
    {
        //_baseStartingHealth = baseTower.GetBaseStartingHealth();
        _slider.maxValue = val;
        _slider.minValue = 0;
        //UpdateBaseHealth();
    }

    public void SliderValueSet(float val)
    {
        _slider.value = val;
    }

    public void SliderTextSet(string text)
    {
        _text.SetText(text);
    }

    public float GetMaxValue()
    {
        return _slider.maxValue;
    }
}