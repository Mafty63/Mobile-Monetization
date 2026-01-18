using System.Collections.Generic;
using UnityEngine;

namespace MobileCore.Advertisements.Providers
{
    [System.Serializable]
    public class MetaAudienceNetworkContainer
    {
        // Meta Audience Network Test Placement IDs
        // Note: For real ads, you must use IDs from your Monetization Manager.
        // For testing, you can use these or add your device hash to test devices.
        // See: https://developers.facebook.com/docs/audience-network/setting-up/test/placement-ids
        
        public static readonly string ANDROID_BANNER_TEST_ID = "IMG_16_9_APP_INSTALL#YOUR_PLACEMENT_ID";
        public static readonly string IOS_BANNER_TEST_ID = "IMG_16_9_APP_INSTALL#YOUR_PLACEMENT_ID";
        public static readonly string ANDROID_INTERSTITIAL_TEST_ID = "IMG_16_9_APP_INSTALL#YOUR_PLACEMENT_ID";
        public static readonly string IOS_INTERSTITIAL_TEST_ID = "IMG_16_9_APP_INSTALL#YOUR_PLACEMENT_ID";
        public static readonly string ANDROID_REWARDED_TEST_ID = "VID_HD_9_16_39S_APP_INSTALL#YOUR_PLACEMENT_ID";
        public static readonly string IOS_REWARDED_TEST_ID = "VID_HD_9_16_39S_APP_INSTALL#YOUR_PLACEMENT_ID";

        // Application ID (Not always required for initialization in code, but good for reference)
        [Header("Application ID")]
        [Tooltip("Meta Audience Network usually reads App ID from AndroidManifest/Info.plist or during initialization.")]
        [SerializeField] string androidAppID = "";
        public string AndroidAppID => androidAppID;
        [SerializeField] string iOSAppID = "";
        public string IOSAppID => iOSAppID;

        // Banner ID
        [Header("Banner ID")]
        [SerializeField] string androidBannerID = ANDROID_BANNER_TEST_ID;
        public string AndroidBannerID => androidBannerID;
        [SerializeField] string iOSBannerID = IOS_BANNER_TEST_ID;
        public string IOSBannerID => iOSBannerID;

        // Interstitial ID
        [Header("Interstitial ID")]
        [SerializeField] string androidInterstitialID = ANDROID_INTERSTITIAL_TEST_ID;
        public string AndroidInterstitialID => androidInterstitialID;
        [SerializeField] string iOSInterstitialID = IOS_INTERSTITIAL_TEST_ID;
        public string IOSInterstitialID => iOSInterstitialID;

        // Rewarded Video ID
        [Header("Rewarded Video ID")]
        [SerializeField] string androidRewardedVideoID = ANDROID_REWARDED_TEST_ID;
        public string AndroidRewardedVideoID => androidRewardedVideoID;
        [SerializeField] string iOSRewardedVideoID = IOS_REWARDED_TEST_ID;
        public string IOSRewardedVideoID => iOSRewardedVideoID;

        [Header("Banner Settings")]
        [SerializeField] BannerPosition bannerPosition = BannerPosition.Bottom;
        public BannerPosition BannerPosition => bannerPosition;

        [Header("Test Devices")]
        [Tooltip("Add device hash IDs here to enable test ads on specific devices.")]
        [SerializeField] List<string> testDevicesIDs = new List<string>();
        public List<string> TestDevicesIDs => testDevicesIDs;
    }
}
