#pragma warning disable 0414

using MobileCore.Advertisements.Providers;
using UnityEngine;

namespace MobileCore.Advertisements
{
    [HelpURL("https://quick-setup-website.pages.dev/documentation/mobile-monetization/ads-inspector/")]
    [CreateAssetMenu(fileName = "AdsSettings", menuName = "Mobile Core/Settings/Ads Settings")]
    public class AdsSettings : ScriptableObject, ISerializationCallbackReceiver
    {
        [InspectorName("Banner")]
        [Tooltip("Ad provider used for banner ads.")]
        [SerializeField] private AdProvider bannerType = AdProvider.Disable;
        public AdProvider BannerType => bannerType;

        [InspectorName("Interstitial")]
        [Tooltip("Ad provider used for interstitial ads.")]
        [SerializeField] private AdProvider interstitialType = AdProvider.Disable;
        public AdProvider InterstitialType => interstitialType;

        [InspectorName("Rewarded")]
        [Tooltip("Ad provider used for rewarded video ads.")]
        [SerializeField] private AdProvider rewardedVideoType = AdProvider.Disable;
        public AdProvider RewardedVideoType => rewardedVideoType;

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            ValidateProviderSettings();
        }

        private void OnValidate()
        {
            ValidateProviderSettings();
        }

        private void ValidateProviderSettings()
        {
            if (!System.Enum.IsDefined(typeof(AdProvider), bannerType))
            {
                bannerType = AdProvider.Disable;
            }

            if (!System.Enum.IsDefined(typeof(AdProvider), interstitialType))
            {
                interstitialType = AdProvider.Disable;
            }

            if (!System.Enum.IsDefined(typeof(AdProvider), rewardedVideoType))
            {
                rewardedVideoType = AdProvider.Disable;
            }
        }

        [AdsProviderContainer("AdMob", 1, "https://developers.google.com/admob/unity/quick-start", dashboardUrl: "https://apps.admob.com")]
        [SerializeField] private AdMobContainer adMobContainer;
        public AdMobContainer AdMobContainer => adMobContainer;

        [AdsProviderContainer("Unity Ads", 2, "com.unity.ads", true, dashboardUrl: "https://dashboard.unity3d.com/monetization")]
        [SerializeField] private UnityAdsLegacyContainer unityAdsContainer;
        public UnityAdsLegacyContainer UnityAdsContainer => unityAdsContainer;

        [AdsProviderContainer("LevelPlay", 3, "com.unity.services.levelplay", true, dashboardUrl: "https://dashboard.unity3d.com/monetization")]
        [SerializeField] private LevelPlayContainer levelPlayContainer;
        public LevelPlayContainer LevelPlayContainer => levelPlayContainer;

        [AdsProviderContainer("AppLovin", 4, "https://dash.applovin.com/documentation/mediation/unity/getting-started", dashboardUrl: "https://dash.applovin.com")]
        [SerializeField] private AppLovinContainer appLovinContainer;
        public AppLovinContainer AppLovinContainer => appLovinContainer;


        [Tooltip("Enables logging. Use it to debug advertisement logic.")]
        [SerializeField] private bool systemLogs = false;
        public bool SystemLogs => systemLogs;

        [Space]
        [InspectorName("First Delay (s)")]
        [Tooltip("Delay in seconds before the first interstitial can appear on the very first game launch (first-time install only).")]
        [SerializeField] private float interstitialFirstStartDelay = 40f;
        public float InterstitialFirstStartDelay => interstitialFirstStartDelay;

        [InspectorName("Subsequent Delay (s)")]
        [Tooltip("Delay in seconds before the first interstitial can appear on subsequent game launches (second launch and beyond).")]
        [SerializeField] private float interstitialStartDelay = 40f;
        public float InterstitialStartDelay => interstitialStartDelay;

        [InspectorName("Min Show Delay (s)")]
        [Tooltip("Minimum delay in seconds between consecutive interstitial ads. Resets every time an interstitial is successfully shown.")]
        [SerializeField] private float interstitialShowingDelay = 30f;
        public float InterstitialShowingDelay => interstitialShowingDelay;

        [Space]
        [InspectorName("Ads On Start")]
        [Tooltip("Load and display ads automatically when the application starts.")]
        [SerializeField] private bool adOnStart = true;
        public bool AdOnStart => adOnStart;

        [InspectorName("Reward If NoAds")]
        [Tooltip("If NoAds is purchased, skip the rewarded video ad and grant the reward directly. " +
                 "Common UX pattern: players who paid should not be forced to watch ads to earn rewards.")]
        [SerializeField] private bool grantRewardIfNoAds = true;
        public bool GrantRewardIfNoAds => grantRewardIfNoAds;

        [InspectorName("Auto Show Interstitial")]
        [Tooltip("If enabled, interstitials will be shown automatically based on the delays configured below.")]
        [SerializeField] private bool autoShowInterstitial;
        public bool AutoShowInterstitial => autoShowInterstitial;

        [InspectorName("GDPR Enabled")]
        [Tooltip("Show a GDPR consent panel on first launch before initializing any ad provider.")]
        [SerializeField] private bool isGDPREnabled = false;
        public bool IsGDPREnabled => isGDPREnabled;

        [InspectorName("GDPR Prefab")]
        [Tooltip("The GDPR consent panel prefab. Automatically assigned by default.")]
        [SerializeField] private GameObject gdprPrefab;
        public GameObject GDPRPrefab => gdprPrefab;

#if UNITY_EDITOR
        public void SetDefaultGDPRPrefab(GameObject prefab)
        {
            if (gdprPrefab == null)
            {
                gdprPrefab = prefab;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif

        [InspectorName("IDFA Enabled")]
        [Tooltip("Request App Tracking Transparency (ATT) permission on iOS before initializing ads.")]
        [SerializeField] private bool isIDFAEnabled = false;
        public bool IsIDFAEnabled => isIDFAEnabled;

        [InspectorName("Privacy Link")]
        [Tooltip("URL to your app's privacy policy page.")]
        [SerializeField] private string privacyLink = "https://mywebsite.com/privacy";
        public string PrivacyLink => privacyLink;

        [InspectorName("Terms Link")]
        [Tooltip("URL to your app's terms of use page.")]
        [SerializeField] private string termsOfUseLink = "https://mywebsite.com/terms";
        public string TermsOfUseLink => termsOfUseLink;
    }

    public enum AdProvider
    {
        Disable = 0,
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

    }
}