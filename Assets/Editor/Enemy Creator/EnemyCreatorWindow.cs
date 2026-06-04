using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Authoring tool that creates a new enemy as a Prefab Variant of a chosen base
/// enemy prefab. You type the Enemy.cs values, pick a sprite, and either assign an
/// existing AnimatorController or have the tool build a fresh looping clip +
/// controller from a sliced sprite sheet (sample rate defaults to 8, editable).
/// </summary>
public class EnemyCreatorWindow : EditorWindow
{
    private const string DefaultEnemyFolder = "Assets/Prefabs/Enemies";
    private const string DefaultAnimFolder = "Assets/Animation/Enemies";

    private enum AnimMode { CreateFromSheet, UseExistingController, None }

    // ---- Base / output ----
    private GameObject _basePrefab;
    private string _enemyName = "NewEnemy";
    private string _prefabFolder = DefaultEnemyFolder + "/Generic";

    // ---- Enemy stats (mirrors Enemy.cs serialized fields) ----
    private float _moveSpeed = 4f;
    private float _health = 5f;
    private int _damage = 1;
    private float _exp = 1f;
    private int _coinValue = 10;
    private Vector3 _sliderOffset = new Vector3(0, 2, 0);

    // ---- Visual / animation ----
    private Sprite _defaultSprite;
    private AnimMode _animMode = AnimMode.CreateFromSheet;
    private Texture2D _spriteSheet;
    private int _samples = 8;
    private string _animFolder = DefaultAnimFolder + "/Generic";
    private AnimatorController _existingController;

    private Vector2 _scroll;

    [MenuItem("Tools/Enemy Creator")]
    public static void Open() => GetWindow<EnemyCreatorWindow>("Enemy Creator");

    // ============================================================ GUI

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        // ---- Base & output ----
        EditorGUILayout.LabelField("Base & Output", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        _basePrefab = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Base Enemy Prefab", "Template prefab to make a Variant of. Must have an Enemy component on its root."),
            _basePrefab, typeof(GameObject), false);
        if (EditorGUI.EndChangeCheck()) OnBaseChanged();

        if (_basePrefab != null && _basePrefab.GetComponent<Enemy>() == null)
            EditorGUILayout.HelpBox("This prefab has no Enemy component on its root.", MessageType.Error);

        _enemyName = EditorGUILayout.TextField("New Enemy Name", _enemyName);
        _prefabFolder = DrawFolderField("Prefab Folder", _prefabFolder);

        EditorGUILayout.Space();

        // ---- Stats ----
        EditorGUILayout.LabelField("Enemy Stats", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(_basePrefab == null))
        {
            _moveSpeed = EditorGUILayout.FloatField("Move Speed", _moveSpeed);
            _health = EditorGUILayout.FloatField("Health", _health);
            _damage = EditorGUILayout.IntField("Damage", _damage);
            _exp = EditorGUILayout.FloatField("Exp", _exp);
            _coinValue = EditorGUILayout.IntField("Coin Value", _coinValue);
            _sliderOffset = EditorGUILayout.Vector3Field("Health Bar Offset", _sliderOffset);
            if (_basePrefab != null && GUILayout.Button("Reset stats from base", EditorStyles.miniButton))
                LoadStatsFromBase();
        }

        EditorGUILayout.Space();

        // ---- Sprite & animation ----
        EditorGUILayout.LabelField("Sprite & Animation", EditorStyles.boldLabel);
        _animMode = (AnimMode)EditorGUILayout.EnumPopup("Animation", _animMode);

        if (_animMode == AnimMode.CreateFromSheet)
        {
            _spriteSheet = (Texture2D)EditorGUILayout.ObjectField(
                new GUIContent("Sprite Sheet (sliced)", "A texture imported as Sprite Mode = Multiple and sliced into frames."),
                _spriteSheet, typeof(Texture2D), false);
            _samples = Mathf.Max(1, EditorGUILayout.IntField(
                new GUIContent("Samples (fps)", "Animation sample rate / frames per second."), _samples));
            _animFolder = DrawFolderField("Animation Folder", _animFolder);

            if (_spriteSheet != null)
            {
                int frames = LoadFrames(_spriteSheet).Count;
                EditorGUILayout.HelpBox(frames > 0
                    ? $"{frames} frame(s) found · clip loops at {_samples} fps (~{frames / (float)_samples:F2}s)."
                    : "No sliced sprites found. Set the texture's Sprite Mode to Multiple and slice it.",
                    frames > 0 ? MessageType.Info : MessageType.Warning);
            }
        }
        else if (_animMode == AnimMode.UseExistingController)
        {
            _existingController = (AnimatorController)EditorGUILayout.ObjectField(
                "Animator Controller", _existingController, typeof(AnimatorController), false);
        }

        _defaultSprite = (Sprite)EditorGUILayout.ObjectField(
            new GUIContent("Default Sprite", "Resting sprite for the SpriteRenderer. Empty = use the sheet's first frame."),
            _defaultSprite, typeof(Sprite), false);

        EditorGUILayout.Space();

        // ---- Create ----
        bool can = Validate(out string message, out MessageType type);
        if (!string.IsNullOrEmpty(message))
            EditorGUILayout.HelpBox(message, type);

        using (new EditorGUI.DisabledScope(!can))
        {
            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button("Create Enemy", GUILayout.Height(32))) CreateEnemy();
            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.EndScrollView();
    }

    // ============================================================ Base sync

    private void OnBaseChanged()
    {
        if (_basePrefab == null) return;
        LoadStatsFromBase();

        string basePath = AssetDatabase.GetAssetPath(_basePrefab);
        string dir = ToUnixDir(Path.GetDirectoryName(basePath));
        if (!string.IsNullOrEmpty(dir)) _prefabFolder = dir;

        Animator anim = _basePrefab.GetComponentInChildren<Animator>();
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            string cdir = ToUnixDir(Path.GetDirectoryName(AssetDatabase.GetAssetPath(anim.runtimeAnimatorController)));
            if (!string.IsNullOrEmpty(cdir)) _animFolder = cdir;
        }
    }

    private void LoadStatsFromBase()
    {
        Enemy e = _basePrefab != null ? _basePrefab.GetComponent<Enemy>() : null;
        if (e == null) return;
        var so = new SerializedObject(e);
        _moveSpeed = so.FindProperty("_moveSpeed").floatValue;
        _health = so.FindProperty("_health").floatValue;
        _damage = so.FindProperty("damage").intValue;
        _exp = so.FindProperty("exp").floatValue;
        _coinValue = so.FindProperty("coinValue").intValue;
        _sliderOffset = so.FindProperty("sliderOffset").vector3Value;
    }

    // ============================================================ Validation

    private bool Validate(out string message, out MessageType type)
    {
        message = ""; type = MessageType.None;

        if (_basePrefab == null) { message = "Assign a base enemy prefab."; type = MessageType.Info; return false; }
        if (_basePrefab.GetComponent<Enemy>() == null) { message = "Base prefab needs an Enemy component on its root."; type = MessageType.Error; return false; }
        if (string.IsNullOrWhiteSpace(_enemyName)) { message = "Enter a name for the new enemy."; type = MessageType.Info; return false; }
        if (!_prefabFolder.StartsWith("Assets")) { message = "Prefab folder must be inside the project (start with 'Assets')."; type = MessageType.Error; return false; }

        if (_animMode == AnimMode.CreateFromSheet)
        {
            if (_spriteSheet == null) { message = "Assign a sliced sprite sheet, or change the Animation mode."; type = MessageType.Info; return false; }
            if (LoadFrames(_spriteSheet).Count == 0) { message = "The sprite sheet has no sliced sub-sprites."; type = MessageType.Error; return false; }
            if (!_animFolder.StartsWith("Assets")) { message = "Animation folder must be inside the project."; type = MessageType.Error; return false; }
        }
        else if (_animMode == AnimMode.UseExistingController && _existingController == null)
        {
            message = "Assign an Animator Controller, or change the Animation mode."; type = MessageType.Info; return false;
        }

        return true;
    }

    // ============================================================ Creation

    private void CreateEnemy()
    {
        string name = Sanitize(_enemyName);

        // 1. Build / resolve the animator controller.
        AnimatorController controller = null;
        if (_animMode == AnimMode.CreateFromSheet)
        {
            EnsureFolder(_animFolder);
            List<Sprite> frames = LoadFrames(_spriteSheet);
            string spritePath = GetSpriteRendererPath();

            AnimationClip clip = BuildSpriteClip(frames, _samples, spritePath);
            string clipPath = AssetDatabase.GenerateUniqueAssetPath($"{_animFolder}/{name}_Move.anim");
            AssetDatabase.CreateAsset(clip, clipPath);

            string ctrlPath = AssetDatabase.GenerateUniqueAssetPath($"{_animFolder}/{name}_Animc.controller");
            controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(ctrlPath, clip);

            if (_defaultSprite == null && frames.Count > 0) _defaultSprite = frames[0];
        }
        else if (_animMode == AnimMode.UseExistingController)
        {
            controller = _existingController;
        }

        // 2. Instantiate the base, override values, save as a Variant.
        EnsureFolder(_prefabFolder);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(_basePrefab);
        try
        {
            instance.name = name;

            Enemy enemy = instance.GetComponent<Enemy>();
            var so = new SerializedObject(enemy);
            so.FindProperty("_moveSpeed").floatValue = _moveSpeed;
            so.FindProperty("_health").floatValue = _health;
            so.FindProperty("damage").intValue = _damage;
            so.FindProperty("exp").floatValue = _exp;
            so.FindProperty("coinValue").intValue = _coinValue;
            so.FindProperty("sliderOffset").vector3Value = _sliderOffset;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (_defaultSprite != null)
            {
                SpriteRenderer sr = instance.GetComponent<SpriteRenderer>() ?? instance.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) sr.sprite = _defaultSprite;
            }

            if (controller != null)
            {
                Animator anim = instance.GetComponent<Animator>() ?? instance.GetComponentInChildren<Animator>();
                if (anim != null) anim.runtimeAnimatorController = controller;
            }

            string prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{_prefabFolder}/{name}.prefab");
            GameObject variant = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath, out bool ok);
            AssetDatabase.SaveAssets();

            if (ok && variant != null)
            {
                EditorGUIUtility.PingObject(variant);
                Selection.activeObject = variant;
                Debug.Log($"<color=green>Enemy Creator:</color> created '{name}' (Variant of {_basePrefab.name}) at {prefabPath}");
            }
            else
            {
                Debug.LogError("Enemy Creator: failed to save the prefab variant.");
            }
        }
        finally
        {
            DestroyImmediate(instance);
        }
    }

    /// <summary>Builds a looping sprite-keyframe clip bound to the SpriteRenderer at 'spritePath'.</summary>
    private static AnimationClip BuildSpriteClip(List<Sprite> frames, int samples, string spritePath)
    {
        var clip = new AnimationClip { frameRate = samples };

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(spritePath, typeof(SpriteRenderer), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[frames.Count];
        for (int i = 0; i < frames.Count; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / (float)samples, value = frames[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        return clip;
    }

    /// <summary>Path of the SpriteRenderer relative to the Animator (clip binding root). "" if on the same object.</summary>
    private string GetSpriteRendererPath()
    {
        Animator anim = _basePrefab.GetComponentInChildren<Animator>();
        SpriteRenderer sr = _basePrefab.GetComponent<SpriteRenderer>() ?? _basePrefab.GetComponentInChildren<SpriteRenderer>();
        if (sr == null) return "";
        Transform root = anim != null ? anim.transform : _basePrefab.transform;
        return AnimationUtility.CalculateTransformPath(sr.transform, root);
    }

    // ============================================================ Helpers

    /// <summary>Loads a sliced texture's sub-sprites, ordered naturally (frame_0, frame_1, …, frame_10).</summary>
    private static List<Sprite> LoadFrames(Texture2D sheet)
    {
        var list = new List<Sprite>();
        if (sheet == null) return list;

        string path = AssetDatabase.GetAssetPath(sheet);
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
            if (o is Sprite s) list.Add(s);

        list.Sort((a, b) => NaturalCompare(a.name, b.name));
        return list;
    }

    private static int NaturalCompare(string a, string b)
    {
        int ia = a.Length, ib = b.Length;
        while (ia > 0 && char.IsDigit(a[ia - 1])) ia--;
        while (ib > 0 && char.IsDigit(b[ib - 1])) ib--;

        int cmp = string.Compare(a.Substring(0, ia), b.Substring(0, ib), System.StringComparison.OrdinalIgnoreCase);
        if (cmp != 0) return cmp;

        string da = a.Substring(ia), db = b.Substring(ib);
        if (da.Length == 0 || db.Length == 0)
            return string.Compare(a, b, System.StringComparison.OrdinalIgnoreCase);
        return int.Parse(da).CompareTo(int.Parse(db));
    }

    private string DrawFolderField(string label, string value)
    {
        EditorGUILayout.BeginHorizontal();
        value = EditorGUILayout.TextField(label, value);
        if (GUILayout.Button("…", GUILayout.Width(28)))
        {
            string abs = EditorUtility.OpenFolderPanel("Choose folder (inside Assets)", value, "");
            if (!string.IsNullOrEmpty(abs))
            {
                string rel = AbsoluteToProject(abs);
                if (rel != null) { value = rel; GUI.changed = true; GUI.FocusControl(null); }
                else EditorUtility.DisplayDialog("Invalid folder", "The folder must be inside this project's Assets folder.", "OK");
            }
        }
        EditorGUILayout.EndHorizontal();
        return value;
    }

    private static string AbsoluteToProject(string abs)
    {
        abs = abs.Replace("\\", "/");
        string data = Application.dataPath.Replace("\\", "/"); // …/Assets
        if (abs == data) return "Assets";
        if (abs.StartsWith(data + "/")) return "Assets" + abs.Substring(data.Length);
        return null;
    }

    private static void EnsureFolder(string folder)
    {
        folder = folder.Replace("\\", "/").TrimEnd('/');
        if (string.IsNullOrEmpty(folder) || folder == "Assets" || AssetDatabase.IsValidFolder(folder)) return;
        string parent = ToUnixDir(Path.GetDirectoryName(folder));
        string leaf = Path.GetFileName(folder);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    private static string ToUnixDir(string p) => string.IsNullOrEmpty(p) ? p : p.Replace("\\", "/");

    private static string Sanitize(string s)
    {
        s = (s ?? "").Trim();
        foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return string.IsNullOrEmpty(s) ? "NewEnemy" : s;
    }
}
