using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WaveSO))]
public class WaveSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector (Lists, Variables, etc.)
        DrawDefaultInspector();

        WaveSO wave = (WaveSO)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Calculations", EditorStyles.boldLabel);

        // The Button
        if (GUILayout.Button("Calculate Total Gold & Exp", GUILayout.Height(30)))
        {
            wave.CalculateTotalStats();
            
            // Mark object as dirty so Unity saves the calculated values to the file
            EditorUtility.SetDirty(wave);
        }

        // Optional: Show the results immediately under the button for quick reference
        // (Even though they are already shown in the Default Inspector under "General")
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = Color.green;
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Total Gold: {wave.totalGoldValue}", style);
        EditorGUILayout.LabelField($"Total Exp: {wave.totalExpValue}", style);
        EditorGUILayout.EndHorizontal();
    }
}