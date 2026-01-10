using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum TypeToChange
{
    Text,
    Image
}

public class ButtonGeneral : Button
{
    [SerializeField] private TextMeshProUGUI text;
    
    [Header("Colors")]
    [SerializeField] private Color32 defaultColor = new Color32(255, 255, 255, 255);
    [SerializeField] private Color32 highlightedColor = new Color32(255, 255, 255, 255);
    [SerializeField] private Color32 pressedColor = new Color32(255, 255, 255, 255);
    [SerializeField] private Color32 disabledColor = new Color32(255, 255, 255, 255);

    protected override void Awake()
    {
        base.Awake();
        if (text == null)
        {
            text = GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        base.DoStateTransition(state, instant);
        
        if (state == SelectionState.Normal)
        {
            text.color = defaultColor;
        }
        else if (state == SelectionState.Highlighted)
        {
            text.color = highlightedColor;
        }
        else if (state == SelectionState.Pressed)
        {
            text.color = pressedColor;
        }
        else
        {
            text.color = disabledColor;
        }
    }
    
    private void SwapColor(TypeToChange type)
    {
        switch (type)
        {
            case TypeToChange.Text:
                break;
        }
    }
}
