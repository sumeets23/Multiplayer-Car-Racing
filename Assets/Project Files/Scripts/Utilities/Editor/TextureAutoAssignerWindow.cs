using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor Window Tool: Assigns textures from a folder to a list of materials by name matching.
/// Open via: Tools > Texture Auto Assigner
/// </summary>
public class TextureAutoAssignerWindow : EditorWindow
{
    // ────────────────────────────────────────────────────────────────────────────
    //  Data
    // ────────────────────────────────────────────────────────────────────────────

    [Serializable]
    private class MaterialEntry
    {
        public Material material;
        public string overrideName = ""; // optional name override for matching
    }

    private List<MaterialEntry> _materials = new List<MaterialEntry>();
    private string _textureFolderPath = "Assets/Textures";

    // Which shader property to assign the texture to
    private string _targetProperty = "_BaseMap"; // URP default; also try "_MainTex"
    private readonly string[] _propertyPresets = { "_BaseMap", "_MainTex", "_BumpMap", "_MetallicGlossMap", "_EmissionMap", "_OcclusionMap" };
    private int _propertyPresetIndex = 0;

    // Matching options
    private bool _caseInsensitive = true;
    private bool _partialMatch = true;

    // Scroll position for the materials list
    private Vector2 _scroll;

    // ── Matching preview list ──
    private List<(MaterialEntry entry, Texture2D matched)> _preview = new List<(MaterialEntry, Texture2D)>();
    private bool _previewDirty = true;

    // ── Styles ──
    private GUIStyle _headerStyle;
    private GUIStyle _subHeaderStyle;
    private GUIStyle _matchedStyle;
    private GUIStyle _unmatchedStyle;
    private bool _stylesInitialized;

    // ────────────────────────────────────────────────────────────────────────────
    //  Init
    // ────────────────────────────────────────────────────────────────────────────

    [MenuItem("Tools/Texture Auto Assigner")]
    public static void OpenWindow()
    {
        var window = GetWindow<TextureAutoAssignerWindow>("Texture Auto Assigner");
        window.minSize = new Vector2(480, 580);
        window.Show();
    }

    private void InitStyles()
    {
        if (_stylesInitialized) return;

        _headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 15,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.9f, 0.9f, 1f) }
        };

        _subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11,
            normal = { textColor = new Color(0.7f, 0.85f, 1f) }
        };

        _matchedStyle = new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = new Color(0.4f, 1f, 0.5f) }
        };

        _unmatchedStyle = new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = new Color(1f, 0.4f, 0.4f) }
        };

        _stylesInitialized = true;
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  GUI
    // ────────────────────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        InitStyles();

        DrawBackground();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("⬡  Texture Auto Assigner", _headerStyle, GUILayout.Height(28));
        EditorGUILayout.Space(4);
        DrawDivider();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawFolderSection();
        DrawDivider();
        DrawOptionsSection();
        DrawDivider();
        DrawMaterialsSection();
        DrawDivider();
        DrawPreviewSection();
        DrawDivider();
        DrawActionButtons();

        EditorGUILayout.EndScrollView();
    }

    // ── Background tint ──
    private void DrawBackground()
    {
        var rect = new Rect(0, 0, position.width, position.height);
        EditorGUI.DrawRect(rect, new Color(0.13f, 0.13f, 0.17f, 1f));
    }

    private void DrawDivider()
    {
        EditorGUILayout.Space(4);
        var rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.4f, 1f));
        EditorGUILayout.Space(4);
    }

    // ── Folder / property section ──
    private void DrawFolderSection()
    {
        EditorGUILayout.LabelField("📁  Texture Folder", _subHeaderStyle);
        EditorGUILayout.Space(2);

        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        _textureFolderPath = EditorGUILayout.TextField("Folder (Assets/…)", _textureFolderPath);
        if (EditorGUI.EndChangeCheck()) _previewDirty = true;

        if (GUILayout.Button("Browse", GUILayout.Width(70)))
        {
            string picked = EditorUtility.OpenFolderPanel("Select Texture Folder", "Assets", "");
            if (!string.IsNullOrEmpty(picked))
            {
                // Convert absolute path → relative
                if (picked.StartsWith(Application.dataPath))
                    picked = "Assets" + picked.Substring(Application.dataPath.Length);
                _textureFolderPath = picked;
                _previewDirty = true;
            }
        }
        EditorGUILayout.EndHorizontal();

        // Show texture count from folder
        var textures = LoadTexturesFromFolder();
        EditorGUILayout.HelpBox($"Textures found in folder: {textures.Count}", MessageType.None);
    }

    // ── Options section ──
    private void DrawOptionsSection()
    {
        EditorGUILayout.LabelField("⚙  Matching Options", _subHeaderStyle);
        EditorGUILayout.Space(2);

        // Shader property presets
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        int newIndex = EditorGUILayout.Popup("Shader Property", _propertyPresetIndex, _propertyPresets);
        if (EditorGUI.EndChangeCheck() && newIndex != _propertyPresetIndex)
        {
            _propertyPresetIndex = newIndex;
            _targetProperty = _propertyPresets[_propertyPresetIndex];
            _previewDirty = true;
        }
        _targetProperty = EditorGUILayout.TextField(_targetProperty, GUILayout.Width(130));
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginChangeCheck();
        _caseInsensitive = EditorGUILayout.Toggle("Case Insensitive", _caseInsensitive);
        _partialMatch    = EditorGUILayout.Toggle("Allow Partial Match", _partialMatch);
        if (EditorGUI.EndChangeCheck()) _previewDirty = true;
    }

    // ── Materials list ──
    private void DrawMaterialsSection()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("🎨  Materials List", _subHeaderStyle);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("＋ Add", GUILayout.Width(70)))
        {
            _materials.Add(new MaterialEntry());
            _previewDirty = true;
        }
        if (GUILayout.Button("Clear", GUILayout.Width(60)))
        {
            _materials.Clear();
            _previewDirty = true;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4);

        // Drag-and-drop a folder of materials
        DrawMaterialDragDropArea();

        int toRemove = -1;
        for (int i = 0; i < _materials.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            _materials[i].material = (Material)EditorGUILayout.ObjectField(
                _materials[i].material, typeof(Material), false, GUILayout.Width(200));
            if (EditorGUI.EndChangeCheck()) _previewDirty = true;

            EditorGUI.BeginChangeCheck();
            _materials[i].overrideName = EditorGUILayout.TextField(
                new GUIContent("", "Override name for matching (leave blank to use material name)"),
                _materials[i].overrideName, GUILayout.ExpandWidth(true));
            if (EditorGUI.EndChangeCheck()) _previewDirty = true;

            if (GUILayout.Button("✕", GUILayout.Width(24)))
                toRemove = i;

            EditorGUILayout.EndHorizontal();
        }

        if (toRemove >= 0)
        {
            _materials.RemoveAt(toRemove);
            _previewDirty = true;
        }
    }

    // Drag-and-drop zone to bulk-add materials from the Project window
    private void DrawMaterialDragDropArea()
    {
        var dropRect = GUILayoutUtility.GetRect(0, 36, GUILayout.ExpandWidth(true));
        var style = new GUIStyle(EditorStyles.helpBox) { alignment = TextAnchor.MiddleCenter };
        GUI.Box(dropRect, "⟵ Drop Materials here to bulk-add", style);

        Event evt = Event.current;
        if (evt.type == EventType.DragUpdated && dropRect.Contains(evt.mousePosition))
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            evt.Use();
        }
        if (evt.type == EventType.DragPerform && dropRect.Contains(evt.mousePosition))
        {
            DragAndDrop.AcceptDrag();
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is Material mat)
                {
                    if (_materials.All(m => m.material != mat))
                    {
                        _materials.Add(new MaterialEntry { material = mat });
                        _previewDirty = true;
                    }
                }
            }
            evt.Use();
        }
        EditorGUILayout.Space(3);
    }

    // ── Live preview ──
    private void DrawPreviewSection()
    {
        EditorGUILayout.LabelField("🔍  Match Preview", _subHeaderStyle);
        EditorGUILayout.Space(2);

        if (_previewDirty)
        {
            RebuildPreview();
            _previewDirty = false;
        }

        if (_preview.Count == 0)
        {
            EditorGUILayout.HelpBox("Add materials and a texture folder to see matches here.", MessageType.Info);
            return;
        }

        foreach (var (entry, matched) in _preview)
        {
            if (entry.material == null) continue;

            EditorGUILayout.BeginHorizontal();
            string matName = string.IsNullOrEmpty(entry.overrideName) ? entry.material.name : entry.overrideName + " (override)";

            if (matched != null)
            {
                EditorGUILayout.LabelField($"✔  {matName}  →  {matched.name}", _matchedStyle);
                // Tiny thumbnail
                var thumbRect = GUILayoutUtility.GetRect(24, 24, GUILayout.Width(24));
                EditorGUI.DrawPreviewTexture(thumbRect, matched);
            }
            else
            {
                EditorGUILayout.LabelField($"✘  {matName}  →  no match", _unmatchedStyle);
            }
            EditorGUILayout.EndHorizontal();
        }

        int matched_count = _preview.Count(p => p.matched != null);
        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox($"Matched {matched_count} / {_preview.Count} materials.", MessageType.None);
    }

    // ── Action buttons ──
    private void DrawActionButtons()
    {
        EditorGUILayout.Space(6);

        var btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            fixedHeight = 40
        };

        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.5f);
        if (GUILayout.Button("✅  Assign Textures", btnStyle))
        {
            AssignTextures();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(4);

        GUI.backgroundColor = new Color(0.8f, 0.4f, 0.4f);
        if (GUILayout.Button("↺  Refresh Preview", GUILayout.Height(28)))
        {
            _previewDirty = true;
            Repaint();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space(6);
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  Core Logic
    // ────────────────────────────────────────────────────────────────────────────

    private List<Texture2D> LoadTexturesFromFolder()
    {
        var list = new List<Texture2D>();
        if (!AssetDatabase.IsValidFolder(_textureFolderPath))
            return list;

        // Find all texture GUIDs recursively in the folder
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { _textureFolderPath });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
                list.Add(tex);
        }
        return list;
    }

    private Texture2D FindBestMatch(string materialName, List<Texture2D> textures)
    {
        var comparison = _caseInsensitive
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        // 1. Exact match (material name == texture name)
        foreach (var tex in textures)
            if (tex.name.Equals(materialName, comparison))
                return tex;

        if (!_partialMatch) return null;

        // 2. Texture name contains material name
        foreach (var tex in textures)
            if (tex.name.Contains(materialName, comparison))
                return tex;

        // 3. Material name contains texture name
        foreach (var tex in textures)
            if (materialName.Contains(tex.name, comparison))
                return tex;

        // 4. Longest common substring (fuzzy)
        Texture2D bestTex = null;
        int bestScore = 0;
        foreach (var tex in textures)
        {
            int score = LongestCommonSubstring(
                _caseInsensitive ? materialName.ToLower() : materialName,
                _caseInsensitive ? tex.name.ToLower() : tex.name);

            if (score > bestScore && score >= 3) // at least 3 chars in common
            {
                bestScore = score;
                bestTex = tex;
            }
        }
        return bestTex;
    }

    private void RebuildPreview()
    {
        _preview.Clear();
        var textures = LoadTexturesFromFolder();
        foreach (var entry in _materials)
        {
            if (entry.material == null) continue;
            string matchName = string.IsNullOrEmpty(entry.overrideName)
                ? entry.material.name
                : entry.overrideName;
            var matched = FindBestMatch(matchName, textures);
            _preview.Add((entry, matched));
        }
    }

    private void AssignTextures()
    {
        RebuildPreview();

        int count = 0;
        foreach (var (entry, matched) in _preview)
        {
            if (entry.material == null || matched == null) continue;

            Undo.RecordObject(entry.material, "Assign Texture");

            // Check if shader has the target property
            if (entry.material.HasProperty(_targetProperty))
            {
                entry.material.SetTexture(_targetProperty, matched);
                EditorUtility.SetDirty(entry.material);
                count++;
                Debug.Log($"[TextureAutoAssigner] Assigned '{matched.name}' → '{entry.material.name}' ({_targetProperty})");
            }
            else
            {
                Debug.LogWarning($"[TextureAutoAssigner] Material '{entry.material.name}' has no property '{_targetProperty}'. Skipping.");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Done!", $"Assigned textures to {count} material(s).", "OK");
    }

    // ────────────────────────────────────────────────────────────────────────────
    //  Utility: Longest Common Substring length
    // ────────────────────────────────────────────────────────────────────────────

    private static int LongestCommonSubstring(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0;
        int[,] dp = new int[s1.Length + 1, s2.Length + 1];
        int max = 0;
        for (int i = 1; i <= s1.Length; i++)
            for (int j = 1; j <= s2.Length; j++)
                if (s1[i - 1] == s2[j - 1])
                {
                    dp[i, j] = dp[i - 1, j - 1] + 1;
                    if (dp[i, j] > max) max = dp[i, j];
                }
        return max;
    }
}
