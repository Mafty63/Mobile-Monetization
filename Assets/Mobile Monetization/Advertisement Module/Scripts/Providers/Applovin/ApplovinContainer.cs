using System.Collections.Generic;
using UnityEngine;

namespace MobileCore.Advertisements.Providers
{
    [System.Serializable]
    public class AppLovinContainer
    {
        // Template IDs untuk testing - SDK Keys harus dari dashboard AppLovin
        public static readonly string ANDROID_SDK_KEY_TEMPLATE = "YOUR_ANDROID_SDK_KEY";
        public static readonly string IOS_SDK_KEY_TEMPLATE = "YOUR_IOS_SDK_KEY";

        // Test Unit IDs dari AppLovin
        public static readonly string ANDROID_BANNER_TEST_ID = "c192d3f0d6c8bf71";
        public static readonly string IOS_BANNER_TEST_ID = "c192d3f0d6c8bf71";
        public static readonly string ANDROID_INTERSTITIAL_TEST_ID = "4b6b2fef8bd6a3cd";
        public static readonly string IOS_INTERSTITIAL_TEST_ID = "4b6b2fef8bd6a3cd";
        public static readonly string ANDROID_REWARDED_VIDEO_TEST_ID = "d64b5b9c5a1a7e36";
        public static readonly string IOS_REWARDED_VIDEO_TEST_ID = "d64b5b9c5a1a7e36";

        [SerializeField] private string androidSdkKey = ANDROID_SDK_KEY_TEMPLATE;
        public string AndroidSdkKey => androidSdkKey;

        [SerializeField] private string iosSdkKey = IOS_SDK_KEY_TEMPLATE;
        public string IosSdkKey => iosSdkKey;

        [SerializeField] private string androidBannerID = ANDROID_BANNER_TEST_ID;
        public string AndroidBannerID => androidBannerID;

        [SerializeField] private string iOSBannerID = IOS_BANNER_TEST_ID;
        public string IOSBannerID => iOSBannerID;

        [SerializeField] private string androidInterstitialID = ANDROID_INTERSTITIAL_TEST_ID;
        public string AndroidInterstitialID => androidInterstitialID;

        [SerializeField] private string iOSInterstitialID = IOS_INTERSTITIAL_TEST_ID;
        public string IOSInterstitialID => iOSInterstitialID;

        [SerializeField] private string androidRewardedVideoID = ANDROID_REWARDED_VIDEO_TEST_ID;
        public string AndroidRewardedVideoID => androidRewardedVideoID;

        [SerializeField] private string iOSRewardedVideoID = IOS_REWARDED_VIDEO_TEST_ID;
        public string IOSRewardedVideoID => iOSRewardedVideoID;

        [SerializeField] private BannerPosition bannerPosition = BannerPosition.Bottom;
        public BannerPosition BannerPosition => bannerPosition;

        [SerializeField] private BannerPlacementType bannerSize = BannerPlacementType.Banner;
        public BannerPlacementType BannerSize => bannerSize;

        [SerializeField] private bool enableVerboseLogging = false;
        public bool EnableVerboseLogging => enableVerboseLogging;

        public enum BannerPlacementType
        {
            Banner = 0,           // Standard banner (320x50)
            Leader = 1,           // Leaderboard banner (728x90)
            MRec = 2,             // Medium rectangle (300x250)
            Adaptive = 3          // Adaptive banner (auto size)
        }
    }
}