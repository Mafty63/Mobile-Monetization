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
        var defineAttributeType = GetTypeFromAllAssemblies("MobileCore.DefineSystem.DefineAttribute");

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

    public static Type GetTypeFromAllAssemblies(string typeName)
    {
        var type = Type.GetType(typeName);
        if (type != null) return type;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(typeName);
            if (type != null) return type;
        }
        return null;
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
    private bool showAllPlatforms = false;

    // Platform yang relevan untuk Mobile Monetization
    private static readonly BuildTargetGroup[] releventPlatforms = new[]
    {
        BuildTargetGroup.Android,
        BuildTargetGroup.iOS,
    };

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
        var defineAttributeType = SDKDefinesManager.GetTypeFromAllAssemblies("MobileCore.DefineSystem.DefineAttribute");

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

        GUILayout.Space(6);
        EditorGUILayout.LabelField("Define Symbols Manager", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Uses [Define(\"SYMBOL\", \"TypeName\")] attributes on Manager classes to auto-manage define symbols.", MessageType.Info);
        GUILayout.Space(6);

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
            // Header row
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("", GUILayout.Width(20));
            EditorGUILayout.LabelField("Define Symbol", EditorStyles.toolbarButton, GUILayout.MinWidth(160));
            EditorGUILayout.LabelField("Description", EditorStyles.toolbarButton, GUILayout.MinWidth(120));
            EditorGUILayout.LabelField("SDK Found", EditorStyles.toolbarButton, GUILayout.Width(72));
            EditorGUILayout.LabelField("Define Set", EditorStyles.toolbarButton, GUILayout.Width(72));
            EditorGUILayout.EndHorizontal();

            foreach (var info in defineInfos)
            {
                string status = info.TypeExists ? (info.DefineExists ? "✅" : "⚠️") : "❌";
                string tooltip = $"Type: {info.TypeCheck}\nSource: {info.SourceClass}";

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                // Status icon
                EditorGUILayout.LabelField(new GUIContent(status, tooltip), GUILayout.Width(20));

                // Define Symbol name
                EditorGUILayout.LabelField(new GUIContent(info.DefineSymbol, tooltip), EditorStyles.boldLabel, GUILayout.MinWidth(160));

                // Description
                string desc = string.IsNullOrEmpty(info.Description) ? "-" : info.Description;
                EditorGUILayout.LabelField(new GUIContent(desc, tooltip), EditorStyles.miniLabel, GUILayout.MinWidth(120));

                // SDK Found badge
                Color prevColor = GUI.color;
                GUI.color = info.TypeExists ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f);
                EditorGUILayout.LabelField(info.TypeExists ? "✔ Found" : "✘ Missing",
                    EditorStyles.miniLabel, GUILayout.Width(72));

                // Define Set badge
                GUI.color = info.DefineExists ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.7f, 0.3f);
                EditorGUILayout.LabelField(info.DefineExists ? "✔ Set" : "✘ Not Set",
                    EditorStyles.miniLabel, GUILayout.Width(72));

                GUI.color = prevColor;

                EditorGUILayout.EndHorizontal();
            }
        }

        GUILayout.Space(10);

        EditorGUILayout.LabelField("Current Define Symbols", EditorStyles.boldLabel);
        GUILayout.Space(3);

#pragma warning disable 0618
        // Header row
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUILayout.LabelField("Platform", EditorStyles.toolbarButton, GUILayout.Width(90));
        EditorGUILayout.LabelField("Active Defines", EditorStyles.toolbarButton);
        EditorGUILayout.EndHorizontal();

        // Tampilkan hanya platform mobile yang relevan
        foreach (var targetGroup in releventPlatforms)
        {
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            int count = string.IsNullOrEmpty(defines) ? 0 : defines.Split(';').Length;
            string tooltip = string.IsNullOrEmpty(defines) ? "(none)" : defines.Replace(";", "\n");

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField(new GUIContent(targetGroup.ToString(), tooltip),
                EditorStyles.boldLabel, GUILayout.Width(90));
            EditorGUILayout.LabelField(new GUIContent(
                string.IsNullOrEmpty(defines) ? "(none)" : defines,
                tooltip),
                EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"{count} define{(count == 1 ? "" : "s")}",
                EditorStyles.miniLabel, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
        }

        // Foldout untuk semua platform lainnya
        GUILayout.Space(4);
        showAllPlatforms = EditorGUILayout.Foldout(showAllPlatforms, "Other Platforms", true);
        if (showAllPlatforms)
        {
            EditorGUI.indentLevel++;
            foreach (var targetGroup in SDKDefinesManager.GetValidBuildTargetGroups())
            {
                if (System.Array.IndexOf(releventPlatforms, targetGroup) >= 0)
                    continue;

                string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
                int count = string.IsNullOrEmpty(defines) ? 0 : defines.Split(';').Length;
                string tooltip = string.IsNullOrEmpty(defines) ? "(none)" : defines.Replace(";", "\n");

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField(new GUIContent(targetGroup.ToString(), tooltip),
                    EditorStyles.miniLabel, GUILayout.Width(180));
                EditorGUILayout.LabelField($"{count} define{(count == 1 ? "" : "s")}",
                    EditorStyles.miniLabel, GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
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

