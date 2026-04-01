using System.Collections.Generic;
using UnityEngine;

namespace MobileCore.Advertisements.Providers
{
    [System.Serializable]
    public class MetaAudienceNetworkContainer
    {
        // Meta Audience Network Official Test Placement IDs
        // Source: https://developers.facebook.com/docs/audience-network/setting-up/test/placement-ids
        // These are global test IDs provided by Meta — safe to use during development.
        public static readonly string ANDROID_APP_TEST_ID = "1484169265118381";
        public static readonly string IOS_APP_TEST_ID = "1484169265118381";
        public static readonly string ANDROID_BANNER_TEST_ID = "IMG_16_9_APP_INSTALL#1484169265118381_1484172318451409";
        public static readonly string IOS_BANNER_TEST_ID = "IMG_16_9_APP_INSTALL#1484169265118381_1484172198451421";
        public static readonly string ANDROID_INTERSTITIAL_TEST_ID = "IMG_16_9_APP_INSTALL#1484169265118381_1484171998451441";
        public static readonly string IOS_INTERSTITIAL_TEST_ID = "IMG_16_9_APP_INSTALL#1484169265118381_1484171848451456";
        public static readonly string ANDROID_REWARDED_TEST_ID = "VID_HD_9_16_39S_APP_INSTALL#1484169265118381_1484170641784910";
        public static readonly string IOS_REWARDED_TEST_ID = "VID_HD_9_16_39S_APP_INSTALL#1484169265118381_1484170508451590";

        // Application ID
        [Header("Application ID")]
        [Tooltip("Meta Audience Network App ID. For testing, the test App ID is pre-filled.")]
        [SerializeField] string androidAppID = ANDROID_APP_TEST_ID;
        public string AndroidAppID => androidAppID;
        [SerializeField] string iOSAppID = IOS_APP_TEST_ID;
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
    }
}
