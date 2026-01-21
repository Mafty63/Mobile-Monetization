#pragma warning disable 0414

using MobileCore.Advertisements.Providers;
using UnityEngine;

namespace MobileCore.Advertisements
{
    public class AdsSettings : ScriptableObject
    {
        [SerializeField] private AdProvider bannerType = AdProvider.Dummy;
        public AdProvider BannerType => bannerType;

        [SerializeField] private AdProvider interstitialType = AdProvider.Dummy;
        public AdProvider InterstitialType => interstitialType;

        [SerializeField] private AdProvider rewardedVideoType = AdProvider.Dummy;
        public AdProvider RewardedVideoType => rewardedVideoType;

        [AdsProviderContainer("AdMob", 1, "https://developers.google.com/admob/unity/quick-start")]
        [SerializeField] private AdMobContainer adMobContainer;
        public AdMobContainer AdMobContainer => adMobContainer;

        [AdsProviderContainer("Unity Ads", 2, "https://docs.unity.com/ads/InstallingTheUnityAdsSDK.html")]
        [SerializeField] private UnityAdsLegacyContainer unityAdsContainer;
        public UnityAdsLegacyContainer UnityAdsContainer => unityAdsContainer;

        [AdsProviderContainer("LevelPlay", 3, "https://developers.is.com/ironsource-mobile/unity/sdk-integration-guide/")]
        [SerializeField] private LevelPlayContainer levelPlayContainer;
        public LevelPlayContainer LevelPlayContainer => levelPlayContainer;

        [AdsProviderContainer("AppLovin", 4, "https://dash.applovin.com/documentation/mediation/unity/getting-started")]
        [SerializeField] private AppLovinContainer appLovinContainer;
        public AppLovinContainer AppLovinContainer => appLovinContainer;

        [AdsProviderContainer("Vungle", 5, "https://support.vungle.com/hc/en-us/articles/360003455452-Integrate-Vungle-SDK-for-Unity")]
        [SerializeField] private VungleContainer vungleContainer;
        public VungleContainer VungleContainer => vungleContainer;

        [AdsProviderContainer("Mintegral", 6, "https://developer.mintegral.com/doc.html?channel=unity&id=1")]
        [SerializeField] private MintegralContainer mintegralContainer;
        public MintegralContainer MintegralContainer => mintegralContainer;

        [AdsProviderContainer("Chartboost", 7, "https://answers.chartboost.com/en-us/articles/download")]
        [SerializeField] private ChartboostContainer chartboostContainer;
        public ChartboostContainer ChartboostContainer => chartboostContainer;

        [AdsProviderContainer("Meta Audience Network", 8, "https://developers.facebook.com/docs/audience-network/setting-up/platform-steup/unity/add-sdk")]
        [SerializeField] private MetaAudienceNetworkContainer metaAudienceNetworkContainer;
        public MetaAudienceNetworkContainer MetaAudienceNetworkContainer => metaAudienceNetworkContainer;

        [AdsProviderContainer("Dummy", 1000)]
        [SerializeField] private AdDummyContainer dummyContainer;
        public AdDummyContainer DummyContainer => dummyContainer;


        [Tooltip("Enables development mode to setup advertisement providers.")]
        [SerializeField] private bool testMode = false;
        public bool TestMode => testMode;

        [Tooltip("Enables logging. Use it to debug advertisement logic.")]
        [SerializeField] private bool systemLogs = false;
        public bool SystemLogs => systemLogs;

        [Space]
        [Tooltip("Delay in seconds before interstitial appearings on first game launch. (first time playing)")]
        [SerializeField] private float interstitialFirstStartDelay = 40f;
        public float InterstitialFirstStartDelay => interstitialFirstStartDelay;

        [Tooltip("Delay in seconds before interstitial appearings.")]
        [SerializeField] private float interstitialStartDelay = 40f;
        public float InterstitialStartDelay => interstitialStartDelay;

        [Tooltip("Delay in seconds between interstitial appearings.")]
        [SerializeField] private float interstitialShowingDelay = 30f;
        public float InterstitialShowingDelay => interstitialShowingDelay;

        [Space]
        [SerializeField] private bool adOnStart = true;
        public bool AdOnStart => adOnStart;

        [SerializeField] private bool autoShowInterstitial;
        public bool AutoShowInterstitial => autoShowInterstitial;

        [SerializeField] private bool isGDPREnabled = false;
        public bool IsGDPREnabled => isGDPREnabled;

        [SerializeField] private bool isIDFAEnabled = false;
        public bool IsIDFAEnabled => isIDFAEnabled;

        [SerializeField] private string trackingDescription = "Your data will be used to deliver personalized ads to you.";
        public string TrackingDescription => trackingDescription;

        [SerializeField] private string privacyLink = "https://mywebsite.com/privacy";
        public string PrivacyLink => privacyLink;

        [SerializeField] private string termsOfUseLink = "https://mywebsite.com/terms";
        public string TermsOfUseLink => termsOfUseLink;

        public bool IsDummyEnabled()
        {
            if (bannerType == AdProvider.Dummy)
                return true;

            if (interstitialType == AdProvider.Dummy)
                return true;

            if (rewardedVideoType == AdProvider.Dummy)
                return true;

            return false;
        }
    }

    public enum AdProvider
    {
        Disable = 0,
        Dummy = 1,
#if ADMOB_PROVIDER
        AdMob = 2,
#endif
#if UNITYADS_PROVIDER
        UnityAds = 3,
#endif
#if LEVELPLAY_PROVIDER
        LevelPlay = 4,
#endif
#if APPLOVIN_PROVIDER
        AppLovin = 5,
#endif
#if VUNGLE_PROVIDER
        Vungle = 6,
#endif
#if MINTEGRAL_PROVIDER
        Mintegral = 7,
#endif
#if CHARTBOOST_PROVIDER
        Chartboost = 8,
#endif
#if META_AUDIENCE_NETWORK_PROVIDER
        MetaAudienceNetwork = 9,
#endif
    }
}