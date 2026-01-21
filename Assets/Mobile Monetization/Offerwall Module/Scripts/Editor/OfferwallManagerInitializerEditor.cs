#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using MobileCore.MainModule.Editor;

namespace MobileCore.Offerwall.Editor
{
    [CustomEditor(typeof(OfferwallManagerInitializer))]
    public class OfferwallManagerInitializerEditor : UnityEditor.Editor
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
                Debug.LogError($"Error in OfferwallManagerInitializerEditor.OnInspectorGUI: {e}");
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeaderInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header
            EditorGUILayout.LabelField("OFFERWALL MANAGER", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox("Initialize and manage offerwall systems like Tapjoy in your application.", MessageType.Info);
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
                EditorGUILayout.LabelField(new GUIContent("Settings", "Offerwall Settings configuration asset"), EditorStyleTemplate.GrayMiniLabelStyle, GUILayout.Width(100));

                if (_settingsProp.objectReferenceValue == null)
                {
                    EditorGUILayout.LabelField("No Offerwall Settings assigned", EditorStyleTemplate.GrayMiniLabelStyle);
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(_settingsProp.objectReferenceValue, typeof(OfferwallSettings), false);
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

                if (GUILayout.Button("OPEN OFFERWALL SETTINGS", openButtonStyle))
                {
                    Selection.activeObject = _settingsProp.objectReferenceValue;
                }

                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("Click above to open and configure the offerwall settings.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("No Offerwall Settings assigned. Please assign an OfferwallSettings asset to configure offerwalls.", MessageType.Error);

                // Check if we can create settings
                string assetPath = AssetDatabase.GetAssetPath(target);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    var createButtonStyle = EditorStyleTemplate.CreateButtonStyle(new Color(0.2f, 0.6f, 0.2f), null, 25);

                    if (GUILayout.Button("CREATE OFFERWALL SETTINGS", createButtonStyle))
                    {
                        CreateNewOfferwallSettings();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Save this asset first before creating Offerwall Settings.", MessageType.Warning);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void CreateNewOfferwallSettings()
        {
            try
            {
                string assetPath = AssetDatabase.GetAssetPath(target);
                if (string.IsNullOrEmpty(assetPath))
                {
                    Debug.LogError("Cannot create OfferwallSettings: Target asset is not saved yet.");
                    return;
                }

                var newSettings = ScriptableObject.CreateInstance<OfferwallSettings>();
                if (newSettings == null)
                {
                    Debug.LogError("Failed to create OfferwallSettings instance.");
                    return;
                }

                newSettings.name = "OfferwallSettings";
                AssetDatabase.AddObjectToAsset(newSettings, target);
                _settingsProp.objectReferenceValue = newSettings;

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.SetDirty(target);
                serializedObject.ApplyModifiedProperties();

                Debug.Log("Automatically created OfferwallSettings for OfferwallManagerInitializer");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to create OfferwallSettings: " + e.Message);
            }
        }
    }
}
#endif
