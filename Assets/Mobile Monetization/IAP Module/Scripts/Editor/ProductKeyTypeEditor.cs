#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using MobileCore.MainModule.Editor;

namespace MobileCore.IAPModule.Editor
{
    public class ProductKeyTypeEditor : EditorWindow
    {
        [Serializable]
        private class EnumValue
        {
            public string name;
            public int value;
        }

        private List<EnumValue> enumValues = new List<EnumValue>();
        private Vector2 scrollPosition;
        private string newName = "";
        private string enumFilePath;

        public static void ShowWindow()
        {
            GetWindow<ProductKeyTypeEditor>("ProductKeyType Editor");
        }

        private void OnEnable()
        {
            // Set path yang benar ke Wrappers folder
            enumFilePath = "Assets/Mobile Core/IAP Module/Scripts/ProductKeyType.cs";

            // Jika file tidak ada di path tersebut, cari file yang ada
            if (!File.Exists(enumFilePath))
            {
                FindEnumFilePath();
            }

            LoadCurrentEnumValues();
        }

        private void FindEnumFilePath()
        {
            // Cari file ProductKeyType.cs di seluruh project
            string[] guids = AssetDatabase.FindAssets("ProductKeyType t:script");
            if (guids.Length > 0)
            {
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.Contains("IAP Module"))
                    {
                        enumFilePath = path;
                        return;
                    }
                }
                enumFilePath = AssetDatabase.GUIDToAssetPath(guids[0]);
            }
        }

        private void LoadCurrentEnumValues()
        {
            enumValues.Clear();

            if (string.IsNullOrEmpty(enumFilePath) || !File.Exists(enumFilePath))
            {
                // Create default values jika file tidak ditemukan
                CreateDefaultEnumValues();
                return;
            }

            try
            {
                // Baca file enum dan parse values-nya
                string[] fileLines = File.ReadAllLines(enumFilePath);
                bool inEnum = false;

                foreach (string line in fileLines)
                {
                    string trimmedLine = line.Trim();

                    if (trimmedLine.Contains("public enum ProductKeyType"))
                    {
                        inEnum = true;
                        continue;
                    }

                    if (inEnum && trimmedLine == "}")
                    {
                        break;
                    }

                    if (inEnum && trimmedLine.Contains("="))
                    {
                        // Parse line seperti: "NoAds = 0,"
                        string[] parts = trimmedLine.Split('=');
                        if (parts.Length == 2)
                        {
                            string name = parts[0].Trim();
                            string valueStr = parts[1].Trim().TrimEnd(',');

                            if (int.TryParse(valueStr, out int value))
                            {
                                enumValues.Add(new EnumValue
                                {
                                    name = name,
                                    value = value
                                });
                            }
                        }
                    }
                }

                // Urutkan berdasarkan value
                enumValues = enumValues.OrderBy(v => v.value).ToList();

                // Jika tidak ada values yang ditemukan, buat default
                if (enumValues.Count == 0)
                {
                    CreateDefaultEnumValues();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading enum values: {ex.Message}");
                CreateDefaultEnumValues();
            }
        }

        private void CreateDefaultEnumValues()
        {
            enumValues.Clear();

            // Default values - NoAds tidak bisa dihapus
            enumValues.Add(new EnumValue { name = "NoAds", value = 0 });
        }

        private void OnGUI()
        {

            // Header
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("ProductKeyType Enum Editor", EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            if (string.IsNullOrEmpty(enumFilePath))
            {
                EditorGUILayout.HelpBox("Could not find ProductKeyType.cs file!", MessageType.Error);
                return;
            }

            // File info
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.HelpBox($"Editing enum at: {enumFilePath}", MessageType.Info);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Add new value section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Add New Value", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.textArea);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name:", EditorStyles.miniLabel, GUILayout.Width(40));
            var textFieldStyle = EditorStyleTemplate.GrayTextFieldBackgroundStyle;
            newName = EditorGUILayout.TextField(newName, textFieldStyle);
            EditorGUILayout.EndHorizontal();

            // Validasi: cek apakah ada space
            bool hasSpace = !string.IsNullOrEmpty(newName) && newName.Contains(" ");
            if (hasSpace)
            {
                EditorGUILayout.HelpBox("Enum name cannot contain spaces. Use camelCase or PascalCase instead (e.g., 'NoAds', 'RemoveAds').", MessageType.Warning);
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var buttonStyle = EditorStyleTemplate.GrayButtonStyle;
            buttonStyle.fixedHeight = 22f;

            // Nonaktifkan tombol jika ada space atau kosong
            bool canAdd = !string.IsNullOrEmpty(newName) && !hasSpace;
            EditorGUI.BeginDisabledGroup(!canAdd);
            if (GUILayout.Button("Add Value", buttonStyle, GUILayout.Width(100)) && canAdd)
            {
                if (enumValues.Any(v => v.name == newName))
                {
                    EditorUtility.DisplayDialog("Error", $"Name '{newName}' already exists!", "OK");
                }
                else
                {
                    // Cari value tertinggi dan tambah 1
                    int newValue = enumValues.Count > 0 ? enumValues.Max(v => v.value) + 1 : 0;

                    enumValues.Add(new EnumValue
                    {
                        name = newName,
                        value = newValue
                    });

                    // Urutkan ulang
                    enumValues = enumValues.OrderBy(v => v.value).ToList();
                    newName = "";
                }
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Current values section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Current Values ({enumValues.Count})", EditorStyles.boldLabel);

            if (enumValues.Count > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.textArea);
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

                for (int i = 0; i < enumValues.Count; i++)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    var enumValue = enumValues[i];

                    EditorGUILayout.BeginHorizontal();

                    // Value (readonly)
                    EditorGUILayout.BeginVertical(GUILayout.Width(60));
                    EditorGUILayout.LabelField("Value:", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField(enumValue.value.ToString(), EditorStyles.miniBoldLabel);
                    EditorGUILayout.EndVertical();

                    // Name (editable)
                    EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                    EditorGUILayout.LabelField("Name:", EditorStyles.miniLabel);

                    // Nonaktifkan editing untuk NoAds
                    if (enumValue.name == "NoAds")
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.TextField(enumValue.name, textFieldStyle);
                        EditorGUI.EndDisabledGroup();
                    }
                    else
                    {
                        string newNameValue = EditorGUILayout.TextField(enumValue.name, textFieldStyle);
                        if (newNameValue != enumValue.name)
                        {
                            if (!string.IsNullOrEmpty(newNameValue) && !enumValues.Any(v => v.name == newNameValue))
                            {
                                enumValue.name = newNameValue;
                            }
                            else if (enumValues.Any(v => v.name == newNameValue))
                            {
                                EditorUtility.DisplayDialog("Error", $"Name '{newNameValue}' already exists!", "OK");
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();

                    // Delete button
                    EditorGUILayout.BeginVertical(GUILayout.Width(60));
                    EditorGUILayout.LabelField(" ", EditorStyles.miniLabel);

                    // Nonaktifkan tombol delete untuk NoAds
                    if (enumValue.name == "NoAds")
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        GUILayout.Button("Delete", buttonStyle, GUILayout.Width(60));
                        EditorGUI.EndDisabledGroup();
                    }
                    else
                    {
                        if (GUILayout.Button("Delete", buttonStyle, GUILayout.Width(60)))
                        {
                            if (IsValueInUse(enumValue.value))
                            {
                                EditorUtility.DisplayDialog("Cannot Delete",
                                    $"Value {enumValue.value} is in use by products!", "OK");
                            }
                            else
                            {
                                enumValues.RemoveAt(i);
                                // Setelah hapus, kita perlu mengatur ulang values agar berurutan
                                ReorderEnumValues();
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();

                    if (i < enumValues.Count - 1)
                    {
                        EditorGUILayout.Space(2);
                    }
                }

                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("No enum values defined.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Action buttons
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.textArea);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Update Enum File", buttonStyle, GUILayout.Width(120)))
            {
                UpdateEnumFile();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Show in Project", buttonStyle, GUILayout.Width(120)))
            {
                if (!string.IsNullOrEmpty(enumFilePath) && File.Exists(enumFilePath))
                {
                    UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(enumFilePath);
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Reset button
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset to Default", buttonStyle, GUILayout.Width(120)))
            {
                if (EditorUtility.DisplayDialog("Reset Enum",
                    "Are you sure you want to reset to default values?", "Yes", "No"))
                {
                    CreateDefaultEnumValues();
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Usage info
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.HelpBox(
                "Values are automatically sorted. When you add a new value, it will be assigned the next available number.\n\nNote: 'NoAds' cannot be deleted or renamed.",
                MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private void ReorderEnumValues()
        {
            // Setelah menghapus, kita urutkan ulang dan beri nilai yang berurutan
            for (int i = 0; i < enumValues.Count; i++)
            {
                enumValues[i].value = i;
            }
        }

        private bool IsValueInUse(int value)
        {
            // Cari di semua IAPSettings apakah value ini digunakan
            var allSettings = Resources.FindObjectsOfTypeAll<IAPSettings>();
            foreach (var settings in allSettings)
            {
                if (settings.StoreItems != null)
                {
                    foreach (var item in settings.StoreItems)
                    {
                        if ((int)item.ProductKeyType == value)
                            return true;
                    }
                }
            }
            return false;
        }

        private void UpdateEnumFile()
        {
            try
            {
                if (string.IsNullOrEmpty(enumFilePath))
                {
                    EditorUtility.DisplayDialog("Error", "Enum file path is not set!", "OK");
                    return;
                }

                // Buat directory jika belum ada
                string directory = Path.GetDirectoryName(enumFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Generate kode enum baru
                string newEnumCode = GenerateEnumCode();
                File.WriteAllText(enumFilePath, newEnumCode);

                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("Success",
                    "Enum file updated successfully! Scripts will recompile automatically.", "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error",
                    $"Failed to update enum file: {ex.Message}", "OK");
            }
        }

        private string GenerateEnumCode()
        {
            string code = "namespace MobileCore.IAPModule\n";
            code += "{\n";
            code += "    public enum ProductKeyType\n";
            code += "    {\n";

            foreach (var value in enumValues.OrderBy(v => v.value))
            {
                code += $"        {value.name} = {value.value},\n";
            }

            code += "    }\n";
            code += "}\n";

            return code;
        }
    }
}
#endif