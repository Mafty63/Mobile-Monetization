#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Reflection;

/// <summary>
/// Manager for handling define symbols based on DefineAttribute declared in Manager classes.
/// </summary>
public static class SDKDefinesManager
{
    static SDKDefinesManager()
    {
    }

    /// <summary>
    /// Scans all classes with DefineAttribute and updates define symbols based on type availability.
    /// </summary>
    [MenuItem("Tools/MobileCore/Refresh All Define Symbols")]
    public static void RefreshAllDefines()
    {
        Debug.Log("=== Scanning Define Attributes ===");

        var defineConfigs = ScanDefineAttributes();

        if (defineConfigs.Count == 0)
        {
            Debug.LogWarning("No Define attributes found. Make sure you have added [Define] attributes to your Manager classes.");
            return;
        }

        Debug.Log($"Found {defineConfigs.Count} define declarations");

        foreach (var config in defineConfigs)
        {
            CheckAndUpdateDefine(config);
        }

        Debug.Log("=== Define Symbols Refresh Complete ===");
        EditorUtility.DisplayDialog("Define Symbols Updated",
            $"Processed {defineConfigs.Count} define declarations.\nCheck Console for details.",
            "OK");
    }

    private static List<DefineConfig> ScanDefineAttributes()
    {
        var configs = new List<DefineConfig>();
        var defineAttributeType = Type.GetType("MobileCore.DefineSystem.DefineAttribute");

        if (defineAttributeType == null)
        {
            Debug.LogError("DefineAttribute type not found! Make sure DefineAttribute.cs is compiled.");
            return configs;
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    var attributes = type.GetCustomAttributes(defineAttributeType, false);
                    foreach (var attr in attributes)
                    {
                        var defineSymbolProp = defineAttributeType.GetProperty("DefineSymbol");
                        var typeCheckProp = defineAttributeType.GetProperty("TypeCheck");
                        var descriptionProp = defineAttributeType.GetProperty("Description");

                        if (defineSymbolProp != null && typeCheckProp != null)
                        {
                            configs.Add(new DefineConfig
                            {
                                DefineSymbol = defineSymbolProp.GetValue(attr)?.ToString() ?? "",
                                TypeCheck = typeCheckProp.GetValue(attr)?.ToString() ?? "",
                                Description = descriptionProp?.GetValue(attr)?.ToString() ?? "",
                                SourceClass = type.FullName
                            });
                        }
                    }
                }
            }
            catch (ReflectionTypeLoadException)
            {
                continue;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error scanning assembly {assembly.GetName().Name}: {ex.Message}");
            }
        }

        return configs;
    }

    private static void CheckAndUpdateDefine(DefineConfig config)
    {
        bool typeExists = TypeExistsInAssemblies(config.TypeCheck);

        if (typeExists)
        {
            if (AddDefineSymbol(config.DefineSymbol))
            {
                Debug.Log($"✅ [{config.SourceClass}] Define '{config.DefineSymbol}' added (Type '{config.TypeCheck}' found)");
            }
            else
            {
                Debug.Log($"✓ [{config.SourceClass}] Define '{config.DefineSymbol}' already exists (Type '{config.TypeCheck}' found)");
            }
        }
        else
        {
            if (RemoveDefineSymbol(config.DefineSymbol))
            {
                Debug.Log($"❌ [{config.SourceClass}] Define '{config.DefineSymbol}' removed (Type '{config.TypeCheck}' not found)");
            }
            else
            {
                Debug.Log($"- [{config.SourceClass}] Define '{config.DefineSymbol}' not set (Type '{config.TypeCheck}' not found)");
            }
        }
    }

    public static bool TypeExistsInAssemblies(string typeName)
    {
        try
        {
            var type = Type.GetType(typeName);
            if (type != null)
            {
                return true;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var types = assembly.GetTypes();
                    foreach (var t in types)
                    {
                        if (t.FullName != null && t.FullName == typeName)
                        {
                            return true;
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    continue;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Error loading types from assembly {assembly.GetName().Name}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Error checking type {typeName}: {ex.Message}");
        }

        return false;
    }

#pragma warning disable 0618
    public static bool AddDefineSymbol(string defineSymbol)
    {
        bool changed = false;

        foreach (BuildTargetGroup targetGroup in GetValidBuildTargetGroups())
        {
            string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup) ?? "";
            HashSet<string> defines = new HashSet<string>(currentDefines.Split(';'));

            if (defines.Add(defineSymbol))
            {
                string newDefines = string.Join(";", defines.OrderBy(d => d));
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, newDefines);
                changed = true;
            }
        }

        return changed;
    }

    public static bool RemoveDefineSymbol(string defineSymbol)
    {
        bool changed = false;

        foreach (BuildTargetGroup targetGroup in GetValidBuildTargetGroups())
        {
            string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup) ?? "";
            HashSet<string> defines = new HashSet<string>(currentDefines.Split(';'));

            if (defines.Remove(defineSymbol))
            {
                string newDefines = string.Join(";", defines.OrderBy(d => d));
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, newDefines);
                changed = true;
            }
        }

        return changed;
    }
#pragma warning restore 0618

    public static List<BuildTargetGroup> GetValidBuildTargetGroups()
    {
        List<BuildTargetGroup> validGroups = new List<BuildTargetGroup>();
        foreach (BuildTargetGroup targetGroup in Enum.GetValues(typeof(BuildTargetGroup)))
        {
            if (targetGroup == BuildTargetGroup.Unknown)
                continue;

            try
            {
                var field = targetGroup.GetType().GetField(targetGroup.ToString());
                if (field == null)
                    continue;

                var obsoletes = field.GetCustomAttributes(typeof(ObsoleteAttribute), false);
                if (obsoletes.Length > 0)
                    continue;

#pragma warning disable 0618
                PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
#pragma warning restore 0618
                validGroups.Add(targetGroup);
            }
            catch
            {
            }
        }
        return validGroups;
    }

    [MenuItem("Tools/MobileCore/Reset All Define Symbols")]
    public static void ResetAllDefines()
    {
        if (EditorUtility.DisplayDialog("Reset All Define Symbols",
            "Are you sure you want to remove ALL define symbols detected from Define attributes?",
            "Yes, Reset All", "Cancel"))
        {
            var defineConfigs = ScanDefineAttributes();
            foreach (var config in defineConfigs)
            {
                RemoveDefineSymbol(config.DefineSymbol);
            }
            Debug.Log("All define symbols have been reset.");
        }
    }

    private class DefineConfig
    {
        public string DefineSymbol;
        public string TypeCheck;
        public string Description;
        public string SourceClass;
    }
}

/// <summary>
/// Editor window to manually manage define symbols.
/// </summary>
public class SDKDefinesEditorWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private List<DefineInfo> defineInfos = new List<DefineInfo>();

    [MenuItem("Tools/MobileCore/Define Symbols Manager")]
    public static void ShowWindow()
    {
        GetWindow<SDKDefinesEditorWindow>("Define Symbols Manager");
    }

    private void OnEnable()
    {
        RefreshDefineInfo();
    }

    private void RefreshDefineInfo()
    {
        defineInfos.Clear();
        var defineAttributeType = Type.GetType("MobileCore.DefineSystem.DefineAttribute");

        if (defineAttributeType == null)
        {
            Debug.LogError("DefineAttribute type not found! Make sure DefineAttribute.cs is compiled.");
            return;
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    var attributes = type.GetCustomAttributes(defineAttributeType, false);
                    foreach (var attr in attributes)
                    {
                        var defineSymbolProp = defineAttributeType.GetProperty("DefineSymbol");
                        var typeCheckProp = defineAttributeType.GetProperty("TypeCheck");
                        var descriptionProp = defineAttributeType.GetProperty("Description");

                        if (defineSymbolProp != null && typeCheckProp != null)
                        {
                            string defineSymbol = defineSymbolProp.GetValue(attr)?.ToString() ?? "";
                            string typeCheck = typeCheckProp.GetValue(attr)?.ToString() ?? "";
                            string description = descriptionProp?.GetValue(attr)?.ToString() ?? "";

                            bool typeExists = SDKDefinesManager.TypeExistsInAssemblies(typeCheck);
                            bool defineExists = IsDefineSymbolSet(defineSymbol);

                            defineInfos.Add(new DefineInfo
                            {
                                DefineSymbol = defineSymbol,
                                TypeCheck = typeCheck,
                                Description = description,
                                SourceClass = type.FullName,
                                TypeExists = typeExists,
                                DefineExists = defineExists
                            });
                        }
                    }
                }
            }
            catch (ReflectionTypeLoadException)
            {
                continue;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error scanning assembly {assembly.GetName().Name}: {ex.Message}");
            }
        }
    }

#pragma warning disable 0618
    private bool IsDefineSymbolSet(string defineSymbol)
    {
        foreach (var targetGroup in SDKDefinesManager.GetValidBuildTargetGroups())
        {
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup) ?? "";
            if (defines.Contains(defineSymbol))
            {
                return true;
            }
        }
        return false;
    }
#pragma warning restore 0618

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Define Symbols Manager", EditorStyles.boldLabel);
        GUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "This system uses [Define] attributes on Manager classes to automatically manage define symbols.\n" +
            "Add [Define(\"SYMBOL\", \"TypeName\")] attributes to your Manager classes to declare required defines.",
            MessageType.Info);
        GUILayout.Space(10);

        if (GUILayout.Button("Refresh All Define Symbols", GUILayout.Height(30)))
        {
            SDKDefinesManager.RefreshAllDefines();
            RefreshDefineInfo();
        }

        if (GUILayout.Button("Reset All Define Symbols", GUILayout.Height(25)))
        {
            SDKDefinesManager.ResetAllDefines();
            RefreshDefineInfo();
        }

        GUILayout.Space(15);

        EditorGUILayout.LabelField($"Detected Define Declarations ({defineInfos.Count})", EditorStyles.boldLabel);
        GUILayout.Space(5);

        if (defineInfos.Count == 0)
        {
            EditorGUILayout.HelpBox("No [Define] attributes found. Add [Define] attributes to your Manager classes.", MessageType.Warning);
        }
        else
        {
            foreach (var info in defineInfos)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                string status = info.TypeExists ? (info.DefineExists ? "✅" : "⚠️") : "❌";
                EditorGUILayout.LabelField($"{status} {info.DefineSymbol}", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField($"Type Check: {info.TypeCheck}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Source: {info.SourceClass}", EditorStyles.miniLabel);

                if (!string.IsNullOrEmpty(info.Description))
                {
                    EditorGUILayout.LabelField($"Description: {info.Description}", EditorStyles.miniLabel);
                }

                EditorGUILayout.LabelField($"Type Exists: {info.TypeExists} | Define Set: {info.DefineExists}", EditorStyles.miniLabel);

                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }
        }

        GUILayout.Space(15);

        EditorGUILayout.LabelField("Current Define Symbols", EditorStyles.boldLabel);
        GUILayout.Space(5);

#pragma warning disable 0618
        foreach (var targetGroup in SDKDefinesManager.GetValidBuildTargetGroups())
        {
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            EditorGUILayout.LabelField(targetGroup.ToString(), EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextArea(defines, GUILayout.Height(50));
            EditorGUI.EndDisabledGroup();
            GUILayout.Space(5);
        }
#pragma warning restore 0618

        if (GUILayout.Button("Open Player Settings"))
        {
            SettingsService.OpenProjectSettings("Project/Player");
        }

        EditorGUILayout.EndScrollView();
    }

    private class DefineInfo
    {
        public string DefineSymbol;
        public string TypeCheck;
        public string Description;
        public string SourceClass;
        public bool TypeExists;
        public bool DefineExists;
    }
}
#endif

