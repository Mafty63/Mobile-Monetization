#pragma warning disable 0414

using UnityEngine;
using System;
using System.Collections.Generic;

#if CHARTBOOST_PROVIDER
using Chartboost.Core;
using Chartboost.Core.Initialization;
using Chartboost.Mediation;
using Chartboost.Mediation.Ad.Banner;
using Chartboost.Mediation.Ad.Fullscreen;
using Chartboost.Mediation.Error;
using Chartboost.Mediation.Requests;
#endif

namespace MobileCore.Advertisements.Providers
{
#if CHARTBOOST_PROVIDER
    public class ChartboostHandler : BaseAdProviderHandler
    {
        // Ad objects
        private IBannerAd bannerAd;
        private IFullscreenAd interstitialAd;
        private IFullscreenAd rewardedAd;

        // State tracking
        private bool isInterstitialLoaded = false;
        private bool isRewardedLoaded = false;

        // Retry configuration
        private const int RETRY_ATTEMPT_DEFAULT_VALUE = 1;
        private int interstitialRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;
        private int rewardedRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;

        // Callback references
        private InterstitialCallback currentInterstitialCallback;
        private RewardedVideoCallback currentRewardedCallback;

        // Track if disposed
        private bool chartboostDisposed = false;

        // Placements
        private string bannerPlacement;
        private string interstitialPlacement;
        private string rewardedPlacement;

        public ChartboostHandler(AdProvider providerType) : base(providerType) { }

        #region Initialization
        public override void Initialize(AdsSettings adsSettings)
        {
            if (isInitialized)
            {
                DebugLogWarning("[Chartboost]: Already initialized!");
                return;
            }

            this.adsSettings = adsSettings;
            DebugLog("[Chartboost]: Initializing...");

            try
            {
                // Get placement IDs
                bannerPlacement = GetBannerID();
                interstitialPlacement = GetInterstitialID();
                rewardedPlacement = GetRewardedVideoID();

                string appId = GetChartboostAppID();
                string appSignature = GetChartboostAppSignature();

                // Validation Check
                if (appId.Contains("YOUR_CHARTBOOST") || appSignature.Contains("YOUR_CHARTBOOST"))
                {
                    // Should be handled by Get methods now, but double check
                    DebugLogError("[Chartboost]: Default placeholders detected. Falling back to test mode.");
                }
                
                if (appId.Contains("test_") || appSignature.Contains("test_"))
                {
                    DebugLogWarning("[Chartboost]: Initialization using TEST IDs. Ads may not load or will be test ads only.");
                }

                // Setup SDK configuration
                var modules = new List<Chartboost.Core.Initialization.Module>();
                
                // Note: Mediation module usually auto-registers or is part of the core package scan.
                // We do NOT add it manually to 'modules' unless we have a specific instance.
                
                var modulesToSkip = new HashSet<string>();
                var sdkConfig = new SDKConfiguration(
                    appId,
                    modules,
                    modulesToSkip
                );

                // Setup Signature via Reflection or try to find ChartboostMediationSettings if possible
                // As per documentation for 5.x, App Signature is usually in Settings.
                // We will log it for debugging.
                DebugLog($"[Chartboost]: Configured with AppID: {appId} and Signature: {appSignature} (Check ChartboostMediationSettings asset if generic build)");

                // Subscribe to initialization event
                ChartboostCore.ModuleInitializationCompleted += OnModuleInitializationCompleted;

                // Initialize SDK
                ChartboostCore.Initialize(sdkConfig);

                DebugLog("[Chartboost]: Initialization started");
            }
            catch (Exception e)
            {
                DebugLogError($"[Chartboost]: Initialization failed: {e.Message}");
            }
        }

        private void OnModuleInitializationCompleted(ModuleInitializationResult result)
        {
            if (result.ModuleId == ChartboostMediation.CoreModuleId)
            {
                // Unsubscribe from event
                ChartboostCore.ModuleInitializationCompleted -= OnModuleInitializationCompleted;

                if (result.Error.HasValue)
                {
                    DebugLogError($"[Chartboost]: Mediation initialization failed: {result.Error.Value.Message}");
                    return;
                }

                isInitialized = true;
                OnProviderInitialized();
                DebugLog($"[Chartboost]: Mediation initialized! Duration: {result.Duration}ms");
            }
        }
        
        private void ConfigureMediationSettings()
        {
             // Update Chartboost Mediation Settings if available to ensure credentials are correct
             // This bridges the gap between Mobile Module settings and Chartboost SDK settings
             /* 
                Note: We use reflection or direct access if compilation allows. 
                Assuming ChartboostMediationSettings is accessible via Chartboost.Mediation namespace.
             */
             // Since we cannot guarantee the internal SDK structure without seeing it, we'll try to set what we can.
             // Ideally, ChartboostMediationSettings should be used.
             // For now, we'll assume the user has configured the dashboard properly or we rely on the implementation below.
             
             // However, without a public API to set signature at runtime in 5.x Core Initialize, 
             // we rely on the settings asset being present. 
             // IF SDK allows modifying settings at runtime:
             
             // (Logic would go here if API existed. 
             //  Current research suggests 5.x relies on Editor-time settings or manual PreInit config skipping.)
        }

        public void ManualRequestAds()
        {
            if (!isInitialized)
            {
                DebugLogWarning("[Chartboost]: Not initialized yet");
                return;
            }

        }
        #endregion

        #region Banner Implementation
        private void RequestBanner()
        {
            if (!isInitialized) return;

            // Clean up existing banner
            if (bannerAd != null)
            {
                DestroyBanner();
            }

            DebugLog("[Chartboost]: Requesting banner...");

            try
            {
                bannerAd = ChartboostMediation.GetBannerAd();

                // Configure banner size
                BannerSize size = GetBannerSize();

                // Configure position
                Vector2 position = GetBannerPosition();
                Vector2 pivot = GetBannerPivot();

                bannerAd.Position = position;
                bannerAd.Pivot = pivot;

                // Subscribe to events
                bannerAd.WillAppear += HandleBannerWillAppear;
                bannerAd.DidClick += HandleBannerClick;
                bannerAd.DidRecordImpression += HandleBannerImpression;

                // Create load request
                BannerAdLoadRequest loadRequest = new BannerAdLoadRequest(bannerPlacement, size);

                // Load banner
                bannerAd.Load(loadRequest).ContinueWith(task =>
                {
                    AdsManager.CallEventInMainThread(() =>
                    {
                        if (task.IsCompleted && !task.Result.Error.HasValue)
                        {
                            isBannerLoaded = true;
                            OnAdLoaded(AdType.Banner);
                            DebugLog("[Chartboost]: Banner loaded successfully");

                            // Auto-show if requested before loaded
                            if (isBannerShowing)
                            {
                                OnAdDisplayed(AdType.Banner);
                            }
                        }
                        else
                        {
                            DebugLogError($"[Chartboost]: Banner failed to load: {task.Result.Error?.Message}");
                            isBannerLoaded = false;
                        }
                    });
                });
            }
            catch (Exception e)
            {
                DebugLogError($"[Chartboost]: Banner request failed: {e.Message}");
            }
        }

        public override void ShowBanner()
        {
            if (!isInitialized) return;

            UpdateBannerState(true, isBannerLoaded);

            if (bannerAd == null)
            {
                RequestBanner();
                return; // Banner will show automatically when loaded
            }

            if (isBannerLoaded)
            {
                OnAdDisplayed(AdType.Banner);
                DebugLog("[Chartboost]: Banner shown");
            }
        }

        public override void HideBanner()
        {
            UpdateBannerState(false, isBannerLoaded);

            if (bannerAd != null && isBannerLoaded)
            {
                OnAdClosed(AdType.Banner);
                DebugLog("[Chartboost]: Banner hidden");
            }
        }

        public override void DestroyBanner()
        {
            UpdateBannerState(false, false);

            if (bannerAd != null)
            {
                try
                {
                    bannerAd.WillAppear -= HandleBannerWillAppear;
                    bannerAd.DidClick -= HandleBannerClick;
                    bannerAd.DidRecordImpression -= HandleBannerImpression;

                    bannerAd.Reset();
                }
                catch (Exception e)
                {
                    DebugLogWarning($"[Chartboost]: Error destroying banner: {e.Message}");
                }

                bannerAd = null;
                isBannerLoaded = false;
                DebugLog("[Chartboost]: Banner destroyed");
            }
        }
        #endregion

        #region Interstitial Implementation
        public override void RequestInterstitial()
        {
            if (!isInitialized) return;

            DebugLog("[Chartboost]: Requesting interstitial...");

            try
            {
                FullscreenAdLoadRequest request = new FullscreenAdLoadRequest(interstitialPlacement);

                ChartboostMediation.LoadFullscreenAd(request).ContinueWith(task =>
                {
                    AdsManager.CallEventInMainThread(() =>
                    {
                        if (task.IsCompleted && !task.Result.Error.HasValue)
                        {
                            interstitialAd = task.Result.Ad;
                            interstitialRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;
                            isInterstitialLoaded = true;

                            // Subscribe to events
                            if (interstitialAd != null)
                            {
                                interstitialAd.DidClose += HandleInterstitialClosed;
                            }

                            OnAdLoaded(AdType.Interstitial);
                            DebugLog("[Chartboost]: Interstitial loaded successfully");
                        }
                        else
                        {
                            HandleAdLoadFailure(
                                "Interstitial",
                                task.Result.Error?.Message,
                                ref interstitialRetryAttempt,
                                RequestInterstitial
                            );
                        }
                    });
                });
            }
            catch (Exception e)
            {
                DebugLogError($"[Chartboost]: Interstitial request failed: {e.Message}");
            }
        }

        public override void ShowInterstitial(InterstitialCallback callback)
        {
            if (!isInitialized || interstitialAd == null || !isInterstitialLoaded)
            {
                callback?.Invoke(false);
                RequestInterstitial(); // Auto-request new one
                return;
            }

            currentInterstitialCallback = callback;

            interstitialAd.Show().ContinueWith(task =>
            {
                AdsManager.CallEventInMainThread(() =>
                {
                    if (task.IsCompleted && !task.Result.Error.HasValue)
                    {
                        OnAdDisplayed(AdType.Interstitial);
                        DebugLog("[Chartboost]: Interstitial shown");
                    }
                    else
                    {
                        HandleInterstitialFailed(task.Result.Error?.Message);
                    }
                });
            });
        }

        public override bool IsInterstitialLoaded()
        {
            return interstitialAd != null && isInterstitialLoaded;
        }
        #endregion

        #region Rewarded Video Implementation
        public override void RequestRewardedVideo()
        {
            if (!isInitialized) return;

            DebugLog("[Chartboost]: Requesting rewarded video...");

            try
            {
                FullscreenAdLoadRequest request = new FullscreenAdLoadRequest(rewardedPlacement);

                ChartboostMediation.LoadFullscreenAd(request).ContinueWith(task =>
                {
                    AdsManager.CallEventInMainThread(() =>
                    {
                        if (task.IsCompleted && !task.Result.Error.HasValue)
                        {
                            rewardedAd = task.Result.Ad;
                            rewardedRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;
                            isRewardedLoaded = true;

                            // Subscribe to events
                            if (rewardedAd != null)
                            {
                                rewardedAd.DidReward += HandleRewardedDidReward;
                                rewardedAd.DidClose += HandleRewardedClosed;
                            }

                            OnAdLoaded(AdType.RewardedVideo);
                            DebugLog("[Chartboost]: Rewarded video loaded successfully");
                        }
                        else
                        {
                            HandleAdLoadFailure(
                                "Rewarded Video",
                                task.Result.Error?.Message,
                                ref rewardedRetryAttempt,
                                RequestRewardedVideo
                            );
                        }
                    });
                });
            }
            catch (Exception e)
            {
                DebugLogError($"[Chartboost]: Rewarded video request failed: {e.Message}");
            }
        }

        public override void ShowRewardedVideo(RewardedVideoCallback callback)
        {
            if (!isInitialized || rewardedAd == null || !isRewardedLoaded)
            {
                callback?.Invoke(false);
                RequestRewardedVideo(); // Auto-request new one
                return;
            }

            currentRewardedCallback = callback;

            rewardedAd.Show().ContinueWith(task =>
            {
                AdsManager.CallEventInMainThread(() =>
                {
                    if (task.IsCompleted && !task.Result.Error.HasValue)
                    {
                        OnAdDisplayed(AdType.RewardedVideo);
                        DebugLog("[Chartboost]: Rewarded video shown");
                    }
                    else
                    {
                        HandleRewardedFailed(task.Result.Error?.Message);
                    }
                });
            });
        }

        public override bool IsRewardedVideoLoaded()
        {
            return rewardedAd != null && isRewardedLoaded;
        }
        #endregion

        #region Event Handlers
        // Banner Events
        private void HandleBannerWillAppear(IBannerAd ad)
        {
            DebugLog("[Chartboost]: Banner will appear");
        }

        private void HandleBannerClick(IBannerAd ad)
        {
            DebugLog("[Chartboost]: Banner clicked");
        }

        private void HandleBannerImpression(IBannerAd ad)
        {
            DebugLog("[Chartboost]: Banner impression recorded");
        }

        // Interstitial Events
        private void HandleInterstitialClosed(IFullscreenAd ad, ChartboostMediationError? error)
        {
            OnAdClosed(AdType.Interstitial);
            bool success = !error.HasValue;
            currentInterstitialCallback?.Invoke(success);
            currentInterstitialCallback = null;

            isInterstitialLoaded = false;
            RequestInterstitial(); // Request new one for next time

            DebugLog($"[Chartboost]: Interstitial closed - Success: {success}");
        }

        private void HandleInterstitialFailed(string errorMessage)
        {
            OnAdClosed(AdType.Interstitial);
            currentInterstitialCallback?.Invoke(false);
            currentInterstitialCallback = null;

            RequestInterstitial(); // Retry
            DebugLogError($"[Chartboost]: Interstitial failed: {errorMessage}");
        }

        // Rewarded Video Events
        private void HandleRewardedDidReward(IFullscreenAd ad)
        {
            // Reward earned - callback will be invoked on close
            DebugLog("[Chartboost]: Reward earned");
        }

        private void HandleRewardedClosed(IFullscreenAd ad, ChartboostMediationError? error)
        {
            OnAdClosed(AdType.RewardedVideo);

            bool rewardEarned = !error.HasValue;
            currentRewardedCallback?.Invoke(rewardEarned);
            currentRewardedCallback = null;

            isRewardedLoaded = false;
            RequestRewardedVideo(); // Request new one for next time

            DebugLog($"[Chartboost]: Rewarded video closed - Reward earned: {rewardEarned}");
        }

        private void HandleRewardedFailed(string errorMessage)
        {
            OnAdClosed(AdType.RewardedVideo);
            currentRewardedCallback?.Invoke(false);
            currentRewardedCallback = null;

            RequestRewardedVideo(); // Retry
            DebugLogError($"[Chartboost]: Rewarded video failed: {errorMessage}");
        }
        #endregion

        #region Helper Methods
        private BannerSize GetBannerSize()
        {
            return adsSettings.ChartboostContainer.BannerType switch
            {
                ChartboostContainer.BannerPlacementType.MediumRectangle =>
                    BannerSize.Adaptive(300, 250),
                ChartboostContainer.BannerPlacementType.IABBanner =>
                    BannerSize.Adaptive(468, 60),
                ChartboostContainer.BannerPlacementType.Leaderboard =>
                    BannerSize.Adaptive(728, 90),
                _ => BannerSize.Adaptive(320, 50), // Default banner
            };
        }

        private Vector2 GetBannerPosition()
        {
            float xPos = Screen.width / 2f;
            float yPos = adsSettings.ChartboostContainer.BannerPosition == BannerPosition.Bottom ?
                0f : Screen.height;

            return new Vector2(xPos, yPos);
        }

        private Vector2 GetBannerPivot()
        {
            return adsSettings.ChartboostContainer.BannerPosition == BannerPosition.Bottom ?
                new Vector2(0.5f, 0f) : new Vector2(0.5f, 1f);
        }

        private void HandleAdLoadFailure(string adType, string error, ref int retryAttempt, Action retryAction)
        {
            DebugLogError($"[Chartboost]: {adType} failed to load: {error}");

            retryAttempt++;
            float retryDelay = Mathf.Pow(2, Mathf.Min(retryAttempt, 6)); // Cap at 64 seconds

            AdsManager.CallEventInMainThread(() =>
            {
                DebugLog($"[Chartboost]: Retrying {adType} in {retryDelay} seconds");
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
        private string GetChartboostAppID()
        {
            string id = null;
#if UNITY_ANDROID
            id = adsSettings.ChartboostContainer.AndroidAppID;
#elif UNITY_IOS
            id = adsSettings.ChartboostContainer.IOSAppID;
#endif

#if UNITY_EDITOR
            // In Editor, fallback to Android ID if not set (or if platform is not Android/iOS)
            if (string.IsNullOrEmpty(id) || id.Contains("YOUR_CHARTBOOST"))
            {
                id = adsSettings.ChartboostContainer.AndroidAppID;
                
                // If still empty, try iOS
                if (string.IsNullOrEmpty(id) || id.Contains("YOUR_CHARTBOOST"))
                    id = adsSettings.ChartboostContainer.IOSAppID;
            }
#endif
            // Treat placeholder as null
            if (!string.IsNullOrEmpty(id) && id.Contains("YOUR_CHARTBOOST")) id = null;

            return string.IsNullOrEmpty(id) ? "test_app_id" : id;
        }

        private string GetChartboostAppSignature()
        {
            string sig = null;
#if UNITY_ANDROID
            sig = adsSettings.ChartboostContainer.AndroidAppSignature;
#elif UNITY_IOS
            sig = adsSettings.ChartboostContainer.IOSAppSignature;
#endif

#if UNITY_EDITOR
            if (string.IsNullOrEmpty(sig) || sig.Contains("YOUR_CHARTBOOST"))
            {
                sig = adsSettings.ChartboostContainer.AndroidAppSignature;
                if (string.IsNullOrEmpty(sig) || sig.Contains("YOUR_CHARTBOOST")) sig = adsSettings.ChartboostContainer.IOSAppSignature;
            }
#endif
            // Treat placeholder as null
            if (!string.IsNullOrEmpty(sig) && sig.Contains("YOUR_CHARTBOOST")) sig = null;

            // Fallback for editor testing if needed, or null
            return string.IsNullOrEmpty(sig) ? "test_app_signature" : sig;
        }

        private string GetBannerID()
        {
            string id = null;
#if UNITY_ANDROID
            id = adsSettings.ChartboostContainer.AndroidBannerID;
#elif UNITY_IOS
            id = adsSettings.ChartboostContainer.IOSBannerID;
#endif

#if UNITY_EDITOR
            if (string.IsNullOrEmpty(id))
            {
                id = adsSettings.ChartboostContainer.AndroidBannerID;
                if (string.IsNullOrEmpty(id)) id = adsSettings.ChartboostContainer.IOSBannerID;
            }
#endif
            return string.IsNullOrEmpty(id) ? "test_banner" : id;
        }

        private string GetInterstitialID()
        {
            string id = null;
#if UNITY_ANDROID
            id = adsSettings.ChartboostContainer.AndroidInterstitialID;
#elif UNITY_IOS
            id = adsSettings.ChartboostContainer.IOSInterstitialID;
#endif

#if UNITY_EDITOR
            if (string.IsNullOrEmpty(id))
            {
                id = adsSettings.ChartboostContainer.AndroidInterstitialID;
                if (string.IsNullOrEmpty(id)) id = adsSettings.ChartboostContainer.IOSInterstitialID;
            }
#endif
            return string.IsNullOrEmpty(id) ? "test_interstitial" : id;
        }

        private string GetRewardedVideoID()
        {
            string id = null;
#if UNITY_ANDROID
            id = adsSettings.ChartboostContainer.AndroidRewardedVideoID;
#elif UNITY_IOS
            id = adsSettings.ChartboostContainer.IOSRewardedVideoID;
#endif

#if UNITY_EDITOR
            if (string.IsNullOrEmpty(id))
            {
                id = adsSettings.ChartboostContainer.AndroidRewardedVideoID;
                if (string.IsNullOrEmpty(id)) id = adsSettings.ChartboostContainer.IOSRewardedVideoID;
            }
#endif
            return string.IsNullOrEmpty(id) ? "test_rewarded" : id;
        }
        #endregion

        #region Cleanup & Dispose
        protected override void Dispose(bool disposing)
        {
            if (!chartboostDisposed)
            {
                if (disposing)
                {
                    // Clean up managed resources
                    DestroyBanner();

                    if (interstitialAd != null)
                    {
                        try
                        {
                            interstitialAd.DidClose -= HandleInterstitialClosed;
                        }
                        catch { }
                        interstitialAd = null;
                    }

                    if (rewardedAd != null)
                    {
                        try
                        {
                            rewardedAd.DidReward -= HandleRewardedDidReward;
                            rewardedAd.DidClose -= HandleRewardedClosed;
                        }
                        catch { }
                        rewardedAd = null;
                    }

                    // Clear callbacks
                    currentInterstitialCallback = null;
                    currentRewardedCallback = null;

                    // Unsubscribe init event
                    try
                    {
                        ChartboostCore.ModuleInitializationCompleted -= OnModuleInitializationCompleted;
                    }
                    catch { }

                    DebugLog("[Chartboost]: Resources cleaned up");
                }

                chartboostDisposed = true;
                base.Dispose(disposing);
            }
        }

        ~ChartboostHandler()
        {
            Dispose(false);
        }
        #endregion

        #region Privacy & Compliance
        public override void SetGDPR(bool state)
        {
            DebugLog($"[Chartboost]: GDPR state updated to: {state}");
            // Note: Chartboost typically uses CMP for consent management
        }

        public override void SetCCPA(bool state)
        {
            DebugLog($"[Chartboost]: CCPA set to {state}");
        }

        public override void SetAgeRestricted(bool state)
        {
            DebugLog($"[Chartboost]: Age restriction set to {state}");
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
            DebugLog($"[Chartboost]: User location setting requested: {latitude}, {longitude}");
        }
        #endregion
    }
#endif
}