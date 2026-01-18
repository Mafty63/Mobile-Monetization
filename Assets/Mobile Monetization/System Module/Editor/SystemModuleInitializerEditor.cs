#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using MobileCore.MainModule.Editor;

namespace MobileCore.SystemModule.Editor
{
    [CustomEditor(typeof(SystemModuleInitializer))]
    public class SystemModuleInitializerEditor : UnityEditor.Editor
    {
        private SerializedProperty canvasProperty;
        private SerializedProperty screenSettingsProperty;

        // Foldout states
        private bool showCanvasConfiguration = true;
        private bool showScreenSettings = true;

        // Sleep timeout options
        private enum SleepTimeoutOption
        {
            SystemSetting = -2,
            NeverSleep = -1,
            [InspectorName("System Default (Custom)")] SystemDefault = 0
        }

        private void OnEnable()
        {
            try
            {
                // Fields were renamed to PascalCase in SystemModuleInitializer
                // SystemCanvas (previously systemCanvas) and ScreenSettings (previously screenSettings)
                // Note: serialzedObject.FindProperty uses the serialized field name found in the debug view of inspector, 
                // which usually matches the field name in the script.
                
                // Try finding with new PascalCase names first
                canvasProperty = serializedObject.FindProperty("SystemCanvas");
                if (canvasProperty == null)
                     canvasProperty = serializedObject.FindProperty("systemCanvas");
                     
                screenSettingsProperty = serializedObject.FindProperty("ScreenSettings");
                if (screenSettingsProperty == null)
                    screenSettingsProperty = serializedObject.FindProperty("screenSettings");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to initialize SystemModuleInitializerEditor: {e.Message}");
            }
        }

        public override void OnInspectorGUI()
        {
            // Early return if properties are not properly initialized
            if (canvasProperty == null || screenSettingsProperty == null)
            {
                EditorGUILayout.HelpBox("Editor properties not properly initialized. Please check the script.", MessageType.Error);
                if (GUILayout.Button("Reload Editor"))
                {
                    OnEnable();
                }
                return;
            }

            serializedObject.Update();

            try
            {
                DrawHeaderInfo();
                EditorGUILayout.Space();

                DrawCanvasConfiguration();
                EditorGUILayout.Space();

                DrawScreenSettings();
            }
            catch (ExitGUIException)
            {
                throw;
            }
            catch (System.Exception e)
            {
                EditorGUILayout.HelpBox($"Error drawing inspector: {e.Message}", MessageType.Error);
                Debug.LogError($"Error in CoreSystemInitModuleEditor.OnInspectorGUI: {e}");
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeaderInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("CORE SYSTEM SETTINGS", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("Configure core system initialization settings including canvas and screen configuration.", MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private void DrawCanvasConfiguration()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            showCanvasConfiguration = EditorGUILayout.Foldout(showCanvasConfiguration, "CANVAS CONFIGURATION", true, EditorStyles.foldoutHeader);
            EditorGUILayout.EndHorizontal();

            if (showCanvasConfiguration)
            {
                EditorGUILayout.Space();

                EditorGUILayout.BeginVertical(EditorStyles.textArea);

                // Canvas reference - FIXED: Added proper error handling and layout
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Canvas Prefab", "Canvas prefab containing core UI elements like message system and loading panels"),
                    EditorStyles.miniLabel, GUILayout.Width(120));

                // Use a safer approach for ObjectField
                Rect objectFieldRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.objectField);
                canvasProperty.objectReferenceValue = EditorGUI.ObjectField(objectFieldRect, canvasProperty.objectReferenceValue, typeof(GameObject), false);
                EditorGUILayout.EndHorizontal();

                if (canvasProperty.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Canvas prefab is required. It should contain the core UI elements like message system and loading panels.", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox("Canvas assigned successfully. Make sure it contains all required core UI components.", MessageType.Info);
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawScreenSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            showScreenSettings = EditorGUILayout.Foldout(showScreenSettings, "SCREEN SETTINGS", true, EditorStyles.foldoutHeader);
            EditorGUILayout.EndHorizontal();

            if (showScreenSettings)
            {
                EditorGUILayout.Space();

                if (screenSettingsProperty == null)
                {
                    EditorGUILayout.HelpBox("Screen Settings property not found.", MessageType.Error);
                    EditorGUILayout.EndVertical();
                    return;
                }

                EditorGUILayout.BeginVertical(EditorStyles.textArea);

                var setFrameRateAutomatically = screenSettingsProperty.FindPropertyRelative("setFrameRateAutomatically");
                var defaultFrameRate = screenSettingsProperty.FindPropertyRelative("defaultFrameRate");
                var batterySaveFrameRate = screenSettingsProperty.FindPropertyRelative("batterySaveFrameRate");
                var sleepTimeout = screenSettingsProperty.FindPropertyRelative("sleepTimeout");

                // Find customSleepTimeout safely
                SerializedProperty customSleepTimeout = null;
                try
                {
                    customSleepTimeout = screenSettingsProperty.FindPropertyRelative("customSleepTimeout");
                }
                catch (System.Exception)
                {
                    // Property doesn't exist, which is fine
                }

                bool hasCustomSleepTimeout = customSleepTimeout != null;

                // Get styles
                var toggleStyle = EditorStyleTemplate.GrayToggleBackgroundStyle;
                var popupStyle = EditorStyleTemplate.GrayPopupBackgroundStyle;
                var textFieldStyle = EditorStyleTemplate.GrayTextFieldBackgroundStyle;

                // Frame Rate Settings
                EditorGUILayout.LabelField("Frame Rate Settings", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(5);

                // Auto Detect toggle - FIXED: Safer approach
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Auto Detect Frame Rate", "Automatically set frame rate based on device refresh rate"),
                    EditorStyles.miniLabel, GUILayout.Width(120));

                if (setFrameRateAutomatically != null)
                {
                    setFrameRateAutomatically.boolValue = EditorGUILayout.Toggle(setFrameRateAutomatically.boolValue, toggleStyle);
                }
                else
                {
                    EditorGUILayout.LabelField("Property Missing", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(8);

                if (setFrameRateAutomatically != null && !setFrameRateAutomatically.boolValue)
                {
                    // Manual frame rate settings
                    EditorGUILayout.BeginVertical();

                    // Default Frame Rate
                    if (defaultFrameRate != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(new GUIContent("Default FPS", "Target frame rate for normal operation"),
                            EditorStyles.miniLabel, GUILayout.Width(120));

                        int currentDefaultValue = defaultFrameRate.intValue;
                        int selectedDefaultIndex = FrameRateValueToIndex(currentDefaultValue);

                        EditorGUI.BeginChangeCheck();
                        int newDefaultIndex = EditorGUILayout.Popup(selectedDefaultIndex, GetFrameRateDisplayNames(), popupStyle);
                        if (EditorGUI.EndChangeCheck())
                        {
                            defaultFrameRate.intValue = FrameRateIndexToValue(newDefaultIndex);
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.Space(5);

                    // Battery Save Frame Rate
                    if (batterySaveFrameRate != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(new GUIContent("Battery Save FPS", "Frame rate when device is in low power mode (iOS only)"),
                            EditorStyles.miniLabel, GUILayout.Width(120));

                        int currentBatteryValue = batterySaveFrameRate.intValue;
                        int selectedBatteryIndex = FrameRateValueToIndex(currentBatteryValue);

                        EditorGUI.BeginChangeCheck();
                        int newBatteryIndex = EditorGUILayout.Popup(selectedBatteryIndex, GetFrameRateDisplayNames(), popupStyle);
                        if (EditorGUI.EndChangeCheck())
                        {
                            batterySaveFrameRate.intValue = FrameRateIndexToValue(newBatteryIndex);
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.EndVertical();

                    EditorGUILayout.Space(5);
                    EditorGUILayout.HelpBox(
                        "On iOS devices, the system will automatically switch to Battery Save Frame Rate when low power mode is enabled.",
                        MessageType.Info);
                }
                else if (setFrameRateAutomatically != null)
                {
                    EditorGUILayout.HelpBox(
                        "Frame rate will be automatically set to match the device's screen refresh rate.",
                        MessageType.Info);
                }

                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                EditorGUILayout.Space(5);

                // Sleep Settings
                EditorGUILayout.LabelField("Sleep Timeout Settings", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(5);

                // Sleep timeout with enum popup and style
                if (sleepTimeout != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Sleep Timeout", "Screen sleep timeout setting"),
                        EditorStyles.miniLabel, GUILayout.Width(120));

                    // Convert current sleep timeout value to enum
                    SleepTimeoutOption currentSleepOption = GetSleepTimeoutOptionFromValue(sleepTimeout.intValue);

                    EditorGUI.BeginChangeCheck();
                    SleepTimeoutOption newSleepOption = (SleepTimeoutOption)EditorGUILayout.EnumPopup(currentSleepOption, popupStyle);

                    // Update sleep timeout value
                    if (EditorGUI.EndChangeCheck() && (int)newSleepOption != sleepTimeout.intValue)
                    {
                        sleepTimeout.intValue = (int)newSleepOption;
                    }
                    EditorGUILayout.EndHorizontal();

                    // Show custom timeout field when System Default is selected and customSleepTimeout exists
                    if (sleepTimeout.intValue == 0 && hasCustomSleepTimeout && customSleepTimeout != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(new GUIContent("Custom Timeout", "Custom sleep timeout in seconds (used with System Default)"),
                            EditorStyles.miniLabel, GUILayout.Width(120));
                        customSleepTimeout.intValue = EditorGUILayout.IntField(customSleepTimeout.intValue, textFieldStyle);
                        EditorGUILayout.EndHorizontal();

                        if (customSleepTimeout.intValue <= 0)
                        {
                            EditorGUILayout.HelpBox("Custom timeout must be a positive value.", MessageType.Warning);
                        }
                    }

                    // Handle custom values not in the enum (legacy support)
                    if (sleepTimeout.intValue > 0 && !System.Enum.IsDefined(typeof(SleepTimeoutOption), sleepTimeout.intValue))
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(new GUIContent("Custom Timeout", "Custom sleep timeout in seconds"),
                            EditorStyles.miniLabel, GUILayout.Width(120));
                        sleepTimeout.intValue = EditorGUILayout.IntField(sleepTimeout.intValue, textFieldStyle);
                        EditorGUILayout.EndHorizontal();
                    }

                    // Display helpful information based on sleep timeout value
                    string timeoutInfo = GetSleepTimeoutInfo(sleepTimeout.intValue, hasCustomSleepTimeout && customSleepTimeout != null ? customSleepTimeout.intValue : 0);
                    if (!string.IsNullOrEmpty(timeoutInfo))
                    {
                        EditorGUILayout.HelpBox(timeoutInfo, MessageType.Info);
                    }
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        // Helper methods for frame rate enum conversion
        private string[] GetFrameRateDisplayNames()
        {
            return new string[] { "30 FPS", "60 FPS", "90 FPS", "120 FPS" };
        }

        private int FrameRateValueToIndex(int value)
        {
            return value switch
            {
                30 => 0,
                60 => 1,
                90 => 2,
                120 => 3,
                _ => 1 // Default to 60 FPS if unknown
            };
        }

        private int FrameRateIndexToValue(int index)
        {
            return index switch
            {
                0 => 30,
                1 => 60,
                2 => 90,
                3 => 120,
                _ => 60 // Default to 60 FPS if unknown
            };
        }

        private SleepTimeoutOption GetSleepTimeoutOptionFromValue(int value)
        {
            // Check if value exists in our enum
            if (System.Enum.IsDefined(typeof(SleepTimeoutOption), value))
            {
                return (SleepTimeoutOption)value;
            }

            // For custom positive values, return SystemDefault but we'll handle custom separately
            return value > 0 ? SleepTimeoutOption.SystemDefault : (SleepTimeoutOption)value;
        }

        private string GetSleepTimeoutInfo(int timeout, int customTimeout)
        {
            if (timeout == 0 && customTimeout > 0)
            {
                return $"Screen will sleep after {customTimeout} seconds of inactivity (System Default with custom timeout)";
            }
            else if (timeout == 0)
            {
                return "Screen will sleep according to system default timeout";
            }

            return timeout switch
            {
                -1 => "Screen will never sleep during application runtime",
                -2 => "Screen sleep behavior will follow system settings",
                > 0 => $"Screen will sleep after {timeout} seconds of inactivity",
                _ => "Invalid sleep timeout value"
            };
        }
    }
#endif
}