using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum TypeToChange
{
    Text,
    Image,
    UpgradeIcon
}

public class ButtonGeneral : Button
{
    private Image _image;

    [SerializeField] private bool hasLine;
    
    [SerializeField] private List<Image> _images = new List<Image>();
    
    [SerializeField] private TextMeshProUGUI text;
    
    [SerializeField] private TypeToChange typeToChange;
    
    [Header("Colors")]
    [SerializeField] private Color32 defaultColor = new Color32(255, 255, 255, 255);
    [SerializeField] private Color32 highlightedColor = new Color32(255, 255, 255, 255);
    [SerializeField] private Color32 pressedColor = new Color32(255, 255, 255, 255);
    [SerializeField] private Color32 disabledColor = new Color32(255, 255, 255, 255);
    
    [Header("Sprites")]
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite upgradedSprite;

    [SerializeField] private Image lineImage;
    [SerializeField] private Sprite lineSpriteDefault;
    [SerializeField] private Sprite lineSpriteUpgraded;
    
    
    protected override void Awake()
    {
        base.Awake();
        if (text == null)
        {
            text = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (typeToChange == TypeToChange.UpgradeIcon)
        {
            foreach (Image img in _images)
            {
                img.color = defaultColor;
            }
        }
        
        _image = GetComponent<Image>();

        if (!hasLine)
        {
            lineImage?.gameObject.SetActive(false);
        }
    }

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        base.DoStateTransition(state, instant);

        if (typeToChange == TypeToChange.UpgradeIcon)
        {
            if (state == SelectionState.Normal)
            {
                if (image.sprite != defaultSprite)
                {
                    image.sprite = defaultSprite;
                    image.SetNativeSize();
                }
                foreach (Image image in _images)
                {
                    image.color = defaultColor;
                }
            }
            else if (state == SelectionState.Highlighted)
            {
                if (image.sprite != upgradedSprite)
                {
                    image.sprite = upgradedSprite;
                    image.SetNativeSize();
                }
                foreach (Image image in _images)
                {
                    image.color = highlightedColor;
                }
            }
            else if (state == SelectionState.Pressed)
            {
                if (image.sprite != upgradedSprite)
                {
                    image.sprite = upgradedSprite;
                    image.SetNativeSize();
                }
                foreach (Image image in _images)
                {
                    image.color = pressedColor;
                }
            }
            else if (state == SelectionState.Disabled)
            {
                if (image.sprite != defaultSprite)
                {
                    image.sprite = defaultSprite;
                    image.SetNativeSize();
                }
                foreach (Image image in _images)
                {
                    image.color = disabledColor;
                }
            }
            else
            {
                if (image.sprite != defaultSprite)
                {
                    image.sprite = defaultSprite;
                    image.SetNativeSize();
                }
                foreach (Image image in _images)
                {
                    image.color = defaultColor;
                }
            }

            return;
        }

        if (text == null)
        {
            //Debug.Log("no text attached");
            return;
        }
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

    public void OnClickDisable()
    {
        image.sprite = defaultSprite;
        image.SetNativeSize();

        if (hasLine)
        {
            lineImage.sprite = lineSpriteUpgraded;
        }
        
        GetComponent<Button>().interactable = false;
    }
}
