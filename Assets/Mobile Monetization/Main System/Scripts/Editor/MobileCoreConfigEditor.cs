#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MobileCore.MainModule;
using MobileCore.SystemModule;

namespace MobileCore.MainModule.Editor
{
    [CustomEditor(typeof(MobileCoreConfig))]
    public class MobileCoreConfigEditor : UnityEditor.Editor
    {
        private SerializedProperty modulesProp;

        private class ModuleEditorData
        {
            public MobileModule ModuleAsset;
            public SerializedObject SerializedObject;
            public SerializedProperty EnabledProp;
            public bool IsExpanded = true;
            public bool IsCoreModule;
            public int ListIndex;
        }

        private readonly Dictionary<string, ModuleEditorData> moduleEditorCache = new Dictionary<string, ModuleEditorData>();
        private readonly List<ModuleEditorData> allModulesList = new List<ModuleEditorData>();

        private void OnEnable()
        {
            modulesProp = serializedObject.FindProperty("modules");
            RefreshModuleCache();
        }

        private void OnDisable()
        {
            moduleEditorCache.Clear();
            allModulesList.Clear();
        }

        public override void OnInspectorGUI()
        {
            CleanNullModules();
            serializedObject.Update();
            RefreshModuleCache();

            DrawHeader();
            EditorGUILayout.Space(4);
            DrawModulesSection();

            serializedObject.ApplyModifiedProperties();

            foreach (var data in allModulesList)
                data.SerializedObject?.ApplyModifiedProperties();
        }

        // ─────────────────────────────────────────────────────────────────────────────
        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("MOBILE CORE CONFIG", EditorStyleTemplate.GrayBoldLabelStyle);
            EditorGUILayout.LabelField("Manage initialization modules. Modules are loaded top to bottom at runtime.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }

        // ─────────────────────────────────────────────────────────────────────────────
        private void DrawModulesSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Section header row
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("MODULES", EditorStyleTemplate.GrayBoldLabelStyle, GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
            if (EditorStyleTemplate.DrawButton("+ Add Module", new Color(0.18f, 0.50f, 0.20f),
                    new GUILayoutOption[] { GUILayout.Height(22f), GUILayout.Width(110f) }))
                ShowAddModuleMenu();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            if (allModulesList.Count == 0)
            {
                EditorGUILayout.HelpBox("No modules registered. Click '+ Add Module' to add one.", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < allModulesList.Count; i++)
                {
                    DrawModuleRow(allModulesList[i]);
                    if (i < allModulesList.Count - 1)
                        GUILayout.Space(3f);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawModuleRow(ModuleEditorData data)
        {
            EditorGUILayout.BeginVertical(EditorStyles.textArea);

            // ── Row header ──
            EditorGUILayout.BeginHorizontal();

            data.IsExpanded = EditorGUILayout.Foldout(data.IsExpanded,
                data.ModuleAsset.ModuleName.ToUpper(),
                true, EditorStyleTemplate.GrayFoldoutHeaderStyle);

            GUILayout.FlexibleSpace();

            if (data.IsCoreModule)
            {
                // "ALWAYS ON" label — no toggle, no remove
                GUIStyle alwaysOnStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontStyle = FontStyle.Bold,
                    normal    = { textColor = new Color(0.30f, 0.65f, 1.0f) }
                };
                GUILayout.Label("ALWAYS ON", alwaysOnStyle, GUILayout.Height(20f));
                GUILayout.Space(4);
                if (EditorStyleTemplate.DrawGrayButton("Open", new GUILayoutOption[] { GUILayout.Height(20f), GUILayout.Width(50f) }))
                {
                    Selection.activeObject = data.ModuleAsset;
                    EditorUtility.FocusProjectWindow();
                }
            }
            else
            {
                // DISABLE / ENABLE toggle
                if (data.EnabledProp != null)
                {
                    bool isEnabled = data.EnabledProp.boolValue;
                    if (EditorStyleTemplate.DrawToggleButton(isEnabled,
                            new GUILayoutOption[] { GUILayout.Height(20f), GUILayout.Width(76f) }))
                    {
                        data.EnabledProp.boolValue = !isEnabled;
                        data.SerializedObject.ApplyModifiedProperties();
                    }
                }

                GUILayout.Space(4);

                if (EditorStyleTemplate.DrawGrayButton("Open",
                        new GUILayoutOption[] { GUILayout.Height(20f), GUILayout.Width(50f) }))
                {
                    Selection.activeObject = data.ModuleAsset;
                    EditorUtility.FocusProjectWindow();
                }

                GUILayout.Space(4);

                if (EditorStyleTemplate.DrawGrayButton("✕",
                        new GUILayoutOption[] { GUILayout.Height(20f), GUILayout.Width(22f) }))
                {
                    if (EditorUtility.DisplayDialog("Remove Module",
                            $"Remove '{data.ModuleAsset.ModuleName}' from the registry?", "Yes", "No"))
                    {
                        RemoveModuleFromRegistry(data.ListIndex);
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        return;
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            // ── Row body (expanded) ──
            if (data.IsExpanded)
            {
                bool active = data.IsCoreModule || (data.EnabledProp != null && data.EnabledProp.boolValue);
                if (active)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.textArea);
                    DrawModuleProperties(data);
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    GUILayout.Space(2);
                    EditorGUILayout.HelpBox("Module is disabled — will not run at startup.", MessageType.Warning);
                }
            }

            EditorGUILayout.EndVertical();
        }

        // ── Property renderer ────────────────────────────────────────────────────────
        private void DrawModuleProperties(ModuleEditorData data)
        {
            data.SerializedObject.Update();
            SerializedProperty iterator = data.SerializedObject.GetIterator();
            bool enterChildren = true;
            bool hasProps = false;

            using (new EditorGUI.DisabledScope(true))
            {
                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (iterator.name == "m_Script" || iterator.name == "moduleEnabled")
                        continue;

                    if (!hasProps) { GUILayout.Space(4); hasProps = true; }

                    EditorGUILayout.BeginHorizontal();
                    string niceName = ObjectNames.NicifyVariableName(iterator.name);
                    EditorGUILayout.LabelField(new GUIContent(niceName, iterator.tooltip),
                        EditorStyleTemplate.GrayMiniLabelStyle, GUILayout.Width(140f));
                    EditorGUILayout.PropertyField(iterator, GUIContent.none, true);
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        // ── Cache ────────────────────────────────────────────────────────────────────
        private void RefreshModuleCache()
        {
            var currentGuids = new HashSet<string>();
            for (int i = 0; i < modulesProp.arraySize; i++)
            {
                var obj = modulesProp.GetArrayElementAtIndex(i).objectReferenceValue as MobileModule;
                if (obj == null) continue;
                string g = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
                if (!string.IsNullOrEmpty(g)) currentGuids.Add(g);
            }

            // Evict stale
            var stale = moduleEditorCache.Keys.Where(k => !currentGuids.Contains(k)).ToList();
            foreach (var k in stale) moduleEditorCache.Remove(k);

            allModulesList.Clear();

            for (int i = 0; i < modulesProp.arraySize; i++)
            {
                var module = modulesProp.GetArrayElementAtIndex(i).objectReferenceValue as MobileModule;
                if (module == null) continue;

                string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(module));

                if (!moduleEditorCache.TryGetValue(guid, out ModuleEditorData data))
                {
                    var so = new SerializedObject(module);
                    data = new ModuleEditorData
                    {
                        ModuleAsset    = module,
                        SerializedObject = so,
                        EnabledProp    = so.FindProperty("moduleEnabled"),
                        IsCoreModule   = module is SystemModuleConfig,
                        ListIndex      = i
                    };
                    moduleEditorCache[guid] = data;
                }
                else
                {
                    data.ListIndex   = i;
                    data.ModuleAsset = module;
                    data.IsCoreModule = module is SystemModuleConfig;
                }

                allModulesList.Add(data);
            }
        }

        // ── Add Module Menu ──────────────────────────────────────────────────────────
        private void ShowAddModuleMenu()
        {
            GenericMenu menu = new GenericMenu();
            var types = FindAllModuleTypes();

            foreach (var type in types)
            {
                string displayName   = GetModuleNameFromType(type);
                bool   alreadyAdded  = HasModuleOfType(type);

                if (alreadyAdded)
                    menu.AddDisabledItem(new GUIContent($"{displayName}  (Already Added)"));
                else
                    menu.AddItem(new GUIContent(displayName), false, () => AddModuleToRegistry(type));
            }

            if (types.Count == 0)
                menu.AddDisabledItem(new GUIContent("No MobileModule types found"));

            menu.ShowAsContext();
        }

        private void AddModuleToRegistry(System.Type moduleType)
        {
            serializedObject.Update();

            MobileModule moduleAsset = FindExistingAssetOfType(moduleType);

            if (moduleAsset == null)
            {
                string configPath = AssetDatabase.GetAssetPath(target);
                if (string.IsNullOrEmpty(configPath))
                {
                    EditorUtility.DisplayDialog("Error", "Save the MobileCoreConfig asset first.", "OK");
                    return;
                }

                string dir      = Path.GetDirectoryName(configPath);
                string filePath = AssetDatabase.GenerateUniqueAssetPath(
                    Path.Combine(dir, $"{moduleType.Name}.asset").Replace("\\", "/"));

                try
                {
                    moduleAsset = (MobileModule)ScriptableObject.CreateInstance(moduleType);
                    AssetDatabase.CreateAsset(moduleAsset, filePath);

                    var m = moduleAsset.GetType().GetMethod("CreateEmbeddedSettings");
                    m?.Invoke(moduleAsset, null);

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[MobileCoreConfig] Failed to create module asset: {e}");
                    return;
                }
            }

            modulesProp.arraySize++;
            modulesProp.GetArrayElementAtIndex(modulesProp.arraySize - 1).objectReferenceValue = moduleAsset;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            RefreshModuleCache();
        }

        // ── Helpers ──────────────────────────────────────────────────────────────────
        private List<System.Type> FindAllModuleTypes()
        {
            var result = new List<System.Type>();
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try { result.AddRange(asm.GetTypes().Where(t => t.IsSubclassOf(typeof(MobileModule)) && !t.IsAbstract)); }
                catch { /* skip */ }
            }
            result.Sort((a, b) => string.Compare(a.Name, b.Name, System.StringComparison.Ordinal));
            return result;
        }

        private bool HasModuleOfType(System.Type type)
        {
            for (int i = 0; i < modulesProp.arraySize; i++)
            {
                var obj = modulesProp.GetArrayElementAtIndex(i).objectReferenceValue;
                if (obj != null && obj.GetType() == type) return true;
            }
            return false;
        }

        private string GetModuleNameFromType(System.Type type)
        {
            var temp = ScriptableObject.CreateInstance(type) as MobileModule;
            string name = temp != null ? temp.ModuleName : type.Name;
            if (temp != null) DestroyImmediate(temp);
            return name;
        }

        private MobileModule FindExistingAssetOfType(System.Type type)
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:{type.Name}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath(path, type) as MobileModule;
                if (asset != null) return asset;
            }
            return null;
        }

        private void RemoveModuleFromRegistry(int index)
        {
            serializedObject.Update();
            if (index >= 0 && index < modulesProp.arraySize)
            {
                modulesProp.GetArrayElementAtIndex(index).objectReferenceValue = null;
                modulesProp.DeleteArrayElementAtIndex(index);
                // Unity needs a second delete if the slot becomes null after the first
                if (index < modulesProp.arraySize &&
                    modulesProp.GetArrayElementAtIndex(index).objectReferenceValue == null)
                    modulesProp.DeleteArrayElementAtIndex(index);
            }
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            RefreshModuleCache();
        }

        private void CleanNullModules()
        {
            bool changed = false;
            serializedObject.Update();
            for (int i = modulesProp.arraySize - 1; i >= 0; i--)
            {
                if (modulesProp.GetArrayElementAtIndex(i).objectReferenceValue == null)
                {
                    modulesProp.DeleteArrayElementAtIndex(i);
                    if (i < modulesProp.arraySize &&
                        modulesProp.GetArrayElementAtIndex(i).objectReferenceValue == null)
                        modulesProp.DeleteArrayElementAtIndex(i);
                    changed = true;
                }
            }
            if (changed) { serializedObject.ApplyModifiedProperties(); EditorUtility.SetDirty(target); }
        }
    }
}
#endif
