using UnityEditor;
using UnityEditor.UI;

// The 'true' parameter tells Unity: "Use this editor for ButtonGeneral AND any script that inherits from it"
[CustomEditor(typeof(ButtonGeneral), true)] 
public class ButtonGeneralEditor : ButtonEditor
{
    public override void OnInspectorGUI()
    {
        // 1. Draw the standard Unity Button stuff
        base.OnInspectorGUI();

        serializedObject.Update();

        // 2. Draw the ButtonGeneral specific stuff
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("General Settings", EditorStyles.boldLabel);
        
        // We use FindProperty relative to the current object
        DrawPropertyIfFound("text");
        DrawPropertyIfFound("defaultColor");
        DrawPropertyIfFound("highlightedColor");
        DrawPropertyIfFound("pressedColor");
        DrawPropertyIfFound("disabledColor");
        DrawPropertyIfFound("_images");
        DrawPropertyIfFound("upgradedSprite");
        DrawPropertyIfFound("defaultSprite");
        DrawPropertyIfFound("lineSpriteDefault");
        DrawPropertyIfFound("lineSpriteUpgraded");
        DrawPropertyIfFound("hasLine");
        DrawPropertyIfFound("lineImage");

        // 3. Draw ANY other fields defined in child scripts (like sceneToLoad)
        DrawRemainingProperties();

        serializedObject.ApplyModifiedProperties();
    }

    // Helper to draw a property safely
    private void DrawPropertyIfFound(string name)
    {
        SerializedProperty prop = serializedObject.FindProperty(name);
        if (prop != null)
        {
            EditorGUILayout.PropertyField(prop);
        }
    }

    // Helper to automatically find and draw fields we haven't drawn yet
    private void DrawRemainingProperties()
    {
        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;

        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;

            // Skip standard Unity fields ("m_Script", "m_Interactable", etc.)
            // and skip the fields we already drew manually above
            if (IsPropertyStandard(iterator.name) || IsPropertyAlreadyDrawn(iterator.name))
            {
                continue;
            }

            EditorGUILayout.PropertyField(iterator, true);
        }
    }

    private bool IsPropertyStandard(string name)
    {
        // These are standard internal Unity properties we don't want to double-draw
        return name == "m_Script" || name.StartsWith("m_") || name == "m_OnOnClick";
    }

    private bool IsPropertyAlreadyDrawn(string name)
    {
        // List the variable names from ButtonGeneral you already handled manually
        return name == "text" || name == "defaultColor" || name == "highlightedColor" || 
               name == "pressedColor" || name == "disabledColor" || name == "_images" || name == "upgradedSprite" ||
               name == "defaultSprite" || name == "lineSpriteDefault" || name == "lineSpriteUpgraded" || name == "hasLine" || name == "lineImage";
    }
}