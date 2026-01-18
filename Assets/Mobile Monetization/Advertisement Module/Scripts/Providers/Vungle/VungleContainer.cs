using System.Collections.Generic;
using UnityEngine;

namespace MobileCore.Advertisements.Providers
{
    [System.Serializable]
    public class VungleContainer
    {
        // ✅ Default Official Test App IDs from Vungle
        public static readonly string ANDROID_APP_ID_TEMPLATE = "5c6b1d416b3b280019a2c0a3";
        public static readonly string IOS_APP_ID_TEMPLATE = "5c6b1d416b3b280019a2c0a3";

        // ✅ Default Official Test Placement IDs from Vungle
        public static readonly string ANDROID_BANNER_TEST_ID = "BANNER-1468791";
        public static readonly string IOS_BANNER_TEST_ID = "BANNER-1468791";
        public static readonly string ANDROID_INTERSTITIAL_TEST_ID = "DEFAULT-1468791";
        public static readonly string IOS_INTERSTITIAL_TEST_ID = "DEFAULT-1468791";
        public static readonly string ANDROID_REWARDED_VIDEO_TEST_ID = "REWARDED-1468791";
        public static readonly string IOS_REWARDED_VIDEO_TEST_ID = "REWARDED-1468791";

        [SerializeField] private string androidAppId = ANDROID_APP_ID_TEMPLATE;
        public string AndroidAppId => androidAppId;

        [SerializeField] private string iosAppId = IOS_APP_ID_TEMPLATE;
        public string IOSAppId => iosAppId;

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

        [SerializeField] private BannerPlacement bannerSize = BannerPlacement.Banner;
        public BannerPlacement BannerSize => bannerSize;

        public enum BannerPlacement
        {
            Banner = 0,            // 320x50
            BannerShort = 1,       // 300x50  
            BannerLeaderboard = 2, // 728x90
            Mrec = 3               // 300x250
        }
    }
}
