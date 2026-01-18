// Summary

#pragma warning disable 0649
#pragma warning disable 0162
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

using MobileCore.Advertisements.Providers;
using MobileCore.SystemModule;
using MobileCore.Utilities;
using MobileCore.IAPModule;
using MobileCore.DefineSystem;

namespace MobileCore.Advertisements
{
    [Define("ADMOB_PROVIDER", "GoogleMobileAds.Api.MobileAds", "Google AdMob SDK")]
    [Define("LEVELPLAY_PROVIDER", "IronSource", "IronSource LevelPlay SDK")]
    [Define("UNITYADS_PROVIDER", "UnityEngine.Advertisements.Advertisement", "Unity Ads SDK")]
    [Define("APPLOVIN_PROVIDER", "MaxSdk", "AppLovin MAX SDK")]
    [Define("VUNGLE_PROVIDER", "Vungle.VungleAd", "Vungle SDK")]
    [Define("MINTEGRAL_PROVIDER", "Mintegral", "Mintegral SDK")]
    [Define("CHARTBOOST_PROVIDER", "Chartboost.Mediation.ChartboostMediation", "Chartboost SDK")]
    [Define("META_AUDIENCE_NETWORK_PROVIDER", "AudienceNetwork.AudienceNetworkAds", "Meta Audience Network SDK")]
    public static class AdsManager
    {
        // Constants
        private const ProductKeyType NO_ADS_PRODUCT_KEY = ProductKeyType.NoAds;
        private const int INIT_ATTEMPTS_AMOUNT = 30;
        private const string FIRST_LAUNCH_PREFS = "FIRST_LAUNCH";
        private const string NO_ADS_PREF_NAME = "ADS_STATE";
        private const string NO_ADS_ACTIVE_HASH = "809d08040da0182f4fffa4702095e69e";
        private const string GDPR_PREF_NAME = "GDPR_STATE";

        // Ad Providers
        private static readonly BaseAdProviderHandler[] AD_PROVIDERS = new BaseAdProviderHandler[]
        {
            new AdDummyHandler(AdProvider.Dummy), 
        #if ADMOB_PROVIDER
            new AdMobHandler(AdProvider.AdMob), 
        #endif
        #if APPLOVIN_PROVIDER
            new AppLovinHandler(AdProvider.AppLovin),
        #endif
        #if UNITYADS_PROVIDER
            new UnityAdsLegacyHandler(AdProvider.UnityAds), 
        #endif
        #if LEVELPLAY_PROVIDER
            new LevelPlayHandler(AdProvider.LevelPlay),
        #endif
        #if MINTEGRAL_PROVIDER
            new MintegralHandler(AdProvider.Mintegral),
        #endif
        #if CHARTBOOST_PROVIDER
            new ChartboostHandler(AdProvider.Chartboost),
        #endif
        #if VUNGLE_PROVIDER
            new VungleHandler(AdProvider.Vungle),
        #endif
        #if META_AUDIENCE_NETWORK_PROVIDER
            new MetaAudienceNetworkHandler(AdProvider.MetaAudienceNetwork),
        #endif
        };

        // Module state variables
        private static bool isModuleInitialized;
        private static AdsSettings settings;
        public static AdsSettings Settings => settings;
        private static double lastInterstitialTime;
        private static BaseAdProviderHandler.RewardedVideoCallback rewardedVideoCallback;
        private static BaseAdProviderHandler.InterstitialCallback interstitialCallback;
        private static List<PrimitiveCallback> mainThreadEvents = new List<PrimitiveCallback>();
        private static int mainThreadEventsCount;
        private static bool isFirstAdLoaded = false;
        private static bool waitingForRewardVideoCallback;
        private static bool isBannerActive = true;
        private static Coroutine loadingCoroutine;
        private static bool isForcedAdEnabled;
        
        /// <summary>
        /// Delay in seconds to auto-show banner if it's hidden. 0 to disable.
        /// </summary>
        public static float BannerAutoShowDelay = 0;
        private static double nextBannerAutoShowTime;
        private static bool isBannerActuallyVisible = false;

        // Active modules
        private static Dictionary<AdProvider, BaseAdProviderHandler> advertisingActiveHandlers = new Dictionary<AdProvider, BaseAdProviderHandler>();
        private static AdsManagerInitializer initModule;
        public static AdsManagerInitializer InitModule => initModule;

        // Events
        public static event PrimitiveCallback ForcedAdDisabled;
        public static event AdsModuleCallback AdProviderInitialized;
        public static event AdsEventsCallback AdLoaded;
        public static event AdsEventsCallback AdDisplayed;
        public static event AdsEventsCallback AdClosed;
        public static AdsBoolCallback InterstitialConditions;
        
        /// <summary>
        /// Event triggered when banner ad is shown. UI adapters can subscribe to this.
        /// </summary>
        public static event PrimitiveCallback BannerShown;
        
        /// <summary>
        /// Event triggered when banner ad is hidden. UI adapters can subscribe to this.
        /// </summary>
        public static event PrimitiveCallback BannerHidden;

        // Delegates
        public delegate void AdsModuleCallback(AdProvider advertisingModules);
        public delegate void AdsEventsCallback(AdProvider advertisingModules, AdType advertisingType);
        public delegate bool AdsBoolCallback();

        #region Initialize

        /// <summary>
        /// Initialize Ads Module from initializer.
        /// </summary>
        /// <param name="adsManagerInitModule"></param>
        /// <param name="loadOnStart"></param>
        public static void Initialize(AdsManagerInitializer managerInitializer)
        {
            if (isModuleInitialized)
            {
                LogWarning("[AdsManager]: Module already exists!");
                return;
            }

            isModuleInitialized = true;
            isFirstAdLoaded = false;
            initModule = managerInitializer;
            settings = managerInitializer.Settings;

            isForcedAdEnabled = IsForcedAdEnabled(false);

            // Set first launch preferences
            if (!PlayerPrefs.HasKey(FIRST_LAUNCH_PREFS))
            {
                lastInterstitialTime = Time.time + settings.InterstitialFirstStartDelay;
                PlayerPrefs.SetInt(FIRST_LAUNCH_PREFS, 1);
            }
            else
            {
                lastInterstitialTime = Time.time + settings.InterstitialStartDelay;
            }

            MonoBehaviourExecution.RegisterUpdate(OnUpdate);

            // Populate active ad modules
            foreach (var provider in AD_PROVIDERS)
            {
                if (IsModuleEnabled(provider.ProviderType))
                {
                    advertisingActiveHandlers.Add(provider.ProviderType, provider);
                }
            }

            // Log warnings for misconfigured ad types
            // Log warnings for misconfigured ad types
            if (settings.BannerType != AdProvider.Disable && !advertisingActiveHandlers.ContainsKey(settings.BannerType))
                LogWarning("[AdsManager]: Banner type (" + settings.BannerType + ") is selected, but isn't active!");

            if (settings.InterstitialType != AdProvider.Disable && !advertisingActiveHandlers.ContainsKey(settings.InterstitialType))
                LogWarning("[AdsManager]: Interstitial type (" + settings.InterstitialType + ") is selected, but isn't active!");

            if (settings.RewardedVideoType != AdProvider.Disable && !advertisingActiveHandlers.ContainsKey(settings.RewardedVideoType))
                LogWarning("[AdsManager]: Rewarded Video type (" + settings.RewardedVideoType + ") is selected, but isn't active!");

            IAPManager.OnPurchaseComplete += OnRemoveAdsPurchaseComplete;

            if (settings.IsGDPREnabled && !IsGDPRStateExist())
            {
                ShowGDPRPanel(() =>
                {
                    InitializeAllProvider(settings.AdOnStart);
                });
                return;
            }

            InitializeAllProvider(settings.AdOnStart);
        }

        private static void ShowGDPRPanel(System.Action onCompleted)
        {
            GameObject gdprPanelObject = GameObject.Instantiate(InitModule.GDPRPrefab);
            gdprPanelObject.transform.ResetGlobal();

            GDPRPanel gdprPanel = gdprPanelObject.GetComponent<GDPRPanel>();
            gdprPanel.Initialize(onCompleted);
        }

        /// <summary>
        /// Initialize all ads provider.
        /// </summary>
        /// <param name="loadAds"></param>
        private static void InitializeAllProvider(bool loadAds)
        {
            foreach (var advertisingModule in advertisingActiveHandlers.Keys)
            {
                InitializeProvider(advertisingModule);
            }

            if (loadAds)
            {
                TryToLoadFirstAds();
            }
        }

        /// <summary>
        /// Initialize ads provider.
        /// </summary>
        /// <param name="advertisingModule"></param>
        private static void InitializeProvider(AdProvider advertisingModule)
        {
            if (advertisingActiveHandlers.ContainsKey(advertisingModule))
            {
                if (!advertisingActiveHandlers[advertisingModule].IsInitialized())
                {
                    Log("[AdsManager]: Module " + advertisingModule.ToString() + " trying to initialize!");

                    advertisingActiveHandlers[advertisingModule].Initialize(settings);
                }
                else
                {
                    Log("[AdsManager]: Module " + advertisingModule.ToString() + " is already initialized!");
                }
            }
            else
            {
                LogWarning("[AdsManager]: Module " + advertisingModule.ToString() + " is disabled!");
            }
        }

        #endregion

        /// <summary>
        /// Called every frame.
        /// </summary>
        private static void OnUpdate()
        {
            // Execute queued main thread events
            if (mainThreadEventsCount > 0)
            {
                for (int i = 0; i < mainThreadEventsCount; i++)
                {
                    mainThreadEvents[i]?.Invoke();
                }

                mainThreadEvents.Clear();
                mainThreadEventsCount = 0;
            }

            // Automatically show interstitial ads if enabled
            if (settings.AutoShowInterstitial && lastInterstitialTime < Time.time)
            {
                ShowInterstitial(null);
                ResetInterstitialDelayTime();
            }

            // Auto Show Banner Logic
            if (BannerAutoShowDelay > 0 && !isBannerActuallyVisible && isBannerActive)
            {
                if (Time.time > nextBannerAutoShowTime)
                {
                    Log("[AdsManager]: Auto-showing Banner...");
                    ShowBanner();
                }
            }
        }

        /// <summary>
        /// Attempts to load the first set of ads.
        /// </summary>
        public static void TryToLoadFirstAds()
        {
            if (loadingCoroutine == null)
                loadingCoroutine = MonoBehaviourExecution.InvokeCoroutine(TryToLoadAdsCoroutine());
        }

        private static IEnumerator TryToLoadAdsCoroutine()
        {
            int initAttempts = 0;
            yield return new WaitForSeconds(1.0f);

            while (!isFirstAdLoaded && initAttempts <= INIT_ATTEMPTS_AMOUNT)
            {
                if (LoadFirstAds())
                    break;

                yield return new WaitForSeconds(1.0f * (initAttempts + 1));
                initAttempts++;
            }

            Log("[AdsManager]: First ads have loaded!");
        }

        private static bool LoadFirstAds()
        {
            if (isFirstAdLoaded)
                return true;

            if (settings.IsGDPREnabled && !IsGDPRStateExist())
                return false;

            if (settings.IsIDFAEnabled && !IsIDFADetermined())
                return false;

            bool isRewardedVideoModuleInitialized = IsModuleInititalized(Settings.RewardedVideoType);
            bool isInterstitialModuleInitialized = IsModuleInititalized(Settings.InterstitialType);
            bool isBannerModuleInitialized = IsModuleInititalized(Settings.BannerType);

            bool isRewardedVideoActive = Settings.RewardedVideoType != AdProvider.Disable;
            bool isInterstitialActive = Settings.InterstitialType != AdProvider.Disable;
            bool isBannerActive = Settings.BannerType != AdProvider.Disable;

            if ((!isRewardedVideoActive || isRewardedVideoModuleInitialized) &&
                (!isInterstitialActive || isInterstitialModuleInitialized) &&
                (!isBannerActive || isBannerModuleInitialized))
            {
                if (isRewardedVideoActive)
                    RequestRewardBasedVideo();

                bool isForcedAdEnabled = IsForcedAdEnabled(false);

                if (isInterstitialActive && isForcedAdEnabled)
                    RequestInterstitial();

                if (isBannerActive && isForcedAdEnabled)
                    ShowBanner();

                isFirstAdLoaded = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Executes a callback on the main thread.
        /// </summary>
        /// <param name="callback"></param>
        public static void CallEventInMainThread(PrimitiveCallback callback)
        {
            if (callback != null)
            {
                mainThreadEvents.Add(callback);
                mainThreadEventsCount++;
            }
        }

        /// <summary>
        /// Displays an error message when network issues occur.
        /// </summary>
        public static void ShowErrorMessage()
        {
            Log("[AdsManager]: Network error. Please try again later");
            SystemManager.ShowMessage("Network error. Please try again later");
        }

        /// <summary>
        /// Checks if an ad module is enabled.
        /// </summary>
        /// <param name="advertisingModule"></param>
        /// <returns></returns>
        public static bool IsModuleEnabled(AdProvider advertisingModule)
        {
            if (advertisingModule == AdProvider.Disable)
                return false;

            return (Settings.BannerType == advertisingModule ||
                    Settings.InterstitialType == advertisingModule ||
                    Settings.RewardedVideoType == advertisingModule);
        }

        /// <summary>
        /// Checks if an ad module is active.
        /// </summary>
        /// <param name="advertisingModule"></param>
        /// <returns></returns>
        public static bool IsModuleActive(AdProvider advertisingModule)
        {
            return advertisingActiveHandlers.ContainsKey(advertisingModule);
        }

        /// <summary>
        /// Checks if an ad module is initialized.
        /// </summary>
        /// <param name="advertisingModule"></param>
        /// <returns></returns>
        public static bool IsModuleInititalized(AdProvider advertisingModule)
        {
            if (advertisingActiveHandlers.ContainsKey(advertisingModule))
            {
                return advertisingActiveHandlers[advertisingModule].IsInitialized();
            }

            return false;
        }

        #region Interstitial

        /// <summary>
        /// Checks if an interstitial ad is loaded.
        /// </summary>
        /// <returns></returns>
        public static bool IsInterstitialLoaded()
        {
            if (!isModuleInitialized)
            {
                LogWarning("[AdsManager]: Module is not initialized.");
                return false;
            }
            return IsInterstitialLoaded(settings.InterstitialType);
        }

        /// <summary>
        /// Checks if an interstitial ad is loaded for a specific ad provider.
        /// </summary>
        /// <param name="advertisingModules"></param>
        /// <returns></returns>
        public static bool IsInterstitialLoaded(AdProvider advertisingModules)
        {
            if (!isForcedAdEnabled || !IsModuleActive(advertisingModules))
                return false;

            return advertisingActiveHandlers[advertisingModules].IsInterstitialLoaded();
        }

        /// <summary>
        /// Requests an interstitial ad.
        /// </summary>
        public static void RequestInterstitial()
        {
            if (!isModuleInitialized)
            {
                LogWarning("[AdsManager]: Module is not initialized.");
                return;
            }

            AdProvider advertisingModules = settings.InterstitialType;

            if (!isForcedAdEnabled || !IsModuleActive(advertisingModules) ||
                !advertisingActiveHandlers[advertisingModules].IsInitialized() ||
                advertisingActiveHandlers[advertisingModules].IsInterstitialLoaded())
                return;

            advertisingActiveHandlers[advertisingModules].RequestInterstitial();
        }

        /// <summary>
        /// Shows an interstitial ad.
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="ignoreConditions"></param>
        public static void ShowInterstitial(BaseAdProviderHandler.InterstitialCallback callback, bool ignoreConditions = false)
        {
            if (!isModuleInitialized)
            {
                LogWarning("[AdsManager]: Module is not initialized.");
                return;
            }

            AdProvider advertisingModules = settings.InterstitialType;
            interstitialCallback = callback;

            if (!isForcedAdEnabled || !IsModuleActive(advertisingModules) ||
                (!ignoreConditions && (!CheckInterstitialTime() || !CheckExtraInterstitialCondition())) ||
                !advertisingActiveHandlers[advertisingModules].IsInitialized() ||
                !advertisingActiveHandlers[advertisingModules].IsInterstitialLoaded())
            {
                ExecuteInterstitialCallback(false);
                return;
            }

            advertisingActiveHandlers[advertisingModules].ShowInterstitial(callback);
        }

        /// <summary>
        /// Executes the interstitial callback with the result.
        /// </summary>
        /// <param name="result"></param>
        public static void ExecuteInterstitialCallback(bool result)
        {
            if (interstitialCallback != null)
            {
                CallEventInMainThread(() => interstitialCallback.Invoke(result));
            }
        }

        /// <summary>
        /// Sets the interstitial delay time.
        /// This is used to prevent showing interstitial ads too frequently.
        /// </summary>
        /// <param name="time"></param>
        public static void SetInterstitialDelayTime(float time)
        {
            lastInterstitialTime = Time.time + time;
        }

        /// <summary>
        /// Resets the interstitial delay time to the default value.
        /// This is used to allow interstitial ads to be shown again after a certain period.
        /// </summary>
        public static void ResetInterstitialDelayTime()
        {
            lastInterstitialTime = Time.time + settings.InterstitialShowingDelay;
        }

        private static bool CheckInterstitialTime()
        {
            Log("[AdsManager]: Interstitial Time: " + lastInterstitialTime + "; Time: " + Time.time);

            return lastInterstitialTime < Time.time;
        }

        /// <summary>
        /// Checks if the extra interstitial condition is met.
        /// This is used to determine if an interstitial ad can be shown based on additional conditions.
        /// </summary>
        /// <returns></returns>
        public static bool CheckExtraInterstitialCondition()
        {
            if (InterstitialConditions != null)
            {
                bool state = true;
                System.Delegate[] listDelegates = InterstitialConditions.GetInvocationList();

                foreach (var del in listDelegates)
                {
                    if (!(bool)del.DynamicInvoke())
                    {
                        state = false;
                        break;
                    }
                }

                Log("[AdsManager]: Extra condition interstitial state: " + state);

                return state;
            }

            return true;
        }

        #endregion

        #region Rewarded Video

        /// <summary>
        /// Checks if a rewarded video ad is loaded.
        /// </summary>
        /// <returns></returns>
        public static bool IsRewardBasedVideoLoaded()
        {
            if (!isModuleInitialized)
            {
                LogWarning("[AdsManager]: Module is not initialized.");
                return false;
            }

            AdProvider advertisingModule = settings.RewardedVideoType;

            if (!IsModuleActive(advertisingModule) || !advertisingActiveHandlers[advertisingModule].IsInitialized())
                return false;

            return advertisingActiveHandlers[advertisingModule].IsRewardedVideoLoaded();
        }

        /// <summary>
        /// Requests a rewarded video ad.
        /// </summary>
        public static void RequestRewardBasedVideo()
        {
            if (!isModuleInitialized)
            {
                LogWarning("[AdsManager]: Module is not initialized.");
                return;
            }

            AdProvider advertisingModule = settings.RewardedVideoType;

            if (!IsModuleActive(advertisingModule) ||
                !advertisingActiveHandlers[advertisingModule].IsInitialized() ||
                advertisingActiveHandlers[advertisingModule].IsRewardedVideoLoaded())
                return;

            advertisingActiveHandlers[advertisingModule].RequestRewardedVideo();
        }

        /// <summary>
        /// Shows a rewarded video ad.
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="showErrorMessage"></param>
        public static void ShowRewardBasedVideo(BaseAdProviderHandler.RewardedVideoCallback callback, bool showErrorMessage = true)
        {
            if (!isModuleInitialized)
            {
                LogWarning("[AdsManager]: Module is not initialized.");
                return;
            }

            AdProvider advertisingModule = settings.RewardedVideoType;
            rewardedVideoCallback = callback;
            waitingForRewardVideoCallback = true;

            if (!IsModuleActive(advertisingModule) ||
                !advertisingActiveHandlers[advertisingModule].IsInitialized() ||
                !advertisingActiveHandlers[advertisingModule].IsRewardedVideoLoaded())
            {
                ExecuteRewardVideoCallback(false);

                if (showErrorMessage)
                    ShowErrorMessage();

                return;
            }

            advertisingActiveHandlers[advertisingModule].ShowRewardedVideo(callback);
        }

        /// <summary>
        /// Executes the rewarded video callback with the result.
        /// </summary>
        /// <param name="result"></param>
        public static void ExecuteRewardVideoCallback(bool result)
        {
            if (rewardedVideoCallback != null && waitingForRewardVideoCallback)
            {
                CallEventInMainThread(() => rewardedVideoCallback.Invoke(result));
                waitingForRewardVideoCallback = false;

                Log("[AdsManager]: Reward received: " + result);            }
        }

        #endregion

        #region Banner
        /// <summary>
        /// show banner ad.
        /// </summary>
        public static void ShowBanner()
        {
            if (!isModuleInitialized)
            {
                LogWarning("[AdsManager]: Module is not initialized.");
                return;
            }

            if (!isBannerActive) return;
            Log("[AdsManager]: Show Banner");
            AdProvider advertisingModule = settings.BannerType;

            if (!isForcedAdEnabled || !IsModuleActive(advertisingModule) ||
                !advertisingActiveHandlers[advertisingModule].IsInitialized())
                return;

            advertisingActiveHandlers[advertisingModule].ShowBanner();
            isBannerActuallyVisible = true; // Mark as visible
            
            // Notify UI adapters that banner is shown
            BannerShown?.Invoke();
        }

        /// <summary>
        /// Destroys the banner ad.
        /// </summary>
        public static void DestroyBanner()
        {
            if (!isModuleInitialized)
            {
                LogWarning("[AdsManager]: Module is not initialized.");
                return;
            }

            AdProvider advertisingModule = settings.BannerType;

            if (!IsModuleActive(advertisingModule) ||
                !advertisingActiveHandlers[advertisingModule].IsInitialized())
                return;

            advertisingActiveHandlers[advertisingModule].DestroyBanner();
        }

        /// <summary>
        /// Hides the banner ad.
        /// </summary>
        public static void HideBanner()
        {
            if (!isModuleInitialized)
            {
                LogWarning("[AdsManager]: Module is not initialized.");
                return;
            }

            AdProvider advertisingModule = settings.BannerType;

            if (!IsModuleActive(advertisingModule) ||
                !advertisingActiveHandlers[advertisingModule].IsInitialized())
                return;

            advertisingActiveHandlers[advertisingModule].HideBanner();
            isBannerActuallyVisible = false; // Mark as hidden
            nextBannerAutoShowTime = Time.time + BannerAutoShowDelay; // Reset timer
            
            // Notify UI adapters that banner is hidden
            BannerHidden?.Invoke();
        }

        /// <summary>
        /// Enable the banner ad.
        /// This method is used to show the banner ad when it is enabled.
        /// </summary>
        public static void EnableBanner()
        {
            if (!isModuleInitialized)
            {
                LogWarning("[AdsManager]: Module is not initialized.");
                return;
            }

            isBannerActive = true;
            ShowBanner();
        }

        /// <summary>
        /// Disable the banner ad.
        /// This method is used to hide the banner ad when it is disabled.
        /// </summary>
        public static void DisableBanner()
        {
            if (!isModuleInitialized)
            {
                LogWarning("[AdsManager]: Module is not initialized.");
                return;
            }

            isBannerActive = false;
            HideBanner();
        }

        #endregion

        #region Event
        /// <summary>
        /// Invoked when an ad provider is initialized.
        /// </summary>
        /// <param name="advertisingModule"></param>
        public static void OnProviderInitialized(AdProvider advertisingModule)
        {
            AdProviderInitialized?.Invoke(advertisingModule);
        }

        /// <summary>
        /// Invoked when an ad is loaded.
        /// </summary>
        /// <param name="advertisingModule"></param>
        /// <param name="advertisingType"></param>
        public static void OnProviderAdLoaded(AdProvider advertisingModule, AdType advertisingType)
        {
            AdLoaded?.Invoke(advertisingModule, advertisingType);
        }

        /// <summary>
        /// Invoked when an ad is displayed.
        /// </summary>
        /// <param name="advertisingModule"></param>
        /// <param name="advertisingType"></param>
        public static void OnProviderAdDisplayed(AdProvider advertisingModule, AdType advertisingType)
        {
            AdDisplayed?.Invoke(advertisingModule, advertisingType);

            if (advertisingType == AdType.Interstitial || advertisingType == AdType.RewardedVideo)
            {
                ResetInterstitialDelayTime();
            }
        }

        /// <summary>
        /// Invoked when an ad is closed.
        /// </summary>
        /// <param name="advertisingModule"></param>
        /// <param name="advertisingType"></param>
        public static void OnProviderAdClosed(AdProvider advertisingModule, AdType advertisingType)
        {
            AdClosed?.Invoke(advertisingModule, advertisingType);

            if (advertisingType == AdType.Interstitial || advertisingType == AdType.RewardedVideo)
            {
                ResetInterstitialDelayTime();
            }
            else if (advertisingType == AdType.Banner)
            {
                // Native close detected
                isBannerActuallyVisible = false;
                nextBannerAutoShowTime = Time.time + BannerAutoShowDelay;
                Log("[AdsManager]: Banner closed. Resetting auto-show timer.");
            }
        }

        #endregion

        #region IAP

        private static void OnRemoveAdsPurchaseComplete(ProductKeyType productKeyType)
        {
            if (productKeyType == NO_ADS_PRODUCT_KEY)
            {
                DisableForcedAd();
            }
        }

        /// <summary>
        /// Checks if the forced ad is enabled.
        /// This is used to determine if ads should be displayed or not.
        /// </summary>
        /// <param name="useCachedValue"></param>
        /// <returns></returns>
        public static bool IsForcedAdEnabled(bool useCachedValue = true)
        {
            if (!isModuleInitialized)
            {
                LogWarning("[AdsManager]: Module is not initialized.");
                return false;
            }

            if (useCachedValue)
                return isForcedAdEnabled;

            return !PlayerPrefs.GetString(NO_ADS_PREF_NAME, "").Equals(NO_ADS_ACTIVE_HASH);
        }

        /// <summary>
        /// Enables the forced ad.
        /// This is used to show ads when the user has not purchased the no-ads option.
        /// </summary>
        public static void DisableForcedAd()
        {
            if (!isModuleInitialized)
            {
                LogWarning("[AdsManager]: Module is not initialized.");
                return;
            }

            Log("[Ads Manager]: Banners and interstitials are disabled!");
            PlayerPrefs.SetString(NO_ADS_PREF_NAME, NO_ADS_ACTIVE_HASH);
            isForcedAdEnabled = false;
            ForcedAdDisabled?.Invoke();
            DestroyBanner();
        }

        #endregion

        #region GDPR
        /// <summary>
        /// Set the GDPR state.
        /// This is used to enable or disable GDPR compliance for ads.
        /// </summary>
        /// <param name="state"></param>
        public static void SetGDPR(bool state)
        {
            if (!isModuleInitialized)
            {
                LogWarning("[AdsManager]: Module is not initialized.");
                return;
            }

            PlayerPrefs.SetInt(GDPR_PREF_NAME, state ? 1 : 0);

            foreach (AdProvider activeModule in advertisingActiveHandlers.Keys)
            {
                if (advertisingActiveHandlers[activeModule].IsInitialized())
                {
                    advertisingActiveHandlers[activeModule].SetGDPR(state);
                }
                else
                {
                    InitializeProvider(activeModule);
                }
            }
        }

        /// <summary>
        /// Get the GDPR state.
        /// This is used to check if GDPR compliance is enabled or disabled for ads.
        /// </summary>
        /// <returns></returns>
        public static bool GetGDPRState()
        {
            if (!isModuleInitialized)
            {
                LogWarning("[AdsManager]: Module is not initialized.");
                return false;
            }

            return PlayerPrefs.GetInt(GDPR_PREF_NAME, 0) == 1;
        }

        public static bool IsGDPRStateExist()
        {
            if (!isModuleInitialized)
            {
                LogWarning("[AdsManager]: Module is not initialized.");
                return false;
            }

            return PlayerPrefs.HasKey(GDPR_PREF_NAME);
        }

        #endregion

        #region IDFA

        /// <summary>
        /// Check if IDFA is determined.
        /// This is used to check if the user has granted permission for IDFA tracking on iOS devices.
        /// </summary>
        /// <returns></returns>
        public static bool IsIDFADetermined()
        {
            if (!isModuleInitialized)
            {
                LogWarning("[AdsManager]: Module is not initialized.");
                return false;
            }

#if UNITY_IOS
            if (settings.IsIDFAEnabled)
            {
                return Unity.Advertisement.IosSupport.ATTrackingStatusBinding.GetAuthorizationTrackingStatus() !=
                       Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED;
            }
#endif
            return true;
        }

        #endregion
        /// <summary>
        /// Logs a message if system logs are enabled.
        /// </summary>
        private static void Log(string message)
        {
            if (settings != null && settings.SystemLogs)
            {
                Debug.Log(message);
            }
        }

        /// <summary>
        /// Logs a warning if system logs are enabled.
        /// </summary>
        private static void LogWarning(string message)
        {
            if (settings != null && settings.SystemLogs)
            {
                Debug.LogWarning(message);
            }
        }
    }
}
