using UnityEditor;

namespace MobileCore.DefineSystem.Editor
{
    /// <summary>
    /// Mirrors Watermelon's DefinePostprocessor exactly.
    /// Triggers CheckAutoDefines after script reload or .cs/.dll file changes.
    /// </summary>
    public class DefinePostprocessor : AssetPostprocessor
    {
        private const string PREFS_KEY = "MobileCore_DefinesCheck";

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += OnScriptsReloaded;
                return;
            }

            EditorApplication.delayCall += () => SDKDefinesManager.CheckAutoDefinesSilent();
        }

        private static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            ValidateRequirement(importedAssets, deletedAssets);

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += () =>
                    OnPostprocessAllAssets(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
                return;
            }

            if (EditorPrefs.GetBool(PREFS_KEY, false))
            {
                SDKDefinesManager.CheckAutoDefinesSilent();
                EditorPrefs.SetBool(PREFS_KEY, false);
            }
        }

        private static void ValidateRequirement(string[] importedAssets, string[] deletedAssets)
        {
            if (importedAssets != null)
            {
                foreach (string s in importedAssets)
                {
                    if (s.EndsWith(".cs") || s.EndsWith(".dll"))
                    {
                        EditorPrefs.SetBool(PREFS_KEY, true);
                        return;
                    }
                }
            }

            if (deletedAssets != null)
            {
                foreach (string s in deletedAssets)
                {
                    if (s.EndsWith(".cs") || s.EndsWith(".dll"))
                    {
                        EditorPrefs.SetBool(PREFS_KEY, true);
                        return;
                    }
                }
            }
        }
    }
}
