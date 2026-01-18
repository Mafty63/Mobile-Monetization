using UnityEngine;

namespace MobileCore.Advertisements.Providers
{
    [System.Serializable]
    public class UnityAdsLegacyContainer
    {
        // Test IDs
        public static readonly string ANDROID_BANNER_TEST_ID = "banner";
        public static readonly string IOS_BANNER_TEST_ID = "banner";
        public static readonly string ANDROID_INTERSTITIAL_TEST_ID = "video";
        public static readonly string IOS_INTERSTITIAL_TEST_ID = "video";
        public static readonly string ANDROID_REWARDED_TEST_ID = "rewardedVideo";
        public static readonly string IOS_REWARDED_TEST_ID = "rewardedVideo";
        public static readonly string ANDROID_APP_TEST_ID = "1234567";
        public static readonly string IOS_APP_TEST_ID = "1234567";

        //Application ID
        [Header("Application ID")]
        [SerializeField] string androidAppID = ANDROID_APP_TEST_ID;
        public string AndroidAppID => androidAppID;
        [SerializeField] string iOSAppID = IOS_APP_TEST_ID;
        public string IOSAppID => iOSAppID;

        //Banner ID
        [Header("Banner ID")]
        [SerializeField] string androidBannerID = ANDROID_BANNER_TEST_ID;
        public string AndroidBannerID => androidBannerID;
        [SerializeField] string iOSBannerID = IOS_BANNER_TEST_ID;
        public string IOSBannerID => iOSBannerID;

        //Interstitial ID
        [Header("Interstitial ID")]
        [SerializeField] string androidInterstitialID = ANDROID_INTERSTITIAL_TEST_ID;
        public string AndroidInterstitialID => androidInterstitialID;
        [SerializeField] string iOSInterstitialID = IOS_INTERSTITIAL_TEST_ID;
        public string IOSInterstitialID => iOSInterstitialID;

        //Rewarded Video ID
        [Header("Rewarded Video ID")]
        [SerializeField] string androidRewardedVideoID = ANDROID_REWARDED_TEST_ID;
        public string AndroidRewardedVideoID => androidRewardedVideoID;
        [SerializeField] string iOSRewardedVideoID = IOS_REWARDED_TEST_ID;
        public string IOSRewardedVideoID => iOSRewardedVideoID;

        [Space]
        [SerializeField] BannerPlacement bannerPosition = BannerPlacement.BOTTOM_CENTER;
        public BannerPlacement BannerPosition => bannerPosition;
        public enum BannerPlacement
        {
            TOP_LEFT = 0,
            TOP_CENTER = 1,
            TOP_RIGHT = 2,
            BOTTOM_LEFT = 3,
            BOTTOM_CENTER = 4,
            BOTTOM_RIGHT = 5,
            CENTER = 6
        }
    }
}