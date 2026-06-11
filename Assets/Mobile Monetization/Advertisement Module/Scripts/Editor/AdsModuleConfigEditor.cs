#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using MobileCore.MainModule.Editor;

namespace MobileCore.Advertisements.Editor
{
    [CustomEditor(typeof(AdsModuleConfig))]
    public class AdsModuleConfigEditor : UnityEditor.Editor
    {
        private AdsModuleConfig _config;

        // Inline editor for the embedded AdsSettings sub-asset
        private UnityEditor.Editor _settingsEditor;

        private void OnEnable()
        {
            _config = (AdsModuleConfig)target;

            // Auto-create the embedded sub-asset if missing
            EnsureSubAsset();
            RebuildSettingsEditor();
        }

        private void OnDisable()
        {
            DestroySettingsEditor();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Header
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("ADVERTISEMENT MODULE CONFIG", EditorStyleTemplate.GrayBoldLabelStyle);
            EditorGUILayout.HelpBox(
                "AdsSettings is embedded inside this asset as a sub-asset.\n" +
                "Configure all ad provider settings directly below.",
                MessageType.Info);
            EditorGUILayout.EndVertical();

            GUILayout.Space(8f);

            // Module Enabled toggle (from base MobileModule)
            SerializedProperty enabledProp = serializedObject.FindProperty("moduleEnabled");
            bool isEnabled = true;
            if (enabledProp != null)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Module Enabled", EditorStyleTemplate.GrayTextStyle);
                enabledProp.boolValue = EditorGUILayout.Toggle(enabledProp.boolValue, GUILayout.Width(20f));
                EditorGUILayout.EndHorizontal();
                isEnabled = enabledProp.boolValue;
                GUILayout.Space(6f);
            }

            if (!isEnabled)
            {
                EditorGUILayout.HelpBox("This module is disabled and will not be initialized at runtime.", MessageType.Warning);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            // Inline AdsSettings editor
            if (_config.Settings != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("ADS SETTINGS", EditorStyleTemplate.GrayBoldLabelStyle);

                var divRect = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(divRect, EditorGUIUtility.isProSkin
                    ? new Color(0.32f, 0.32f, 0.35f) : new Color(0.68f, 0.68f, 0.70f));
                GUILayout.Space(6f);

                if (_settingsEditor == null)
                    RebuildSettingsEditor();

                if (_settingsEditor != null)
                    _settingsEditor.OnInspectorGUI();

                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "AdsSettings sub-asset not found inside this asset.\nClick below to repair.",
                    MessageType.Error);

                if (EditorStyleTemplate.DrawButton("Repair Sub-Asset",
                    new Color(0.7f, 0.2f, 0.2f),
                    new GUILayoutOption[] { GUILayout.Height(24f) }))
                {
                    _config.CreateEmbeddedSettings();
                    RebuildSettingsEditor();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        // ── Helpers ──────────────────────────────────────────────────────────────────

        private void EnsureSubAsset()
        {
            if (_config == null) return;
            if (_config.Settings == null && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(_config)))
            {
                _config.CreateEmbeddedSettings();
            }
        }

        private void RebuildSettingsEditor()
        {
            DestroySettingsEditor();
            if (_config != null && _config.Settings != null)
                _settingsEditor = CreateEditor(_config.Settings);
        }

        private void DestroySettingsEditor()
        {
            if (_settingsEditor != null)
            {
                DestroyImmediate(_settingsEditor);
                _settingsEditor = null;
            }
        }
    }
}
#endif
