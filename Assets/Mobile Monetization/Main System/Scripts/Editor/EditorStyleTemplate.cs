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

        // Input field textures
        private static Texture2D _texField;
        private static Texture2D _texFieldHover;
        private static Texture2D _texFieldFocused;

        // Toggle checkbox textures
        private static Texture2D _texToggleNormal;
        private static Texture2D _texToggleNormalHover;
        private static Texture2D _texToggleOn;
        private static Texture2D _texToggleOnHover;

        /// <summary>
        /// Initialize semua styles. Dipanggil otomatis saat pertama kali digunakan.
        /// </summary>
        public static void InitializeStyles()
        {
            float scale = EditorGUIUtility.pixelsPerPoint;
            if (scale < 1f) scale = 1f;

            if (_stylesInitialized && _texGray != null && _texField != null && _texToggleNormal != null) return;

            try
            {
                float overlayAlpha = 0.05f;
                bool isDark = EditorGUIUtility.isProSkin;

                // Modern minimalist colors
                Color grayBackgroundColor = isDark ? new Color(0.16f, 0.16f, 0.18f, 1f) : new Color(0.96f, 0.96f, 0.97f, 1f);
                Color grayToggleColor = isDark ? new Color(0.18f, 0.18f, 0.20f, 1f) : new Color(0.92f, 0.92f, 0.94f, 1f);
                Color grayButtonColor = isDark ? new Color(0.20f, 0.20f, 0.22f, 1f) : new Color(0.88f, 0.88f, 0.90f, 1f);
                Color selectedButtonColor = isDark ? new Color(0.23f, 0.45f, 0.85f, 1f) : new Color(0.30f, 0.55f, 0.95f, 1f);
                Color overlayColor = isDark ? new Color(1f, 1f, 1f, overlayAlpha) : new Color(0f, 0f, 0f, overlayAlpha);
                Color grayTextColor = isDark ? new Color(0.85f, 0.85f, 0.85f) : new Color(0.25f, 0.25f, 0.25f);

                // Premium Input Field & Popup Colors
                Color fieldBgColor        = isDark ? new Color(0.20f, 0.20f, 0.22f, 1f) : new Color(0.98f, 0.98f, 0.98f, 1f);
                Color fieldBorderColor    = isDark ? new Color(0.35f, 0.35f, 0.40f, 1f) : new Color(0.70f, 0.70f, 0.73f, 1f);

                Color fieldHoverBgColor   = isDark ? new Color(0.22f, 0.22f, 0.25f, 1f) : new Color(0.95f, 0.95f, 0.97f, 1f);
                Color fieldHoverBorderColor = isDark ? new Color(0.45f, 0.45f, 0.52f, 1f) : new Color(0.55f, 0.55f, 0.60f, 1f);

                Color fieldFocusBgColor   = isDark ? new Color(0.20f, 0.21f, 0.25f, 1f) : new Color(0.98f, 0.98f, 1.0f, 1f);
                Color fieldFocusBorderColor = isDark ? new Color(0.23f, 0.45f, 0.85f, 1f) : new Color(0.30f, 0.55f, 0.95f, 1f);

                // Create main textures
                _texGray = MakeTex(2, 2, grayBackgroundColor);
                _texToggle = MakeTex(2, 2, grayToggleColor);
                _texButton = MakeTex(2, 2, grayButtonColor);
                _texButtonActive = MakeTex(2, 2, grayButtonColor * 0.85f);
                _texSelectedButton = MakeTex(2, 2, selectedButtonColor);
                _texSelectedButtonActive = MakeTex(2, 2, selectedButtonColor * 0.85f);
                _texOverlay = MakeTex(2, 2, overlayColor);

                _texField = MakeTex(2, 2, fieldBgColor);
                _texFieldHover = MakeTex(2, 2, fieldHoverBgColor);
                _texFieldFocused = MakeTex(2, 2, fieldFocusBgColor);

                // Create custom high-contrast toggle checkbox textures
                _texToggleNormal = MakeToggleTex(false, false, isDark);
                _texToggleNormalHover = MakeToggleTex(false, true, isDark);
                _texToggleOn = MakeToggleTex(true, false, isDark);
                _texToggleOnHover = MakeToggleTex(true, true, isDark);

                // Gray text styles
                _grayTextStyle = new GUIStyle(EditorStyles.label);
                _grayTextStyle.normal.textColor = grayTextColor;

                _grayMiniLabelStyle = new GUIStyle(EditorStyles.miniLabel);
                _grayMiniLabelStyle.normal.textColor = grayTextColor;

                _grayBoldLabelStyle = new GUIStyle(EditorStyles.boldLabel);
                _grayBoldLabelStyle.normal.textColor = grayTextColor;

#if UNITY_2019_3_OR_NEWER
                _grayFoldoutHeaderStyle = new GUIStyle(EditorStyles.foldoutHeader);
#else
                _grayFoldoutHeaderStyle = new GUIStyle(EditorStyles.foldout);
                _grayFoldoutHeaderStyle.fontStyle = FontStyle.Bold;
#endif
                _grayFoldoutHeaderStyle.normal.textColor = grayTextColor;
                _grayFoldoutHeaderStyle.onNormal.textColor = grayTextColor;
                _grayFoldoutHeaderStyle.onActive.textColor = grayTextColor;
                _grayFoldoutHeaderStyle.onFocused.textColor = grayTextColor;
                _grayFoldoutHeaderStyle.onHover.textColor = grayTextColor;

                // Text field — apply distinct flat solid field background for crisp responsive scaling
                _grayTextFieldBackgroundStyle = new GUIStyle(EditorStyles.textField ?? new GUIStyle());
                _grayTextFieldBackgroundStyle.normal.background   = _texField;
                _grayTextFieldBackgroundStyle.hover.background    = _texFieldHover;
                _grayTextFieldBackgroundStyle.focused.background  = _texFieldFocused;
                _grayTextFieldBackgroundStyle.active.background   = _texFieldFocused;
                _grayTextFieldBackgroundStyle.normal.textColor    = grayTextColor;
                _grayTextFieldBackgroundStyle.focused.textColor   = isDark ? Color.white : Color.black;
                _grayTextFieldBackgroundStyle.border              = new RectOffset(0, 0, 0, 0);
                _grayTextFieldBackgroundStyle.padding             = new RectOffset(4, 4, 3, 3);

                // Popup — restore native Unity popup styling to preserve the dropdown arrow and native border
                _grayPopupBackgroundStyle = new GUIStyle(EditorStyles.popup ?? new GUIStyle());
                _grayPopupBackgroundStyle.normal.textColor   = grayTextColor;

                // Toggle — Apply custom high-contrast checkbox graphics and size limits for maximum visibility
                _grayToggleBackgroundStyle = new GUIStyle(EditorStyles.toggle ?? new GUIStyle());
                _grayToggleBackgroundStyle.normal.background = _texToggleNormal;
                _grayToggleBackgroundStyle.onNormal.background = _texToggleOn;
                _grayToggleBackgroundStyle.hover.background = _texToggleNormalHover;
                _grayToggleBackgroundStyle.onHover.background = _texToggleOnHover;
                _grayToggleBackgroundStyle.active.background = _texToggleNormalHover;
                _grayToggleBackgroundStyle.onActive.background = _texToggleOnHover;
                _grayToggleBackgroundStyle.focused.background = _texToggleNormal;
                _grayToggleBackgroundStyle.onFocused.background = _texToggleOn;
                _grayToggleBackgroundStyle.fixedWidth = 16f;
                _grayToggleBackgroundStyle.fixedHeight = 16f;
                _grayToggleBackgroundStyle.border = new RectOffset(0, 0, 0, 0);
                _grayToggleBackgroundStyle.margin = new RectOffset(4, 4, 4, 4);
                _grayToggleBackgroundStyle.padding = new RectOffset(0, 0, 0, 0);
                _grayToggleBackgroundStyle.normal.textColor = grayTextColor;
                _grayToggleBackgroundStyle.onNormal.textColor = grayTextColor;

                // Button style
                _grayButtonStyle = new GUIStyle();
                _grayButtonStyle.normal.background = _texButton;
                _grayButtonStyle.hover.background = _texOverlay;
                _grayButtonStyle.active.background = _texButtonActive;
                _grayButtonStyle.onNormal.background = _texButton;
                _grayButtonStyle.onHover.background = _texOverlay;
                _grayButtonStyle.onActive.background = _texButtonActive;
                _grayButtonStyle.normal.textColor = grayTextColor;
                _grayButtonStyle.alignment = TextAnchor.MiddleCenter;
                _grayButtonStyle.padding = new RectOffset(4, 4, 4, 4);

                // Toggle button style (for selected state)
                _grayToggleButtonStyle = new GUIStyle();
                _grayToggleButtonStyle.normal.background = _texButton;
                _grayToggleButtonStyle.active.background = _texButtonActive;
                _grayToggleButtonStyle.onNormal.background = _texSelectedButton;
                _grayToggleButtonStyle.onActive.background = _texSelectedButtonActive;
                _grayToggleButtonStyle.normal.textColor = grayTextColor;
                _grayToggleButtonStyle.onNormal.textColor = Color.white;
                _grayToggleButtonStyle.alignment = TextAnchor.MiddleCenter;
                _grayToggleButtonStyle.padding = new RectOffset(4, 4, 4, 4);

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
#if UNITY_2019_3_OR_NEWER
                _grayFoldoutHeaderStyle = EditorStyles.foldoutHeader;
#else
                _grayFoldoutHeaderStyle = EditorStyles.foldout;
#endif
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
            result.hideFlags = HideFlags.HideAndDontSave;
            result.SetPixels(pix);
            result.Apply();
            return result;
        }


        private static Texture2D MakeToggleTex(bool checkedState, bool hoverState, bool isDark)
        {
            float scale = EditorGUIUtility.pixelsPerPoint;
            if (scale < 1f) scale = 1f;

            int size = Mathf.RoundToInt(16 * scale);
            Color[] pix = new Color[size * size];

            Color bg = isDark ? new Color(0.20f, 0.20f, 0.22f, 1f) : new Color(0.98f, 0.98f, 0.98f, 1f);
            Color border = isDark ? new Color(0.35f, 0.35f, 0.40f, 1f) : new Color(0.70f, 0.70f, 0.73f, 1f);
            Color checkColor = isDark ? new Color(0.23f, 0.45f, 0.85f, 1f) : new Color(0.30f, 0.55f, 0.95f, 1f);

            if (hoverState)
            {
                bg = isDark ? new Color(0.22f, 0.22f, 0.25f, 1f) : new Color(0.95f, 0.95f, 0.97f, 1f);
                border = isDark ? new Color(0.45f, 0.45f, 0.52f, 1f) : new Color(0.55f, 0.55f, 0.60f, 1f);
            }

            if (checkedState)
            {
                bg = checkColor;
                border = hoverState
                    ? (isDark ? new Color(0.40f, 0.60f, 1.0f, 1f) : new Color(0.40f, 0.65f, 0.98f, 1f))
                    : (isDark ? new Color(0.30f, 0.50f, 0.90f, 1f) : new Color(0.30f, 0.55f, 0.95f, 1f));
            }

            int borderWidth = Mathf.Max(1, Mathf.RoundToInt(1 * scale));

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (x < borderWidth || x >= size - borderWidth || y < borderWidth || y >= size - borderWidth)
                    {
                        pix[y * size + x] = border;
                    }
                    else
                    {
                        if (checkedState)
                        {
                            // Beautiful programmatically drawn checkmark pattern, scaled dynamically
                            bool isCheck = false;

                            float checkX = x / scale;
                            float checkY = y / scale;

                            // Left leg: steep line going down-right (from y=10 down to y=5)
                            if (checkX >= 4f && checkX <= 6f && Mathf.Abs(2f * checkX + checkY - 17f) < 1.2f)
                            {
                                isCheck = true;
                            }
                            // Right leg: 45 degree line going up-right (from y=5 up to y=11)
                            else if (checkX >= 6f && checkX <= 12f && Mathf.Abs(checkY - checkX - (-1.5f)) < 0.8f)
                            {
                                isCheck = true;
                            }
                            // Thicken right leg by 1 pixel for visual balance
                            else if (checkX >= 6f && checkX <= 12f && Mathf.Abs(checkY - checkX - (-0.5f)) < 0.8f)
                            {
                                isCheck = true;
                            }

                            pix[y * size + x] = isCheck ? Color.white : bg;
                        }
                        else
                        {
                            pix[y * size + x] = bg;
                        }
                    }
                }
            }

            Texture2D result = new Texture2D(size, size, TextureFormat.ARGB32, false);
            result.hideFlags = HideFlags.HideAndDontSave;
            result.SetPixels(pix);
            result.Apply();
            return result;
        }




        // Public getters untuk styles - dengan validasi instan terhadap destruksi native object
        public static GUIStyle GrayTextStyle
        {
            get
            {
                if (!_stylesInitialized || _grayTextStyle == null || _texGray == null) InitializeStyles();
                return _grayTextStyle;
            }
        }

        public static GUIStyle GrayMiniLabelStyle
        {
            get
            {
                if (!_stylesInitialized || _grayMiniLabelStyle == null || _texGray == null) InitializeStyles();
                return _grayMiniLabelStyle;
            }
        }

        public static GUIStyle GrayBoldLabelStyle
        {
            get
            {
                if (!_stylesInitialized || _grayBoldLabelStyle == null || _texGray == null) InitializeStyles();
                return _grayBoldLabelStyle;
            }
        }

        public static GUIStyle GrayFoldoutHeaderStyle
        {
            get
            {
                if (!_stylesInitialized || _grayFoldoutHeaderStyle == null || _texGray == null) InitializeStyles();
                return _grayFoldoutHeaderStyle;
            }
        }

        public static GUIStyle GrayTextFieldBackgroundStyle
        {
            get
            {
                if (!_stylesInitialized || _grayTextFieldBackgroundStyle == null || _texField == null) InitializeStyles();
                return _grayTextFieldBackgroundStyle;
            }
        }

        public static GUIStyle GrayPopupBackgroundStyle
        {
            get
            {
                if (!_stylesInitialized || _grayPopupBackgroundStyle == null || _texGray == null) InitializeStyles();
                return _grayPopupBackgroundStyle;
            }
        }

        public static GUIStyle GrayToggleBackgroundStyle
        {
            get
            {
                if (!_stylesInitialized || _grayToggleBackgroundStyle == null || _texToggleNormal == null) InitializeStyles();
                return _grayToggleBackgroundStyle;
            }
        }

        public static GUIStyle GrayButtonStyle
        {
            get
            {
                if (!_stylesInitialized || _grayButtonStyle == null || _texButton == null) InitializeStyles();
                return _grayButtonStyle;
            }
        }

        public static GUIStyle GrayToggleButtonStyle
        {
            get
            {
                if (!_stylesInitialized || _grayToggleButtonStyle == null || _texButton == null) InitializeStyles();
                return _grayToggleButtonStyle;
            }
        }

        public static GUIStyle GrayFieldBackgroundStyle
        {
            get
            {
                if (!_stylesInitialized || _grayFieldBackgroundStyle == null || _texGray == null) InitializeStyles();
                return _grayFieldBackgroundStyle;
            }
        }

        /// <summary>
        /// Buat button style dengan warna custom
        /// </summary>
        public static GUIStyle CreateButtonStyle(Color normalColor, Color? activeColor = null, int height = 20)
        {
            if (!_stylesInitialized) InitializeStyles();

            var style = new GUIStyle()
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

            var style = new GUIStyle()
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

