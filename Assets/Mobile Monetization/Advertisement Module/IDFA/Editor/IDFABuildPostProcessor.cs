#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using MobileCore.Advertisements;

namespace MobileCore.Advertisements.IDFA.Editor
{
    public static class IDFABuildPostProcessor
    {
        [PostProcessBuild(1)]
        public static void OnPostProcessBuild(BuildTarget target, string path)
        {
            if (target == BuildTarget.iOS)
            {
                // Find AdsSettings
                AdsSettings adsSettings = null;
                string[] guids = AssetDatabase.FindAssets("t:AdsSettings");
                if (guids.Length > 0)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    adsSettings = AssetDatabase.LoadAssetAtPath<AdsSettings>(assetPath);
                }

                if (adsSettings == null || !adsSettings.IsIDFAEnabled)
                {
                    return;
                }

                string plistPath = Path.Combine(path, "Info.plist");
                PlistDocument plist = new PlistDocument();
                plist.ReadFromString(File.ReadAllText(plistPath));

                PlistElementDict rootDict = plist.root;
                
                // Add NSUserTrackingUsageDescription
                string trackingDescription = adsSettings.TrackingDescription;
                if (string.IsNullOrEmpty(trackingDescription))
                {
                    trackingDescription = "Your data will be used to deliver personalized ads to you.";
                }
                
                rootDict.SetString("NSUserTrackingUsageDescription", trackingDescription);

                File.WriteAllText(plistPath, plist.WriteToString());
            }
        }
    }
}
#endif
