#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using MobileCore.MainModule.Editor;

namespace MobileCore.SystemModule.Editor
{
    /// <summary>
    /// Custom editor untuk SystemSettings agar menggunakan style template yang konsisten.
    /// Menggambar sleep timeout (int) dengan styled field.
    /// </summary>
    [CustomEditor(typeof(SystemSettings))]
    public class SystemSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty p_systemCanvas;
        private SerializedProperty p_screenSettings;

        // Nested properties dari ScreenSettings
        private SerializedProperty p_setFrameRateAutomatically;
        private SerializedProperty p_defaultFrameRate;
        private SerializedProperty p_batterySaveFrameRate;
        private SerializedProperty p_sleepTimeout;
        private SerializedProperty p_customSleepTimeout;

        private bool showScreenSettings = true;

        private void OnEnable()
        {
            p_systemCanvas = serializedObject.FindProperty("systemCanvas");
            p_screenSettings = serializedObject.FindProperty("screenSettings");

            if (p_screenSettings != null)
            {
                p_setFrameRateAutomatically = p_screenSettings.FindPropertyRelative("setFrameRateAutomatically");
                p_defaultFrameRate = p_screenSettings.FindPropertyRelative("defaultFrameRate");
                p_batterySaveFrameRate = p_screenSettings.FindPropertyRelative("batterySaveFrameRate");
                p_sleepTimeout = p_screenSettings.FindPropertyRelative("sleepTimeout");
                p_customSleepTimeout = p_screenSettings.FindPropertyRelative("customSleepTimeout");
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Core Canvas Settings
            EditorGUILayout.BeginVertical(EditorStyles.textArea);
            EditorGUILayout.LabelField("CORE CANVAS", EditorStyleTemplate.GrayBoldLabelStyle);
            GUILayout.Space(4);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("System Canvas", p_systemCanvas.tooltip), EditorStyles.label, GUILayout.Width(EditorGUIUtility.labelWidth));
            EditorGUILayout.PropertyField(p_systemCanvas, GUIContent.none);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();

            GUILayout.Space(8);

            // Screen & FPS Settings
            EditorGUILayout.BeginVertical(EditorStyles.textArea);
            
            EditorGUILayout.BeginHorizontal();
            showScreenSettings = EditorGUILayout.Foldout(showScreenSettings, "SCREEN & FPS SETTINGS", true, EditorStyleTemplate.GrayFoldoutHeaderStyle);
            EditorGUILayout.EndHorizontal();

            if (showScreenSettings && p_screenSettings != null)
            {
                EditorGUILayout.Space();

                // Frame Rate
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Frame Rate Config", EditorStyles.miniBoldLabel);
                GUILayout.Space(4);

                var popupStyle = EditorStyleTemplate.GrayPopupBackgroundStyle;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Auto Detect FPS", p_setFrameRateAutomatically.tooltip), EditorStyles.label, GUILayout.Width(EditorGUIUtility.labelWidth));
                p_setFrameRateAutomatically.boolValue = EditorStyleTemplate.DrawStyledToggle(p_setFrameRateAutomatically.boolValue);
                EditorGUILayout.EndHorizontal();

                if (!p_setFrameRateAutomatically.boolValue)
                {
                    GUILayout.Space(4);
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Default FPS", p_defaultFrameRate.tooltip), EditorStyles.label, GUILayout.Width(EditorGUIUtility.labelWidth));
                    DrawStyledEnumPopup(p_defaultFrameRate, popupStyle);
                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(2);
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Battery Save FPS (iOS)", p_batterySaveFrameRate.tooltip), EditorStyles.label, GUILayout.Width(EditorGUIUtility.labelWidth));
                    DrawStyledEnumPopup(p_batterySaveFrameRate, popupStyle);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();

                GUILayout.Space(6);

                // Sleep Timeout
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Sleep Timeout Config", EditorStyles.miniBoldLabel);
                GUILayout.Space(4);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Sleep Timeout", p_sleepTimeout.tooltip), EditorStyles.label, GUILayout.Width(EditorGUIUtility.labelWidth));
                EditorStyleTemplate.DrawStyledPropertyField(p_sleepTimeout, GUIContent.none);
                EditorGUILayout.EndHorizontal();

                if (p_sleepTimeout.intValue == 0)
                {
                    GUILayout.Space(4);
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Custom Timeout (s)", p_customSleepTimeout.tooltip), EditorStyles.label, GUILayout.Width(EditorGUIUtility.labelWidth));
                    EditorStyleTemplate.DrawStyledPropertyField(p_customSleepTimeout, GUIContent.none);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawStyledEnumPopup(SerializedProperty prop, GUIStyle style)
        {
            if (prop == null || prop.propertyType != SerializedPropertyType.Enum) return;

            string[] names = prop.enumDisplayNames;
            int current = prop.enumValueIndex;

            EditorGUI.BeginChangeCheck();
            int next = EditorGUILayout.Popup(current, names, style);
            if (EditorGUI.EndChangeCheck() && next != current)
            {
                prop.enumValueIndex = next;
            }
        }
    }
}
#endif
