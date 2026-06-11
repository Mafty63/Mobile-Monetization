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
        private SerializedProperty _dummyCanvasProp;
        private SerializedProperty _gdprPrefabProp;

        // Inline editor for the embedded AdsSettings sub-asset
        private UnityEditor.Editor _settingsEditor;
        private bool _showPrefabs = true;

        private void OnEnable()
        {
            _config = (AdsModuleConfig)target;
            _dummyCanvasProp = serializedObject.FindProperty("dummyCanvasPrefab");
            _gdprPrefabProp  = serializedObject.FindProperty("gdprPrefab");

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

            // Prefabs section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _showPrefabs = EditorGUILayout.Foldout(_showPrefabs, "PREFABS", true, EditorStyleTemplate.GrayFoldoutHeaderStyle);
            if (_showPrefabs)
            {
                GUILayout.Space(4f);
                EditorGUILayout.PropertyField(_dummyCanvasProp, new GUIContent("Dummy Canvas Prefab",
                    "Canvas prefab used for Dummy ad provider (editor/testing)."));
                EditorGUILayout.PropertyField(_gdprPrefabProp, new GUIContent("GDPR Prefab",
                    "Consent dialog shown on first launch when GDPR is enabled."));
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(8f);

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
