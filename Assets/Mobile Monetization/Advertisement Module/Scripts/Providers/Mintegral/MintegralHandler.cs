using UnityEngine;
using System;

namespace MobileCore.Advertisements.Providers
{
#if MINTEGRAL_PROVIDER
    public class MintegralHandler : BaseAdProviderHandler
    {
        // Ad state tracking
        // Note: isBannerShowing is inherited from BaseAdProviderHandler
        private bool isInterstitialReady = false;
        private bool isRewardedReady = false;

        // Retry configuration
        private const int RETRY_ATTEMPT_DEFAULT_VALUE = 1;
        private const int MAX_RETRY_ATTEMPTS = 6;

        private int interstitialRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;
        private int rewardedRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;

        // Callback references
        private InterstitialCallback currentInterstitialCallback;
        private RewardedVideoCallback currentRewardedCallback;

        // Track if disposed
        private bool mintegralDisposed = false;

        public MintegralHandler(AdProvider providerType) : base(providerType) { }

        #region Initialization
        public override void Initialize(AdsSettings adsSettings)
        {
            if (isInitialized)
            {
                DebugLogWarning("[Mintegral]: Already initialized!");
                return;
            }

            this.adsSettings = adsSettings;
            DebugLog("[Mintegral]: Initializing...");

            try
            {
                string appId = GetAppId();
                string appKey = GetAppKey();

                if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appKey))
                {
                    DebugLogError("[Mintegral]: App ID or App Key is missing!");
                    return;
                }

                DebugLog($"[Mintegral]: Initializing with AppId: {appId}, AppKey: {(string.IsNullOrEmpty(appKey) ? "EMPTY" : "SET")}");

                // Initialize SDK
                // Note: The Debug.LogError in Mintegral.initMTGSDK is just a debug log, not an actual error
                DebugLog("[Mintegral]: Calling initMTGSDK...");
                Mintegral.initMTGSDK(appId, appKey);
                DebugLog("[Mintegral]: initMTGSDK called successfully");

                // Register event listeners BEFORE marking as initialized
                // This ensures we catch any events that fire during initialization
                RegisterEventListeners();
                DebugLog("[Mintegral]: Event listeners registered");

                isInitialized = true;
                OnProviderInitialized();

                DebugLog("[Mintegral]: Initialization completed successfully");

                // Set GDPR if available
                if (AdsManager.IsGDPRStateExist())
                {
                    SetGDPR(AdsManager.GetGDPRState());
                }

                // Always request ads after initialization (like LevelPlay does)
                // Request ads in next frame to ensure initialization is complete
                AdsManager.CallEventInMainThread(() =>
                {
                    MonoBehaviourExecution.Instance.StartCoroutine(DelayedAdRequest());
                });
            }
            catch (Exception e)
            {
                DebugLogError($"[Mintegral]: Initialization failed: {e.Message}");
            }
        }

        public void ManualRequestAds()
        {
            if (!isInitialized)
            {
                DebugLogWarning("[Mintegral]: Not initialized yet");
                return;
            }

            DebugLog("[Mintegral]: Manually requesting ads...");

            // Request interstitial if it's the selected type
            if (adsSettings != null && adsSettings.InterstitialType == AdProvider.Mintegral)
            {
                RequestInterstitial();
            }

            // Request rewarded video if it's the selected type
            if (adsSettings != null && adsSettings.RewardedVideoType == AdProvider.Mintegral)
            {
                RequestRewardedVideo();
            }
        }
        #endregion

        #region Banner Implementation
        public override void ShowBanner()
        {
            if (!isInitialized)
            {
                DebugLogWarning("[Mintegral]: Cannot show banner - not initialized");
                return;
            }

            UpdateBannerState(true, isBannerLoaded);

            try
            {
                string adUnitId = GetBannerID();
                var position = ConvertBannerPosition(adsSettings.MintegralContainer.BannerPosition);
                DebugLog($"[Mintegral]: Showing banner with ID: {adUnitId}, Position: {position}");

                // If banner is already loaded, just show it
                if (isBannerLoaded)
                {
                    // Banner should already be visible if loaded
                    OnAdDisplayed(AdType.Banner);
                    DebugLog("[Mintegral]: Banner already loaded, showing");
                    return;
                }

                // Load banner plugins
                string[] bannerAdUnits = { adUnitId };
                Mintegral.loadBannerPluginsForAdUnits(bannerAdUnits);
                DebugLog("[Mintegral]: Banner plugins loaded");

                // Create banner (320x50 standard size)
                // Banner will show automatically when loaded via HandleBannerLoaded
                Mintegral.createBanner(adUnitId, position, 320, 50, false);

                DebugLog("[Mintegral]: Banner request sent, waiting for load...");
            }
            catch (Exception e)
            {
                DebugLogError($"[Mintegral]: Banner failed to show: {e.Message}");
                UpdateBannerState(false, false);
            }
        }

        public override void HideBanner()
        {
            UpdateBannerState(false, isBannerLoaded);

            try
            {
                string adUnitId = GetBannerID();
                Mintegral.destroyBanner(adUnitId);

                OnAdClosed(AdType.Banner);
                DebugLog("[Mintegral]: Banner hidden");
            }
            catch (Exception e)
            {
                DebugLogError($"[Mintegral]: Banner failed to hide: {e.Message}");
            }
        }

        public override void DestroyBanner()
        {
            HideBanner();
        }
        #endregion

        #region Interstitial Implementation
        public override void RequestInterstitial()
        {
            if (!isInitialized)
            {
                DebugLogWarning("[Mintegral]: Cannot request interstitial - not initialized");
                return;
            }

            DebugLog("[Mintegral]: Requesting interstitial...");

            try
            {
                string adUnitId = GetInterstitialID();
                DebugLog($"[Mintegral]: Requesting interstitial with ID: {adUnitId}");

                // Load interstitial plugins
                var interstitialInfo = new MTGInterstitialInfo { adUnitId = adUnitId };
                MTGInterstitialInfo[] interstitialInfos = { interstitialInfo };
                Mintegral.loadInterstitialPluginsForAdUnits(interstitialInfos);
                DebugLog("[Mintegral]: Interstitial plugins loaded");

                Mintegral.requestInterstitialAd(adUnitId);
                isInterstitialReady = false;

                DebugLog("[Mintegral]: Interstitial request sent");
            }
            catch (Exception e)
            {
                DebugLogError($"[Mintegral]: Interstitial request failed: {e.Message}");
                HandleAdLoadFailure("Interstitial", e.Message, ref interstitialRetryAttempt, RequestInterstitial);
            }
        }

        public override void ShowInterstitial(InterstitialCallback callback)
        {
            if (!isInitialized || !isInterstitialReady)
            {
                callback?.Invoke(false);
                RequestInterstitial(); // Auto-request new one
                return;
            }

            currentInterstitialCallback = callback;

            try
            {
                string adUnitId = GetInterstitialID();
                Mintegral.showInterstitialAd(adUnitId);

                OnAdDisplayed(AdType.Interstitial);
                DebugLog("[Mintegral]: Interstitial shown");
            }
            catch (Exception e)
            {
                DebugLogError($"[Mintegral]: Interstitial failed to show: {e.Message}");
                callback?.Invoke(false);
            }
        }

        public override bool IsInterstitialLoaded()
        {
            return isInitialized && isInterstitialReady;
        }
        #endregion

        #region Rewarded Video Implementation
        public override void RequestRewardedVideo()
        {
            if (!isInitialized)
            {
                DebugLogWarning("[Mintegral]: Cannot request rewarded video - not initialized");
                return;
            }

            DebugLog("[Mintegral]: Requesting rewarded video...");

            try
            {
                string adUnitId = GetRewardedVideoID();
                DebugLog($"[Mintegral]: Requesting rewarded video with ID: {adUnitId}");

                // Load rewarded video plugins
                string[] rewardedAdUnits = { adUnitId };
                Mintegral.loadRewardedVideoPluginsForAdUnits(rewardedAdUnits);
                DebugLog("[Mintegral]: Rewarded video plugins loaded");

                Mintegral.requestRewardedVideo(adUnitId);
                isRewardedReady = false;

                DebugLog("[Mintegral]: Rewarded video request sent");
            }
            catch (Exception e)
            {
                DebugLogError($"[Mintegral]: Rewarded video request failed: {e.Message}");
                HandleAdLoadFailure("Rewarded Video", e.Message, ref rewardedRetryAttempt, RequestRewardedVideo);
            }
        }

        public override void ShowRewardedVideo(RewardedVideoCallback callback)
        {
            if (!isInitialized || !isRewardedReady)
            {
                callback?.Invoke(false);
                RequestRewardedVideo(); // Auto-request new one
                return;
            }

            currentRewardedCallback = callback;

            try
            {
                string adUnitId = GetRewardedVideoID();
                Mintegral.showRewardedVideo(adUnitId, "rewardId", "userId");

                OnAdDisplayed(AdType.RewardedVideo);
                DebugLog("[Mintegral]: Rewarded video shown");
            }
            catch (Exception e)
            {
                DebugLogError($"[Mintegral]: Rewarded video failed to show: {e.Message}");
                callback?.Invoke(false);
            }
        }

        public override bool IsRewardedVideoLoaded()
        {
            if (!isInitialized) return false;

            // Check both the flag and SDK status for accuracy
            string adUnitId = GetRewardedVideoID();
            bool sdkReady = Mintegral.isVideoReadyToPlay(adUnitId);

            // Sync flag with SDK status if they differ
            if (isRewardedReady != sdkReady)
            {
                DebugLog($"[Mintegral]: Rewarded video state mismatch - flag: {isRewardedReady}, SDK: {sdkReady}");
                isRewardedReady = sdkReady;
            }

            return isRewardedReady;
        }
        #endregion

        #region Event Handlers
        private void RegisterEventListeners()
        {
            try
            {
                // Banner Events
                MintegralManager.onBannerLoadedEvent += HandleBannerLoaded;
                MintegralManager.onBannerFailedEvent += HandleBannerFailed;
                MintegralManager.onBannerDismissEvent += HandleBannerClosed;
                DebugLog("[Mintegral]: Banner event listeners registered");

                // Interstitial Events
                MintegralManager.onInterstitialLoadedEvent += HandleInterstitialLoaded;
                MintegralManager.onInterstitialFailedEvent += HandleInterstitialFailed;
                MintegralManager.onInterstitialShownFailedEvent += HandleInterstitialShowFailed;
                MintegralManager.onInterstitialDismissedEvent += HandleInterstitialClosed;
                DebugLog("[Mintegral]: Interstitial event listeners registered");

                // Rewarded Video Events
                MintegralManager.onRewardedVideoLoadedEvent += HandleRewardedVideoLoaded;
                MintegralManager.onRewardedVideoFailedEvent += HandleRewardedVideoFailed;
                MintegralManager.onRewardedVideoShownFailedEvent += HandleRewardedVideoShowFailed;
                MintegralManager.onRewardedVideoClosedEvent += HandleRewardedVideoClosed;
                DebugLog("[Mintegral]: Rewarded video event listeners registered");
            }
            catch (Exception e)
            {
                DebugLogError($"[Mintegral]: Failed to register event listeners: {e.Message}");
            }
        }

        // Banner Events
        private void HandleBannerLoaded(string adUnitId)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                UpdateBannerState(isBannerShowing, true);
                OnAdLoaded(AdType.Banner);

                // If banner was requested to show, display it now
                if (isBannerShowing)
                {
                    OnAdDisplayed(AdType.Banner);
                    DebugLog("[Mintegral]: Banner loaded and displayed");
                }
                else
                {
                    DebugLog("[Mintegral]: Banner loaded");
                }
            });
        }

        private void HandleBannerFailed(string error)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                UpdateBannerState(false, false);
                DebugLogError($"[Mintegral]: Banner failed: {error}");
            });
        }

        private void HandleBannerClosed(string adUnitId)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                UpdateBannerState(false, isBannerLoaded);
                OnAdClosed(AdType.Banner);
                DebugLog("[Mintegral]: Banner closed");
            });
        }

        // Interstitial Events
        private void HandleInterstitialLoaded()
        {
            AdsManager.CallEventInMainThread(() =>
            {
                isInterstitialReady = true;
                interstitialRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;
                OnAdLoaded(AdType.Interstitial);
                DebugLog("[Mintegral]: ✅ Interstitial loaded successfully and ready to show");
            });
        }

        private void HandleInterstitialFailed(string error)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                isInterstitialReady = false;
                DebugLogError($"[Mintegral]: ❌ Interstitial failed to load: {error}");
                HandleAdLoadFailure("Interstitial", error, ref interstitialRetryAttempt, RequestInterstitial);
            });
        }

        private void HandleInterstitialShowFailed(string error)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                OnAdClosed(AdType.Interstitial);
                currentInterstitialCallback?.Invoke(false);
                currentInterstitialCallback = null;

                RequestInterstitial(); // Retry
                DebugLogError($"[Mintegral]: Interstitial show failed: {error}");
            });
        }

        private void HandleInterstitialClosed()
        {
            AdsManager.CallEventInMainThread(() =>
            {
                OnAdClosed(AdType.Interstitial);
                isInterstitialReady = false;
                currentInterstitialCallback?.Invoke(true);
                currentInterstitialCallback = null;

                RequestInterstitial(); // Request new one for next time
                DebugLog("[Mintegral]: Interstitial closed");
            });
        }

        // Rewarded Video Events
        private void HandleRewardedVideoLoaded(string adUnitId)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                isRewardedReady = true;
                rewardedRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;
                OnAdLoaded(AdType.RewardedVideo);
                DebugLog($"[Mintegral]: ✅ Rewarded video loaded successfully (AdUnit: {adUnitId}) and ready to show");
            });
        }

        private void HandleRewardedVideoFailed(string error)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                isRewardedReady = false;
                DebugLogError($"[Mintegral]: ❌ Rewarded video failed to load: {error}");
                HandleAdLoadFailure("Rewarded Video", error, ref rewardedRetryAttempt, RequestRewardedVideo);
            });
        }

        private void HandleRewardedVideoShowFailed(string error)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                OnAdClosed(AdType.RewardedVideo);
                currentRewardedCallback?.Invoke(false);
                currentRewardedCallback = null;

                RequestRewardedVideo(); // Retry
                DebugLogError($"[Mintegral]: Rewarded video show failed: {error}");
            });
        }

        private void HandleRewardedVideoClosed(MintegralManager.MTGRewardData rewardData)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                OnAdClosed(AdType.RewardedVideo);
                isRewardedReady = false;

                bool rewardEarned = rewardData != null && rewardData.converted;
                currentRewardedCallback?.Invoke(rewardEarned);
                currentRewardedCallback = null;

                RequestRewardedVideo(); // Request new one for next time
                DebugLog($"[Mintegral]: Rewarded video closed - Reward earned: {rewardEarned}");
            });
        }
        #endregion

        #region Helper Methods
        private Mintegral.BannerAdPosition ConvertBannerPosition(BannerPosition position)
        {
            return position == BannerPosition.Top ?
                Mintegral.BannerAdPosition.TopCenter :
                Mintegral.BannerAdPosition.BottomCenter;
        }

        private void HandleAdLoadFailure(string adType, string error, ref int retryAttempt, Action retryAction)
        {
            DebugLogError($"[Mintegral]: {adType} failed to load: {error}");

            retryAttempt++;
            float retryDelay = Mathf.Pow(2, Mathf.Min(retryAttempt, MAX_RETRY_ATTEMPTS));

            AdsManager.CallEventInMainThread(() =>
            {
                DebugLog($"[Mintegral]: Retrying {adType} in {retryDelay} seconds");
                MonoBehaviourExecution.Instance.StartCoroutine(DelayedCall(retryDelay, retryAction));
            });
        }

        private System.Collections.IEnumerator DelayedCall(float delay, Action action)
        {
            yield return new WaitForSecondsRealtime(delay);
            action?.Invoke();
        }

        private System.Collections.IEnumerator DelayedAdRequest()
        {
            yield return new WaitForSecondsRealtime(0.5f); // Small delay to ensure SDK is ready

            DebugLog("[Mintegral]: Starting delayed ad requests...");

            if (adsSettings == null)
            {
                DebugLogError("[Mintegral]: adsSettings is null! Cannot request ads.");
                yield break;
            }

            // Request interstitial if it's the selected type
            if (adsSettings.InterstitialType == AdProvider.Mintegral)
            {
                DebugLog("[Mintegral]: Auto-requesting interstitial...");
                RequestInterstitial();
            }

            // Request rewarded video if it's the selected type
            if (adsSettings.RewardedVideoType == AdProvider.Mintegral)
            {
                DebugLog("[Mintegral]: Auto-requesting rewarded video...");
                RequestRewardedVideo();
            }
        }
        #endregion

        #region Platform-Specific Methods
        private string GetAppId()
        {
#if UNITY_EDITOR
            // Use test App ID from container even in editor for consistency
            return adsSettings?.MintegralContainer?.AndroidAppId ?? MintegralContainer.ANDROID_APP_ID_TEMPLATE;
#elif UNITY_ANDROID
            string appId = string.IsNullOrEmpty(adsSettings?.MintegralContainer?.AndroidAppId) ? 
                MintegralContainer.ANDROID_APP_ID_TEMPLATE : adsSettings.MintegralContainer.AndroidAppId;
            DebugLog($"[Mintegral]: Using Android App ID: {appId}");
            return appId;
#elif UNITY_IOS
            string appId = string.IsNullOrEmpty(adsSettings?.MintegralContainer?.IOSAppId) ? 
                MintegralContainer.IOS_APP_ID_TEMPLATE : adsSettings.MintegralContainer.IOSAppId;
            DebugLog($"[Mintegral]: Using iOS App ID: {appId}");
            return appId;
#else
            return "unexpected_platform";
#endif
        }

        private string GetAppKey()
        {
#if UNITY_EDITOR
            // Use test App Key from container even in editor for consistency
            return adsSettings?.MintegralContainer?.AndroidAppKey ?? MintegralContainer.ANDROID_APP_KEY_TEMPLATE;
#elif UNITY_ANDROID
            string appKey = string.IsNullOrEmpty(adsSettings?.MintegralContainer?.AndroidAppKey) ? 
                MintegralContainer.ANDROID_APP_KEY_TEMPLATE : adsSettings.MintegralContainer.AndroidAppKey;
            DebugLog($"[Mintegral]: Using Android App Key: {(string.IsNullOrEmpty(appKey) ? "EMPTY" : "SET")}");
            return appKey;
#elif UNITY_IOS
            string appKey = string.IsNullOrEmpty(adsSettings?.MintegralContainer?.IOSAppKey) ? 
                MintegralContainer.IOS_APP_KEY_TEMPLATE : adsSettings.MintegralContainer.IOSAppKey;
            DebugLog($"[Mintegral]: Using iOS App Key: {(string.IsNullOrEmpty(appKey) ? "EMPTY" : "SET")}");
            return appKey;
#else
            return "unexpected_platform";
#endif
        }

        private string GetBannerID()
        {
#if UNITY_EDITOR
            // Use test ID from container even in editor for consistency
            return adsSettings?.MintegralContainer?.AndroidBannerID ?? MintegralContainer.ANDROID_BANNER_TEST_ID;
#elif UNITY_ANDROID
            string adUnitId = string.IsNullOrEmpty(adsSettings?.MintegralContainer?.AndroidBannerID) ? 
                MintegralContainer.ANDROID_BANNER_TEST_ID : adsSettings.MintegralContainer.AndroidBannerID;
            DebugLog($"[Mintegral]: Using Banner ID: {adUnitId}");
            return adUnitId;
#elif UNITY_IOS
            string adUnitId = string.IsNullOrEmpty(adsSettings?.MintegralContainer?.IOSBannerID) ? 
                MintegralContainer.IOS_BANNER_TEST_ID : adsSettings.MintegralContainer.IOSBannerID;
            DebugLog($"[Mintegral]: Using Banner ID: {adUnitId}");
            return adUnitId;
#else
            return "unexpected_platform";
#endif
        }

        private string GetInterstitialID()
        {
#if UNITY_EDITOR
            // Use test ID from container even in editor for consistency
            return adsSettings?.MintegralContainer?.AndroidInterstitialID ?? MintegralContainer.ANDROID_INTERSTITIAL_TEST_ID;
#elif UNITY_ANDROID
            string adUnitId = string.IsNullOrEmpty(adsSettings?.MintegralContainer?.AndroidInterstitialID) ? 
                MintegralContainer.ANDROID_INTERSTITIAL_TEST_ID : adsSettings.MintegralContainer.AndroidInterstitialID;
            DebugLog($"[Mintegral]: Using Interstitial ID: {adUnitId}");
            return adUnitId;
#elif UNITY_IOS
            string adUnitId = string.IsNullOrEmpty(adsSettings?.MintegralContainer?.IOSInterstitialID) ? 
                MintegralContainer.IOS_INTERSTITIAL_TEST_ID : adsSettings.MintegralContainer.IOSInterstitialID;
            DebugLog($"[Mintegral]: Using Interstitial ID: {adUnitId}");
            return adUnitId;
#else
            return "unexpected_platform";
#endif
        }

        private string GetRewardedVideoID()
        {
#if UNITY_EDITOR
            // Use test ID from container even in editor for consistency
            return adsSettings?.MintegralContainer?.AndroidRewardedVideoID ?? MintegralContainer.ANDROID_REWARDED_VIDEO_TEST_ID;
#elif UNITY_ANDROID
            string adUnitId = string.IsNullOrEmpty(adsSettings?.MintegralContainer?.AndroidRewardedVideoID) ? 
                MintegralContainer.ANDROID_REWARDED_VIDEO_TEST_ID : adsSettings.MintegralContainer.AndroidRewardedVideoID;
            DebugLog($"[Mintegral]: Using Rewarded Video ID: {adUnitId}");
            return adUnitId;
#elif UNITY_IOS
            string adUnitId = string.IsNullOrEmpty(adsSettings?.MintegralContainer?.IOSRewardedVideoID) ? 
                MintegralContainer.IOS_REWARDED_VIDEO_TEST_ID : adsSettings.MintegralContainer.IOSRewardedVideoID;
            DebugLog($"[Mintegral]: Using Rewarded Video ID: {adUnitId}");
            return adUnitId;
#else
            return "unexpected_platform";
#endif
        }
        #endregion

        #region Cleanup & Dispose
        protected override void Dispose(bool disposing)
        {
            if (!mintegralDisposed)
            {
                if (disposing)
                {
                    // Clean up managed resources
                    DestroyBanner();

                    // Unsubscribe from events
                    UnsubscribeAllEvents();

                    // Clear callbacks
                    currentInterstitialCallback = null;
                    currentRewardedCallback = null;

                    DebugLog("[Mintegral]: Resources cleaned up");
                }

                mintegralDisposed = true;
                base.Dispose(disposing);
            }
        }

        ~MintegralHandler()
        {
            Dispose(false);
        }

        private void UnsubscribeAllEvents()
        {
            try
            {
                // Banner Events
                MintegralManager.onBannerLoadedEvent -= HandleBannerLoaded;
                MintegralManager.onBannerFailedEvent -= HandleBannerFailed;
                MintegralManager.onBannerDismissEvent -= HandleBannerClosed;

                // Interstitial Events
                MintegralManager.onInterstitialLoadedEvent -= HandleInterstitialLoaded;
                MintegralManager.onInterstitialFailedEvent -= HandleInterstitialFailed;
                MintegralManager.onInterstitialShownFailedEvent -= HandleInterstitialShowFailed;
                MintegralManager.onInterstitialDismissedEvent -= HandleInterstitialClosed;

                // Rewarded Video Events
                MintegralManager.onRewardedVideoLoadedEvent -= HandleRewardedVideoLoaded;
                MintegralManager.onRewardedVideoFailedEvent -= HandleRewardedVideoFailed;
                MintegralManager.onRewardedVideoShownFailedEvent -= HandleRewardedVideoShowFailed;
                MintegralManager.onRewardedVideoClosedEvent -= HandleRewardedVideoClosed;
            }
            catch (Exception e)
            {
                DebugLogWarning($"[Mintegral]: Error unsubscribing events: {e.Message}");
            }
        }
        #endregion

        #region Privacy & Compliance
        public override void SetGDPR(bool state)
        {
            try
            {
                int gdprStatus = state ? 1 : 0;
                Mintegral.setConsentStatusInfoType(gdprStatus);
                DebugLog($"[Mintegral]: GDPR set to: {state}");
            }
            catch (Exception e)
            {
                DebugLogError($"[Mintegral]: Failed to set GDPR: {e.Message}");
            }
        }

        public override void SetCCPA(bool state)
        {
            try
            {
                int doNotTrackStatus = state ? 0 : 1;
                Mintegral.setDoNotTrackStatus(doNotTrackStatus);
                DebugLog($"[Mintegral]: CCPA set to: {state}");
            }
            catch (Exception e)
            {
                DebugLogError($"[Mintegral]: Failed to set CCPA: {e.Message}");
            }
        }

        public override void SetAgeRestricted(bool state)
        {
            SetCCPA(!state);
            DebugLog($"[Mintegral]: Age restriction set to: {state}");
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
            DebugLog($"[Mintegral]: User location setting requested: {latitude}, {longitude}");
        }
        #endregion
    }
#endif
}