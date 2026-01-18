#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using MobileCore.MainModule.Editor;

namespace MobileCore.IAPModule.Editor
{
    [CustomEditor(typeof(IAPManagerInitializer))]
    public class IAPManagerInitializerEditor : UnityEditor.Editor
    {
        private SerializedProperty _settingsProp;

        // Foldout states
        private bool _showReferences = true;

        private void OnEnable()
        {
            _settingsProp = serializedObject.FindProperty("Settings");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            try
            {
                DrawHeaderInfo();
                EditorGUILayout.Space();

                DrawReferencesSection();
                EditorGUILayout.Space();

                DrawOpenSettingsButton();
            }
            catch (System.Exception e)
            {
                EditorGUILayout.HelpBox($"Error drawing inspector: {e.Message}", MessageType.Error);
                Debug.LogError($"Error in IAPManagerInitModuleEditor.OnInspectorGUI: {e}");
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeaderInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header sederhana
            EditorGUILayout.LabelField("IAP MANAGER", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox("Initialize and manage In-App Purchases in your application.", MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private void DrawReferencesSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Foldout untuk References
            EditorGUILayout.BeginHorizontal();
            _showReferences = EditorGUILayout.Foldout(_showReferences, "REFERENCES", true, EditorStyleTemplate.GrayFoldoutHeaderStyle);
            EditorGUILayout.EndHorizontal();

            if (_showReferences)
            {
                EditorGUILayout.Space();

                EditorGUILayout.BeginVertical(EditorStyles.textArea);

                // Settings reference
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Settings", "IAP Settings configuration asset"), EditorStyleTemplate.GrayMiniLabelStyle, GUILayout.Width(100));

                if (_settingsProp.objectReferenceValue == null)
                {
                    EditorGUILayout.LabelField("No IAP Settings assigned", EditorStyleTemplate.GrayMiniLabelStyle);
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(_settingsProp.objectReferenceValue, typeof(IAPSettings), false);
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawOpenSettingsButton()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (_settingsProp.objectReferenceValue != null)
            {
                // Button dengan style yang matching
                var openButtonStyle = EditorStyleTemplate.CreateButtonStyle(new Color(0.1f, 0.5f, 0.9f), null, 25);

                if (GUILayout.Button("OPEN IAP SETTINGS", openButtonStyle))
                {
                    Selection.activeObject = _settingsProp.objectReferenceValue;
                }

                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("Click above to open and configure the IAP settings and products.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("No IAP Settings assigned. Please assign an IAPSettings asset to configure in-app purchases.", MessageType.Error);

                // Check if we can create settings
                string assetPath = AssetDatabase.GetAssetPath(target);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    var createButtonStyle = EditorStyleTemplate.CreateButtonStyle(new Color(0.2f, 0.6f, 0.2f), null, 25);

                    if (GUILayout.Button("CREATE IAP SETTINGS", createButtonStyle))
                    {
                        CreateNewIAPSettings();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Save this asset first before creating IAP Settings.", MessageType.Warning);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void CreateNewIAPSettings()
        {
            try
            {
                string assetPath = AssetDatabase.GetAssetPath(target);
                if (string.IsNullOrEmpty(assetPath))
                {
                    Debug.LogError("Cannot create IAPSettings: Target asset is not saved yet.");
                    return;
                }

                IAPSettings newSettings = ScriptableObject.CreateInstance<IAPSettings>();
                if (newSettings == null)
                {
                    Debug.LogError("Failed to create IAPSettings instance.");
                    return;
                }

                newSettings.name = "IAPSettings";
                AssetDatabase.AddObjectToAsset(newSettings, target);
                _settingsProp.objectReferenceValue = newSettings;

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.SetDirty(target);
                serializedObject.ApplyModifiedProperties();

                Debug.Log("Automatically created IAPSettings for IAPManagerInitModule");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to create IAPSettings: " + e.Message);
            }
        }
    }


}
#endif