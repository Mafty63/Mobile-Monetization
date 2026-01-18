using System.Collections.Generic;
using UnityEngine;

namespace MobileCore.Advertisements.Providers
{
    [System.Serializable]
    public class AdMobContainer
    {
        // Test IDs
        public static readonly string ANDROID_BANNER_TEST_ID = "ca-app-pub-3940256099942544/6300978111";
        public static readonly string IOS_BANNER_TEST_ID = "ca-app-pub-3940256099942544/2934735716";
        public static readonly string ANDROID_INTERSTITIAL_TEST_ID = "ca-app-pub-3940256099942544/1033173712";
        public static readonly string IOS_INTERSTITIAL_TEST_ID = "ca-app-pub-3940256099942544/4411468910";
        public static readonly string ANDROID_REWARDED_TEST_ID = "ca-app-pub-3940256099942544/5224354917";
        public static readonly string IOS_REWARDED_TEST_ID = "ca-app-pub-3940256099942544/1712485313";
        public static readonly string ANDROID_APP_TEST_ID = "ca-app-pub-3940256099942544~3347511713";
        public static readonly string IOS_APP_TEST_ID = "ca-app-pub-3940256099942544~1458002511";

        // Application ID
        [Header("Application ID")]
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

        [SerializeField] BannerPlacementType bannerType = BannerPlacementType.Banner;
        public BannerPlacementType BannerType => bannerType;

        [Header("Test Devices")]
        [SerializeField] List<string> testDevicesIDs = new List<string>();
        public List<string> TestDevicesIDs => testDevicesIDs;

        public enum BannerPlacementType
        {
            Banner = 0,
            MediumRectangle = 1,
            IABBanner = 2,
            Leaderboard = 3
        }
    }
}