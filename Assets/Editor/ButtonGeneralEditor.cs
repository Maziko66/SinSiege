using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(ButtonGeneral))]
public class ButtonGeneralEditor : ButtonEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Custom Settings", EditorStyles.boldLabel);
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("text"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultColor"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("highlightedColor"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("pressedColor"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("disabledColor"));
        
        serializedObject.ApplyModifiedProperties();
    }
}