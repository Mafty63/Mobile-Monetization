#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using MobileCore.MainModule.Editor;

namespace MobileCore.IAPModule.Editor
{
    [CustomEditor(typeof(IapModuleConfig))]
    public class IapModuleConfigEditor : UnityEditor.Editor
    {
        private IapModuleConfig _config;

        // Inline editor for the embedded IAPSettings sub-asset
        private UnityEditor.Editor _settingsEditor;

        private void OnEnable()
        {
            _config = (IapModuleConfig)target;
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
            EditorGUILayout.LabelField("IN-APP PURCHASE MODULE CONFIG", EditorStyleTemplate.GrayBoldLabelStyle);
            EditorGUILayout.HelpBox(
                "IAPSettings is embedded inside this asset as a sub-asset.\n" +
                "Configure all IAP products and settings directly below.",
                MessageType.Info);
            EditorGUILayout.EndVertical();

            GUILayout.Space(8f);

            // Module Enabled toggle
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

            // Inline IAPSettings editor
            if (_config.Settings != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("IAP SETTINGS", EditorStyleTemplate.GrayBoldLabelStyle);

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
                    "IAPSettings sub-asset not found inside this asset.\nClick below to repair.",
                    MessageType.Error);

                if (GUILayout.Button("Repair Sub-Asset",
                    EditorStyleTemplate.CreateButtonStyle(new Color(0.7f, 0.2f, 0.2f), null, 24),
                    GUILayout.Height(24f)))
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
