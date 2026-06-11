#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MobileCore.SystemModule;

namespace MobileCore.MainModule.Editor
{
    /// <summary>
    /// Plugin Setup Panel — accessible via Tools > MobileCore > Plugin Setup
    /// </summary>
    public class PluginSetupPanel : EditorWindow
    {
        // ── Asset Paths ────────────────────────────────────────────────────────────
        private const string ResourcesFolder   = "Assets/Mobile Monetization/Plugin Resources/Resources/Plugin Settings";
        private const string SettingsPath      = ResourcesFolder + "/MobileCoreConfig.asset";
        private const string SceneAdsPath      = "Assets/Mobile Monetization/Example/Adsvertisement/Ads Manager Example.unity";
        private const string SceneIAPPath      = "Assets/Mobile Monetization/Example/IAP/IAPModuleExample.unity";

        // ── State ──────────────────────────────────────────────────────────────────
        private MobileCoreConfig _config;
        private Vector2          _scroll;

        // ── Menu Item ──────────────────────────────────────────────────────────────
        [MenuItem("Tools/MobileCore/Plugin Setup", priority = 0)]
        public static void OpenWindow()
        {
            var win = GetWindow<PluginSetupPanel>(false, "Plugin Setup");
            win.minSize = new Vector2(420f, 520f);
            win.Show();
        }

        // ── Lifecycle ──────────────────────────────────────────────────────────────
        private void OnEnable()
        {
            ReloadConfig();
        }

        private void ReloadConfig()
        {
            _config = AssetDatabase.LoadAssetAtPath<MobileCoreConfig>(SettingsPath);
        }

        private void OnGUI()
        {
            // Initialize styles here — safe because OnGUI only fires when editor rendering is ready
            EditorStyleTemplate.InitializeStyles();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawHeader();
            GUILayout.Space(10f);

            DrawSectionSetupScene();
            GUILayout.Space(6f);

            DrawSectionSettings();
            GUILayout.Space(6f);

            DrawSectionExampleScenes();
            GUILayout.Space(10f);

            EditorGUILayout.EndScrollView();
        }

        // ── Header ─────────────────────────────────────────────────────────────────
        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("MOBILE CORE", EditorStyleTemplate.GrayBoldLabelStyle);
            EditorGUILayout.LabelField("Plugin Setup Panel", EditorStyleTemplate.GrayMiniLabelStyle);
            EditorGUILayout.EndVertical();
        }

        // ── Section: Runtime Status ────────────────────────────────────────────────
        private void DrawSectionSetupScene()
        {
            DrawSection("AUTOMATION STATUS", () =>
            {
                EditorGUILayout.HelpBox(
                    "Scene setup is fully automated!\n" +
                    "The plugin bootstraps itself at runtime — no prefab in the hierarchy needed.",
                    MessageType.Info);

                GUILayout.Space(6f);

                if (FindMainSystemInScene())
                {
                    EditorGUILayout.HelpBox(
                        "A legacy Main System Manager was found in the active scene. You can safely remove it.",
                        MessageType.Warning);

                    if (GUILayout.Button("Clean Scene (Remove Legacy Manager)",
                        EditorStyleTemplate.CreateButtonStyle(new Color(0.7f, 0.2f, 0.2f), null, 26),
                        GUILayout.Height(26f)))
                    {
                        RemoveLegacyManagerFromScene();
                    }
                }
            });
        }

        // ── Section: Settings ──────────────────────────────────────────────────────
        private void DrawSectionSettings()
        {
            DrawSection("PLUGIN SETTINGS", () =>
            {
                bool found = _config != null;

                if (!found)
                {
                    EditorGUILayout.HelpBox(
                        "No MobileCoreConfig found at the expected Resources path.\n" +
                        "Click \"Create All Settings\" to auto-generate the config and all module configs.",
                        MessageType.Warning);

                    GUILayout.Space(6f);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("✦  Create All Settings",
                        EditorStyleTemplate.CreateButtonStyle(new Color(0.18f, 0.52f, 0.28f), null, 26),
                        GUILayout.Height(30f), GUILayout.Width(200f)))
                    {
                        CreateAllSettings();
                        ReloadConfig();
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    // Show path + quick-open
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Asset", EditorStyleTemplate.GrayMiniLabelStyle, GUILayout.Width(42f));
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.ObjectField(_config, typeof(MobileCoreConfig), false);
                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(6f);

                    // Module status
                    DrawModuleStatus();

                    GUILayout.Space(8f);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Open Config",
                        EditorStyleTemplate.CreateButtonStyle(new Color(0.20f, 0.47f, 0.82f), null, 26),
                        GUILayout.Height(26f), GUILayout.Width(140f)))
                    {
                        Selection.activeObject = _config;
                        EditorUtility.FocusProjectWindow();
                    }

                    GUILayout.Space(6f);

                    if (GUILayout.Button("Reload", EditorStyleTemplate.GrayButtonStyle,
                        GUILayout.Height(26f), GUILayout.Width(70f)))
                    {
                        ReloadConfig();
                    }

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
            });
        }

        // ── Module Status Display ──────────────────────────────────────────────────
        private void DrawModuleStatus()
        {
            if (_config == null) return;

            EditorGUILayout.LabelField("Registered Modules:", EditorStyleTemplate.GrayMiniLabelStyle);
            GUILayout.Space(2f);

            var modules = _config.Modules;
            if (modules == null || modules.Count == 0)
            {
                EditorGUILayout.HelpBox("No modules in the config. Open config and add module assets.", MessageType.Info);
                return;
            }

            foreach (var module in modules)
            {
                if (module == null)
                {
                    DrawModuleRow("(Missing Reference)", false, null);
                    continue;
                }
                DrawModuleRow(module.ModuleName, module.ModuleEnabled, module);
            }
        }

        private void DrawModuleRow(string name, bool enabled, MobileModule module)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            Color statusColor = enabled ? new Color(0.2f, 0.7f, 0.3f) : new Color(0.6f, 0.6f, 0.6f);
            string statusLabel = enabled ? "ON" : "OFF";

            GUIStyle statusStyle = EditorStyleTemplate.CreateToggleButtonStyle(enabled, 11);
            GUILayout.Label(statusLabel, statusStyle, GUILayout.Width(34f), GUILayout.Height(18f));
            GUILayout.Space(4f);
            EditorGUILayout.LabelField(name, EditorStyleTemplate.GrayTextStyle, GUILayout.ExpandWidth(true));

            if (module != null)
            {
                if (GUILayout.Button("Open", EditorStyleTemplate.GrayButtonStyle,
                    GUILayout.Width(46f), GUILayout.Height(18f)))
                {
                    Selection.activeObject = module;
                    EditorUtility.FocusProjectWindow();
                }
            }

            EditorGUILayout.EndHorizontal();
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

        // ── Asset Creation ─────────────────────────────────────────────────────────
        private static void CreateAllSettings()
        {
            // Ensure folder exists
            EnsureFolderExists(ResourcesFolder);

            // 1. MobileCoreConfig (main — must live inside Resources)
            MobileCoreConfig coreConfig = AssetDatabase.LoadAssetAtPath<MobileCoreConfig>(SettingsPath);
            if (coreConfig == null)
            {
                coreConfig = ScriptableObject.CreateInstance<MobileCoreConfig>();
                AssetDatabase.CreateAsset(coreConfig, SettingsPath);
                Debug.Log("[PluginSetupPanel] Created: MobileCoreConfig.asset");
            }

            // 2. SystemModuleConfig (same folder)
            SystemModuleConfig systemConfig = GetOrCreate<SystemModuleConfig>(ResourcesFolder + "/SystemModuleConfig.asset");

            // 3. AdsModuleConfig (same folder, with immediate embedded settings generation)
            ScriptableObject adsConfig = GetOrCreateByTypeName(ResourcesFolder + "/AdsModuleConfig.asset", "MobileCore.Advertisements.AdsModuleConfig");
            EnsureEmbeddedSettings(adsConfig);

            // 4. IapModuleConfig (same folder, with immediate embedded settings generation)
            ScriptableObject iapConfig = GetOrCreateByTypeName(ResourcesFolder + "/IapModuleConfig.asset", "MobileCore.IAPModule.IapModuleConfig");
            EnsureEmbeddedSettings(iapConfig);

            // 5. Wire module configs into MobileCoreConfig
            SerializedObject so = new SerializedObject(coreConfig);
            SerializedProperty modulesProp = so.FindProperty("modules");
            AddIfMissing(modulesProp, systemConfig);
            AddIfMissing(modulesProp, adsConfig);
            AddIfMissing(modulesProp, iapConfig);
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = coreConfig;
            EditorUtility.FocusProjectWindow();

            EditorUtility.DisplayDialog(
                "Mobile Core — Setup Complete",
                "All config assets created and wired!\n\n" +
                "• MobileCoreConfig  (in Resources/Plugin Settings)\n" +
                "• SystemModuleConfig (in Resources/Plugin Settings)\n" +
                "• AdsModuleConfig  (AdsSettings embedded — in Resources/Plugin Settings)\n" +
                "• IapModuleConfig  (IAPSettings embedded — in Resources/Plugin Settings)\n\n" +
                "Configuration is fully initialized and ready to use.",
                "OK");
        }

        private static void EnsureEmbeddedSettings(ScriptableObject configObj)
        {
            if (configObj == null) return;
            var method = configObj.GetType().GetMethod("CreateEmbeddedSettings");
            if (method != null)
            {
                method.Invoke(configObj, null);
            }
        }

        /// <summary>Creates a ScriptableObject asset by fully-qualified type name without requiring a compile-time reference.</summary>
        private static ScriptableObject GetOrCreateByTypeName(string path, string typeName)
        {
            var existing = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (existing != null) return existing;

            var type = System.Type.GetType(typeName + ", Assembly-CSharp");
            if (type == null)
            {
                Debug.LogWarning($"[PluginSetupPanel] Could not find type '{typeName}'. Make sure scripts have compiled.");
                return null;
            }

            var instance = ScriptableObject.CreateInstance(type);
            string dir = System.IO.Path.GetDirectoryName(path);
            EnsureFolderExists(dir);
            AssetDatabase.CreateAsset(instance, path);
            Debug.Log($"[PluginSetupPanel] Created: {System.IO.Path.GetFileName(path)}");
            return instance as ScriptableObject;
        }

        private static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                string dir = Path.GetDirectoryName(path);
                EnsureFolderExists(dir);
                AssetDatabase.CreateAsset(asset, path);
                Debug.Log($"[PluginSetupPanel] Created: {Path.GetFileName(path)}");
            }
            return asset;
        }

        private static void AddIfMissing(SerializedProperty listProp, UnityEngine.Object obj)
        {
            if (obj == null) return;
            for (int i = 0; i < listProp.arraySize; i++)
            {
                if (listProp.GetArrayElementAtIndex(i).objectReferenceValue == obj)
                    return; // already present
            }
            listProp.arraySize++;
            listProp.GetArrayElementAtIndex(listProp.arraySize - 1).objectReferenceValue = obj;
        }

        private static void EnsureFolderExists(string unityPath)
        {
            // unityPath is like "Assets/Foo/Bar/Baz"
            string[] parts = unityPath.Replace("\\", "/").Split('/');
            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static string NormalizePath(string path)
        {
            // Resolve ".." manually since Path.GetFullPath might use OS-level paths
            var parts = path.Replace("\\", "/").Split('/');
            var stack = new System.Collections.Generic.Stack<string>();
            foreach (var p in parts)
            {
                if (p == "..") { if (stack.Count > 0) stack.Pop(); }
                else if (p != ".") stack.Push(p);
            }
            var arr = new string[stack.Count];
            stack.CopyTo(arr, 0);
            Array.Reverse(arr);
            return string.Join("/", arr);
        }

        // ── Drawing Helpers ────────────────────────────────────────────────────────
        private static void DrawSection(string title, Action content)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField(title, EditorStyleTemplate.GrayBoldLabelStyle);

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

        private static void RemoveLegacyManagerFromScene()
        {
            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (go.name.Contains("Main System Manager"))
                {
                    Undo.DestroyObjectImmediate(go);
                    Debug.Log("[Plugin Setup] Removed legacy Main System Manager from scene.");
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                    return;
                }
                foreach (Transform child in go.transform)
                {
                    if (child.name.Contains("Main System Manager"))
                    {
                        Undo.DestroyObjectImmediate(child.gameObject);
                        Debug.Log("[Plugin Setup] Removed legacy Main System Manager from scene.");
                        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                        return;
                    }
                }
            }
        }

        private static void OpenScene(string path)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }
    }
}
#endif
