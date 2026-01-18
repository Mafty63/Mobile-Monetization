#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using MobileCore.MainModule.Editor;

namespace MobileCore.Advertisements.Editor
{
    [CustomEditor(typeof(AdsSettings))]
    public class AdsSettingsEditor : UnityEditor.Editor
    {
        private struct ProviderInfo
        {
            public string DisplayName;
            public SerializedProperty Property;
            public int Order;
            public string FieldName;
            public string SdkDownloadUrl;
        }

        private List<ProviderInfo> providerInfos = new List<ProviderInfo>();
        private int selectedTab = 0;
        private const string EditorPrefKey = "MobileCore_AdsSettings_SelectedTab";

        private SerializedProperty p_bannerType;
        private SerializedProperty p_interstitialType;
        private SerializedProperty p_rewardedVideoType;
        private SerializedProperty p_testMode;
        private SerializedProperty p_systemLogs;
        private SerializedProperty p_adOnStart;

        // Privacy / Tracking props
        private SerializedProperty p_isGDPREnabled;
        private SerializedProperty p_isIDFAEnabled;
        private SerializedProperty p_trackingDescription;
        private SerializedProperty p_privacyLink;
        private SerializedProperty p_termsOfUseLink;

        private SerializedProperty p_interstitialFirstStartDelay;
        private SerializedProperty p_interstitialStartDelay;
        private SerializedProperty p_interstitialShowingDelay;
        private SerializedProperty p_autoShowInterstitial;

        // Foldout states
        private bool showProviderConfiguration = true;
        private bool showGeneralSettings = false;
        private bool showPrivacySettings = false;
        private bool showInterstitialSettings = false;

        private void OnEnable()
        {
            selectedTab = EditorPrefs.GetInt(EditorPrefKey, 0);

            // Get existing properties
            p_bannerType = serializedObject.FindProperty("bannerType");
            p_interstitialType = serializedObject.FindProperty("interstitialType");
            p_rewardedVideoType = serializedObject.FindProperty("rewardedVideoType");
            p_testMode = serializedObject.FindProperty("testMode");
            p_systemLogs = serializedObject.FindProperty("systemLogs");
            p_adOnStart = serializedObject.FindProperty("adOnStart");

            p_isGDPREnabled = serializedObject.FindProperty("isGDPREnabled");
            p_isIDFAEnabled = serializedObject.FindProperty("isIDFAEnabled");
            p_trackingDescription = serializedObject.FindProperty("trackingDescription");
            p_privacyLink = serializedObject.FindProperty("privacyLink");
            p_termsOfUseLink = serializedObject.FindProperty("termsOfUseLink");

            p_interstitialFirstStartDelay = serializedObject.FindProperty("interstitialFirstStartDelay");
            p_interstitialStartDelay = serializedObject.FindProperty("interstitialStartDelay");
            p_interstitialShowingDelay = serializedObject.FindProperty("interstitialShowingDelay");
            p_autoShowInterstitial = serializedObject.FindProperty("autoShowInterstitial");

            // Auto-detect provider containers
            DetectProviderContainers();
        }

        private void DetectProviderContainers()
        {
            providerInfos.Clear();

            // Get all fields from AdsSettings using reflection
            var fields = typeof(AdsSettings).GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var field in fields)
            {
                // Check if field has our custom attribute
                var attribute = System.Attribute.GetCustomAttribute(
                    field, typeof(AdsProviderContainerAttribute)) as AdsProviderContainerAttribute;

                if (attribute != null)
                {
                    // Get the serialized property
                    var prop = serializedObject.FindProperty(field.Name);
                    if (prop != null)
                    {
                        providerInfos.Add(new ProviderInfo
                        {
                            DisplayName = attribute.DisplayName,
                            Property = prop,
                            Order = attribute.Order,
                            FieldName = field.Name,
                            SdkDownloadUrl = attribute.SdkDownloadUrl
                        });
                    }
                }
            }

            // Sort by order
            providerInfos.Sort((a, b) => a.Order.CompareTo(b.Order));
        }


        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            try
            {
                DrawHeaderInfo();
                EditorGUILayout.Space();

                DrawProviderConfiguration();
                EditorGUILayout.Space();

                DrawGeneralSettings();
                EditorGUILayout.Space();

                DrawPrivacySettings();
                EditorGUILayout.Space();

                DrawInterstitialSettings();
            }
            catch (System.Exception e)
            {
                EditorGUILayout.HelpBox($"Error drawing inspector: {e.Message}", MessageType.Error);
                Debug.LogError($"Error in AdsSettingsEditor.OnInspectorGUI: {e}");
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeaderInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("ADVERTISEMENT SETTINGS", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("Configure advertisement providers and settings.", MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private void DrawProviderConfiguration()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            showProviderConfiguration = EditorGUILayout.Foldout(showProviderConfiguration, "PROVIDER CONFIGURATION", true, EditorStyles.foldoutHeader);
            EditorGUILayout.EndHorizontal();

            if (!showProviderConfiguration)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical(EditorStyles.textArea);
            EditorGUILayout.LabelField("Ad Type Providers", EditorStyles.miniBoldLabel);

            EditorGUILayout.BeginHorizontal();

            // Banner
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField(new GUIContent("Banner", "Select the ad provider for banner ads"), EditorStyles.miniLabel, GUILayout.Width(60));
            DrawStyledPopup(p_bannerType, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndVertical();

            // Interstitial
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField(new GUIContent("Interstitial", "Select the ad provider for interstitial ads"), EditorStyles.miniLabel, GUILayout.Width(70));
            DrawStyledPopup(p_interstitialType, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndVertical();

            // Rewarded
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField(new GUIContent("Rewarded", "Select the ad provider for rewarded video ads"), EditorStyles.miniLabel, GUILayout.Width(70));
            DrawStyledPopup(p_rewardedVideoType, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Provider Settings (buttons) - AUTOMATIC
            EditorGUILayout.BeginVertical(EditorStyles.textArea);
            EditorGUILayout.LabelField("Provider Settings", EditorStyles.miniBoldLabel);

            var buttonStyle = EditorStyleTemplate.GrayToggleButtonStyle;
            buttonStyle.fixedHeight = 22f;
            buttonStyle.fontSize = 9;
            buttonStyle.padding = new RectOffset(4, 4, 2, 2);

            float availableWidth = Mathf.Max(200f, EditorGUIUtility.currentViewWidth - 40f);
            int buttonsPerRow = 3;
            float buttonWidth = (availableWidth - 20f) / buttonsPerRow;
            float buttonHeight = 22f;

            // Draw provider buttons automatically
            for (int i = 0; i < providerInfos.Count; i += buttonsPerRow)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                for (int j = 0; j < buttonsPerRow; j++)
                {
                    int index = i + j;
                    if (index < providerInfos.Count)
                    {
                        var providerInfo = providerInfos[index];
                        if (GUILayout.Toggle(selectedTab == index, providerInfo.DisplayName,
                            buttonStyle, GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                        {
                            selectedTab = index;
                        }
                        if (j < buttonsPerRow - 1 && index < providerInfos.Count - 1)
                        {
                            GUILayout.Space(10f);
                        }
                    }
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                if (i + buttonsPerRow < providerInfos.Count)
                {
                    EditorGUILayout.Space(5f);
                }
            }

            EditorPrefs.SetInt(EditorPrefKey, selectedTab);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            DrawSelectedContainerExpanded();
            EditorGUILayout.EndVertical();
        }

        private void DrawStyledPopup(SerializedProperty prop, params GUILayoutOption[] options)
        {
            if (prop == null || prop.propertyType != SerializedPropertyType.Enum) return;

            string[] enumNames = prop.enumDisplayNames;
            int currentIndex = prop.enumValueIndex;

            var popupStyle = EditorStyleTemplate.GrayPopupBackgroundStyle;

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup(currentIndex, enumNames, popupStyle, options);
            if (EditorGUI.EndChangeCheck() && newIndex != currentIndex)
            {
                prop.enumValueIndex = newIndex;
            }
        }

        private void DrawSelectedContainerExpanded()
        {
            if (selectedTab < 0 || selectedTab >= providerInfos.Count) return;

            var providerInfo = providerInfos[selectedTab];
            var containerProp = providerInfo.Property;
            var providerName = providerInfo.DisplayName;
            var sdkUrl = providerInfo.SdkDownloadUrl;

            EditorGUILayout.BeginVertical(EditorStyles.textArea);
            EditorGUILayout.LabelField($"{providerName} Settings", EditorStyles.boldLabel);

            if (containerProp.propertyType == SerializedPropertyType.ObjectReference)
            {
                EditorGUILayout.PropertyField(containerProp, new GUIContent(containerProp.displayName));
                var obj = containerProp.objectReferenceValue;
                if (obj != null)
                {
                    UnityEditor.Editor createdEditor = null;
                    try
                    {
                        createdEditor = UnityEditor.Editor.CreateEditor(obj);
                        if (createdEditor != null)
                        {
                            EditorGUILayout.BeginVertical(GUI.skin.box);
                            createdEditor.OnInspectorGUI();
                            EditorGUILayout.EndVertical();
                        }
                    }
                    finally
                    {
                        if (createdEditor != null) UnityEngine.Object.DestroyImmediate(createdEditor);
                    }

                    var serializedObj = new SerializedObject(obj);
                    var missing = ValidateSerializedObject(serializedObj);
                    if (missing.Count > 0)
                    {
                        EditorGUILayout.HelpBox("Missing required fields:\n- " + string.Join("\n- ", missing), MessageType.Error);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox($"No {providerName} container assigned.", MessageType.Warning);
                }
            }
            else
            {
                DrawAllChildrenWithGrayBackground(containerProp);
                var missing = ValidateNestedContainer(containerProp);
                if (missing.Count > 0)
                {
                    EditorGUILayout.HelpBox("Missing required fields:\n- " + string.Join("\n- ", missing), MessageType.Error);
                }
            }

            // Download SDK button - SEKARANG OTOMATIS DARI ATTRIBUTE
            if (!string.IsNullOrEmpty(sdkUrl))
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                string buttonText = $"Download {providerName} SDK";
                var downloadButtonStyle = EditorStyleTemplate.GrayButtonStyle;
                if (GUILayout.Button(buttonText, downloadButtonStyle, GUILayout.Height(25), GUILayout.Width(150)))
                {
                    Application.OpenURL(sdkUrl);
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox($"Click the button above to download and install the {providerName} SDK documentation and integration guide.", MessageType.Info);
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
                    var popupStyle = EditorStyleTemplate.GrayPopupBackgroundStyle;

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
                            DrawStyledPopup(iterator, GUILayout.ExpandWidth(true));
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
                EditorGUILayout.LabelField(new GUIContent("", "Enables development mode to setup advertisement providers"), EditorStyles.miniLabel, GUILayout.Width(20));
                EditorGUILayout.EndHorizontal();

                // System Logs
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("System Logs", EditorStyles.label, GUILayout.Width(EditorGUIUtility.labelWidth - 20));
                p_systemLogs.boolValue = EditorGUILayout.Toggle(p_systemLogs.boolValue, toggleStyle);
                EditorGUILayout.LabelField(new GUIContent("", "Enables logging. Use it to debug advertisement logic"), EditorStyles.miniLabel, GUILayout.Width(20));
                EditorGUILayout.EndHorizontal();

                // Ads On Start
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Ads On Start", EditorStyles.label, GUILayout.Width(EditorGUIUtility.labelWidth - 20));
                p_adOnStart.boolValue = EditorGUILayout.Toggle(p_adOnStart.boolValue, toggleStyle);
                EditorGUILayout.LabelField(new GUIContent("", "Enable ads when the application starts"), EditorStyles.miniLabel, GUILayout.Width(20));
                EditorGUILayout.EndHorizontal();

                if (p_testMode.boolValue) EditorGUILayout.HelpBox("Test Mode: Using test ads", MessageType.Warning);

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPrivacySettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            showPrivacySettings = EditorGUILayout.Foldout(showPrivacySettings, "PRIVACY & COMPLIANCE", true, EditorStyles.foldoutHeader);
            EditorGUILayout.EndHorizontal();

            if (showPrivacySettings)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.textArea);

                var toggleStyle = EditorStyleTemplate.GrayToggleBackgroundStyle;
                var textFieldStyle = EditorStyleTemplate.GrayTextFieldBackgroundStyle;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("GDPR Enabled", EditorStyles.label, GUILayout.Width(EditorGUIUtility.labelWidth - 10));
                p_isGDPREnabled.boolValue = EditorGUILayout.Toggle(p_isGDPREnabled.boolValue, toggleStyle);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("IDFA Enabled", EditorStyles.label, GUILayout.Width(EditorGUIUtility.labelWidth - 10));
                p_isIDFAEnabled.boolValue = EditorGUILayout.Toggle(p_isIDFAEnabled.boolValue, toggleStyle);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                EditorGUILayout.BeginVertical();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Description", "The description that will be shown to users when asking for tracking permission"), EditorStyles.miniLabel, GUILayout.Width(80));
                p_trackingDescription.stringValue = EditorGUILayout.TextField(p_trackingDescription.stringValue, textFieldStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(2);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Privacy Link", "Link to your privacy policy"), EditorStyles.miniLabel, GUILayout.Width(80));
                p_privacyLink.stringValue = EditorGUILayout.TextField(p_privacyLink.stringValue, textFieldStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(2);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Terms Link", "Link to your terms of use"), EditorStyles.miniLabel, GUILayout.Width(80));
                p_termsOfUseLink.stringValue = EditorGUILayout.TextField(p_termsOfUseLink.stringValue, textFieldStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();

                List<string> missing = new List<string>();
                if (string.IsNullOrEmpty(p_trackingDescription.stringValue)) missing.Add("Tracking Description");
                if (string.IsNullOrEmpty(p_privacyLink.stringValue)) missing.Add("Privacy Policy Link");
                if (string.IsNullOrEmpty(p_termsOfUseLink.stringValue)) missing.Add("Terms of Use Link");

                if (missing.Count > 0) EditorGUILayout.HelpBox("Missing privacy fields", MessageType.Warning);

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawInterstitialSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            showInterstitialSettings = EditorGUILayout.Foldout(showInterstitialSettings, "INTERSTITIAL TIMING", true, EditorStyles.foldoutHeader);
            EditorGUILayout.EndHorizontal();

            if (showInterstitialSettings)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.textArea);

                var toggleStyle = EditorStyleTemplate.GrayToggleBackgroundStyle;
                var textFieldStyle = EditorStyleTemplate.GrayTextFieldBackgroundStyle;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Auto Show Interstitial", "If enabled, interstitials will be shown automatically based on the delays below"), EditorStyles.label, GUILayout.Width(150));
                p_autoShowInterstitial.boolValue = EditorGUILayout.Toggle(p_autoShowInterstitial.boolValue, toggleStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                if (p_autoShowInterstitial.boolValue)
                {
                    EditorGUILayout.BeginVertical();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("First Delay (s)", "Delay in seconds before interstitial appearings on first game launch. (first time playing)"), EditorStyles.miniLabel, GUILayout.Width(120));
                    p_interstitialFirstStartDelay.floatValue = EditorGUILayout.FloatField(p_interstitialFirstStartDelay.floatValue, textFieldStyle);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(2);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Subsequent Delay (s)", "Delay in seconds before interstitial appearings"), EditorStyles.miniLabel, GUILayout.Width(120));
                    p_interstitialStartDelay.floatValue = EditorGUILayout.FloatField(p_interstitialStartDelay.floatValue, textFieldStyle);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(2);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Min Show Delay (s)", "Delay in seconds between interstitial appearings"), EditorStyles.miniLabel, GUILayout.Width(120));
                    p_interstitialShowingDelay.floatValue = EditorGUILayout.FloatField(p_interstitialShowingDelay.floatValue, textFieldStyle);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                }
                else
                {
                    EditorGUILayout.HelpBox("Auto show interstitial is disabled. Interstitials must be shown manually.", MessageType.Info);
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        // Helper methods for validation
        private List<string> ValidateSerializedObject(SerializedObject targetObject)
        {
            var missing = new List<string>();
            var iterator = targetObject.GetIterator();
            if (iterator.NextVisible(true))
            {
                var end = iterator.GetEndProperty();
                while (!SerializedProperty.EqualContents(iterator, end))
                {
                    if (iterator.name == "m_Script")
                    {
                        if (!iterator.NextVisible(false)) break;
                        continue;
                    }
                    if (iterator.propertyType == SerializedPropertyType.String)
                    {
                        if (string.IsNullOrEmpty(iterator.stringValue)) missing.Add(iterator.displayName);
                    }
                    else if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        if (iterator.objectReferenceValue == null) missing.Add(iterator.displayName);
                    }
                    if (!iterator.NextVisible(false)) break;
                }
            }
            return missing;
        }

        private List<string> ValidateNestedContainer(SerializedProperty containerProp)
        {
            var missing = new List<string>();
            SerializedProperty iterator = containerProp.Copy();
            SerializedProperty endProp = iterator.GetEndProperty();

            if (iterator.NextVisible(true))
            {
                while (!SerializedProperty.EqualContents(iterator, endProp))
                {
                    if (iterator.propertyType == SerializedPropertyType.String)
                    {
                        if (string.IsNullOrEmpty(iterator.stringValue)) missing.Add(iterator.displayName);
                    }
                    else if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        if (iterator.objectReferenceValue == null) missing.Add(iterator.displayName);
                    }
                    if (!iterator.NextVisible(false)) break;
                }
            }
            return missing;
        }
    }
}
#endif