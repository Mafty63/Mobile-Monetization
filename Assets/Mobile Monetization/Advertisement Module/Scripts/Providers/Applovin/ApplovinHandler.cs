using UnityEngine;
using System;
using System.Collections;

#if APPLOVIN_PROVIDER
#endif

namespace MobileCore.Advertisements.Providers
{
#if APPLOVIN_PROVIDER
    public class AppLovinHandler : BaseAdProviderHandler
    {
        // Retry configuration
        private const int RETRY_ATTEMPT_DEFAULT_VALUE = 1;
        private int interstitialRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;
        private int rewardedRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;

        // State tracking untuk banner
        private bool isBannerCreated = false;
        private bool isBannerShown = false;

        // Callback references
        private InterstitialCallback currentInterstitialCallback;
        private RewardedVideoCallback currentRewardedCallback;

        private bool handlerDisposed = false;

        public AppLovinHandler(AdProvider providerType) : base(providerType) { }

        #region Initialization
        public override void Initialize(AdsSettings adsSettings)
        {
            if (isInitialized)
            {
                DebugLogWarning("[AppLovin]: Already initialized!");
                return;
            }

            this.adsSettings = adsSettings;
            DebugLog("[AppLovin]: Initializing...");

            try
            {
                // Set SDK key
                string sdkKey = GetSdkKey();
                MaxSdk.SetSdkKey(sdkKey);

                // Set user ID
                MaxSdk.SetUserId(SystemInfo.deviceUniqueIdentifier);

                // Enable verbose logging if configured
                MaxSdk.SetVerboseLogging(adsSettings.AppLovinContainer.EnableVerboseLogging);

                // Initialize SDK
                MaxSdkCallbacks.OnSdkInitializedEvent += OnSdkInitialized;
                MaxSdk.InitializeSdk();

                DebugLog("[AppLovin]: Initialization started");
            }
            catch (Exception e)
            {
                DebugLogError($"[AppLovin]: Initialization failed: {e.Message}");
            }
        }

        private void OnSdkInitialized(MaxSdkBase.SdkConfiguration config)
        {
            isInitialized = true;
            OnProviderInitialized();
            DebugLog("[AppLovin]: SDK initialized successfully");

            // Setup callbacks
            SetupInterstitialCallbacks();
            SetupRewardedCallbacks();
            SetupBannerCallbacks();

        }

        public void ManualRequestAds()
        {
            if (!isInitialized)
            {
                DebugLogWarning("[AppLovin]: Not initialized yet");
                return;
            }

        }
        #endregion

        #region Banner Implementation
        private void CreateBanner()
        {
            if (!isInitialized) return;

            if (isBannerCreated)
            {
                // Banner already created, just update if needed
                return;
            }

            DebugLog("[AppLovin]: Creating banner...");

            try
            {
                string bannerId = GetBannerID();
                var bannerPosition = GetBannerPosition();

                MaxSdk.CreateBanner(bannerId, bannerPosition);

                // Set adaptive banner if configured
                if (adsSettings.AppLovinContainer.BannerSize == AppLovinContainer.BannerPlacementType.Adaptive)
                {
                    MaxSdk.SetBannerExtraParameter(bannerId, "adaptive_banner", "true");
                }

                // Set transparent background
                MaxSdk.SetBannerBackgroundColor(bannerId, Color.clear);

                // Hide initially
                MaxSdk.HideBanner(bannerId);

                isBannerCreated = true;
                UpdateBannerState(false, false); // Created but not shown or loaded yet
                DebugLog("[AppLovin]: Banner created");
            }
            catch (Exception e)
            {
                DebugLogError($"[AppLovin]: Banner creation failed: {e.Message}");
            }
        }

        public override void ShowBanner()
        {
            if (!isInitialized) return;

            // Update state - we want to show banner
            if (!isBannerCreated)
            {
                CreateBanner();
            }

            if (isBannerCreated && !isBannerShown)
            {
                MaxSdk.ShowBanner(GetBannerID());
                isBannerShown = true;
                OnAdDisplayed(AdType.Banner);
                DebugLog("[AppLovin]: Banner shown");
            }

            UpdateBannerState(isBannerShown, isBannerCreated);
        }

        public override void HideBanner()
        {
            if (isBannerCreated && isBannerShown)
            {
                MaxSdk.HideBanner(GetBannerID());
                isBannerShown = false;
                OnAdClosed(AdType.Banner);
                DebugLog("[AppLovin]: Banner hidden");
            }

            UpdateBannerState(false, isBannerCreated);
        }

        public override void DestroyBanner()
        {
            if (isBannerCreated)
            {
                MaxSdk.DestroyBanner(GetBannerID());
                isBannerCreated = false;
                isBannerShown = false;
                OnAdClosed(AdType.Banner);
                DebugLog("[AppLovin]: Banner destroyed");
            }

            UpdateBannerState(false, false);
        }

        private MaxSdkBase.BannerPosition GetBannerPosition()
        {
            return adsSettings.AppLovinContainer.BannerPosition switch
            {
                BannerPosition.Top => MaxSdkBase.BannerPosition.TopCenter,
                _ => MaxSdkBase.BannerPosition.BottomCenter,
            };
        }
        #endregion

        #region Interstitial Implementation
        private void SetupInterstitialCallbacks()
        {
            string interstitialId = GetInterstitialID();

            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoaded;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailed;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialDisplayed;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialHidden;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialDisplayFailed;
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnInterstitialClicked;
        }

        public override void RequestInterstitial()
        {
            if (!isInitialized) return;

            DebugLog("[AppLovin]: Requesting interstitial...");
            MaxSdk.LoadInterstitial(GetInterstitialID());
        }

        public override void ShowInterstitial(InterstitialCallback callback)
        {
            if (!isInitialized || !IsInterstitialLoaded())
            {
                callback?.Invoke(false);
                RequestInterstitial(); // Auto-request new one
                return;
            }

            currentInterstitialCallback = callback;
            MaxSdk.ShowInterstitial(GetInterstitialID());
        }

        public override bool IsInterstitialLoaded()
        {
            return isInitialized && MaxSdk.IsInterstitialReady(GetInterstitialID());
        }

        // Interstitial Event Handlers
        private void OnInterstitialLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            interstitialRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;
            OnAdLoaded(AdType.Interstitial);
            DebugLog("[AppLovin]: Interstitial loaded");
        }

        private void OnInterstitialLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            HandleAdLoadFailure("Interstitial", errorInfo.Message, ref interstitialRetryAttempt, RequestInterstitial);
        }

        private void OnInterstitialDisplayed(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            OnAdDisplayed(AdType.Interstitial);
            DebugLog("[AppLovin]: Interstitial displayed");
        }

        private void OnInterstitialHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            OnAdClosed(AdType.Interstitial);
            currentInterstitialCallback?.Invoke(true);
            currentInterstitialCallback = null;

            RequestInterstitial(); // Request new one
            DebugLog("[AppLovin]: Interstitial hidden");
        }

        private void OnInterstitialDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            OnAdClosed(AdType.Interstitial);
            currentInterstitialCallback?.Invoke(false);
            currentInterstitialCallback = null;

            RequestInterstitial(); // Retry
            DebugLogError($"[AppLovin]: Interstitial display failed: {errorInfo.Message}");
        }

        private void OnInterstitialClicked(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            DebugLog("[AppLovin]: Interstitial clicked");
        }
        #endregion

        #region Rewarded Video Implementation
        private void SetupRewardedCallbacks()
        {
            string rewardedId = GetRewardedVideoID();

            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoaded;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdLoadFailed;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedAdDisplayed;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdHidden;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdDisplayFailed;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedReward;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedAdClicked;
        }

        public override void RequestRewardedVideo()
        {
            if (!isInitialized) return;

            DebugLog("[AppLovin]: Requesting rewarded video...");
            MaxSdk.LoadRewardedAd(GetRewardedVideoID());
        }

        public override void ShowRewardedVideo(RewardedVideoCallback callback)
        {
            if (!isInitialized || !IsRewardedVideoLoaded())
            {
                callback?.Invoke(false);
                RequestRewardedVideo(); // Auto-request new one
                return;
            }

            currentRewardedCallback = callback;
            MaxSdk.ShowRewardedAd(GetRewardedVideoID());
        }

        public override bool IsRewardedVideoLoaded()
        {
            return isInitialized && MaxSdk.IsRewardedAdReady(GetRewardedVideoID());
        }

        // Rewarded Video Event Handlers
        private void OnRewardedAdLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            rewardedRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;
            OnAdLoaded(AdType.RewardedVideo);
            DebugLog("[AppLovin]: Rewarded video loaded");
        }

        private void OnRewardedAdLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            HandleAdLoadFailure("Rewarded Video", errorInfo.Message, ref rewardedRetryAttempt, RequestRewardedVideo);
        }

        private void OnRewardedAdDisplayed(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            OnAdDisplayed(AdType.RewardedVideo);
            DebugLog("[AppLovin]: Rewarded video displayed");
        }

        private void OnRewardedAdHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            OnAdClosed(AdType.RewardedVideo);

            // User closed without earning reward
            if (currentRewardedCallback != null)
            {
                currentRewardedCallback.Invoke(false);
                currentRewardedCallback = null;
            }

            DebugLog("[AppLovin]: Rewarded video hidden");
        }

        private void OnRewardedAdDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            OnAdClosed(AdType.RewardedVideo);
            currentRewardedCallback?.Invoke(false);
            currentRewardedCallback = null;

            RequestRewardedVideo(); // Retry
            DebugLogError($"[AppLovin]: Rewarded video display failed: {errorInfo.Message}");
        }

        private void OnRewardedAdReceivedReward(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            currentRewardedCallback?.Invoke(true);
            currentRewardedCallback = null;
            DebugLog("[AppLovin]: Rewarded video reward earned");
        }

        private void OnRewardedAdClicked(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            DebugLog("[AppLovin]: Rewarded video clicked");
        }
        #endregion

        #region Banner Callbacks
        private void SetupBannerCallbacks()
        {
            string bannerId = GetBannerID();

            MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoaded;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdLoadFailed;
            MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerAdClicked;
            MaxSdkCallbacks.Banner.OnAdExpandedEvent += OnBannerAdExpanded;
            MaxSdkCallbacks.Banner.OnAdCollapsedEvent += OnBannerAdCollapsed;
        }

        private void OnBannerAdLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            UpdateBannerState(isBannerShown, true);
            OnAdLoaded(AdType.Banner);
            DebugLog("[AppLovin]: Banner loaded");
        }

        private void OnBannerAdLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            UpdateBannerState(false, false);
            DebugLogError($"[AppLovin]: Banner failed to load: {errorInfo.Message}");
        }

        private void OnBannerAdClicked(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            DebugLog("[AppLovin]: Banner clicked");
        }

        private void OnBannerAdExpanded(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            DebugLog("[AppLovin]: Banner expanded");
        }

        private void OnBannerAdCollapsed(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            DebugLog("[AppLovin]: Banner collapsed");
        }
        #endregion

        #region Helper Methods
        private void HandleAdLoadFailure(string adType, string error, ref int retryAttempt, Action retryAction)
        {
            DebugLogError($"[AppLovin]: {adType} failed to load: {error}");

            retryAttempt++;
            float retryDelay = Mathf.Pow(2, Mathf.Min(retryAttempt, 6)); // Cap at 64 seconds

            AdsManager.CallEventInMainThread(() =>
            {
                DebugLog($"[AppLovin]: Retrying {adType} in {retryDelay} seconds");

                // Gunakan MonoBehaviourExecution.Instance untuk menjalankan coroutine
                MonoBehaviourExecution.Instance.StartCoroutine(DelayedCall(retryDelay, retryAction));
            });
        }

        private IEnumerator DelayedCall(float delay, Action action)
        {
            yield return new WaitForSecondsRealtime(delay);
            action?.Invoke();
        }
        #endregion

        #region Platform-Specific Methods
        private string GetSdkKey()
        {
#if UNITY_EDITOR
            return "unused";
#elif UNITY_ANDROID
            return !string.IsNullOrEmpty(adsSettings.AppLovinContainer.AndroidSdkKey) 
                ? adsSettings.AppLovinContainer.AndroidSdkKey 
                : "YOUR_ANDROID_SDK_KEY";
#elif UNITY_IOS
            return !string.IsNullOrEmpty(adsSettings.AppLovinContainer.IosSdkKey) 
                ? adsSettings.AppLovinContainer.IosSdkKey 
                : "YOUR_IOS_SDK_KEY";
#else
            return "unexpected_platform";
#endif
        }

        private string GetBannerID()
        {
#if UNITY_EDITOR
            return "unused";
#elif UNITY_ANDROID
            return !string.IsNullOrEmpty(adsSettings.AppLovinContainer.AndroidBannerID) 
                ? adsSettings.AppLovinContainer.AndroidBannerID 
                : "YOUR_ANDROID_BANNER_ID";
#elif UNITY_IOS
            return !string.IsNullOrEmpty(adsSettings.AppLovinContainer.IOSBannerID) 
                ? adsSettings.AppLovinContainer.IOSBannerID 
                : "YOUR_IOS_BANNER_ID";
#else
            return "unexpected_platform";
#endif
        }

        private string GetInterstitialID()
        {
#if UNITY_EDITOR
            return "unused";
#elif UNITY_ANDROID
            return !string.IsNullOrEmpty(adsSettings.AppLovinContainer.AndroidInterstitialID) 
                ? adsSettings.AppLovinContainer.AndroidInterstitialID 
                : "YOUR_ANDROID_INTERSTITIAL_ID";
#elif UNITY_IOS
            return !string.IsNullOrEmpty(adsSettings.AppLovinContainer.IOSInterstitialID) 
                ? adsSettings.AppLovinContainer.IOSInterstitialID 
                : "YOUR_IOS_INTERSTITIAL_ID";
#else
            return "unexpected_platform";
#endif
        }

        private string GetRewardedVideoID()
        {
#if UNITY_EDITOR
            return "unused";
#elif UNITY_ANDROID
            return !string.IsNullOrEmpty(adsSettings.AppLovinContainer.AndroidRewardedVideoID) 
                ? adsSettings.AppLovinContainer.AndroidRewardedVideoID 
                : "YOUR_ANDROID_REWARDED_ID";
#elif UNITY_IOS
            return !string.IsNullOrEmpty(adsSettings.AppLovinContainer.IOSRewardedVideoID) 
                ? adsSettings.AppLovinContainer.IOSRewardedVideoID 
                : "YOUR_IOS_REWARDED_ID";
#else
            return "unexpected_platform";
#endif
        }
        #endregion

        #region Cleanup & Dispose
        protected override void Dispose(bool disposing)
        {
            if (!handlerDisposed)
            {
                if (disposing)
                {
                    // Clean up managed resources
                    DestroyBanner();

                    // Unsubscribe from all events
                    UnsubscribeAllEvents();

                    // Clear callbacks
                    currentInterstitialCallback = null;
                    currentRewardedCallback = null;

                    DebugLog("[AppLovin]: Resources cleaned up");
                }

                handlerDisposed = true;
                base.Dispose(disposing);
            }
        }

        private void UnsubscribeAllEvents()
        {
            try
            {
                MaxSdkCallbacks.OnSdkInitializedEvent -= OnSdkInitialized;

                // Interstitial
                MaxSdkCallbacks.Interstitial.OnAdLoadedEvent -= OnInterstitialLoaded;
                MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent -= OnInterstitialLoadFailed;
                MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent -= OnInterstitialDisplayed;
                MaxSdkCallbacks.Interstitial.OnAdHiddenEvent -= OnInterstitialHidden;
                MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent -= OnInterstitialDisplayFailed;
                MaxSdkCallbacks.Interstitial.OnAdClickedEvent -= OnInterstitialClicked;

                // Rewarded
                MaxSdkCallbacks.Rewarded.OnAdLoadedEvent -= OnRewardedAdLoaded;
                MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent -= OnRewardedAdLoadFailed;
                MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent -= OnRewardedAdDisplayed;
                MaxSdkCallbacks.Rewarded.OnAdHiddenEvent -= OnRewardedAdHidden;
                MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent -= OnRewardedAdDisplayFailed;
                MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent -= OnRewardedAdReceivedReward;
                MaxSdkCallbacks.Rewarded.OnAdClickedEvent -= OnRewardedAdClicked;

                // Banner
                MaxSdkCallbacks.Banner.OnAdLoadedEvent -= OnBannerAdLoaded;
                MaxSdkCallbacks.Banner.OnAdLoadFailedEvent -= OnBannerAdLoadFailed;
                MaxSdkCallbacks.Banner.OnAdClickedEvent -= OnBannerAdClicked;
                MaxSdkCallbacks.Banner.OnAdExpandedEvent -= OnBannerAdExpanded;
                MaxSdkCallbacks.Banner.OnAdCollapsedEvent -= OnBannerAdCollapsed;
            }
            catch (Exception e)
            {
                DebugLogError($"[AppLovin]: Error unsubscribing events: {e.Message}");
            }
        }

        ~AppLovinHandler()
        {
            Dispose(false);
        }
        #endregion

        #region Privacy & Compliance
        public override void SetGDPR(bool state)
        {
            MaxSdk.SetHasUserConsent(state);
            DebugLog($"[AppLovin]: GDPR set to {state}");
        }

        public override void SetCCPA(bool state)
        {
            // DoNotSell = true berarti user TIDAK menyetujui penjualan data
            MaxSdk.SetDoNotSell(!state);
            DebugLog($"[AppLovin]: CCPA set to {state} (DoNotSell: {!state})");
        }

        public override void SetAgeRestricted(bool state)
        {
            // Periksa apakah method SetIsAgeRestrictedUser ada
            try
            {
                // Coba akses method via reflection untuk menghindari compile error
                var method = typeof(MaxSdk).GetMethod("SetIsAgeRestrictedUser");
                if (method != null)
                {
                    method.Invoke(null, new object[] { state });
                    DebugLog($"[AppLovin]: Age restriction set to {state}");
                }
                else
                {
                    // Fallback: gunakan GDPR untuk age restriction
                    DebugLog($"[AppLovin]: Age restriction not available, using GDPR consent: {state}");
                    SetGDPR(state);
                }
            }
            catch
            {
                DebugLog($"[AppLovin]: Age restriction setting failed, using GDPR instead: {state}");
                SetGDPR(state);
            }
        }

        public override void SetCOPPA(bool state)
        {
            // COPPA adalah subset dari age restriction
            SetAgeRestricted(state);
        }

        public override void SetUserConsent(bool state)
        {
            SetGDPR(state);
        }

        public override void SetUserLocation(double latitude, double longitude)
        {
            // AppLovin tidak memerlukan set location manual
            DebugLog($"[AppLovin]: User location setting requested: {latitude}, {longitude}");
        }
        #endregion
    }
#endif
}