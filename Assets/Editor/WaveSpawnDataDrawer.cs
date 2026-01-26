using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(WaveSpawnData))]
public class WaveSpawnDataDrawer : PropertyDrawer
{
    private const float LineHeight = 18f;
    private const float Padding = 4f;
    private const float PreviewSize = 60f; // Size of the current enemy icon

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var mode = (SpawnModMode)property.FindPropertyRelative("modificationMode").enumValueIndex;

        // Base lines: 1 (for the big Preview + Name area) + 1 (for Enum)
        // We treat the Preview area as roughly 3 lines height
        float height = PreviewSize + Padding + LineHeight + Padding;

        int extraLines = 0;
        if (mode == SpawnModMode.Multiplier) extraLines = 5;
        else if (mode == SpawnModMode.CustomValue) extraLines = 5;

        return height + ((LineHeight + Padding) * extraLines) + Padding;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // -- 1. Load the Database (Find it automatically) --
        EnemyDatabase database = AssetDatabase.LoadAssetAtPath<EnemyDatabase>("Assets/Scripts/EnemyDatabase.asset");
        // NOTE: If you saved the database elsewhere, change the path above, 
        // OR use the slower method below if path varies:
        if (database == null)
        {
             string[] guids = AssetDatabase.FindAssets("t:EnemyDatabase");
             if (guids.Length > 0) database = AssetDatabase.LoadAssetAtPath<EnemyDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }


        Rect rect = new Rect(position.x, position.y, position.width, LineHeight);

        // -- 2. Draw Custom Enemy Selector --
        SerializedProperty prefabProp = property.FindPropertyRelative("enemyPrefab");
        Enemy currentEnemy = (Enemy)prefabProp.objectReferenceValue;

        // Area for the Big Button
        Rect previewRect = new Rect(position.x, position.y, PreviewSize, PreviewSize);
        Rect infoRect = new Rect(position.x + PreviewSize + 10, position.y + (PreviewSize/4), position.width - PreviewSize - 10, LineHeight);

        // Draw the Button with Preview
        Texture2D preview = null;
        if (currentEnemy != null) preview = AssetPreview.GetAssetPreview(currentEnemy.gameObject);

        GUIContent btnContent = (preview != null) ? new GUIContent(preview) : new GUIContent("None");
        
        // Draw the button (Click to open Popup)
        if (GUI.Button(previewRect, btnContent))
        {
            if (database != null)
            {
                PopupWindow.Show(previewRect, new EnemySelectorPopup(database, prefabProp));
            }
            else
            {
                Debug.LogError("EnemyDatabase not found! Please create one via Create -> TowerDefense -> EnemyDatabase");
            }
        }

        // Draw Name Label next to it
        string nameLabel = currentEnemy != null ? currentEnemy.name : "Select Enemy";
        EditorGUI.LabelField(infoRect, nameLabel, EditorStyles.boldLabel);
        
        // Move rect down past the preview
        rect.y += PreviewSize + Padding;

        // -- 3. Draw Modification Mode Enum --
        SerializedProperty modeProp = property.FindPropertyRelative("modificationMode");
        EditorGUI.PropertyField(rect, modeProp, new GUIContent("Mode"));
        rect.y += LineHeight + Padding;

        SpawnModMode mode = (SpawnModMode)modeProp.enumValueIndex;

        // -- 4. Draw Stats --
        if (mode != SpawnModMode.NoModification)
        {
            EditorGUI.indentLevel++;

            if (mode == SpawnModMode.Multiplier)
            {
                DrawStatField(ref rect, property, "hpMultiplier", "HP Mult");
                DrawStatField(ref rect, property, "speedMultiplier", "Speed Mult");
                DrawStatField(ref rect, property, "damageMultiplier", "Dmg Mult");
                DrawStatField(ref rect, property, "goldMultiplier", "Gold Mult");
                DrawStatField(ref rect, property, "expMultiplier", "Exp Mult");
            }
            else if (mode == SpawnModMode.CustomValue)
            {
                DrawStatField(ref rect, property, "customHealth", "Health");
                DrawStatField(ref rect, property, "customSpeed", "Speed");
                DrawStatField(ref rect, property, "customDamage", "Damage");
                DrawStatField(ref rect, property, "customGold", "Gold");
                DrawStatField(ref rect, property, "customExp", "Exp");
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    private void DrawStatField(ref Rect rect, SerializedProperty rootProp, string relativeName, string label)
    {
        EditorGUI.PropertyField(rect, rootProp.FindPropertyRelative(relativeName), new GUIContent(label));
        rect.y += LineHeight + Padding;
    }
}