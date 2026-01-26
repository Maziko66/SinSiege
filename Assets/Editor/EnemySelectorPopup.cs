using UnityEngine;
using UnityEditor;

public class EnemySelectorPopup : PopupWindowContent
{
    private readonly EnemyDatabase _database;
    private readonly SerializedProperty _property;
    private Vector2 _scrollPos;
    
    // Constants for layout
    private const float IconSize = 70f; 
    private const float Padding = 5f;
    private const int Columns = 3;

    public EnemySelectorPopup(EnemyDatabase database, SerializedProperty property)
    {
        _database = database;
        _property = property;
    }

    public override Vector2 GetWindowSize()
    {
        // Calculate exact width needed for 3 columns + scrollbar space (approx 25px)
        float width = (IconSize + Padding) * Columns + 25f;
        return new Vector2(width, 450); 
    }

    public override void OnGUI(Rect rect)
    {
        GUILayout.Label("Select Enemy", EditorStyles.boldLabel);
        
        if (_database == null || _database.allEnemies.Count == 0)
        {
            GUILayout.Label("No Enemies found in Database.");
            return;
        }

        _scrollPos = GUILayout.BeginScrollView(_scrollPos);

        int index = 0;
        while (index < _database.allEnemies.Count)
        {
            GUILayout.BeginHorizontal();
            
            for (int i = 0; i < Columns; i++)
            {
                if (index >= _database.allEnemies.Count) 
                {
                    // Fill empty space to keep alignment if last row is incomplete
                    GUILayout.Label("", GUILayout.Width(IconSize));
                }
                else
                {
                    Enemy enemy = _database.allEnemies[index];
                    if (enemy != null)
                    {
                        DrawEnemyCell(enemy);
                    }
                    index++;
                }
                
                // Add spacing between columns
                if (i < Columns - 1) GUILayout.Space(Padding);
            }
            GUILayout.EndHorizontal();
            
            // Add spacing between rows
            GUILayout.Space(10);
        }

        GUILayout.EndScrollView();
    }

    private void DrawEnemyCell(Enemy enemy)
    {
        GUILayout.BeginVertical(GUILayout.Width(IconSize)); 

        // 1. Icon
        Texture2D preview = AssetPreview.GetAssetPreview(enemy.gameObject);
        GUIContent btnContent = (preview != null) ? new GUIContent(preview, enemy.name) : new GUIContent("Load..", enemy.name);

        if (GUILayout.Button(btnContent, GUILayout.Width(IconSize), GUILayout.Height(IconSize)))
        {
            SelectEnemy(enemy);
        }

        // 2. Name
        GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
        labelStyle.alignment = TextAnchor.UpperCenter;
        labelStyle.wordWrap = true;
        labelStyle.clipping = TextClipping.Clip;

        GUILayout.Label(enemy.name, labelStyle, GUILayout.Width(IconSize));

        GUILayout.EndVertical();
    }

    private void SelectEnemy(Enemy enemy)
    {
        _property.serializedObject.Update();
        _property.objectReferenceValue = enemy;
        _property.serializedObject.ApplyModifiedProperties();
        editorWindow.Close();
    }
}