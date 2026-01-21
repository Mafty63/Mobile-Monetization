#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using MobileCore.MainModule.Editor;
using MobileCore.Offerwall;

namespace MobileCore.Offerwall.Editor
{
    [CustomEditor(typeof(OfferwallSettings))]
    public class OfferwallSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty p_testMode;
        private SerializedProperty p_showLogs;
        private SerializedProperty p_tapjoyContainer;

        private bool showGeneralSettings = true;
        private bool showTapjoySettings = true;

        private void OnEnable()
        {
            p_testMode = serializedObject.FindProperty("testMode");
            p_showLogs = serializedObject.FindProperty("showLogs");
            p_tapjoyContainer = serializedObject.FindProperty("tapjoyContainer");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            try
            {
                DrawHeaderInfo();
                EditorGUILayout.Space();

                DrawGeneralSettings();
                EditorGUILayout.Space();

                DrawTapjoySettings();
            }
            catch (System.Exception e)
            {
                EditorGUILayout.HelpBox($"Error drawing inspector: {e.Message}", MessageType.Error);
                Debug.LogError($"Error in OfferwallSettingsEditor.OnInspectorGUI: {e}");
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeaderInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("OFFERWALL SETTINGS", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("Configure Offerwall providers and integration settings.", MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private void DrawGeneralSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            showGeneralSettings = EditorGUILayout.Foldout(showGeneralSettings, "GENERAL SETTINGS", true, EditorStyles.foldoutHeader);
            EditorGUILayout.EndHorizontal();

            if (showGeneralSettings)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.textArea);

                var toggleStyle = EditorStyleTemplate.GrayToggleBackgroundStyle;

                // Test Mode
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Test Mode", EditorStyles.label, GUILayout.Width(EditorGUIUtility.labelWidth - 20));
                p_testMode.boolValue = EditorGUILayout.Toggle(p_testMode.boolValue, toggleStyle);
                EditorGUILayout.LabelField(new GUIContent("", "Enables development mode for providers"), EditorStyles.miniLabel, GUILayout.Width(20));
                EditorGUILayout.EndHorizontal();

                // System Logs
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("System Logs", EditorStyles.label, GUILayout.Width(EditorGUIUtility.labelWidth - 20));
                p_showLogs.boolValue = EditorGUILayout.Toggle(p_showLogs.boolValue, toggleStyle);
                EditorGUILayout.LabelField(new GUIContent("", "Enables extended logging"), EditorStyles.miniLabel, GUILayout.Width(20));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTapjoySettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            showTapjoySettings = EditorGUILayout.Foldout(showTapjoySettings, "TAPJOY CONFIGURATION", true, EditorStyles.foldoutHeader);
            EditorGUILayout.EndHorizontal();

            if (showTapjoySettings)
            {
                EditorGUILayout.Space();
                
                // Container Header
                EditorGUILayout.BeginVertical(EditorStyles.textArea);
                EditorGUILayout.LabelField("Tapjoy Settings", EditorStyles.boldLabel);
                
                if (p_tapjoyContainer != null)
                {
                    DrawAllChildrenWithGrayBackground(p_tapjoyContainer);
                }
                else
                {
                    EditorGUILayout.HelpBox("Tapjoy Container property not found.", MessageType.Error);
                }

                // Download SDK Button logic similar to AdsSettings
                EditorGUILayout.Space(10);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("Download Tapjoy SDK", EditorStyleTemplate.GrayButtonStyle, GUILayout.Height(25), GUILayout.Width(150)))
                {
                    Application.OpenURL("https://github.com/Tapjoy/Tapjoy-Unity-Plugin/releases");
                }
                
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("Make sure to install Tapjoy Unity SDK v13.x or newer.", MessageType.Info);

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAllChildrenWithGrayBackground(SerializedProperty prop)
        {
            SerializedProperty iterator = prop.Copy();
            SerializedProperty endProp = iterator.GetEndProperty();
            bool enterChildren = true;

            if (iterator.NextVisible(enterChildren))
            {
                while (!SerializedProperty.EqualContents(iterator, endProp))
                {
                    if (iterator.name == "m_Script")
                    {
                        if (!iterator.NextVisible(false)) break;
                        continue;
                    }

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent(iterator.displayName, iterator.tooltip), EditorStyles.label, GUILayout.Width(EditorGUIUtility.labelWidth));

                    var textFieldStyle = EditorStyleTemplate.GrayTextFieldBackgroundStyle;
                    var toggleStyle = EditorStyleTemplate.GrayToggleBackgroundStyle;
                    
                    switch (iterator.propertyType)
                    {
                        case SerializedPropertyType.String:
                            iterator.stringValue = EditorGUILayout.TextField(iterator.stringValue, textFieldStyle);
                            break;
                        case SerializedPropertyType.Integer:
                            iterator.intValue = EditorGUILayout.IntField(iterator.intValue, textFieldStyle);
                            break;
                        case SerializedPropertyType.Float:
                            iterator.floatValue = EditorGUILayout.FloatField(iterator.floatValue, textFieldStyle);
                            break;
                        case SerializedPropertyType.Boolean:
                            iterator.boolValue = EditorGUILayout.Toggle(iterator.boolValue, toggleStyle);
                            break;
                        case SerializedPropertyType.Enum:
                            EditorGUILayout.PropertyField(iterator, GUIContent.none);
                            break;
                        default:
                            EditorGUILayout.PropertyField(iterator, GUIContent.none);
                            break;
                    }

                    EditorGUILayout.EndHorizontal();

                    if (!iterator.NextVisible(false)) break;
                }
            }
        }
    }
}
#endif
