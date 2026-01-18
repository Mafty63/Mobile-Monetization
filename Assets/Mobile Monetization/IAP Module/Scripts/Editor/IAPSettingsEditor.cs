#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using MobileCore.MainModule.Editor;

namespace MobileCore.IAPModule.Editor
{
    [CustomEditor(typeof(IAPSettings))]
    public class IAPSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty p_useTestMode;
        private SerializedProperty p_storeItems;

        // Foldout states
        private bool showStoreConfiguration = true;
        private bool showProducts = true;
        private const string EditorPrefKey = "MobileCore_IAPSettings_ProductsFoldout";

        private void OnEnable()
        {
            showProducts = EditorPrefs.GetBool(EditorPrefKey, true);

            try
            {
                p_useTestMode = serializedObject.FindProperty("useTestMode");
                p_storeItems = serializedObject.FindProperty("storeItems");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in IAPSettingsEditor.OnEnable: {e.Message}");
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            try
            {
                DrawHeaderInfo();
                EditorGUILayout.Space();

                DrawStoreConfiguration();
                EditorGUILayout.Space();

                DrawProductsSection();
            }
            catch (System.Exception e)
            {
                EditorGUILayout.HelpBox($"Error drawing inspector: {e.Message}", MessageType.Error);
                Debug.LogError($"Error in IAPSettingsEditor.OnInspectorGUI: {e}");
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeaderInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header dengan status
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("IN-APP PURCHASE SETTINGS", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));

            // Status indicator di kanan
            string status = p_useTestMode.boolValue ?
                "<color=orange>TEST MODE</color>" :
                "<color=green>LIVE MODE</color>";
            GUIStyle statusStyle = new GUIStyle(EditorStyles.miniLabel);
            statusStyle.richText = true;
            statusStyle.alignment = TextAnchor.MiddleRight;
            EditorGUILayout.LabelField(status, statusStyle, GUILayout.Width(80));

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("Configure your IAP products and store behavior.", MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private void DrawStoreConfiguration()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Foldout untuk Store Configuration
            EditorGUILayout.BeginHorizontal();
            showStoreConfiguration = EditorGUILayout.Foldout(showStoreConfiguration, "STORE CONFIGURATION", true, EditorStyles.foldoutHeader);
            EditorGUILayout.EndHorizontal();

            if (showStoreConfiguration)
            {
                EditorGUILayout.Space();

                // Tombol Edit ProductKeyType
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                var buttonStyle = EditorStyleTemplate.GrayButtonStyle;
                buttonStyle.fixedHeight = 25f;
                if (GUILayout.Button("Edit ProductKeyType Enum", buttonStyle, GUILayout.Width(180)))
                {
                    ProductKeyTypeEditor.ShowWindow();
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                // Test Mode setting - CORRECTED VERSION
                EditorGUILayout.BeginVertical(EditorStyles.textArea);

                var toggleStyle = EditorStyleTemplate.GrayToggleBackgroundStyle;

                // Use horizontal layout like in AdsSettingsEditor
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Test Mode", EditorStyles.label, GUILayout.Width(EditorGUIUtility.labelWidth - 20));
                p_useTestMode.boolValue = EditorGUILayout.Toggle(p_useTestMode.boolValue, toggleStyle);
                EditorGUILayout.LabelField(new GUIContent("", "Enable test mode for testing without real purchases"), EditorStyles.miniLabel, GUILayout.Width(20));
                EditorGUILayout.EndHorizontal();

                if (p_useTestMode.boolValue)
                {
                    EditorGUILayout.HelpBox(
                        "Test Mode is active. Use this for testing purchases without real transactions.",
                        MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "Real store is active. Make sure to configure proper product IDs for each platform.",
                        MessageType.Info);
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawProductsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header dengan counter dan buttons
            EditorGUILayout.BeginHorizontal();
            showProducts = EditorGUILayout.Foldout(showProducts, $"PRODUCTS ({p_storeItems?.arraySize ?? 0})", true, EditorStyles.foldoutHeader);
            EditorPrefs.SetBool(EditorPrefKey, showProducts);

            GUILayout.FlexibleSpace();

            if (p_storeItems != null)
            {
                var buttonStyle = EditorStyleTemplate.GrayButtonStyle;
                buttonStyle.fixedHeight = 22f;

                if (GUILayout.Button("Add Product", buttonStyle, GUILayout.Width(100)))
                {
                    AddNewProduct();
                }

                if (p_storeItems.arraySize > 0 && GUILayout.Button("Clear All", buttonStyle, GUILayout.Width(100)))
                {
                    if (EditorUtility.DisplayDialog("Clear All Products",
                        "Are you sure you want to remove all products?", "Yes", "No"))
                    {
                        p_storeItems.ClearArray();
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            if (showProducts && p_storeItems != null)
            {
                EditorGUILayout.Space();

                // Summary statistics
                var items = (target as IAPSettings)?.StoreItems;
                if (items != null && items.Length > 0)
                {
                    int consumableCount = items.Count(i => i.ProductType == ProductType.Consumable);
                    int nonConsumableCount = items.Count(i => i.ProductType == ProductType.NonConsumable);
                    int subscriptionCount = items.Count(i => i.ProductType == ProductType.Subscription);

                    EditorGUILayout.BeginVertical(EditorStyles.textArea);
                    EditorGUILayout.LabelField("Product Summary", EditorStyles.miniBoldLabel);

                    float labelWidth = 100f;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Consumable:", EditorStyles.miniBoldLabel, GUILayout.Width(labelWidth));
                    EditorGUILayout.LabelField(consumableCount.ToString(), EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Non-Consumable:", EditorStyles.miniBoldLabel, GUILayout.Width(labelWidth));
                    EditorGUILayout.LabelField(nonConsumableCount.ToString(), EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Subscription:", EditorStyles.miniBoldLabel, GUILayout.Width(labelWidth));
                    EditorGUILayout.LabelField(subscriptionCount.ToString(), EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();
                }

                // Products list
                if (p_storeItems.arraySize > 0)
                {
                    for (int i = 0; i < p_storeItems.arraySize; i++)
                    {
                        DrawProductItem(i);
                        if (i < p_storeItems.arraySize - 1)
                        {
                            EditorGUILayout.Space();
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No products configured. Click 'Add Product' to create your first IAP product.", MessageType.Info);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawProductItem(int index)
        {
            if (p_storeItems == null || index >= p_storeItems.arraySize) return;

            SerializedProperty itemProperty = p_storeItems.GetArrayElementAtIndex(index);
            if (itemProperty == null) return;

            SerializedProperty androidIDProperty = itemProperty.FindPropertyRelative("androidID");
            SerializedProperty iOSIDProperty = itemProperty.FindPropertyRelative("iOSID");
            SerializedProperty productKeyTypeProperty = itemProperty.FindPropertyRelative("productKeyType");
            SerializedProperty productTypeProperty = itemProperty.FindPropertyRelative("productType");

            if (androidIDProperty == null || iOSIDProperty == null ||
                productKeyTypeProperty == null || productTypeProperty == null) return;

            EditorGUILayout.BeginVertical(EditorStyles.textArea);

            // Header dengan delete button
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Product #{index + 1}", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));

            var buttonStyle = EditorStyleTemplate.GrayButtonStyle;
            if (GUILayout.Button("Delete", buttonStyle, GUILayout.Width(60)))
            {
                p_storeItems.DeleteArrayElementAtIndex(index);
                serializedObject.ApplyModifiedProperties();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Layout vertikal selalu untuk konsistensi
            DrawProductFieldsVertical(androidIDProperty, iOSIDProperty, productKeyTypeProperty, productTypeProperty);

            EditorGUILayout.EndVertical();
        }

        private void DrawProductFieldsVertical(SerializedProperty androidIDProperty, SerializedProperty iOSIDProperty,
                                            SerializedProperty productKeyTypeProperty, SerializedProperty productTypeProperty)
        {
            var textFieldStyle = EditorStyleTemplate.GrayTextFieldBackgroundStyle;
            var popupStyle = EditorStyleTemplate.GrayPopupBackgroundStyle;

            // Key Type
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Key Type", EditorStyles.miniLabel);
            productKeyTypeProperty.enumValueIndex = EditorGUILayout.Popup(productKeyTypeProperty.enumValueIndex, productKeyTypeProperty.enumDisplayNames, popupStyle);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Product Type
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Product Type", EditorStyles.miniLabel);
            productTypeProperty.enumValueIndex = EditorGUILayout.Popup(productTypeProperty.enumValueIndex, productTypeProperty.enumDisplayNames, popupStyle);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Platform IDs - Layout vertikal compact
            EditorGUILayout.BeginVertical();

            // Android ID
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Android ID", EditorStyles.miniLabel, GUILayout.Width(80));
            androidIDProperty.stringValue = EditorGUILayout.TextField(androidIDProperty.stringValue, textFieldStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            // iOS ID
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("iOS ID", EditorStyles.miniLabel, GUILayout.Width(80));
            iOSIDProperty.stringValue = EditorGUILayout.TextField(iOSIDProperty.stringValue, textFieldStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Current Platform Info
            DrawPlatformInfoCompact(androidIDProperty, iOSIDProperty, productKeyTypeProperty);
        }

        private void DrawPlatformInfoCompact(SerializedProperty androidIDProperty, SerializedProperty iOSIDProperty, SerializedProperty productKeyTypeProperty)
        {
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
                androidIDProperty.stringValue;
#elif UNITY_IOS
                iOSIDProperty.stringValue;
#else
                $"unknown_platform_{productKeyTypeProperty.enumValueIndex}";
#endif

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Layout compact untuk platform info
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Platform:", EditorStyles.miniBoldLabel, GUILayout.Width(50));
            EditorGUILayout.LabelField(currentPlatform, EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Current ID:", EditorStyles.miniBoldLabel, GUILayout.Width(65));

            string displayID = string.IsNullOrEmpty(currentID) ? "<color=red>EMPTY</color>" :
                              (currentID.Length > 30 ? currentID.Substring(0, 27) + "..." : currentID);

            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
            labelStyle.richText = true;
            labelStyle.wordWrap = true;
            EditorGUILayout.LabelField(displayID, labelStyle, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            if (string.IsNullOrEmpty(currentID))
            {
                EditorGUILayout.HelpBox("Product ID is empty and will cause errors!", MessageType.Error);
            }
            else if (currentID.Length > 30)
            {
                EditorGUILayout.HelpBox($"Full ID: {currentID}", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void AddNewProduct()
        {
            if (p_storeItems == null) return;

            int newIndex = p_storeItems.arraySize;
            p_storeItems.InsertArrayElementAtIndex(newIndex);

            SerializedProperty newItem = p_storeItems.GetArrayElementAtIndex(newIndex);
            if (newItem == null) return;

            // Find the next available product number
            int maxProductNumber = 0;
            string baseProductID = "com.company.product";

            // Check existing products to find the highest number
            for (int i = 0; i < p_storeItems.arraySize; i++)
            {
                SerializedProperty item = p_storeItems.GetArrayElementAtIndex(i);
                if (item == null) continue;

                SerializedProperty androidProp = item.FindPropertyRelative("androidID");
                if (androidProp != null)
                {
                    string androidID = androidProp.stringValue;
                    if (androidID.StartsWith($"{baseProductID}.new"))
                    {
                        // Extract number from product ID
                        string numberPart = androidID.Substring($"{baseProductID}.new".Length);
                        if (int.TryParse(numberPart, out int currentNumber))
                        {
                            maxProductNumber = Mathf.Max(maxProductNumber, currentNumber);
                        }
                    }
                }
            }

            // Use next number
            int nextProductNumber = maxProductNumber + 1;
            string newProductID = $"{baseProductID}.new{nextProductNumber}";

            // Set default values
            SerializedProperty newAndroidProp = newItem.FindPropertyRelative("androidID");
            SerializedProperty newIosProp = newItem.FindPropertyRelative("iOSID");
            SerializedProperty keyTypeProp = newItem.FindPropertyRelative("productKeyType");
            SerializedProperty typeProp = newItem.FindPropertyRelative("productType");

            if (newAndroidProp != null) newAndroidProp.stringValue = newProductID;
            if (newIosProp != null) newIosProp.stringValue = newProductID;
            if (keyTypeProp != null) keyTypeProp.enumValueIndex = 0;
            if (typeProp != null) typeProp.enumValueIndex = 0;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif