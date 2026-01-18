#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using MobileCore.IAPModule;

[CustomPropertyDrawer(typeof(IAPItem))]
public class IAPItemDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Tinggi berdasarkan jumlah property yang ditampilkan
        return EditorGUIUtility.singleLineHeight * 7 + EditorGUIUtility.standardVerticalSpacing * 6;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Draw label
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        // Calculate rects
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        Rect typeRect = new Rect(position.x, position.y, position.width, lineHeight);
        Rect keyTypeRect = new Rect(position.x, position.y + lineHeight + spacing, position.width, lineHeight);
        Rect androidRect = new Rect(position.x, position.y + (lineHeight + spacing) * 2, position.width, lineHeight);
        Rect iOSRect = new Rect(position.x, position.y + (lineHeight + spacing) * 3, position.width, lineHeight);
        Rect platformRect = new Rect(position.x, position.y + (lineHeight + spacing) * 4, position.width, lineHeight * 2);

        // Draw fields
        EditorGUI.PropertyField(typeRect, property.FindPropertyRelative("productType"), new GUIContent("Type"));
        EditorGUI.PropertyField(keyTypeRect, property.FindPropertyRelative("productKeyType"), new GUIContent("Key Type"));
        EditorGUI.PropertyField(androidRect, property.FindPropertyRelative("androidID"), new GUIContent("Android ID"));
        EditorGUI.PropertyField(iOSRect, property.FindPropertyRelative("iOSID"), new GUIContent("iOS ID"));

        // Platform info
        EditorGUI.HelpBox(platformRect, GetPlatformInfo(property), MessageType.None);

        EditorGUI.indentLevel = indent;
        EditorGUI.EndProperty();
    }

    private string GetPlatformInfo(SerializedProperty property)
    {
        string androidID = property.FindPropertyRelative("androidID").stringValue;
        string iOSID = property.FindPropertyRelative("iOSID").stringValue;
        var keyType = property.FindPropertyRelative("productKeyType");
        string keyTypeName = keyType.enumNames[keyType.enumValueIndex];

        string currentPlatform =
#if UNITY_ANDROID
            "Android";
#elif UNITY_IOS
            "iOS";
#else
            "Other";
#endif

        string currentID =
#if UNITY_ANDROID
            androidID;
#elif UNITY_IOS
            iOSID;
#else
            $"unknown_platform_{keyTypeName}";
#endif

        return $"Current Platform: {currentPlatform}\nWill Use ID: {currentID}";
    }
}
#endif