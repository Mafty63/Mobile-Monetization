#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MobileCore.MainModule;

namespace MobileCore.MainModule.Editor
{
    [CustomEditor(typeof(MobileCoreConfig))]
    public class MobileCoreConfigEditor : UnityEditor.Editor
    {
        private SerializedProperty modulesProp;

        private void OnEnable()
        {
            modulesProp = serializedObject.FindProperty("modules");
        }

        public override void OnInspectorGUI()
        {
            // Auto clean any destroyed/null module assets from the list before drawing
            CleanNullModules();

            serializedObject.Update();

            DrawHeader();
            GUILayout.Space(8);

            DrawModuleList();

            GUILayout.Space(8);
            DrawAddModuleHint();

            serializedObject.ApplyModifiedProperties();
        }

        // ── Header ──────────────────────────────────────────────────────────────────
        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("MOBILE CORE CONFIGURATION", EditorStyleTemplate.GrayBoldLabelStyle);
            EditorGUILayout.HelpBox(
                "Register all active MobileModule configuration assets below.\n" +
                "At startup, the system initializes these modules sequentially in the order listed.",
                MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        // ── Module List ──────────────────────────────────────────────────────────────
        private void DrawModuleList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Header Row with Title + Clean-up option if there are empty slots
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("MODULE REGISTRY", EditorStyleTemplate.GrayBoldLabelStyle);
            
            if (HasEmptySlots())
            {
                if (GUILayout.Button("Clean Empty Slots", EditorStyles.miniButton, GUILayout.Width(120f)))
                {
                    RemoveEmptySlots();
                }
            }
            EditorGUILayout.EndHorizontal();

            var divRect = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
            bool dark = EditorGUIUtility.isProSkin;
            EditorGUI.DrawRect(divRect, dark ? new Color(0.32f, 0.32f, 0.35f) : new Color(0.68f, 0.68f, 0.70f));
            GUILayout.Space(8f);

            if (modulesProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No modules registered. Click '+ Add Module' below to choose a module type. The system will auto-generate the configuration and settings for you.", MessageType.Warning);
            }
            else
            {
                for (int i = 0; i < modulesProp.arraySize; i++)
                {
                    DrawModuleEntry(i);
                    if (i < modulesProp.arraySize - 1)
                        GUILayout.Space(6f);
                }
            }

            GUILayout.Space(10f);

            // Add Module Button
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ Add Module",
                EditorStyleTemplate.CreateButtonStyle(new Color(0.20f, 0.55f, 0.28f), null, 24),
                GUILayout.Height(24f), GUILayout.Width(160f)))
            {
                ShowAddModuleMenu();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4f);
            EditorGUILayout.EndVertical();
        }

        private void DrawModuleEntry(int index)
        {
            SerializedProperty element = modulesProp.GetArrayElementAtIndex(index);
            MobileModule module = element.objectReferenceValue as MobileModule;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Top row: index label + module name + remove button
            EditorGUILayout.BeginHorizontal();

            string labelText = module != null ? $"[{index}]  {module.ModuleName}" : $"[{index}]  (Empty Slot)";
            EditorGUILayout.LabelField(labelText, EditorStyleTemplate.GrayTextStyle, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("✕", EditorStyleTemplate.GrayButtonStyle, GUILayout.Width(22f), GUILayout.Height(18f)))
            {
                element.objectReferenceValue = null;
                modulesProp.DeleteArrayElementAtIndex(index);
                // For safety on Unity's array serialization behavior
                if (index < modulesProp.arraySize && modulesProp.GetArrayElementAtIndex(index).objectReferenceValue == null)
                {
                    modulesProp.DeleteArrayElementAtIndex(index);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(2f);

            // Object field is made READ-ONLY (non-editable) to prevent incorrect manual drag/drop and bypasses
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(element.objectReferenceValue, typeof(MobileModule), false);
            EditorGUI.EndDisabledGroup();

            // Quick-select and navigation button
            if (module != null)
            {
                GUILayout.Space(4f);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Open Module Config",
                    EditorStyleTemplate.CreateButtonStyle(new Color(0.20f, 0.47f, 0.82f), null, 20),
                    GUILayout.Height(20f), GUILayout.Width(150f)))
                {
                    Selection.activeObject = module;
                    EditorUtility.FocusProjectWindow();
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        // ── Help Hint Box ───────────────────────────────────────────────────────────
        private void DrawAddModuleHint()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("HOW TO CREATE NEW MODULE TYPES", EditorStyleTemplate.GrayBoldLabelStyle);
            GUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "1. Create a C# script extending MobileModule.\n" +
                "2. Override ModuleName and implement Initialize(GameObject parent).\n" +
                "3. Decorate the class with [CreateAssetMenu].\n" +
                "4. Clicking '+ Add Module' will automatically detect your class and let you generate it directly.",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }

        // ── Dynamic Module Creation Menu ────────────────────────────────────────────
        private void ShowAddModuleMenu()
        {
            GenericMenu menu = new GenericMenu();
            List<System.Type> moduleTypes = FindAllModuleTypes();

            foreach (var type in moduleTypes)
            {
                string displayName = GetModuleNameFromType(type);
                bool alreadyRegistered = HasModuleOfType(type);

                if (alreadyRegistered)
                {
                    menu.AddDisabledItem(new GUIContent($"{displayName} (Already Registered)"));
                }
                else
                {
                    menu.AddItem(new GUIContent(displayName), false, () => AddModuleToRegistry(type));
                }
            }

            if (moduleTypes.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No MobileModule classes found in compilation assemblies"));
            }

            menu.ShowAsContext();
        }

        private void AddModuleToRegistry(System.Type moduleType)
        {
            serializedObject.Update();

            // 1. Search if an asset file of this type already exists in the project
            MobileModule moduleAsset = FindExistingAssetOfType(moduleType);

            // 2. If it doesn't exist, create a fresh ScriptableObject asset in the same folder as MobileCoreConfig
            if (moduleAsset == null)
            {
                string configAssetPath = AssetDatabase.GetAssetPath(target);
                if (string.IsNullOrEmpty(configAssetPath))
                {
                    EditorUtility.DisplayDialog("Error", "Please save the MobileCoreConfig asset first before creating modules.", "OK");
                    return;
                }

                string targetDirectory = Path.GetDirectoryName(configAssetPath); // Simpan di folder yang sama (Plugin Settings)

                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                string filename = $"{moduleType.Name}.asset";
                string fullPath = Path.Combine(targetDirectory, filename).Replace("\\", "/");
                string uniquePath = AssetDatabase.GenerateUniqueAssetPath(fullPath);

                try
                {
                    moduleAsset = ScriptableObject.CreateInstance(moduleType) as MobileModule;
                    AssetDatabase.CreateAsset(moduleAsset, uniquePath);
                    
                    // Auto-generate embedded settings immediately so the settings are populated instantly
                    var method = moduleAsset.GetType().GetMethod("CreateEmbeddedSettings");
                    if (method != null)
                    {
                        method.Invoke(moduleAsset, null);
                    }

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to generate scriptable asset for module type '{moduleType.Name}': {e}");
                    return;
                }
            }

            // 3. Register the asset in the config list
            int targetIndex = -1;
            for (int i = 0; i < modulesProp.arraySize; i++)
            {
                if (modulesProp.GetArrayElementAtIndex(i).objectReferenceValue == null)
                {
                    targetIndex = i;
                    break;
                }
            }

            if (targetIndex == -1)
            {
                modulesProp.arraySize++;
                targetIndex = modulesProp.arraySize - 1;
            }

            modulesProp.GetArrayElementAtIndex(targetIndex).objectReferenceValue = moduleAsset;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        // ── Helper Logic ─────────────────────────────────────────────────────────────
        private List<System.Type> FindAllModuleTypes()
        {
            var types = new List<System.Type>();
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    var assemblyTypes = assembly.GetTypes()
                        .Where(t => t.IsSubclassOf(typeof(MobileModule)) && !t.IsAbstract);
                    types.AddRange(assemblyTypes);
                }
                catch { /* Ignore dynamic assemblies */ }
            }
            types.Sort((a, b) => string.Compare(a.Name, b.Name, System.StringComparison.Ordinal));
            return types;
        }

        private bool HasModuleOfType(System.Type type)
        {
            for (int i = 0; i < modulesProp.arraySize; i++)
            {
                var obj = modulesProp.GetArrayElementAtIndex(i).objectReferenceValue;
                if (obj != null && obj.GetType() == type)
                    return true;
            }
            return false;
        }

        private string GetModuleNameFromType(System.Type type)
        {
            var temp = ScriptableObject.CreateInstance(type) as MobileModule;
            string name = temp != null ? temp.ModuleName : type.Name;
            if (temp != null)
            {
                DestroyImmediate(temp);
            }
            return name;
        }

        private MobileModule FindExistingAssetOfType(System.Type type)
        {
            string[] guids = AssetDatabase.FindAssets($"t:{type.Name}");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath(path, type) as MobileModule;
                if (asset != null)
                    return asset;
            }
            return null;
        }

        private bool HasEmptySlots()
        {
            for (int i = 0; i < modulesProp.arraySize; i++)
            {
                if (modulesProp.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    return true;
            }
            return false;
        }

        private void RemoveEmptySlots()
        {
            serializedObject.Update();
            for (int i = modulesProp.arraySize - 1; i >= 0; i--)
            {
                if (modulesProp.GetArrayElementAtIndex(i).objectReferenceValue == null)
                {
                    modulesProp.DeleteArrayElementAtIndex(i);
                    if (i < modulesProp.arraySize && modulesProp.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    {
                        modulesProp.DeleteArrayElementAtIndex(i);
                    }
                }
            }
            serializedObject.ApplyModifiedProperties();
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
                    if (i < modulesProp.arraySize && modulesProp.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    {
                        modulesProp.DeleteArrayElementAtIndex(i);
                    }
                    changed = true;
                }
            }
            if (changed)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
        }
    }
}
#endif
