#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using MobileCore.MainModule.Editor;

namespace MobileCore.Advertisements.Editor
{
    [CustomEditor(typeof(AdsManagerInitializer))]
    public class AdsManagerInitializerEditor : UnityEditor.Editor
    {
        private SerializedProperty _settingsProp;
        private SerializedProperty _dummyCanvasPrefabProp;
        private SerializedProperty _gdprPrefabProp;

        // Foldout states
        private bool _showReferences = true;

        private void OnEnable()
        {
            _settingsProp = serializedObject.FindProperty("Settings");
            _dummyCanvasPrefabProp = serializedObject.FindProperty("DummyCanvasPrefab");
            _gdprPrefabProp = serializedObject.FindProperty("GDPRPrefab");
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
                Debug.LogError($"Error in AdsManagerInitModuleEditor.OnInspectorGUI: {e}");
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeaderInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header sederhana
            EditorGUILayout.LabelField("ADVERTISEMENT MANAGER", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox("Initialize and manage advertisement systems in your application.", MessageType.Info);
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

                // Settings reference - non-editable
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Settings", "Ads Settings configuration asset"), EditorStyleTemplate.GrayMiniLabelStyle, GUILayout.Width(100));

                if (_settingsProp.objectReferenceValue == null)
                {
                    EditorGUILayout.LabelField("No Ads Settings assigned", EditorStyleTemplate.GrayMiniLabelStyle);
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(_settingsProp.objectReferenceValue, typeof(AdsSettings), false);
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(2);

                // Dummy Canvas Prefab
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Dummy Canvas", "Prefab used for testing ads in editor mode"), EditorStyleTemplate.GrayMiniLabelStyle, GUILayout.Width(100));
                _dummyCanvasPrefabProp.objectReferenceValue = EditorGUILayout.ObjectField(_dummyCanvasPrefabProp.objectReferenceValue, typeof(GameObject), false);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(2);

                // GDPR Prefab
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("GDPR Prefab", "Prefab for GDPR consent dialog"), EditorStyleTemplate.GrayMiniLabelStyle, GUILayout.Width(100));
                _gdprPrefabProp.objectReferenceValue = EditorGUILayout.ObjectField(_gdprPrefabProp.objectReferenceValue, typeof(GameObject), false);
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

                if (GUILayout.Button("OPEN ADS SETTINGS", openButtonStyle))
                {
                    Selection.activeObject = _settingsProp.objectReferenceValue;
                }

                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("Click above to open and configure the advertisement settings.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("No Ads Settings assigned. Please assign an AdsSettings asset to configure advertisements.", MessageType.Error);

                // Check if we can create settings
                string assetPath = AssetDatabase.GetAssetPath(target);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    var createButtonStyle = EditorStyleTemplate.CreateButtonStyle(new Color(0.2f, 0.6f, 0.2f), null, 25);

                    if (GUILayout.Button("CREATE ADS SETTINGS", createButtonStyle))
                    {
                        CreateNewAdsSettings();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Save this asset first before creating Ads Settings.", MessageType.Warning);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void CreateNewAdsSettings()
        {
            try
            {
                string assetPath = AssetDatabase.GetAssetPath(target);
                if (string.IsNullOrEmpty(assetPath))
                {
                    Debug.LogError("Cannot create AdsSettings: Target asset is not saved yet.");
                    return;
                }

                var newSettings = ScriptableObject.CreateInstance<AdsSettings>();
                if (newSettings == null)
                {
                    Debug.LogError("Failed to create AdsSettings instance.");
                    return;
                }

                newSettings.name = "AdsSettings";
                AssetDatabase.AddObjectToAsset(newSettings, target);
                _settingsProp.objectReferenceValue = newSettings;

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.SetDirty(target);
                serializedObject.ApplyModifiedProperties();

                Debug.Log("Automatically created AdsSettings for AdsManagerInitModule");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to create AdsSettings: " + e.Message);
            }
        }
    }


}
#endif