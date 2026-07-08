#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MobileCore.MainModule.Editor
{
    /// <summary>
    /// Template style untuk custom editor yang konsisten di seluruh Mobile Core modules.
    /// Menggunakan EditorGUI.DrawRect untuk menggambar background field solid yang bersih,
    /// terhindar dari resiko bug/stretch texture di resolusi tinggi/DPI retina.
    /// </summary>
    public static class EditorStyleTemplate
    {
        private static bool _stylesInitialized;

        // Label / text styles
        private static GUIStyle _grayTextStyle;
        private static GUIStyle _grayMiniLabelStyle;
        private static GUIStyle _grayBoldLabelStyle;
        private static GUIStyle _grayFoldoutHeaderStyle;

        // Bare styles (tanpa background texture agar transparan di atas DrawRect)
        private static GUIStyle _bareTextFieldStyle;

        // Other controls
        private static GUIStyle _grayPopupStyle;
        private static GUIStyle _grayToggleStyle;
        private static GUIStyle _grayButtonStyle;
        private static GUIStyle _grayToggleButtonStyle;
        private static GUIStyle _grayFieldBackgroundStyle;

        // Colors
        private static Color _fieldBgColor;
        private static Color _fieldBorderColor;
        private static Color _fieldFocusBorderColor;
        private static Color _grayButtonColor;
        private static Color _selectedButtonColor;
        private static Color _grayTextColor;
        private static bool  _isDark;

        // ── Initialization ────────────────────────────────────────────────────────────

        public static void InitializeStyles()
        {
            if (EditorStyles.label == null) return;
            if (_stylesInitialized && _grayTextStyle != null) return;

            try
            {
                _isDark = EditorGUIUtility.isProSkin;

                _grayTextColor = _isDark
                    ? new Color(0.85f, 0.85f, 0.85f)
                    : new Color(0.25f, 0.25f, 0.25f);

                // Field colors: Menggunakan abu-abu premium yang jelas terlihat (tidak tenggelam/hitam pekat)
                _fieldBgColor          = _isDark ? new Color(0.30f, 0.30f, 0.33f) : new Color(0.96f, 0.96f, 0.97f);
                _fieldBorderColor      = _isDark ? new Color(0.42f, 0.42f, 0.45f) : new Color(0.70f, 0.70f, 0.73f);
                _fieldFocusBorderColor = _isDark ? new Color(0.23f, 0.45f, 0.85f) : new Color(0.30f, 0.55f, 0.95f);

                _grayButtonColor     = _isDark ? new Color(0.26f, 0.26f, 0.28f) : new Color(0.82f, 0.82f, 0.84f);
                _selectedButtonColor = _isDark ? new Color(0.23f, 0.45f, 0.85f) : new Color(0.30f, 0.55f, 0.95f);

                // ── Label styles ──────────────────────────────────────────────────────

                _grayTextStyle = new GUIStyle(EditorStyles.label);
                _grayTextStyle.normal.textColor = _grayTextColor;

                _grayMiniLabelStyle = new GUIStyle(EditorStyles.miniLabel);
                _grayMiniLabelStyle.normal.textColor = _grayTextColor;

                _grayBoldLabelStyle = new GUIStyle(EditorStyles.boldLabel);
                _grayBoldLabelStyle.normal.textColor = _grayTextColor;

#if UNITY_2019_3_OR_NEWER
                _grayFoldoutHeaderStyle = new GUIStyle(EditorStyles.foldoutHeader);
#else
                _grayFoldoutHeaderStyle = new GUIStyle(EditorStyles.foldout);
                _grayFoldoutHeaderStyle.fontStyle = FontStyle.Bold;
#endif
                _grayFoldoutHeaderStyle.normal.textColor    = _grayTextColor;
                _grayFoldoutHeaderStyle.onNormal.textColor  = _grayTextColor;
                _grayFoldoutHeaderStyle.onActive.textColor  = _grayTextColor;
                _grayFoldoutHeaderStyle.onFocused.textColor = _grayTextColor;
                _grayFoldoutHeaderStyle.onHover.textColor   = _grayTextColor;

                // ── Bare TextField Style (100% Transparan menggunakan GUIStyle.none) ──

                _bareTextFieldStyle = new GUIStyle(GUIStyle.none);
                _bareTextFieldStyle.normal.textColor    = _grayTextColor;
                _bareTextFieldStyle.focused.textColor   = _isDark ? Color.white : Color.black;
                _bareTextFieldStyle.border              = new RectOffset(0, 0, 0, 0);
                _bareTextFieldStyle.padding             = new RectOffset(5, 5, 2, 2);
                _bareTextFieldStyle.alignment           = TextAnchor.MiddleLeft;

                // ── Other controls ────────────────────────────────────────────────────

                _grayPopupStyle = new GUIStyle(EditorStyles.popup);
                _grayPopupStyle.normal.textColor = _grayTextColor;

                _grayToggleStyle = new GUIStyle(EditorStyles.toggle);
                _grayToggleStyle.normal.textColor   = _grayTextColor;
                _grayToggleStyle.onNormal.textColor = _grayTextColor;

                _grayButtonStyle = new GUIStyle(GUI.skin.button);
                _grayButtonStyle.normal.textColor = _grayTextColor;
                _grayButtonStyle.alignment        = TextAnchor.MiddleCenter;
                _grayButtonStyle.padding          = new RectOffset(4, 4, 4, 4);

                _grayToggleButtonStyle = new GUIStyle(GUI.skin.button);
                _grayToggleButtonStyle.normal.textColor   = _grayTextColor;
                _grayToggleButtonStyle.onNormal.textColor = Color.white;
                _grayToggleButtonStyle.alignment          = TextAnchor.MiddleCenter;
                _grayToggleButtonStyle.padding            = new RectOffset(4, 4, 4, 4);

                _grayFieldBackgroundStyle = new GUIStyle(EditorStyles.label);

                _stylesInitialized = true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"EditorStyleTemplate: gagal inisialisasi — fallback ke default Unity. ({e.Message})");

                _grayTextStyle            = EditorStyles.label;
                _grayMiniLabelStyle       = EditorStyles.miniLabel;
                _grayBoldLabelStyle       = EditorStyles.boldLabel;
#if UNITY_2019_3_OR_NEWER
                _grayFoldoutHeaderStyle   = EditorStyles.foldoutHeader;
#else
                _grayFoldoutHeaderStyle   = EditorStyles.foldout;
#endif
                _bareTextFieldStyle       = EditorStyles.textField;
                _grayPopupStyle           = EditorStyles.popup;
                _grayToggleStyle          = EditorStyles.toggle;
                _grayButtonStyle          = GUI.skin.button;
                _grayToggleButtonStyle    = GUI.skin.button;
                _grayFieldBackgroundStyle = EditorStyles.label;

                _fieldBgColor          = new Color(0.25f, 0.25f, 0.28f);
                _fieldBorderColor      = new Color(0.38f, 0.38f, 0.42f);
                _fieldFocusBorderColor = new Color(0.23f, 0.45f, 0.85f);
                _grayButtonColor       = new Color(0.26f, 0.26f, 0.28f);
                _selectedButtonColor   = new Color(0.23f, 0.45f, 0.85f);
                _grayTextColor         = Color.white;

                _stylesInitialized = true;
            }
        }

        // ── Public style getters ───────────────────────────────────────────────────────

        public static GUIStyle GrayTextStyle
        {
            get { if (!_stylesInitialized || _grayTextStyle == null) InitializeStyles(); return _grayTextStyle; }
        }

        public static GUIStyle GrayMiniLabelStyle
        {
            get { if (!_stylesInitialized || _grayMiniLabelStyle == null) InitializeStyles(); return _grayMiniLabelStyle; }
        }

        public static GUIStyle GrayBoldLabelStyle
        {
            get { if (!_stylesInitialized || _grayBoldLabelStyle == null) InitializeStyles(); return _grayBoldLabelStyle; }
        }

        public static GUIStyle GrayFoldoutHeaderStyle
        {
            get { if (!_stylesInitialized || _grayFoldoutHeaderStyle == null) InitializeStyles(); return _grayFoldoutHeaderStyle; }
        }

        public static GUIStyle GrayTextFieldBackgroundStyle
        {
            get { return EditorStyles.textField; }
        }

        public static GUIStyle GrayPopupBackgroundStyle
        {
            get { if (!_stylesInitialized || _grayPopupStyle == null) InitializeStyles(); return _grayPopupStyle; }
        }

        public static GUIStyle GrayToggleBackgroundStyle
        {
            get { return EditorStyles.toggle; }
        }

        public static GUIStyle GrayButtonStyle
        {
            get { if (!_stylesInitialized || _grayButtonStyle == null) InitializeStyles(); return _grayButtonStyle; }
        }

        public static GUIStyle GrayToggleButtonStyle
        {
            get { if (!_stylesInitialized || _grayToggleButtonStyle == null) InitializeStyles(); return _grayToggleButtonStyle; }
        }

        public static GUIStyle GrayFieldBackgroundStyle
        {
            get { if (!_stylesInitialized || _grayFieldBackgroundStyle == null) InitializeStyles(); return _grayFieldBackgroundStyle; }
        }

        // ── Draw helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Menggambar text field solid via EditorGUI.DrawRect.
        /// </summary>
        public static string DrawColoredTextField(Rect rect, string value)
        {
            if (!_stylesInitialized) InitializeStyles();

            int controlId = GUIUtility.GetControlID(FocusType.Keyboard, rect);
            bool isFocused = GUIUtility.keyboardControl == controlId;

            Color border = isFocused ? _fieldFocusBorderColor : _fieldBorderColor;

            EditorGUI.DrawRect(rect, border);
            EditorGUI.DrawRect(new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2), _fieldBgColor);

            return EditorGUI.TextField(rect, value, _bareTextFieldStyle);
        }

        /// <summary>
        /// Menggambar integer field solid via EditorGUI.DrawRect.
        /// </summary>
        public static int DrawColoredIntField(Rect rect, int value)
        {
            if (!_stylesInitialized) InitializeStyles();

            int controlId = GUIUtility.GetControlID(FocusType.Keyboard, rect);
            bool isFocused = GUIUtility.keyboardControl == controlId;

            Color border = isFocused ? _fieldFocusBorderColor : _fieldBorderColor;

            EditorGUI.DrawRect(rect, border);
            EditorGUI.DrawRect(new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2), _fieldBgColor);

            return EditorGUI.IntField(rect, value, _bareTextFieldStyle);
        }

        /// <summary>
        /// Menggambar float field solid via EditorGUI.DrawRect.
        /// </summary>
        public static float DrawColoredFloatField(Rect rect, float value)
        {
            if (!_stylesInitialized) InitializeStyles();

            int controlId = GUIUtility.GetControlID(FocusType.Keyboard, rect);
            bool isFocused = GUIUtility.keyboardControl == controlId;

            Color border = isFocused ? _fieldFocusBorderColor : _fieldBorderColor;

            EditorGUI.DrawRect(rect, border);
            EditorGUI.DrawRect(new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2), _fieldBgColor);

            return EditorGUI.FloatField(rect, value, _bareTextFieldStyle);
        }

        /// <summary>
        /// Menggambar toggle checkbox solid kustom via EditorGUI.DrawRect (tanpa texture, pixel-perfect).
        /// Saat bernilai true, box diisi warna biru aksen dengan tanda centang putih '✓'.
        /// </summary>
        public static bool DrawStyledToggle(Rect rect, bool value)
        {
            if (!_stylesInitialized) InitializeStyles();

            float size = 14f;
            Rect boxRect = new Rect(
                rect.x + (rect.width - size) / 2f,
                rect.y + (rect.height - size) / 2f,
                size,
                size
            );

            Color border = value ? _fieldFocusBorderColor : _fieldBorderColor;
            Color bg = value ? _fieldFocusBorderColor : _fieldBgColor;

            EditorGUI.DrawRect(boxRect, border);
            EditorGUI.DrawRect(new Rect(boxRect.x + 1, boxRect.y + 1, boxRect.width - 2, boxRect.height - 2), bg);

            if (value)
            {
                GUIStyle checkStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white },
                    fontSize = 10
                };
                GUI.Label(boxRect, "✓", checkStyle);
            }

            // Area klik diperluas ke seluruh lebar baris jika diinginkan, tapi di sini dibatasi ke box
            if (GUI.Button(boxRect, GUIContent.none, GUIStyle.none))
            {
                value = !value;
                GUI.changed = true;
            }

            return value;
        }

        /// <summary>
        /// Menggambar toggle checkbox solid kustom dengan layout otomatis.
        /// </summary>
        public static bool DrawStyledToggle(bool value, GUILayoutOption[] options = null)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, GUILayout.Width(20f));
            return DrawStyledToggle(rect, value);
        }

        /// <summary>
        /// Drop-in replacement untuk EditorGUILayout.PropertyField yang menggambar
        /// string, integer, float, dan boolean field dengan background solid via EditorGUI.DrawRect.
        /// Tipe property lain diteruskan ke PropertyField bawaan Unity.
        /// </summary>
        public static void DrawStyledPropertyField(SerializedProperty property, GUIContent label = null, bool includeChildren = false)
        {
            if (!_stylesInitialized) InitializeStyles();

            if (property.propertyType == SerializedPropertyType.String ||
                property.propertyType == SerializedPropertyType.Integer ||
                property.propertyType == SerializedPropertyType.Float)
            {
                Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

                EditorGUI.BeginChangeCheck();
                if (property.propertyType == SerializedPropertyType.String)
                {
                    string newVal = DrawColoredTextField(rect, property.stringValue);
                    if (EditorGUI.EndChangeCheck()) property.stringValue = newVal;
                }
                else if (property.propertyType == SerializedPropertyType.Integer)
                {
                    int newVal = DrawColoredIntField(rect, property.intValue);
                    if (EditorGUI.EndChangeCheck()) property.intValue = newVal;
                }
                else if (property.propertyType == SerializedPropertyType.Float)
                {
                    float newVal = DrawColoredFloatField(rect, property.floatValue);
                    if (EditorGUI.EndChangeCheck()) property.floatValue = newVal;
                }
            }
            else if (property.propertyType == SerializedPropertyType.Boolean)
            {
                Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, GUILayout.Width(20f));
                EditorGUI.BeginChangeCheck();
                bool newVal = DrawStyledToggle(rect, property.boolValue);
                if (EditorGUI.EndChangeCheck()) property.boolValue = newVal;
            }
            else
            {
                EditorGUILayout.PropertyField(property, label ?? GUIContent.none, includeChildren);
            }
        }

        // ── Other Draw helpers ─────────────────────────────────────────────────────────

        public static bool DrawButton(string label, Color color, GUILayoutOption[] options = null)
        {
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = color;

            GUIStyle style = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            style.normal.textColor = Color.white;
            style.hover.textColor  = Color.white;
            style.active.textColor = Color.white;

            bool clicked = GUILayout.Button(label, style, options ?? new GUILayoutOption[0]);
            GUI.backgroundColor = prev;
            return clicked;
        }

        public static bool DrawToggleButton(bool isEnabled, GUILayoutOption[] options = null)
        {
            Color color = isEnabled
                ? new Color(0.75f, 0.22f, 0.22f)
                : new Color(0.20f, 0.55f, 0.28f);
            string btnLabel = isEnabled ? "DISABLE" : "ENABLE";
            return DrawButton(btnLabel, color, options);
        }

        public static bool DrawGrayButton(string label, GUILayoutOption[] options = null)
        {
            if (!_stylesInitialized) InitializeStyles();

            Color textColor = _isDark
                ? new Color(0.85f, 0.85f, 0.85f)
                : new Color(0.25f, 0.25f, 0.25f);

            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = _grayButtonColor;

            GUIStyle style = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = textColor;

            bool clicked = GUILayout.Button(label, style, options ?? new GUILayoutOption[0]);
            GUI.backgroundColor = prev;
            return clicked;
        }

        public static void DrawStatusLabel(string label, bool enabled, GUILayoutOption[] options = null)
        {
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = enabled
                ? new Color(0.20f, 0.55f, 0.28f)
                : new Color(0.45f, 0.45f, 0.45f);

            GUIStyle style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor = Color.white;

            GUILayout.Label(label, style, options ?? new GUILayoutOption[0]);
            GUI.backgroundColor = prev;
        }

        public static bool DrawStatusButton(string label, bool enabled, GUILayoutOption[] options = null)
        {
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = enabled
                ? new Color(0.20f, 0.55f, 0.28f)
                : new Color(0.45f, 0.45f, 0.45f);

            GUIStyle style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor = Color.white;

            bool clicked = GUILayout.Button(label, style, options ?? new GUILayoutOption[0]);
            GUI.backgroundColor = prev;
            return clicked;
        }

        public static GUIStyle CreateButtonStyle(Color normalColor, Color? activeColor = null, int height = 20)
        {
            var style = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = height,
                alignment   = TextAnchor.MiddleCenter,
                fontStyle   = FontStyle.Bold
            };
            style.normal.textColor = Color.white;
            return style;
        }

        public static GUIStyle CreateToggleButtonStyle(bool isEnabled, int height = 20)
        {
            var style = new GUIStyle(GUI.skin.box)
            {
                fixedHeight = height,
                alignment   = TextAnchor.MiddleCenter,
                fontStyle   = FontStyle.Bold
            };
            style.normal.textColor = Color.white;
            return style;
        }
    }
}
#endif
