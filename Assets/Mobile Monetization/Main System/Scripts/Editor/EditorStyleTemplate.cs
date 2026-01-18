#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MobileCore.MainModule.Editor
{
    /// <summary>
    /// Template style untuk custom editor yang konsisten di seluruh Mobile Core modules.
    /// Gunakan class ini untuk mendapatkan style yang seragam.
    /// </summary>
    public static class EditorStyleTemplate
    {
        private static bool _stylesInitialized = false;

        // Gray text styles
        private static GUIStyle _grayTextStyle;
        private static GUIStyle _grayMiniLabelStyle;
        private static GUIStyle _grayBoldLabelStyle;
        private static GUIStyle _grayFoldoutHeaderStyle;

        // Background styles
        private static GUIStyle _grayTextFieldBackgroundStyle;
        private static GUIStyle _grayPopupBackgroundStyle;
        private static GUIStyle _grayToggleBackgroundStyle;
        private static GUIStyle _grayButtonStyle;
        private static GUIStyle _grayToggleButtonStyle;
        private static GUIStyle _grayFieldBackgroundStyle;

        // Button colors
        private static Texture2D _texGray;
        private static Texture2D _texToggle;
        private static Texture2D _texButton;
        private static Texture2D _texButtonActive;
        private static Texture2D _texSelectedButton;
        private static Texture2D _texSelectedButtonActive;
        private static Texture2D _texOverlay;

        /// <summary>
        /// Initialize semua styles. Dipanggil otomatis saat pertama kali digunakan.
        /// </summary>
        public static void InitializeStyles()
        {
            if (_stylesInitialized) return;

            try
            {
                float overlayAlpha = 0.08f;
                bool isDark = EditorGUIUtility.isProSkin;

                // Define colors
                Color grayBackgroundColor = isDark ? new Color(0.12f, 0.12f, 0.12f, 1f) : new Color(0.95f, 0.95f, 0.95f, 1f);
                Color grayToggleColor = isDark ? new Color(0.15f, 0.15f, 0.15f, 1f) : new Color(0.9f, 0.9f, 0.9f, 1f);
                Color grayButtonColor = isDark ? new Color(0.18f, 0.18f, 0.18f, 1f) : new Color(0.85f, 0.85f, 0.85f, 1f);
                Color selectedButtonColor = isDark ? new Color(0.3f, 0.5f, 0.9f, 1f) : new Color(0.7f, 0.7f, 0.9f, 1f);
                Color overlayColor = isDark ? new Color(1f, 1f, 1f, overlayAlpha) : new Color(0f, 0f, 0f, overlayAlpha);
                Color grayTextColor = new Color(0.7f, 0.7f, 0.7f);

                // Create textures
                _texGray = MakeTex(2, 2, grayBackgroundColor);
                _texToggle = MakeTex(2, 2, grayToggleColor);
                _texButton = MakeTex(2, 2, grayButtonColor);
                _texButtonActive = MakeTex(2, 2, grayButtonColor * 0.9f);
                _texSelectedButton = MakeTex(2, 2, selectedButtonColor);
                _texSelectedButtonActive = MakeTex(2, 2, selectedButtonColor * 0.9f);
                _texOverlay = MakeTex(2, 2, overlayColor);

                // Gray text styles
                _grayTextStyle = new GUIStyle(EditorStyles.label);
                _grayTextStyle.normal.textColor = grayTextColor;

                _grayMiniLabelStyle = new GUIStyle(EditorStyles.miniLabel);
                _grayMiniLabelStyle.normal.textColor = grayTextColor;

                _grayBoldLabelStyle = new GUIStyle(EditorStyles.boldLabel);
                _grayBoldLabelStyle.normal.textColor = grayTextColor;

                _grayFoldoutHeaderStyle = new GUIStyle(EditorStyles.foldoutHeader);
                _grayFoldoutHeaderStyle.normal.textColor = grayTextColor;
                _grayFoldoutHeaderStyle.onNormal.textColor = grayTextColor;
                _grayFoldoutHeaderStyle.onActive.textColor = grayTextColor;
                _grayFoldoutHeaderStyle.onFocused.textColor = grayTextColor;
                _grayFoldoutHeaderStyle.onHover.textColor = grayTextColor;

                // Text field background style
                _grayTextFieldBackgroundStyle = new GUIStyle(EditorStyles.textField ?? new GUIStyle());
                _grayTextFieldBackgroundStyle.normal.background = _texGray;
                _grayTextFieldBackgroundStyle.hover.background = _texOverlay;
                _grayTextFieldBackgroundStyle.active.background = _texOverlay;
                _grayTextFieldBackgroundStyle.onNormal.background = _texGray;
                _grayTextFieldBackgroundStyle.padding = new RectOffset(4, 4, 4, 4);

                // Popup background style
                _grayPopupBackgroundStyle = new GUIStyle(EditorStyles.popup ?? new GUIStyle());
                _grayPopupBackgroundStyle.normal.background = _texGray;

                // Toggle background style
                _grayToggleBackgroundStyle = new GUIStyle(EditorStyles.toggle ?? new GUIStyle());
                _grayToggleBackgroundStyle.normal.background = _texToggle;
                _grayToggleBackgroundStyle.onNormal.background = _texToggle;

                // Button style
                _grayButtonStyle = new GUIStyle(EditorStyles.miniButton ?? new GUIStyle());
                _grayButtonStyle.normal.background = _texButton;
                _grayButtonStyle.hover.background = _texOverlay;
                _grayButtonStyle.active.background = _texButtonActive;
                _grayButtonStyle.onNormal.background = _texButton;
                _grayButtonStyle.onHover.background = _texOverlay;
                _grayButtonStyle.onActive.background = _texButtonActive;
                _grayButtonStyle.padding = new RectOffset(4, 4, 2, 2);

                // Toggle button style (for selected state)
                _grayToggleButtonStyle = new GUIStyle(EditorStyles.miniButton ?? new GUIStyle());
                _grayToggleButtonStyle.normal.background = _texButton;
                _grayToggleButtonStyle.active.background = _texButtonActive;
                _grayToggleButtonStyle.onNormal.background = _texSelectedButton;
                _grayToggleButtonStyle.onActive.background = _texSelectedButtonActive;

                // Field background style
                _grayFieldBackgroundStyle = new GUIStyle(EditorStyles.label ?? new GUIStyle());

                _stylesInitialized = true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to initialize EditorStyleTemplate: {e.Message}");
                // Fallback to default styles
                _grayTextStyle = EditorStyles.label;
                _grayMiniLabelStyle = EditorStyles.miniLabel;
                _grayBoldLabelStyle = EditorStyles.boldLabel;
                _grayFoldoutHeaderStyle = EditorStyles.foldoutHeader;
                _grayTextFieldBackgroundStyle = EditorStyles.textField ?? new GUIStyle();
                _grayPopupBackgroundStyle = EditorStyles.popup ?? new GUIStyle();
                _grayToggleBackgroundStyle = EditorStyles.toggle ?? new GUIStyle();
                _grayButtonStyle = EditorStyles.miniButton ?? new GUIStyle();
                _grayToggleButtonStyle = EditorStyles.miniButton ?? new GUIStyle();
                _grayFieldBackgroundStyle = EditorStyles.label ?? new GUIStyle();
                _stylesInitialized = true;
            }
        }

        private static Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            Texture2D result = new Texture2D(width, height, TextureFormat.ARGB32, false);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        // Public getters untuk styles
        public static GUIStyle GrayTextStyle
        {
            get
            {
                if (!_stylesInitialized) InitializeStyles();
                return _grayTextStyle;
            }
        }

        public static GUIStyle GrayMiniLabelStyle
        {
            get
            {
                if (!_stylesInitialized) InitializeStyles();
                return _grayMiniLabelStyle;
            }
        }

        public static GUIStyle GrayBoldLabelStyle
        {
            get
            {
                if (!_stylesInitialized) InitializeStyles();
                return _grayBoldLabelStyle;
            }
        }

        public static GUIStyle GrayFoldoutHeaderStyle
        {
            get
            {
                if (!_stylesInitialized) InitializeStyles();
                return _grayFoldoutHeaderStyle;
            }
        }

        public static GUIStyle GrayTextFieldBackgroundStyle
        {
            get
            {
                if (!_stylesInitialized) InitializeStyles();
                return _grayTextFieldBackgroundStyle;
            }
        }

        public static GUIStyle GrayPopupBackgroundStyle
        {
            get
            {
                if (!_stylesInitialized) InitializeStyles();
                return _grayPopupBackgroundStyle;
            }
        }

        public static GUIStyle GrayToggleBackgroundStyle
        {
            get
            {
                if (!_stylesInitialized) InitializeStyles();
                return _grayToggleBackgroundStyle;
            }
        }

        public static GUIStyle GrayButtonStyle
        {
            get
            {
                if (!_stylesInitialized) InitializeStyles();
                return _grayButtonStyle;
            }
        }

        public static GUIStyle GrayToggleButtonStyle
        {
            get
            {
                if (!_stylesInitialized) InitializeStyles();
                return _grayToggleButtonStyle;
            }
        }

        public static GUIStyle GrayFieldBackgroundStyle
        {
            get
            {
                if (!_stylesInitialized) InitializeStyles();
                return _grayFieldBackgroundStyle;
            }
        }

        /// <summary>
        /// Buat button style dengan warna custom
        /// </summary>
        public static GUIStyle CreateButtonStyle(Color normalColor, Color? activeColor = null, int height = 20)
        {
            if (!_stylesInitialized) InitializeStyles();

            var style = new GUIStyle(EditorStyles.miniButton)
            {
                fixedHeight = height,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = {
                    textColor = Color.white,
                    background = MakeTex(2, 2, normalColor)
                }
            };

            if (activeColor.HasValue)
            {
                style.active.background = MakeTex(2, 2, activeColor.Value);
            }

            return style;
        }

        /// <summary>
        /// Buat toggle button style dengan warna enabled/disabled
        /// </summary>
        public static GUIStyle CreateToggleButtonStyle(bool isEnabled, int height = 20)
        {
            if (!_stylesInitialized) InitializeStyles();

            var style = new GUIStyle(EditorStyles.miniButton)
            {
                fixedHeight = height,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = {
                    textColor = Color.white
                }
            };

            if (isEnabled)
            {
                style.normal.background = MakeTex(2, 2, new Color(0.2f, 0.8f, 0.3f));
            }
            else
            {
                style.normal.background = MakeTex(2, 2, new Color(0.8f, 0.2f, 0.2f));
            }

            return style;
        }
    }
}
#endif

