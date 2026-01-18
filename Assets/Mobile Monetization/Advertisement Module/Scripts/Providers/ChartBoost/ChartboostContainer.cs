using System.Collections.Generic;
using UnityEngine;

namespace MobileCore.Advertisements.Providers
{
    [System.Serializable]
    public class ChartboostContainer
    {
        // Placeholder placement names / App ID / Signature — ganti sesuai dashboard Anda
        public static readonly string ANDROID_BANNER_TEST_ID = "default_banner";
        public static readonly string IOS_BANNER_TEST_ID = "default_banner";
        public static readonly string ANDROID_INTERSTITIAL_TEST_ID = "default_interstitial";
        public static readonly string IOS_INTERSTITIAL_TEST_ID = "default_interstitial";
        public static readonly string ANDROID_REWARDED_TEST_ID = "default_rewarded";
        public static readonly string IOS_REWARDED_TEST_ID = "default_rewarded";

        public static readonly string ANDROID_APP_TEST_ID = "test_android_app_id";
        public static readonly string IOS_APP_TEST_ID = "test_ios_app_id";
        public static readonly string ANDROID_APP_TEST_SIGNATURE = "test_android_app_signature";
        public static readonly string IOS_APP_TEST_SIGNATURE = "test_ios_app_signature";

        // Application ID / Signature
        [Header("Chartboost Application ID")]
        [SerializeField] string androidAppID = ANDROID_APP_TEST_ID;
        public string AndroidAppID => androidAppID;
        [SerializeField] string iOSAppID = IOS_APP_TEST_ID;
        public string IOSAppID => iOSAppID;

        [Header("Chartboost App Signature")]
        [SerializeField] string androidAppSignature = ANDROID_APP_TEST_SIGNATURE;
        public string AndroidAppSignature => androidAppSignature;
        [SerializeField] string iOSAppSignature = IOS_APP_TEST_SIGNATURE;
        public string IOSAppSignature => iOSAppSignature;

        // Placement names
        [Header("Banner Placement Name")]
        [SerializeField] string androidBannerID = ANDROID_BANNER_TEST_ID;
        public string AndroidBannerID => androidBannerID;
        [SerializeField] string iOSBannerID = IOS_BANNER_TEST_ID;
        public string IOSBannerID => iOSBannerID;

        [Header("Interstitial Placement Name")]
        [SerializeField] string androidInterstitialID = ANDROID_INTERSTITIAL_TEST_ID;
        public string AndroidInterstitialID => androidInterstitialID;
        [SerializeField] string iOSInterstitialID = IOS_INTERSTITIAL_TEST_ID;
        public string IOSInterstitialID => iOSInterstitialID;

        [Header("Rewarded Video Placement Name")]
        [SerializeField] string androidRewardedVideoID = ANDROID_REWARDED_TEST_ID;
        public string AndroidRewardedVideoID => androidRewardedVideoID;
        [SerializeField] string iOSRewardedVideoID = IOS_REWARDED_TEST_ID;
        public string IOSRewardedVideoID => iOSRewardedVideoID;

        [Header("Banner Settings (use enums to avoid manual sizes)")]
        [SerializeField] BannerPosition bannerPosition = BannerPosition.Bottom;
        public BannerPosition BannerPosition => bannerPosition;

        [SerializeField] BannerPlacementType bannerType = BannerPlacementType.Banner;
        public BannerPlacementType BannerType => bannerType;
        public enum BannerPlacementType
        {
            Banner = 0,           // 320x50
            MediumRectangle = 1,  // 300x250
            IABBanner = 2,        // 468x60
            Leaderboard = 3       // 728x90
        }
    }
}
