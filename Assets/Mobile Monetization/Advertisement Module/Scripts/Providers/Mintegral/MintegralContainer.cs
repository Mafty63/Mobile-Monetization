using System.Collections.Generic;
using UnityEngine;

namespace MobileCore.Advertisements.Providers
{
    [System.Serializable]
    public class MintegralContainer
    {
        // Mintegral Test App IDs
        public static readonly string ANDROID_APP_ID_TEMPLATE = "118690";
        public static readonly string IOS_APP_ID_TEMPLATE = "118692";
        public static readonly string ANDROID_APP_KEY_TEMPLATE = "7c22942a749fe6a6e361c675ddc4e5a9";
        public static readonly string IOS_APP_KEY_TEMPLATE = "854d0a8c6b34f5ae84905b87a4b6c5b7";

        // Mintegral Test Unit IDs
        public static readonly string ANDROID_BANNER_TEST_ID = "176869";
        public static readonly string IOS_BANNER_TEST_ID = "176870";
        public static readonly string ANDROID_INTERSTITIAL_TEST_ID = "176873";
        public static readonly string IOS_INTERSTITIAL_TEST_ID = "176874";
        public static readonly string ANDROID_REWARDED_VIDEO_TEST_ID = "176875";
        public static readonly string IOS_REWARDED_VIDEO_TEST_ID = "176876";

        [SerializeField] private string androidAppId = ANDROID_APP_ID_TEMPLATE;
        public string AndroidAppId => androidAppId;

        [SerializeField] private string iosAppId = IOS_APP_ID_TEMPLATE;
        public string IOSAppId => iosAppId;

        [SerializeField] private string androidAppKey = ANDROID_APP_KEY_TEMPLATE;
        public string AndroidAppKey => androidAppKey;

        [SerializeField] private string iosAppKey = IOS_APP_KEY_TEMPLATE;
        public string IOSAppKey => iosAppKey;

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
    }
}