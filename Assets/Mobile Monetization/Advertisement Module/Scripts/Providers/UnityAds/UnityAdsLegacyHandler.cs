#pragma warning disable 0414

using UnityEngine;
using System;

#if UNITYADS_PROVIDER
using UnityEngine.Advertisements;
#endif

namespace MobileCore.Advertisements.Providers
{
#if UNITYADS_PROVIDER
    public class UnityAdsLegacyHandler : BaseAdProviderHandler
    {
        // Placement IDs
        private string bannerPlacement;
        private string interstitialPlacement;
        private string rewardedPlacement;

        // Ad state tracking
        private new bool isBannerShowing = false;
        private bool isInterstitialLoaded = false;
        private bool isRewardedLoaded = false;

        // Retry configuration
        private const int RETRY_ATTEMPT_DEFAULT_VALUE = 1;
        private const int MAX_RETRY_ATTEMPTS = 6;
        private const int MAX_INIT_ATTEMPTS = 5;

        private int interstitialRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;
        private int rewardedRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;
        private int initAttemptCount = 0;

        // Callback references
        private InterstitialCallback currentInterstitialCallback;
        private RewardedVideoCallback currentRewardedCallback;

        // Track if disposed
        private bool unityAdsDisposed = false;

        // Listener component
        private UnityAdsListener listener;

        public UnityAdsLegacyHandler(AdProvider providerType) : base(providerType) { }

        #region Initialization
        public override void Initialize(AdsSettings adsSettings)
        {
            if (isInitialized)
            {
                DebugLogWarning("[UnityAds]: Already initialized!");
                return;
            }

            this.adsSettings = adsSettings;

            if (!Advertisement.isSupported)
            {
                DebugLogError("[UnityAds]: Unity Ads isn't supported on this platform!");
                return;
            }

            DebugLog("[UnityAds]: Initializing...");

            try
            {
                // Get placement IDs
                bannerPlacement = GetBannerID();
                interstitialPlacement = GetInterstitialID();
                rewardedPlacement = GetRewardedVideoID();
                string appId = GetAppID();

                // Create listener component
                CreateListener();

                // Initialize SDK
                Advertisement.Initialize(appId, adsSettings.TestMode, listener);

                // Set banner position
                Advertisement.Banner.SetPosition((UnityEngine.Advertisements.BannerPosition)adsSettings.UnityAdsContainer.BannerPosition);

                // Set GDPR if available
                if (AdsManager.IsGDPRStateExist())
                {
                    SetGDPR(AdsManager.GetGDPRState());
                }

                DebugLog($"[UnityAds]: Initialization started (Test Mode: {adsSettings.TestMode})");
            }
            catch (Exception e)
            {
                DebugLogError($"[UnityAds]: Initialization failed: {e.Message}");
            }
        }

        private void CreateListener()
        {
            if (listener == null)
            {
                var listenerObject = new GameObject("UnityAdsListener");
                UnityEngine.Object.DontDestroyOnLoad(listenerObject);
                listener = listenerObject.AddComponent<UnityAdsListener>();
                listener.Init(this, adsSettings);
            }
        }

        private void OnInitializationComplete()
        {
            AdsManager.CallEventInMainThread(() =>
            {
                isInitialized = true;
                initAttemptCount = 0;
                OnProviderInitialized();
                DebugLog("[UnityAds]: Initialized successfully!");

            });
        }

        private void OnInitializationFailed(string error)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                initAttemptCount++;
                DebugLogError($"[UnityAds]: Initialization failed: {error}");

                if (initAttemptCount < MAX_INIT_ATTEMPTS)
                {
                    float retryDelay = Mathf.Pow(2, Mathf.Min(initAttemptCount, MAX_RETRY_ATTEMPTS));

                    MonoBehaviourExecution.Instance.StartCoroutine(DelayedCall(retryDelay, () =>
                    {
                        string appId = GetAppID();
                        Advertisement.Initialize(appId, adsSettings.TestMode, listener);
                    }));
                }
            });
        }
        #endregion

        #region Banner Implementation
        public override void ShowBanner()
        {
            if (!isInitialized) return;

            UpdateBannerState(true, true);

            Advertisement.Banner.Show(bannerPlacement);
            OnAdDisplayed(AdType.Banner);
            DebugLog("[UnityAds]: Banner shown");
        }

        public override void HideBanner()
        {
            if (!isBannerShowing) 
            {
                return;
            }

            try
            {
                Advertisement.Banner.Hide(false);
                OnAdClosed(AdType.Banner);
                DebugLog("[UnityAds]: Banner hidden");
            }
            catch (Exception)
            {
                // Suppress errors during shutdown/cleanup
            }
            finally
            {
                UpdateBannerState(false, true);
            }
        }

        public override void DestroyBanner()
        {
            try
            {
                Advertisement.Banner.Hide(true);
                OnAdClosed(AdType.Banner);
                DebugLog("[UnityAds]: Banner destroyed");
            }
            catch (Exception)
            {
                // Suppress errors during shutdown
            }
            finally
            {
                UpdateBannerState(false, false);
            }
        }
        #endregion

        #region Interstitial Implementation
        public override void RequestInterstitial()
        {
            if (!isInitialized) return;

            DebugLog("[UnityAds]: Requesting interstitial...");

            Advertisement.Load(interstitialPlacement, listener);
        }

        public override void ShowInterstitial(InterstitialCallback callback)
        {
            if (!isInitialized || !isInterstitialLoaded)
            {
                callback?.Invoke(false);
                RequestInterstitial(); // Auto-request new one
                return;
            }

            currentInterstitialCallback = callback;
            Advertisement.Show(interstitialPlacement, listener);
        }

        public override bool IsInterstitialLoaded()
        {
            return isInitialized && isInterstitialLoaded;
        }

        private void OnInterstitialLoaded()
        {
            AdsManager.CallEventInMainThread(() =>
            {
                isInterstitialLoaded = true;
                interstitialRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;
                OnAdLoaded(AdType.Interstitial);
                DebugLog("[UnityAds]: Interstitial loaded");
            });
        }

        private void OnInterstitialFailed(string error)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                isInterstitialLoaded = false;
                HandleAdLoadFailure("Interstitial", error, ref interstitialRetryAttempt, RequestInterstitial);
            });
        }

        private void OnInterstitialShown()
        {
            AdsManager.CallEventInMainThread(() =>
            {
                OnAdDisplayed(AdType.Interstitial);
                DebugLog("[UnityAds]: Interstitial shown");
            });
        }

        private void OnInterstitialClosed(bool completed)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                OnAdClosed(AdType.Interstitial);
                isInterstitialLoaded = false;
                currentInterstitialCallback?.Invoke(completed);
                currentInterstitialCallback = null;

                RequestInterstitial(); // Request new one for next time
                DebugLog($"[UnityAds]: Interstitial closed - Completed: {completed}");
            });
        }
        #endregion

        #region Rewarded Video Implementation
        public override void RequestRewardedVideo()
        {
            if (!isInitialized) return;

            DebugLog("[UnityAds]: Requesting rewarded video...");

            Advertisement.Load(rewardedPlacement, listener);
        }

        public override void ShowRewardedVideo(RewardedVideoCallback callback)
        {
            if (!isInitialized || !isRewardedLoaded)
            {
                callback?.Invoke(false);
                RequestRewardedVideo(); // Auto-request new one
                return;
            }

            currentRewardedCallback = callback;
            Advertisement.Show(rewardedPlacement, listener);
        }

        public override bool IsRewardedVideoLoaded()
        {
            return isInitialized && isRewardedLoaded;
        }

        private void OnRewardedVideoLoaded()
        {
            AdsManager.CallEventInMainThread(() =>
            {
                isRewardedLoaded = true;
                rewardedRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;
                OnAdLoaded(AdType.RewardedVideo);
                DebugLog("[UnityAds]: Rewarded video loaded");
            });
        }

        private void OnRewardedVideoFailed(string error)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                isRewardedLoaded = false;
                HandleAdLoadFailure("Rewarded Video", error, ref rewardedRetryAttempt, RequestRewardedVideo);
            });
        }

        private void OnRewardedVideoShown()
        {
            AdsManager.CallEventInMainThread(() =>
            {
                OnAdDisplayed(AdType.RewardedVideo);
                DebugLog("[UnityAds]: Rewarded video shown");
            });
        }

        private void OnRewardedVideoClosed(bool completed)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                OnAdClosed(AdType.RewardedVideo);
                isRewardedLoaded = false;
                currentRewardedCallback?.Invoke(completed);
                currentRewardedCallback = null;

                RequestRewardedVideo(); // Request new one for next time
                DebugLog($"[UnityAds]: Rewarded video closed - Completed: {completed}");
            });
        }
        #endregion

        #region Helper Methods
        private void HandleAdLoadFailure(string adType, string error, ref int retryAttempt, Action retryAction)
        {
            DebugLogError($"[UnityAds]: {adType} failed to load: {error}");

            retryAttempt++;
            float retryDelay = Mathf.Pow(2, Mathf.Min(retryAttempt, MAX_RETRY_ATTEMPTS));

            AdsManager.CallEventInMainThread(() =>
            {
                DebugLog($"[UnityAds]: Retrying {adType} in {retryDelay} seconds");
                MonoBehaviourExecution.Instance.StartCoroutine(DelayedCall(retryDelay, retryAction));
            });
        }

        private System.Collections.IEnumerator DelayedCall(float delay, Action action)
        {
            yield return new WaitForSecondsRealtime(delay);
            action?.Invoke();
        }
        #endregion

        #region Platform-Specific Methods
        private string GetAppID()
        {
#if UNITY_ANDROID
            return adsSettings.UnityAdsContainer.AndroidAppID;
#elif UNITY_IOS
            return adsSettings.UnityAdsContainer.IOSAppID;
#else
            return string.Empty;
#endif
        }

        private string GetBannerID()
        {
#if UNITY_ANDROID
            return adsSettings.UnityAdsContainer.AndroidBannerID;
#elif UNITY_IOS
            return adsSettings.UnityAdsContainer.IOSBannerID;
#else
            return string.Empty;
#endif
        }

        private string GetInterstitialID()
        {
#if UNITY_ANDROID
            return adsSettings.UnityAdsContainer.AndroidInterstitialID;
#elif UNITY_IOS
            return adsSettings.UnityAdsContainer.IOSInterstitialID;
#else
            return string.Empty;
#endif
        }

        private string GetRewardedVideoID()
        {
#if UNITY_ANDROID
            return adsSettings.UnityAdsContainer.AndroidRewardedVideoID;
#elif UNITY_IOS
            return adsSettings.UnityAdsContainer.IOSRewardedVideoID;
#else
            return string.Empty;
#endif
        }
        #endregion

        #region Cleanup & Dispose
        protected override void Dispose(bool disposing)
        {
            if (!unityAdsDisposed)
            {
                if (disposing)
                {
                    // Clean up managed resources
                    DestroyBanner();

                    // Destroy listener
                    if (listener != null)
                    {
                        UnityEngine.Object.Destroy(listener.gameObject);
                        listener = null;
                    }

                    // Clear callbacks
                    currentInterstitialCallback = null;
                    currentRewardedCallback = null;

                    DebugLog("[UnityAds]: Resources cleaned up");
                }

                unityAdsDisposed = true;
                base.Dispose(disposing);
            }
        }

        ~UnityAdsLegacyHandler()
        {
            Dispose(false);
        }
        #endregion

        #region Privacy & Compliance
        public override void SetGDPR(bool state)
        {
            string gdprState = state ? "true" : "false";

            var gdprMetaData = new MetaData("gdpr");
            gdprMetaData.Set("consent", gdprState);
            Advertisement.SetMetaData(gdprMetaData);

            var privacyMetaData = new MetaData("privacy");
            privacyMetaData.Set("consent", gdprState);
            Advertisement.SetMetaData(privacyMetaData);

            DebugLog($"[UnityAds]: GDPR set to: {state}");
        }

        public override void SetCCPA(bool state)
        {
            var metaData = new MetaData("privacy");
            metaData.Set("consent", state ? "true" : "false");
            Advertisement.SetMetaData(metaData);

            DebugLog($"[UnityAds]: CCPA set to: {state}");
        }

        public override void SetAgeRestricted(bool state)
        {
            SetCCPA(state);
            DebugLog($"[UnityAds]: Age restriction set to: {state}");
        }

        public override void SetCOPPA(bool state)
        {
            SetAgeRestricted(state);
        }

        public override void SetUserConsent(bool state)
        {
            SetGDPR(state);
        }

        public override void SetUserLocation(double latitude, double longitude)
        {
            DebugLog($"[UnityAds]: User location setting requested: {latitude}, {longitude}");
        }
        #endregion

        #region Listener Class
        private class UnityAdsListener : MonoBehaviour,
            IUnityAdsInitializationListener,
            IUnityAdsLoadListener,
            IUnityAdsShowListener
        {
            private UnityAdsLegacyHandler handler;
            private AdsSettings settings;

            public void Init(UnityAdsLegacyHandler handler, AdsSettings settings)
            {
                this.handler = handler;
                this.settings = settings;
            }

            // Initialization
            public void OnInitializationComplete()
            {
                handler?.OnInitializationComplete();
            }

            public void OnInitializationFailed(UnityAdsInitializationError error, string message)
            {
                handler?.OnInitializationFailed($"{error}: {message}");
            }

            // Load
            public void OnUnityAdsAdLoaded(string placementId)
            {
                if (handler == null) return;

                if (placementId == handler.bannerPlacement)
                {
                    AdsManager.CallEventInMainThread(() =>
                    {
                        AdsManager.OnProviderAdLoaded(handler.providerType, AdType.Banner);
                    });
                }
                else if (placementId == handler.interstitialPlacement)
                {
                    handler.OnInterstitialLoaded();
                }
                else if (placementId == handler.rewardedPlacement)
                {
                    handler.OnRewardedVideoLoaded();
                }
            }

            public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
            {
                if (handler == null) return;

                if (placementId == handler.interstitialPlacement)
                {
                    handler.OnInterstitialFailed($"{error}: {message}");
                }
                else if (placementId == handler.rewardedPlacement)
                {
                    handler.OnRewardedVideoFailed($"{error}: {message}");
                }
            }

            // Show
            public void OnUnityAdsShowStart(string placementId)
            {
                if (handler == null) return;

                if (placementId == handler.interstitialPlacement)
                {
                    handler.OnInterstitialShown();
                }
                else if (placementId == handler.rewardedPlacement)
                {
                    handler.OnRewardedVideoShown();
                }
            }

            public void OnUnityAdsShowClick(string placementId) { }

            public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
            {
                if (handler == null) return;

                bool completed = showCompletionState == UnityAdsShowCompletionState.COMPLETED;

                if (placementId == handler.interstitialPlacement)
                {
                    handler.OnInterstitialClosed(completed);
                }
                else if (placementId == handler.rewardedPlacement)
                {
                    handler.OnRewardedVideoClosed(completed);
                }
            }

            public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
            {
                if (handler == null) return;

                if (placementId == handler.interstitialPlacement)
                {
                    handler.OnInterstitialClosed(false);
                }
                else if (placementId == handler.rewardedPlacement)
                {
                    handler.OnRewardedVideoClosed(false);
                }
            }

            private void OnDestroy()
            {
                handler = null;
            }
        }
        #endregion
    }
#endif
}