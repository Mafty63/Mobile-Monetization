#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MobileCore.MainModule.Editor
{
    /// <summary>
    /// Plugin Setup Panel — accessible via Tools > MobileCore > Plugin Setup
    /// </summary>
    public class PluginSetupPanel : EditorWindow
    {
        // ── Asset Paths ────────────────────────────────────────────────────────────
        private const string PrefabPath   = "Assets/Mobile Monetization/Plugin Resources/Plugin Resources/Prefabs/Main System Manager.prefab";
        private const string SettingsPath = "Assets/Mobile Monetization/Plugin Resources/Plugin Settings/MainSystemSettings.asset";
        private const string SceneAdsPath = "Assets/Mobile Monetization/Example/Adsvertisement/Ads Manager Example.unity";
        private const string SceneIAPPath = "Assets/Mobile Monetization/Example/IAP/IAPModuleExample.unity";

        // ── State ──────────────────────────────────────────────────────────────────
        private UnityEngine.Object _settingsAsset;
        private Vector2            _scroll;

        // ── Menu Item ──────────────────────────────────────────────────────────────
        [MenuItem("Tools/MobileCore/Plugin Setup", priority = 0)]
        public static void OpenWindow()
        {
            var win = GetWindow<PluginSetupPanel>(false, "Plugin Setup");
            win.minSize = new Vector2(400f, 480f);
            win.Show();
        }

        // ── Lifecycle ──────────────────────────────────────────────────────────────
        private void OnEnable()
        {
            EditorStyleTemplate.InitializeStyles();
            _settingsAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(SettingsPath);
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawHeader();
            GUILayout.Space(10f);

            DrawSectionSetupScene();
            GUILayout.Space(6f);

            DrawSectionExampleScenes();
            GUILayout.Space(6f);

            DrawSectionSettings();
            GUILayout.Space(10f);

            EditorGUILayout.EndScrollView();
        }

        // ── Header ─────────────────────────────────────────────────────────────────
        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("MOBILE MONETIZATION", EditorStyleTemplate.GrayBoldLabelStyle);
            EditorGUILayout.LabelField("Plugin Setup Panel", EditorStyleTemplate.GrayMiniLabelStyle);

            EditorGUILayout.EndVertical();
        }

        // ── Section: Setup Scene ───────────────────────────────────────────────────
        private void DrawSectionSetupScene()
        {
            DrawSection("SETUP SCENE", () =>
            {
                EditorGUILayout.LabelField(
                    "Add Main System Manager to the active scene. " +
                    "If it already exists, no duplicate will be created.",
                    EditorStyles.wordWrappedMiniLabel);

                GUILayout.Space(6f);

                // Prefab reference row
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Prefab", EditorStyleTemplate.GrayMiniLabelStyle, GUILayout.Width(52f));
                using (new EditorGUI.DisabledScope(true))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                    EditorGUILayout.ObjectField(prefab, typeof(GameObject), false);
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(8f);

                // Action button
                bool prefabExists   = !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(PrefabPath));
                bool alreadySpawned = FindMainSystemInScene();

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (alreadySpawned)
                {
                    using (new EditorGUI.DisabledScope(true))
                        GUILayout.Button("Already in Scene", EditorStyleTemplate.GrayButtonStyle, GUILayout.Height(26f), GUILayout.Width(190f));
                }
                else if (!prefabExists)
                {
                    using (new EditorGUI.DisabledScope(true))
                        GUILayout.Button("Prefab Not Found", EditorStyleTemplate.GrayButtonStyle, GUILayout.Height(26f), GUILayout.Width(190f));
                }
                else
                {
                    if (GUILayout.Button("Setup Scene",
                        EditorStyleTemplate.CreateButtonStyle(new Color(0.18f, 0.60f, 0.35f), null, 26),
                        GUILayout.Height(26f), GUILayout.Width(190f)))
                    {
                        SpawnMainSystemPrefab();
                    }
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            });
        }

        // ── Section: Example Scenes ────────────────────────────────────────────────
        private void DrawSectionExampleScenes()
        {
            DrawSection("EXAMPLE SCENES", () =>
            {
                EditorGUILayout.LabelField(
                    "Open example scenes to explore the Ads and IAP module implementations.",
                    EditorStyles.wordWrappedMiniLabel);

                GUILayout.Space(6f);

                DrawOpenSceneRow("Ads Manager Example", SceneAdsPath);
                GUILayout.Space(4f);
                DrawOpenSceneRow("IAP Module Example", SceneIAPPath);
            });
        }

        // ── Section: Settings ──────────────────────────────────────────────────────
        private void DrawSectionSettings()
        {
            DrawSection("MAIN SYSTEM SETTINGS", () =>
            {
                EditorGUILayout.LabelField(
                    "Open Main System Settings in the Inspector to configure the plugin.",
                    EditorStyles.wordWrappedMiniLabel);

                GUILayout.Space(6f);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Asset", EditorStyleTemplate.GrayMiniLabelStyle, GUILayout.Width(52f));
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField(_settingsAsset, typeof(UnityEngine.Object), false);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(8f);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                bool found = _settingsAsset != null;
                using (new EditorGUI.DisabledScope(!found))
                {
                    if (GUILayout.Button("Open Settings",
                        EditorStyleTemplate.CreateButtonStyle(new Color(0.20f, 0.47f, 0.82f), null, 26),
                        GUILayout.Height(26f), GUILayout.Width(160f)))
                    {
                        Selection.activeObject = _settingsAsset;
                        EditorUtility.FocusProjectWindow();
                    }
                }

                if (!found)
                {
                    GUILayout.Space(6f);
                    if (GUILayout.Button("Reload", EditorStyleTemplate.GrayButtonStyle, GUILayout.Height(26f), GUILayout.Width(70f)))
                        _settingsAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(SettingsPath);
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            });
        }

        // ── Drawing Helpers ────────────────────────────────────────────────────────
        private static void DrawSection(string title, Action content)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField(title, EditorStyleTemplate.GrayBoldLabelStyle);

            // Thin divider
            var divRect = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
            bool dark = EditorGUIUtility.isProSkin;
            EditorGUI.DrawRect(divRect, dark ? new Color(0.32f, 0.32f, 0.35f) : new Color(0.68f, 0.68f, 0.70f));

            GUILayout.Space(8f);
            content?.Invoke();

            EditorGUILayout.EndVertical();
        }

        private static void DrawOpenSceneRow(string label, string scenePath)
        {
            bool exists = !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(scenePath));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, EditorStyleTemplate.GrayTextStyle, GUILayout.ExpandWidth(true));

            if (!exists)
                EditorGUILayout.LabelField("Not Found", EditorStyleTemplate.GrayMiniLabelStyle, GUILayout.Width(62f));

            using (new EditorGUI.DisabledScope(!exists))
            {
                if (GUILayout.Button("Open",
                    EditorStyleTemplate.CreateButtonStyle(new Color(0.20f, 0.47f, 0.82f), null, 22),
                    GUILayout.Height(22f), GUILayout.Width(58f)))
                {
                    OpenScene(scenePath);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        // ── Scene Helpers ──────────────────────────────────────────────────────────
        private static bool FindMainSystemInScene()
        {
            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (go.name.Contains("Main System Manager")) return true;
                foreach (Transform child in go.transform)
                    if (child.name.Contains("Main System Manager")) return true;
            }
            return false;
        }

        private static void SpawnMainSystemPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Prefab not found:\n" + PrefabPath, "OK");
                return;
            }

            var go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (go == null) return;

            Undo.RegisterCreatedObjectUndo(go, "Spawn Main System Manager");
            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[Plugin Setup] Main System Manager successfully spawned.");
        }

        private static void OpenScene(string path)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }
    }
}
#endif
