using UnityEngine;
using System.Collections.Generic;
using System;

#if VUNGLE_PROVIDER
// No namespace import needed since Vungle is a static class
#endif

namespace MobileCore.Advertisements.Providers
{
#if VUNGLE_PROVIDER
    public class VungleHandler : BaseAdProviderHandler
    {
        // Ad state tracking
        private bool isBannerShowing = false;
        private bool isBannerLoaded = false;
        private bool isInterstitialLoaded = false;
        private bool isRewardedLoaded = false;

        // Current banner placement
        private string currentBannerPlacement;

        // Retry configuration
        private const int RETRY_ATTEMPT_DEFAULT_VALUE = 1;
        private const int MAX_RETRY_ATTEMPTS = 6;

        private int interstitialRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;
        private int rewardedRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;

        // Callback references
        private InterstitialCallback currentInterstitialCallback;
        private RewardedVideoCallback currentRewardedCallback;

        // Track if disposed
        private bool vungleDisposed = false;

        public VungleHandler(AdProvider providerType) : base(providerType) { }

        #region Initialization
        public override void Initialize(AdsSettings adsSettings)
        {
            if (isInitialized)
            {
                DebugLogWarning("[Vungle]: Already initialized!");
                return;
            }

            this.adsSettings = adsSettings;
            DebugLog("[Vungle]: Initializing...");

            try
            {
                string appId = GetAppId();

                if (string.IsNullOrEmpty(appId))
                {
                    DebugLogError("[Vungle]: App ID is null or empty!");
                    return;
                }

                DebugLog($"[Vungle]: Initializing with App ID: {appId}");

                // Register event handlers
                RegisterVungleEvents();

                // Initialize Vungle
                Vungle.init(appId);

                DebugLog("[Vungle]: Initialization started");
            }
            catch (Exception e)
            {
                DebugLogError($"[Vungle]: Initialization failed: {e.Message}");
            }
        }

        private void OnVungleInitialized()
        {
            AdsManager.CallEventInMainThread(() =>
            {
                isInitialized = true;
                OnProviderInitialized();
                DebugLog("[Vungle]: Initialized successfully!");
            });
        }

        public void ManualRequestAds()
        {
            if (!isInitialized)
            {
                DebugLogWarning("[Vungle]: Not initialized yet");
                return;
            }

        }
        #endregion

        #region Banner Implementation
        private void RequestBanner()
        {
            if (!isInitialized) return;

            DebugLog("[Vungle]: Requesting banner...");

            try
            {
                string placementId = GetBannerID();

                if (string.IsNullOrEmpty(placementId))
                {
                    DebugLogError("[Vungle]: Banner placement ID is null or empty!");
                    return;
                }

                // Close existing banner
                if (!string.IsNullOrEmpty(currentBannerPlacement))
                {
                    Vungle.closeBanner(currentBannerPlacement);
                }

                var position = ConvertBannerPosition(adsSettings.VungleContainer.BannerPosition);
                var size = ConvertBannerSize(adsSettings.VungleContainer.BannerSize);

                Vungle.loadBanner(placementId, size, position);
                currentBannerPlacement = placementId;

                DebugLog("[Vungle]: Banner request sent");
            }
            catch (Exception e)
            {
                DebugLogError($"[Vungle]: Banner request failed: {e.Message}");
            }
        }

        public override void ShowBanner()
        {
            if (!isInitialized) return;

            UpdateBannerState(true, isBannerLoaded);

            if (string.IsNullOrEmpty(currentBannerPlacement) || !isBannerLoaded)
            {
                RequestBanner();
                return; // Banner will show automatically when loaded
            }

            if (isBannerLoaded)
            {
                Vungle.showBanner(currentBannerPlacement);
                OnAdDisplayed(AdType.Banner);
                DebugLog("[Vungle]: Banner shown");
            }
        }

        public override void HideBanner()
        {
            UpdateBannerState(false, isBannerLoaded);

            if (!string.IsNullOrEmpty(currentBannerPlacement) && isBannerLoaded)
            {
                Vungle.closeBanner(currentBannerPlacement);
                OnAdClosed(AdType.Banner);
                DebugLog("[Vungle]: Banner hidden");
            }
        }

        public override void DestroyBanner()
        {
            UpdateBannerState(false, false);

            if (!string.IsNullOrEmpty(currentBannerPlacement))
            {
                Vungle.closeBanner(currentBannerPlacement);
                currentBannerPlacement = null;
                isBannerLoaded = false;
                DebugLog("[Vungle]: Banner destroyed");
            }
        }
        #endregion

        #region Interstitial Implementation
        public override void RequestInterstitial()
        {
            if (!isInitialized) return;

            DebugLog("[Vungle]: Requesting interstitial...");

            try
            {
                string placementId = GetInterstitialID();

                if (string.IsNullOrEmpty(placementId))
                {
                    DebugLogError("[Vungle]: Interstitial placement ID is null or empty!");
                    return;
                }

                Vungle.loadAd(placementId);
            }
            catch (Exception e)
            {
                DebugLogError($"[Vungle]: Interstitial request failed: {e.Message}");
                HandleAdLoadFailure("Interstitial", e.Message, ref interstitialRetryAttempt, RequestInterstitial);
            }
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

            try
            {
                string placementId = GetInterstitialID();
                var options = new Dictionary<string, object>
                {
                    ["orientation"] = 3, // Auto-rotate
                    ["closeIncentivized"] = false
                };

                Vungle.playAd(options, placementId);
                OnAdDisplayed(AdType.Interstitial);
                DebugLog("[Vungle]: Interstitial shown");
            }
            catch (Exception e)
            {
                DebugLogError($"[Vungle]: Interstitial failed to show: {e.Message}");
                callback?.Invoke(false);
            }
        }

        public override bool IsInterstitialLoaded()
        {
            return isInitialized && isInterstitialLoaded;
        }
        #endregion

        #region Rewarded Video Implementation
        public override void RequestRewardedVideo()
        {
            if (!isInitialized) return;

            DebugLog("[Vungle]: Requesting rewarded video...");

            try
            {
                string placementId = GetRewardedVideoID();

                if (string.IsNullOrEmpty(placementId))
                {
                    DebugLogError("[Vungle]: Rewarded video placement ID is null or empty!");
                    return;
                }

                Vungle.loadAd(placementId);
            }
            catch (Exception e)
            {
                DebugLogError($"[Vungle]: Rewarded video request failed: {e.Message}");
                HandleAdLoadFailure("Rewarded Video", e.Message, ref rewardedRetryAttempt, RequestRewardedVideo);
            }
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

            try
            {
                string placementId = GetRewardedVideoID();
                var options = new Dictionary<string, object>
                {
                    ["incentivized"] = true,
                    ["user"] = "user_id",
                    ["orientation"] = 3, // Auto-rotate
                    ["closeIncentivized"] = false
                };

                Vungle.playAd(options, placementId);
                OnAdDisplayed(AdType.RewardedVideo);
                DebugLog("[Vungle]: Rewarded video shown");
            }
            catch (Exception e)
            {
                DebugLogError($"[Vungle]: Rewarded video failed to show: {e.Message}");
                callback?.Invoke(false);
            }
        }

        public override bool IsRewardedVideoLoaded()
        {
            return isInitialized && isRewardedLoaded;
        }
        #endregion

        #region Event Handlers
        private void RegisterVungleEvents()
        {
            Vungle.onInitializeEvent += OnVungleInitialized;
            Vungle.adPlayableEvent += HandleAdPlayable;
            Vungle.onAdStartedEvent += HandleAdStarted;
            Vungle.onAdEndEvent += HandleAdEnded;
            Vungle.onAdRewardedEvent += HandleAdRewarded;
            Vungle.onLogEvent += HandleVungleLog;
            Vungle.onErrorEvent += HandleVungleError;
        }

        private void HandleAdPlayable(string placementId, bool isPlayable)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                if (placementId == GetBannerID())
                {
                    isBannerLoaded = isPlayable;
                    if (isPlayable)
                    {
                        OnAdLoaded(AdType.Banner);
                        DebugLog("[Vungle]: Banner loaded");

                        // Auto-show if requested before loaded
                        if (isBannerShowing)
                        {
                            Vungle.showBanner(placementId);
                            OnAdDisplayed(AdType.Banner);
                        }
                    }
                }
                else if (placementId == GetInterstitialID())
                {
                    isInterstitialLoaded = isPlayable;
                    if (isPlayable)
                    {
                        interstitialRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;
                        OnAdLoaded(AdType.Interstitial);
                        DebugLog("[Vungle]: Interstitial loaded");
                    }
                    else
                    {
                        HandleAdLoadFailure("Interstitial", "Not playable", ref interstitialRetryAttempt, RequestInterstitial);
                    }
                }
                else if (placementId == GetRewardedVideoID())
                {
                    isRewardedLoaded = isPlayable;
                    if (isPlayable)
                    {
                        rewardedRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;
                        OnAdLoaded(AdType.RewardedVideo);
                        DebugLog("[Vungle]: Rewarded video loaded");
                    }
                    else
                    {
                        HandleAdLoadFailure("Rewarded Video", "Not playable", ref rewardedRetryAttempt, RequestRewardedVideo);
                    }
                }
            });
        }

        private void HandleAdStarted(string placementId)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                if (placementId == GetInterstitialID())
                {
                    OnAdDisplayed(AdType.Interstitial);
                }
                else if (placementId == GetRewardedVideoID())
                {
                    OnAdDisplayed(AdType.RewardedVideo);
                }
                else if (placementId == GetBannerID())
                {
                    // Banner already displayed when shown
                }

                DebugLog($"[Vungle]: Ad started - {placementId}");
            });
        }

        private void HandleAdEnded(string placementId)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                if (placementId == GetInterstitialID())
                {
                    OnAdClosed(AdType.Interstitial);
                    currentInterstitialCallback?.Invoke(true);
                    currentInterstitialCallback = null;

                    RequestInterstitial(); // Request new one for next time
                    DebugLog("[Vungle]: Interstitial ended");
                }
                else if (placementId == GetRewardedVideoID())
                {
                    OnAdClosed(AdType.RewardedVideo);
                    // Reward callback will be called in HandleAdRewarded
                    DebugLog("[Vungle]: Rewarded video ended");
                }
                else if (placementId == GetBannerID())
                {
                    OnAdClosed(AdType.Banner);
                    DebugLog("[Vungle]: Banner ended");
                }
            });
        }

        private void HandleAdRewarded(string placementId)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                if (placementId == GetRewardedVideoID())
                {
                    currentRewardedCallback?.Invoke(true);
                    currentRewardedCallback = null;
                    DebugLog("[Vungle]: Reward earned");

                    RequestRewardedVideo(); // Request new one for next time
                }
            });
        }

        private void HandleVungleLog(string message)
        {
            DebugLog($"[Vungle]: {message}");
        }

        private void HandleVungleError(string message)
        {
            DebugLogError($"[Vungle]: Error - {message}");
        }
        #endregion

        #region Helper Methods
        private Vungle.VungleBannerPosition ConvertBannerPosition(BannerPosition position)
        {
            return position == BannerPosition.Top ?
                Vungle.VungleBannerPosition.TopCenter :
                Vungle.VungleBannerPosition.BottomCenter;
        }

        private Vungle.VungleBannerSize ConvertBannerSize(VungleContainer.BannerPlacement size)
        {
            return size switch
            {
                VungleContainer.BannerPlacement.BannerShort => Vungle.VungleBannerSize.VungleAdSizeBannerShort,
                VungleContainer.BannerPlacement.BannerLeaderboard => Vungle.VungleBannerSize.VungleAdSizeBannerLeaderboard,
                VungleContainer.BannerPlacement.Mrec => Vungle.VungleBannerSize.VungleAdSizeBannerMedium,
                _ => Vungle.VungleBannerSize.VungleAdSizeBanner,
            };
        }

        private void HandleAdLoadFailure(string adType, string error, ref int retryAttempt, Action retryAction)
        {
            DebugLogError($"[Vungle]: {adType} failed to load: {error}");

            retryAttempt++;
            float retryDelay = Mathf.Pow(2, Mathf.Min(retryAttempt, MAX_RETRY_ATTEMPTS));

            AdsManager.CallEventInMainThread(() =>
            {
                DebugLog($"[Vungle]: Retrying {adType} in {retryDelay} seconds");
                MonoBehaviourExecution.Instance.StartCoroutine(DelayedCall(retryDelay, retryAction));
            });
        }

        private System.Collections.IEnumerator DelayedCall(float delay, Action action)
        {
            yield return new WaitForSecondsRealtime(delay);
            action?.Invoke();
        }

        private void UnregisterVungleEvents()
        {
            try
            {
                Vungle.onInitializeEvent -= OnVungleInitialized;
                Vungle.adPlayableEvent -= HandleAdPlayable;
                Vungle.onAdStartedEvent -= HandleAdStarted;
                Vungle.onAdEndEvent -= HandleAdEnded;
                Vungle.onAdRewardedEvent -= HandleAdRewarded;
                Vungle.onLogEvent -= HandleVungleLog;
                Vungle.onErrorEvent -= HandleVungleError;
            }
            catch (Exception e)
            {
                DebugLogWarning($"[Vungle]: Error unsubscribing events: {e.Message}");
            }
        }
        #endregion

        #region Platform-Specific Methods
        private string GetAppId()
        {
#if UNITY_EDITOR
            return "test_app_id";
#elif UNITY_ANDROID
            return string.IsNullOrEmpty(adsSettings.VungleContainer.AndroidAppId) ? 
                "test_app_id" : adsSettings.VungleContainer.AndroidAppId;
#elif UNITY_IOS
            return string.IsNullOrEmpty(adsSettings.VungleContainer.IOSAppId) ? 
                "test_app_id" : adsSettings.VungleContainer.IOSAppId;
#else
            return "unexpected_platform";
#endif
        }

        private string GetBannerID()
        {
#if UNITY_EDITOR
            return "test_banner";
#elif UNITY_ANDROID
            return string.IsNullOrEmpty(adsSettings.VungleContainer.AndroidBannerID) ? 
                "test_banner" : adsSettings.VungleContainer.AndroidBannerID;
#elif UNITY_IOS
            return string.IsNullOrEmpty(adsSettings.VungleContainer.IOSBannerID) ? 
                "test_banner" : adsSettings.VungleContainer.IOSBannerID;
#else
            return "unexpected_platform";
#endif
        }

        private string GetInterstitialID()
        {
#if UNITY_EDITOR
            return "test_interstitial";
#elif UNITY_ANDROID
            return string.IsNullOrEmpty(adsSettings.VungleContainer.AndroidInterstitialID) ? 
                "test_interstitial" : adsSettings.VungleContainer.AndroidInterstitialID;
#elif UNITY_IOS
            return string.IsNullOrEmpty(adsSettings.VungleContainer.IOSInterstitialID) ? 
                "test_interstitial" : adsSettings.VungleContainer.IOSInterstitialID;
#else
            return "unexpected_platform";
#endif
        }

        private string GetRewardedVideoID()
        {
#if UNITY_EDITOR
            return "test_rewarded";
#elif UNITY_ANDROID
            return string.IsNullOrEmpty(adsSettings.VungleContainer.AndroidRewardedVideoID) ? 
                "test_rewarded" : adsSettings.VungleContainer.AndroidRewardedVideoID;
#elif UNITY_IOS
            return string.IsNullOrEmpty(adsSettings.VungleContainer.IOSRewardedVideoID) ? 
                "test_rewarded" : adsSettings.VungleContainer.IOSRewardedVideoID;
#else
            return "unexpected_platform";
#endif
        }
        #endregion

        #region Cleanup & Dispose
        protected override void Dispose(bool disposing)
        {
            if (!vungleDisposed)
            {
                if (disposing)
                {
                    // Clean up managed resources
                    DestroyBanner();

                    // Unsubscribe from events
                    UnregisterVungleEvents();

                    // Clear callbacks
                    currentInterstitialCallback = null;
                    currentRewardedCallback = null;

                    DebugLog("[Vungle]: Resources cleaned up");
                }

                vungleDisposed = true;
                base.Dispose(disposing);
            }
        }

        ~VungleHandler()
        {
            Dispose(false);
        }
        #endregion

        #region Privacy & Compliance
        public override void SetGDPR(bool state)
        {
            try
            {
                var consentStatus = state ? Vungle.Consent.Accepted : Vungle.Consent.Denied;
                Vungle.updateConsentStatus(consentStatus);
                DebugLog($"[Vungle]: GDPR set to: {state}");
            }
            catch (Exception e)
            {
                DebugLogError($"[Vungle]: Failed to set GDPR: {e.Message}");
            }
        }

        public override void SetCCPA(bool state)
        {
            try
            {
                var consentStatus = state ? Vungle.Consent.Accepted : Vungle.Consent.Denied;
                Vungle.updateCCPAStatus(consentStatus);
                DebugLog($"[Vungle]: CCPA set to: {state}");
            }
            catch (Exception e)
            {
                DebugLogError($"[Vungle]: Failed to set CCPA: {e.Message}");
            }
        }

        public override void SetAgeRestricted(bool state)
        {
            SetCOPPA(state);
            DebugLog($"[Vungle]: Age restriction set to: {state}");
        }

        public override void SetCOPPA(bool state)
        {
            try
            {
                Vungle.updateCoppaStatus(state);
                DebugLog($"[Vungle]: COPPA set to: {state}");
            }
            catch (Exception e)
            {
                DebugLogError($"[Vungle]: Failed to set COPPA: {e.Message}");
            }
        }

        public override void SetUserConsent(bool state)
        {
            SetGDPR(state);
        }

        public override void SetUserLocation(double latitude, double longitude)
        {
            DebugLog($"[Vungle]: User location setting requested: {latitude}, {longitude}");
        }
        #endregion
    }
#endif
}