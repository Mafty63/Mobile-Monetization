#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace MobileCore.DefineSystem.Editor
{
    // ─────────────────────────────────────────────────────────────────────────────
    // SDKDefinesManager  —  Mirrors Watermelon's DefineManager pattern
    // ─────────────────────────────────────────────────────────────────────────────
    public static class SDKDefinesManager
    {
        [MenuItem("Tools/MobileCore/Check Auto Defines")]
        public static void CheckAutoDefines()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += CheckAutoDefines;
                return;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var registered = GetRegisteredDefines(assemblies);
            var states = new List<DefineCheckState>();

            foreach (var reg in registered)
            {
                bool found = false;
                foreach (Assembly asm in assemblies)
                {
                    try { if (asm.GetType(reg.AssemblyType, false) != null) { found = true; break; } }
                    catch { }
                }
                states.Add(new DefineCheckState(reg.DefineSymbol, found));
            }

            ApplyDefineStates(states);
        }

        public static void CheckAutoDefinesSilent()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += CheckAutoDefinesSilent;
                return;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var registered = GetRegisteredDefines(assemblies);
            var states = new List<DefineCheckState>();

            foreach (var reg in registered)
            {
                bool found = false;
                foreach (Assembly asm in assemblies)
                {
                    try { if (asm.GetType(reg.AssemblyType, false) != null) { found = true; break; } }
                    catch { }
                }
                states.Add(new DefineCheckState(reg.DefineSymbol, found));
            }

            ApplyDefineStates(states);
        }

        public static bool AddDefineSymbol(string define)
        {
            string current = GetCurrentDefines();
            if (Array.FindIndex(current.Split(';'), x => x == define) != -1) return false;
            SetDefines(define + ";" + current);
            return true;
        }

        public static bool RemoveDefineSymbol(string define)
        {
            string   current = GetCurrentDefines();
            string[] parts   = current.Split(';');
            int      idx     = Array.FindIndex(parts, x => x == define);
            if (idx == -1) return false;

            string newLine = "";
            for (int i = 0; i < parts.Length; i++)
                if (i != idx && !string.IsNullOrEmpty(parts[i]))
                    newLine += parts[i] + ";";

            SetDefines(newLine);
            return true;
        }

        private static List<RegisteredSDKDefine> GetRegisteredDefines(Assembly[] assemblies)
        {
            var result   = new List<RegisteredSDKDefine>();
            var attrType = typeof(MobileCore.DefineSystem.DefineAttribute);

            foreach (Assembly asm in assemblies)
            {
                try
                {
                    foreach (Type t in asm.GetTypes())
                    {
                        var attrs = (MobileCore.DefineSystem.DefineAttribute[])Attribute.GetCustomAttributes(t, attrType);
                        foreach (var attr in attrs)
                        {
                            if (!string.IsNullOrEmpty(attr.TypeCheck) &&
                                result.FindIndex(r => r.DefineSymbol == attr.DefineSymbol) == -1)
                                result.Add(new RegisteredSDKDefine(attr.DefineSymbol, attr.TypeCheck));
                        }
                    }
                }
                catch (ReflectionTypeLoadException) { }
                catch { }
            }

            return result;
        }

        private static void ApplyDefineStates(List<DefineCheckState> states)
        {
            string   current = GetCurrentDefines();
            string[] arr     = current.Split(';');
            string   result  = current;
            bool     changed = false;

            foreach (var state in states)
            {
                bool alreadySet = Array.FindIndex(arr, x => x == state.DefineSymbol) != -1;

                if (state.IsEnabled && !alreadySet)
                {
                    result  = state.DefineSymbol + ";" + result;
                    changed = true;
                    Debug.Log($"[Define System] Added: {state.DefineSymbol}");
                }
                else if (!state.IsEnabled && alreadySet)
                {
                    string[] p = result.Split(';');
                    result = "";
                    foreach (string s in p)
                        if (s != state.DefineSymbol && !string.IsNullOrEmpty(s))
                            result += s + ";";
                    changed = true;
                    Debug.Log($"[Define System] Removed: {state.DefineSymbol}");
                }
            }

            if (changed) SetDefines(result);
        }

#pragma warning disable 0618
        public static string GetCurrentDefines()
            => PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android) ?? "";

        public static void SetDefines(string line)
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, line);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, line);
        }
#pragma warning restore 0618

        public class RegisteredSDKDefine
        {
            public string DefineSymbol { get; private set; }
            public string AssemblyType { get; private set; }
            public RegisteredSDKDefine(string d, string a) { DefineSymbol = d; AssemblyType = a; }
        }

        public class DefineCheckState
        {
            public string DefineSymbol { get; private set; }
            public bool   IsEnabled    { get; private set; }
            public DefineCheckState(string d, bool e) { DefineSymbol = d; IsEnabled = e; }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // DefinesManagerWindow
    // ─────────────────────────────────────────────────────────────────────────────
    public class DefinesManagerWindow : EditorWindow
    {
        private DefineEntry[] _defines;
        private bool          _isDefinesSame;
        private bool          _requireRefresh;
        private Vector2       _scroll;

        // Styles
        private GUIStyle _headerStyle;
        private GUIStyle _symbolStyle;
        private GUIStyle _descStyle;
        private GUIStyle _pillOn;
        private GUIStyle _pillOff;
        private GUIStyle _btnApply;
        private GUIStyle _btnCheck;
        private GUIStyle _rowStyle;
        private bool     _stylesReady;

        private static readonly Color ColorGreen  = new Color(0.20f, 0.75f, 0.42f);
        private static readonly Color ColorGray   = new Color(0.40f, 0.40f, 0.46f);
        private static readonly Color ColorBlue   = new Color(0.20f, 0.55f, 0.92f);
        private static readonly Color ColorDark   = new Color(0.25f, 0.25f, 0.30f);

        [MenuItem("Tools/MobileCore/Define Symbols Manager")]
        public static void ShowWindow()
        {
            var w = GetWindow<DefinesManagerWindow>(true);
            w.minSize      = new Vector2(400, 260);
            w.titleContent = new GUIContent("Define Manager");
        }

        [MenuItem("Tools/MobileCore/EMERGENCY: Remove All Defines")]
        public static void EmergencyRemoveAll()
        {
            if (!EditorUtility.DisplayDialog("⚠️ Remove All Defines",
                "Hapus semua define MobileCore dari Player Settings?", "Ya, Hapus", "Batal")) return;

            string current   = SDKDefinesManager.GetCurrentDefines();
            var    managed   = new List<string>();
            var    attrType  = typeof(MobileCore.DefineSystem.DefineAttribute);

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (Type t in asm.GetTypes())
                    {
                        var attrs = (MobileCore.DefineSystem.DefineAttribute[])Attribute.GetCustomAttributes(t, attrType);
                        foreach (var a in attrs)
                            if (!managed.Contains(a.DefineSymbol)) managed.Add(a.DefineSymbol);
                    }
                }
                catch { }
            }

            string newLine = "";
            foreach (string p in current.Split(';'))
                if (!string.IsNullOrEmpty(p) && !managed.Contains(p))
                    newLine += p + ";";

            SDKDefinesManager.SetDefines(newLine);
            Debug.Log("[Define System] Emergency: defines removed.");
        }

        protected void OnEnable()
        {
            _requireRefresh = true;
            CacheDefines();
        }

        private void CacheDefines()
        {
            var entries  = new List<DefineEntry>();
            var attrType = typeof(MobileCore.DefineSystem.DefineAttribute);
            string[] cur = SDKDefinesManager.GetCurrentDefines().Split(';');

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (Type t in asm.GetTypes())
                    {
                        var attrs = (MobileCore.DefineSystem.DefineAttribute[])Attribute.GetCustomAttributes(t, attrType);
                        foreach (var attr in attrs)
                        {
                            if (entries.FindIndex(e => e.Symbol == attr.DefineSymbol) == -1)
                            {
                                bool on = Array.FindIndex(cur, x => x == attr.DefineSymbol) != -1;
                                entries.Add(new DefineEntry(attr.DefineSymbol, attr.Description, on));
                            }
                        }
                    }
                }
                catch (ReflectionTypeLoadException) { }
                catch { }
            }

            _defines = entries.ToArray();
            SyncActive();
        }

        private void SyncActive()
        {
            if (_defines == null) return;
            string[] cur = SDKDefinesManager.GetCurrentDefines().Split(';');
            for (int i = 0; i < _defines.Length; i++)
                _defines[i].IsEnabled = Array.FindIndex(cur, x => x == _defines[i].Symbol) != -1;
        }

        private bool CompareDefines()
        {
            if (_defines == null) return true;
            string[] cur = SDKDefinesManager.GetCurrentDefines().Split(';');
            foreach (var d in _defines)
            {
                bool inCur = Array.FindIndex(cur, x => x == d.Symbol) != -1;
                if (d.IsEnabled != inCur) return false;
            }
            return true;
        }

        private string GetActiveDefinesLine()
        {
            if (_defines == null) return "";
            string line = "";
            foreach (var d in _defines)
                if (d.IsEnabled) line += d.Symbol + ";";
            return line;
        }

        private void InitStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
            _headerStyle.normal.textColor = Color.white;

            _symbolStyle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold };

            _descStyle = new GUIStyle(EditorStyles.miniLabel);
            _descStyle.normal.textColor = new Color(0.55f, 0.55f, 0.62f);

            _pillOn = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize  = 9
            };
            _pillOn.normal.textColor = Color.white;

            _pillOff = new GUIStyle(_pillOn);
            _pillOff.normal.textColor = new Color(0.80f, 0.80f, 0.85f);

            _rowStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(8, 8, 5, 5),
                margin  = new RectOffset(4, 4, 1, 1)
            };

            _btnApply = new GUIStyle(GUI.skin.button)
            {
                fontSize    = 11,
                fontStyle   = FontStyle.Bold,
                fixedHeight = 28
            };
            _btnApply.normal.textColor = Color.white;

            _btnCheck = new GUIStyle(_btnApply) { fontStyle = FontStyle.Normal };
        }

        public void OnGUI()
        {
            // Guard during layout mismatches
            if (Event.current == null) return;

            InitStyles();

            Color prevBg = GUI.backgroundColor;

            // ── Header ────────────────────────────────────────────────────────────
            GUI.backgroundColor = new Color(0.12f, 0.12f, 0.16f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = prevBg;

            EditorGUILayout.LabelField("Define Manager", _headerStyle);
            EditorGUILayout.LabelField("Mobile Monetization  ·  MobileCore", EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();

            GUILayout.Space(2);

            // ── Define rows ───────────────────────────────────────────────────────
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            if (_defines != null && _defines.Length > 0)
            {
                EditorGUI.BeginChangeCheck();

                for (int i = 0; i < _defines.Length; i++)
                {
                    DefineEntry d = _defines[i];

                    // Alternate row tint
                    GUI.backgroundColor = i % 2 == 0
                        ? new Color(0.22f, 0.22f, 0.27f)
                        : new Color(0.25f, 0.25f, 0.30f);

                    EditorGUILayout.BeginHorizontal(_rowStyle);
                    GUI.backgroundColor = prevBg;

                    // Toggle
                    d.IsEnabled = EditorGUILayout.Toggle(d.IsEnabled, GUILayout.Width(18));

                    // Label block
                    EditorGUILayout.BeginVertical(GUILayout.MinWidth(180));
                    _symbolStyle.normal.textColor = d.IsEnabled
                        ? new Color(0.92f, 0.92f, 0.97f)
                        : new Color(0.48f, 0.48f, 0.54f);
                    EditorGUILayout.LabelField(d.Symbol, _symbolStyle);

                    if (!string.IsNullOrEmpty(d.Description))
                        EditorGUILayout.LabelField(d.Description, _descStyle);

                    EditorGUILayout.EndVertical();

                    GUILayout.FlexibleSpace();

                    // Status pill
                    GUI.backgroundColor = d.IsEnabled ? ColorGreen : ColorGray;
                    GUILayout.Label(d.IsEnabled ? "● ACTIVE" : "○  OFF",
                        d.IsEnabled ? _pillOn : _pillOff,
                        GUILayout.Width(62), GUILayout.Height(18));
                    GUI.backgroundColor = prevBg;

                    EditorGUILayout.EndHorizontal();
                }

                if (EditorGUI.EndChangeCheck()) _requireRefresh = true;
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Tidak ada define terdeteksi.\nKlik 'Check Auto Defines' untuk memindai SDK.",
                    MessageType.Info);
            }

            EditorGUILayout.EndScrollView();

            GUILayout.Space(4);

            // ── Footer buttons ────────────────────────────────────────────────────
            if (_requireRefresh) { _isDefinesSame = CompareDefines(); _requireRefresh = false; }

            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = _isDefinesSame ? ColorDark : ColorBlue;
            EditorGUI.BeginDisabledGroup(_isDefinesSame);
            if (GUILayout.Button("▶  Apply Defines", _btnApply))
            {
                EditorGUI.EndDisabledGroup();
                GUI.backgroundColor = prevBg;
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(4);
                SDKDefinesManager.SetDefines(GetActiveDefinesLine());
                return;
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(4);

            GUI.backgroundColor = ColorDark;
            if (GUILayout.Button("⟳  Check Auto Defines", _btnCheck))
            {
                GUI.backgroundColor = prevBg;
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(4);
                SDKDefinesManager.CheckAutoDefines();
                CacheDefines();
                return;
            }
            GUI.backgroundColor = prevBg;

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4);
        }

        [Serializable]
        private class DefineEntry
        {
            public string Symbol;
            public string Description;
            public bool   IsEnabled;

            public DefineEntry(string symbol, string desc, bool on)
            {
                Symbol      = symbol;
                Description = desc;
                IsEnabled   = on;
            }
        }
    }
}
#endif
