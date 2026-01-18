#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MobileCore.MainModule;
using MobileCore.SystemModule;
using System;

namespace MobileCore.MainModule.Editor
{
    [CustomEditor(typeof(MainSystemSettings))]
    public class MainSystemSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty _coreModuleProperty;
        private SerializedProperty _modulesProperty;
        private SerializedObject _cachedSystemSerializedObject;
        private SerializedProperty _cachedSystemCanvasProp;
        private bool _hasAttemptedAutoCreate = false;

        private bool _showCoreModule = true;
        private bool _showModules = true;

        // Dynamic Module Cache
        private class ModuleEditorData
        {
            public BaseManagerInitializer Initializer;
            public SerializedObject SerializedObject;
            public SerializedProperty IsEnabledProp;
            public SerializedProperty SettingsProp;
            public bool IsExpanded = true;
            public string DisplayName;
            public string Description;
        }

        private Dictionary<int, ModuleEditorData> _moduleEditorCache = new Dictionary<int, ModuleEditorData>();
        private List<ModuleEditorData> _currentModulesList = new List<ModuleEditorData>();

        // Layout Constants
        private const float OPEN_BUTTON_WIDTH = 60f;
        private const float TOGGLE_BUTTON_WIDTH = 80f;
        private const float CONTROLS_SECTION_WIDTH = 150f;

        private void OnEnable()
        {
            _coreModuleProperty = serializedObject.FindProperty("coreModule");
            _modulesProperty = serializedObject.FindProperty("modules");
            _hasAttemptedAutoCreate = false;
            
            CacheSystemModuleReferences();
            RefreshModuleCache();
        }

        private void OnDisable()
        {
            _moduleEditorCache.Clear();
            _currentModulesList.Clear();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            CacheSystemModuleReferences();
            RefreshModuleCache();

            string assetPath = AssetDatabase.GetAssetPath(target);
            bool isAssetSaved = !string.IsNullOrEmpty(assetPath);

            // Auto-create Core Module if needed
            if (isAssetSaved && _coreModuleProperty.objectReferenceValue == null && !_hasAttemptedAutoCreate)
            {
                // Defer to next frame to avoid GUI layout errors
                EditorApplication.delayCall += () =>
                {
                    if (target == null) return;
                    _hasAttemptedAutoCreate = true;
                    // PopulateModulesArray(assetPath); // No longer relying on sub-assets scanning
                    Repaint();
                };
            }

            // Draw Inspector
            DrawHeaderInfo();
            EditorGUILayout.Space();

            DrawCoreModuleSection();
            EditorGUILayout.Space();

            DrawModulesSection();
            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();

            // Apply properties for all cached module objects
            if (_cachedSystemSerializedObject != null && _cachedSystemSerializedObject.hasModifiedProperties)
            {
                _cachedSystemSerializedObject.ApplyModifiedProperties();
            }

            foreach (var modData in _currentModulesList)
            {
                if (modData.SerializedObject != null && modData.SerializedObject.hasModifiedProperties)
                {
                    modData.SerializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void RefreshModuleCache()
        {
            _currentModulesList.Clear();
            HashSet<int> currentIds = new HashSet<int>();

            if (_modulesProperty != null && _modulesProperty.isArray)
            {
                for (int i = 0; i < _modulesProperty.arraySize; i++)
                {
                    var prop = _modulesProperty.GetArrayElementAtIndex(i);
                    var moduleObj = prop.objectReferenceValue as BaseManagerInitializer;

                    if (moduleObj == null) continue;

                    int id = moduleObj.GetInstanceID();
                    currentIds.Add(id);

                    if (!_moduleEditorCache.TryGetValue(id, out ModuleEditorData data))
                    {
                        // Create new cache entry
                        data = new ModuleEditorData
                        {
                            Initializer = moduleObj,
                            SerializedObject = new SerializedObject(moduleObj),
                            IsExpanded = true
                        };
                        
                        data.IsEnabledProp = data.SerializedObject.FindProperty("isEnabled");
                        data.SettingsProp = data.SerializedObject.FindProperty("Settings"); // Convention: field name "Settings"
                        
                        // Get Display Name and Description
                         var moduleAttr = moduleObj.GetType().GetCustomAttribute<ModuleAttribute>();
                        if (moduleAttr != null)
                        {
                            data.DisplayName = moduleAttr.DisplayName;
                            data.Description = moduleAttr.Description;
                        }
                        else
                        {
                            data.DisplayName = moduleObj.GetType().Name;
                            data.Description = "No description available.";
                        }

                        _moduleEditorCache[id] = data;
                    }
                    else
                    {
                        // Update existing entry if needed (e.g. if object was recreated? usually instanceID is consistent)
                        if (data.SerializedObject.targetObject == null) // Object destroyed or lost
                        {
                            data.SerializedObject = new SerializedObject(moduleObj);
                            data.IsEnabledProp = data.SerializedObject.FindProperty("isEnabled");
                            data.SettingsProp = data.SerializedObject.FindProperty("Settings");
                        }
                        else
                        {
                            data.SerializedObject.Update();
                        }
                    }

                    _currentModulesList.Add(data);
                }
            }

            // Cleanup dictionary
            var idsToRemove = _moduleEditorCache.Keys.Where(k => !currentIds.Contains(k)).ToList();
            foreach (var id in idsToRemove)
            {
                _moduleEditorCache.Remove(id);
            }
        }
        
        private void CacheSystemModuleReferences()
        {
            if (_coreModuleProperty != null && _coreModuleProperty.objectReferenceValue != null)
            {
                var systemModule = _coreModuleProperty.objectReferenceValue;
                if (_cachedSystemSerializedObject == null || _cachedSystemSerializedObject.targetObject != systemModule)
                {
                    _cachedSystemSerializedObject = new SerializedObject(systemModule);
                    _cachedSystemCanvasProp = _cachedSystemSerializedObject.FindProperty("SystemCanvas");
                }
                else
                {
                    _cachedSystemSerializedObject.Update();
                }
            }
            else
            {
                _cachedSystemSerializedObject = null;
                _cachedSystemCanvasProp = null;
            }
        }
        
        // PopulateModulesArray removed as we no longer scan sub-assets. 
        // Modules are strictly defined by the list in the SO.


        #region Drawing Methods

        private void DrawHeaderInfo()
        {
             EditorGUILayout.BeginVertical(EditorStyles.helpBox);
             EditorGUILayout.BeginHorizontal();
             EditorGUILayout.LabelField("PROJECT INITIALIZATION SETTINGS", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
             EditorGUILayout.EndHorizontal();
             EditorGUILayout.HelpBox("Manage all initialization modules for your project. Core module is always enabled, feature modules can be toggled.", MessageType.Info);
             EditorGUILayout.EndVertical();
        }

        private void DrawCoreModuleSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            _showCoreModule = EditorGUILayout.Foldout(_showCoreModule, "CORE MODULE", true, EditorStyleTemplate.GrayFoldoutHeaderStyle);
            EditorGUILayout.EndHorizontal();

            if (_showCoreModule && _coreModuleProperty != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.textArea);

                UnityEngine.Object coreObj = _coreModuleProperty.objectReferenceValue as UnityEngine.Object;

                if (coreObj != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    // Status Checkmark
                    var statusRect = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20));
                    EditorGUI.DrawRect(statusRect, new Color(0.1f, 0.5f, 0.9f, 0.8f));
                    var statusStyle = new GUIStyle(EditorStyles.whiteMiniLabel) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                    GUI.Label(statusRect, "✓", statusStyle);

                    // Module Info
                    EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                    EditorGUILayout.LabelField(coreObj.name, EditorStyleTemplate.GrayBoldLabelStyle);
                    
                    string description = "Core system required for application functionality";
                    if (coreObj is BaseManagerInitializer coreModule && !string.IsNullOrEmpty(coreModule.ModuleDescription))
                    {
                        description = coreModule.ModuleDescription;
                    }
                    EditorGUILayout.LabelField(description, EditorStyles.wordWrappedMiniLabel);
                    EditorGUILayout.EndVertical();

                    // Controls
                    EditorGUILayout.BeginVertical(GUILayout.Width(CONTROLS_SECTION_WIDTH));
                    GUILayout.FlexibleSpace();
                    var alwaysOnStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.1f, 0.5f, 0.9f) } };
                    EditorGUILayout.LabelField("ALWAYS ON", alwaysOnStyle, GUILayout.Height(20), GUILayout.Width(TOGGLE_BUTTON_WIDTH));
                    GUILayout.Space(4);
                    if (GUILayout.Button("Open", GetOpenButtonStyle(), GUILayout.Height(20), GUILayout.Width(OPEN_BUTTON_WIDTH)))
                    {
                        Selection.activeObject = coreObj;
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();

                    // System Canvas Property
                     if (_cachedSystemSerializedObject != null)
                    {
                        EditorGUILayout.Space(5);
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(new GUIContent("System Canvas", "Canvas used for system UI"), EditorStyleTemplate.GrayMiniLabelStyle, GUILayout.Width(120));
                        if (_cachedSystemCanvasProp != null)
                        {
                            EditorGUILayout.PropertyField(_cachedSystemCanvasProp, GUIContent.none);
                        }
                        else
                        {
                             EditorGUILayout.LabelField("Property 'systemCanvas' not found", EditorStyles.miniLabel);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.HelpBox("Core Module is missing. Please assign a Core System Module.", MessageType.Error);
                    if (GUILayout.Button("GENERATE CORE", GetGenerateButtonStyle(), GUILayout.Height(38), GUILayout.Width(120)))
                    {
                        CreateCoreModule();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawModulesSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            _showModules = EditorGUILayout.Foldout(_showModules, "MODULES", true, EditorStyleTemplate.GrayFoldoutHeaderStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add Module", GetAddModuleButtonStyle(), GUILayout.Height(20), GUILayout.Width(100)))
            {
                ShowAddModuleMenu();
            }
            EditorGUILayout.EndHorizontal();

            if (_showModules)
            {
                EditorGUILayout.Space();
                
                if (_currentModulesList.Count > 0)
                {
                    // Sort modules by order if available in attribute, else by name? 
                    // For now, just keep list order as it is editable in inspector list
                    
                    foreach (var moduleData in _currentModulesList)
                    {
                        DrawModuleConfiguration(moduleData);
                        EditorGUILayout.Space();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No modules added yet. Click 'Add Module' button to add modules.", MessageType.Info);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawModuleConfiguration(ModuleEditorData data)
        {
            EditorGUILayout.BeginVertical(EditorStyles.textArea);
            EditorGUILayout.BeginHorizontal();

            // Header
            string headerTitle = data.DisplayName.ToUpper() + " CONFIGURATION";
            data.IsExpanded = EditorGUILayout.Foldout(data.IsExpanded, headerTitle, true, EditorStyleTemplate.GrayFoldoutHeaderStyle);
            GUILayout.FlexibleSpace();

            // Enable/Disable Toggle
            bool isEnabled = true;
            if (data.IsEnabledProp != null)
            {
                isEnabled = data.IsEnabledProp.boolValue;
                if (GUILayout.Button(isEnabled ? "DISABLE" : "ENABLE", GetToggleStyle(isEnabled), GUILayout.Height(20), GUILayout.Width(TOGGLE_BUTTON_WIDTH)))
                {
                    data.IsEnabledProp.boolValue = !isEnabled;
                    // SerializedObject applied at end of inspector
                }
            }

            GUILayout.Space(4);

            // Open Button
            if (GUILayout.Button("Open", GetOpenButtonStyle(), GUILayout.Height(20), GUILayout.Width(OPEN_BUTTON_WIDTH)))
            {
                // Prioritize opening Settings if available, else Module
                if (data.SettingsProp != null && data.SettingsProp.objectReferenceValue != null)
                {
                    Selection.activeObject = data.SettingsProp.objectReferenceValue;
                }
                else
                {
                    Selection.activeObject = data.Initializer;
                }
            }

            EditorGUILayout.EndHorizontal();

            // Body
            if (data.IsExpanded && isEnabled)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.textArea);

                // Module Description (Optional)
                if (!string.IsNullOrEmpty(data.Description))
                {
                    // EditorGUILayout.HelpBox(data.Description, MessageType.Info);
                }
                
                // Settings Property
                if (data.SettingsProp != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Settings", $"{data.DisplayName} Settings Asset"), EditorStyleTemplate.GrayMiniLabelStyle, GUILayout.Width(120));
                    
                    EditorGUI.BeginDisabledGroup(true); // Always read-only in this view as per original design
                    EditorGUILayout.ObjectField(data.SettingsProp.objectReferenceValue, typeof(ScriptableObject), false);
                    EditorGUI.EndDisabledGroup();
                    
                    EditorGUILayout.EndHorizontal();

                    // Missing Settings Warning & Generation
                    if (data.SettingsProp.objectReferenceValue == null)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.HelpBox($"Settings asset is missing for {data.DisplayName}.", MessageType.Error);
                        
                        // Try to find the type of the Settings field
                        Type settingsType = GetSettingsType(data.Initializer);
                        if (settingsType != null && typeof(ScriptableObject).IsAssignableFrom(settingsType))
                        {
                            string assetPath = AssetDatabase.GetAssetPath(data.Initializer);
                            if (!string.IsNullOrEmpty(assetPath))
                            {
                                if (GUILayout.Button($"GENERATE {settingsType.Name.ToUpper()}", GetGenerateButtonStyle(), GUILayout.Height(25)))
                                {
                                    CreateSettings(data, settingsType);
                                }
                            }
                             else
                            {
                                EditorGUILayout.HelpBox("Save the Module asset first before creating Settings.", MessageType.Warning);
                            }
                        }
                        EditorGUILayout.EndVertical();
                    }
                    EditorGUILayout.Space(2);
                }

                // Draw Other Properties (Dynamic)
                SerializedProperty iterator = data.SerializedObject.GetIterator();
                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false; // Only look at immediate children of the ScriptableObject
                    if (iterator.name == "m_Script" || iterator.name == "isEnabled") continue;
                    
                    // If it is the Settings property, we already drew it above
                    if (data.SettingsProp != null && iterator.name == data.SettingsProp.name) continue;

                    // Draw Property
                     EditorGUILayout.BeginHorizontal();
                     EditorGUILayout.LabelField(new GUIContent(ObjectNames.NicifyVariableName(iterator.name)), EditorStyleTemplate.GrayMiniLabelStyle, GUILayout.Width(120));
                     EditorGUILayout.PropertyField(iterator, GUIContent.none);
                     EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }
            else if (data.IsExpanded && !isEnabled)
            {
                 EditorGUILayout.Space();
                 EditorGUILayout.HelpBox($"{data.DisplayName} is currently disabled.", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Helper Methods

        private void ShowAddModuleMenu()
        {
            GenericMenu menu = new GenericMenu();
            var moduleTypes = FindAllModuleTypes();

            foreach (var type in moduleTypes)
            {
                if (HasModuleOfType(type)) continue;

                string name = type.Name;
                var attr = type.GetCustomAttribute<ModuleAttribute>();
                if (attr != null && !string.IsNullOrEmpty(attr.DisplayName))
                    name = attr.DisplayName;

                menu.AddItem(new GUIContent(name), false, () => AddModule(type));
            }

            if (menu.GetItemCount() == 0)
            {
                menu.AddDisabledItem(new GUIContent("No available modules to add"));
            }

            menu.ShowAsContext();
        }

        private List<Type> FindAllModuleTypes()
        {
            var types = new List<Type>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try 
                {
                    var assemblyTypes = assembly.GetTypes()
                        .Where(t => t.IsSubclassOf(typeof(BaseManagerInitializer)) && !t.IsAbstract && t != typeof(SystemModuleInitializer));
                    types.AddRange(assemblyTypes);
                }
                catch { /* Ignore dynamic assemblies */ }
            }
            return types.OrderBy(t => t.Name).ToList();
        }

        private bool HasModuleOfType(Type type)
        {
            if (_modulesProperty == null) return false;
            for (int i = 0; i < _modulesProperty.arraySize; i++)
            {
                var obj = _modulesProperty.GetArrayElementAtIndex(i).objectReferenceValue;
                if (obj != null && obj.GetType() == type) return true;
            }
            return false;
        }

        private void AddModule(Type moduleType)
        {
             string assetPath = AssetDatabase.GetAssetPath(target);
             if (string.IsNullOrEmpty(assetPath))
             {
                 EditorUtility.DisplayDialog("Error", "Please save the MainSystemSettings asset first.", "OK");
                 return;
             }
             
             string dirPath = System.IO.Path.GetDirectoryName(assetPath);

             try
             {
                 var newModule = CreateInstance(moduleType) as BaseManagerInitializer;
                 
                  // Attempt to set nice name
                 var attr = moduleType.GetCustomAttribute<ModuleAttribute>();
                 string baseName = moduleType.Name;
                 if (attr != null && !string.IsNullOrEmpty(attr.DisplayName))
                    baseName = attr.DisplayName.Replace(" ", ""); // Remove spaces for filename
                 
                 string filename = $"{baseName}.asset";
                 string uniquePath = AssetDatabase.GenerateUniqueAssetPath(System.IO.Path.Combine(dirPath, filename));

                 AssetDatabase.CreateAsset(newModule, uniquePath);
                 
                 int index = _modulesProperty.arraySize;
                 _modulesProperty.arraySize++;
                 _modulesProperty.GetArrayElementAtIndex(index).objectReferenceValue = newModule;
                 
                 serializedObject.ApplyModifiedProperties();
                 AssetDatabase.SaveAssets();
                 EditorUtility.SetDirty(target);
                 
                 RefreshModuleCache();
             }
             catch (Exception e)
             {
                 Debug.LogError($"Failed to add module: {e}");
             }
        }

        private Type GetSettingsType(BaseManagerInitializer initializer)
        {
            if (initializer == null) return null;
            var settingsField = initializer.GetType().GetField("Settings");
            return settingsField?.FieldType;
        }

        private void CreateSettings(ModuleEditorData data, Type settingsType)
        {
            try
            {
                string modulePath = AssetDatabase.GetAssetPath(data.Initializer);
                if (string.IsNullOrEmpty(modulePath))
                {
                    Debug.LogError("Module asset path not found.");
                    return;
                }
                string dirPath = System.IO.Path.GetDirectoryName(modulePath);

                var newSettings = ScriptableObject.CreateInstance(settingsType);
                string name = settingsType.Name;
                
                string filename = $"{name}.asset";
                string uniquePath = AssetDatabase.GenerateUniqueAssetPath(System.IO.Path.Combine(dirPath, filename));

                AssetDatabase.CreateAsset(newSettings, uniquePath);
                
                data.SettingsProp.objectReferenceValue = newSettings;
                data.SerializedObject.ApplyModifiedProperties();
                
                AssetDatabase.SaveAssets();
                EditorUtility.SetDirty(data.Initializer);
                
                Debug.Log($"Created {name} for {data.DisplayName} at {uniquePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to generate settings: {e}");
            }
        }

        private void CreateCoreModule()
        {
             string assetPath = AssetDatabase.GetAssetPath(target);
             if (string.IsNullOrEmpty(assetPath)) return;

             string dirPath = System.IO.Path.GetDirectoryName(assetPath);
             
             // Core Module is SystemModuleInitializer
             var moduleType = typeof(SystemModuleInitializer);
             
             try
             {
                 var newModule = CreateInstance(moduleType) as BaseManagerInitializer;
                 
                  // Attempt to set nice name
                 var attr = moduleType.GetCustomAttribute<ModuleAttribute>();
                 string baseName = moduleType.Name;
                 if (attr != null && !string.IsNullOrEmpty(attr.DisplayName))
                    baseName = attr.DisplayName.Replace(" ", "");
                 
                 string filename = $"{baseName}.asset";
                 string uniquePath = AssetDatabase.GenerateUniqueAssetPath(System.IO.Path.Combine(dirPath, filename));

                 AssetDatabase.CreateAsset(newModule, uniquePath);
                 
                 _coreModuleProperty.objectReferenceValue = newModule;
                 
                 serializedObject.ApplyModifiedProperties();
                 AssetDatabase.SaveAssets();
                 EditorUtility.SetDirty(target);
                 
                 RefreshModuleCache();
                 
                 Debug.Log($"Created Core Module at {uniquePath}");
             }
             catch (Exception e)
             {
                 Debug.LogError($"Failed to generate Core Module: {e}");
             }
        }

        #endregion

        #region Styles

        private GUIStyle GetToggleStyle(bool isEnabled)
        {
             return EditorStyleTemplate.CreateToggleButtonStyle(isEnabled);
        }

        private GUIStyle GetOpenButtonStyle()
        {
             return EditorStyleTemplate.CreateButtonStyle(new Color(0.3f, 0.3f, 0.3f), null, 20);
        }
        
        private GUIStyle GetAddModuleButtonStyle()
        {
            return EditorStyleTemplate.CreateButtonStyle(new Color(0.2f, 0.6f, 0.2f), null, 20);
        }
        
        private GUIStyle GetGenerateButtonStyle()
        {
             return EditorStyleTemplate.CreateButtonStyle(new Color(0.8f, 0.4f, 0.1f), null, 20);
        }

        #endregion
    }
}
#endif