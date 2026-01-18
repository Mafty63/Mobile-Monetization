using UnityEngine;
using System;
using System.Collections.Generic;

#if ADMOB_PROVIDER
using GoogleMobileAds.Api;
#endif

namespace MobileCore.Advertisements.Providers
{
#if ADMOB_PROVIDER
    public class AdMobHandler : BaseAdProviderHandler
    {
        // Ad objects
        private BannerView bannerView;
        private InterstitialAd interstitial;
        private RewardedAd rewardedAd;

        // Retry configuration
        private const int RETRY_ATTEMPT_DEFAULT_VALUE = 1;
        private int interstitialRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;
        private int rewardedRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;

        // Callback references
        private InterstitialCallback currentInterstitialCallback;
        private RewardedVideoCallback currentRewardedCallback;

        // Track if disposed
        private bool adMobDisposed = false;

        public AdMobHandler(AdProvider providerType) : base(providerType) { }

        #region Initialization
        // Sync initialization (legacy)
        public override void Initialize(AdsSettings adsSettings)
        {
            if (isInitialized)
            {
                DebugLogWarning($"[AdMob]: Already initialized!");
                return;
            }

            this.adsSettings = adsSettings;
            DebugLog("[AdMob]: Initializing...");

            try
            {
                // Configure request settings
                var requestConfig = new RequestConfiguration
                {
                    TagForChildDirectedTreatment = TagForChildDirectedTreatment.Unspecified,
                    TestDeviceIds = adsSettings.AdMobContainer.TestDevicesIDs
                };

                MobileAds.SetRequestConfiguration(requestConfig);
                MobileAds.SetiOSAppPauseOnBackground(true);

                // Initialize SDK synchronously using TaskCompletionSource
                var initTask = new System.Threading.Tasks.TaskCompletionSource<bool>();

                MobileAds.Initialize(initStatus =>
                {
                    isInitialized = (initStatus != null);
                    initTask.SetResult(isInitialized);
                });

                // Wait for initialization to complete (blocking)
                initTask.Task.GetAwaiter().GetResult();

                if (isInitialized)
                {
                    OnProviderInitialized();
                    DebugLog("[AdMob]: Initialization completed successfully");

                }
            }
            catch (Exception e)
            {
                DebugLogError($"[AdMob]: Initialization failed: {e.Message}");
            }
        }

        // Async initialization (preferred)
        public override async System.Threading.Tasks.Task<bool> InitializeAsync(AdsSettings adsSettings)
        {
            if (isInitialized)
                return true;

            this.adsSettings = adsSettings;
            DebugLog("[AdMob]: Initializing asynchronously...");

            try
            {
                // Configure request settings with GDPR and test devices
                var requestConfig = new RequestConfiguration
                {
                    TagForChildDirectedTreatment = TagForChildDirectedTreatment.Unspecified,
                    TestDeviceIds = adsSettings.AdMobContainer.TestDevicesIDs
                };

                // Apply GDPR setting if available
                if (AdsManager.IsGDPRStateExist())
                {
                    bool gdprState = AdsManager.GetGDPRState();
                    DebugLog($"[AdMob]: GDPR detected: {gdprState}");
                }

                MobileAds.SetRequestConfiguration(requestConfig);
                MobileAds.SetiOSAppPauseOnBackground(true);

                // Initialize SDK asynchronously
                var initTask = new System.Threading.Tasks.TaskCompletionSource<bool>();

                MobileAds.Initialize(initStatus =>
                {
                    isInitialized = (initStatus != null);
                    initTask.SetResult(isInitialized);
                });

                await initTask.Task;

                if (isInitialized)
                {
                    OnProviderInitialized();
                    DebugLog("[AdMob]: Async initialization completed successfully");
                }

                return isInitialized;
            }
            catch (Exception e)
            {
                DebugLogError($"[AdMob]: Async initialization failed: {e.Message}");
                return false;
            }
        }
        #endregion

        #region Banner Implementation
        private void RequestBanner()
        {
            if (!isInitialized) return;

            // Clean up existing banner
            if (bannerView != null)
            {
                UnsubscribeBannerEvents();
                bannerView.Destroy();
                bannerView = null;
                UpdateBannerState(false, false);
            }

            DebugLog("[AdMob]: Requesting banner...");

            try
            {
                var adSize = GetAdSize();
                var adPosition = GetAdPosition();

                bannerView = new BannerView(GetBannerID(), adSize, adPosition);
                SubscribeBannerEvents(bannerView);
                bannerView.LoadAd(CreateAdRequest());

                DebugLog("[AdMob]: Banner request sent");
            }
            catch (Exception e)
            {
                DebugLogError($"[AdMob]: Banner request failed: {e.Message}");
            }
        }

        public override void ShowBanner()
        {
            if (!isInitialized) return;

            UpdateBannerState(true, isBannerLoaded);

            if (bannerView == null)
            {
                RequestBanner();
                return; // Banner will show automatically when loaded
            }

            if (isBannerLoaded)
            {
                bannerView.Show();
                OnAdDisplayed(AdType.Banner);
                DebugLog("[AdMob]: Banner shown");
            }
            // If not loaded yet, it will show automatically when HandleAdLoaded is called
        }

        public override void HideBanner()
        {
            UpdateBannerState(false, isBannerLoaded);

            if (bannerView != null && isBannerLoaded)
            {
                bannerView.Hide();
                OnAdClosed(AdType.Banner);
                DebugLog("[AdMob]: Banner hidden");
            }
        }

        public override void DestroyBanner()
        {
            UpdateBannerState(false, false);

            if (bannerView != null)
            {
                UnsubscribeBannerEvents();
                bannerView.Destroy();
                bannerView = null;
                DebugLog("[AdMob]: Banner destroyed");
            }
        }
        #endregion

        #region Interstitial Implementation
        public override void RequestInterstitial()
        {
            if (!isInitialized) return;

            // Clean up existing interstitial
            if (interstitial != null)
            {
                UnsubscribeInterstitialEvents();
                interstitial.Destroy();
                interstitial = null;
            }

            DebugLog("[AdMob]: Requesting interstitial...");

            InterstitialAd.Load(GetInterstitialID(), CreateAdRequest(),
                (InterstitialAd ad, LoadAdError error) =>
                {
                    if (error != null || ad == null)
                    {
                        HandleAdLoadFailure("Interstitial", error?.GetMessage(),
                            ref interstitialRetryAttempt, RequestInterstitial);
                        return;
                    }

                    interstitial = ad;
                    interstitialRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;
                    SubscribeInterstitialEvents(ad);
                    OnAdLoaded(AdType.Interstitial);
                    DebugLog("[AdMob]: Interstitial loaded successfully");
                });
        }

        public override void ShowInterstitial(InterstitialCallback callback)
        {
            if (!isInitialized || interstitial == null || !interstitial.CanShowAd())
            {
                callback?.Invoke(false);
                RequestInterstitial(); // Auto-request new one
                return;
            }

            currentInterstitialCallback = callback;
            interstitial.Show();
        }

        public override bool IsInterstitialLoaded()
        {
            return interstitial != null && interstitial.CanShowAd();
        }
        #endregion

        #region Rewarded Video Implementation
        public override void RequestRewardedVideo()
        {
            if (!isInitialized) return;

            DebugLog("[AdMob]: Requesting rewarded video...");

            RewardedAd.Load(GetRewardedVideoID(), CreateAdRequest(),
                (RewardedAd ad, LoadAdError error) =>
                {
                    if (error != null || ad == null)
                    {
                        HandleAdLoadFailure("Rewarded Video", error?.GetMessage(),
                            ref rewardedRetryAttempt, RequestRewardedVideo);
                        return;
                    }

                    rewardedAd = ad;
                    rewardedRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;
                    SubscribeRewardedEvents(ad);
                    OnAdLoaded(AdType.RewardedVideo);
                    DebugLog("[AdMob]: Rewarded video loaded successfully");
                });
        }

        public override void ShowRewardedVideo(RewardedVideoCallback callback)
        {
            if (!isInitialized || rewardedAd == null || !rewardedAd.CanShowAd())
            {
                callback?.Invoke(false);
                RequestRewardedVideo(); // Auto-request new one
                return;
            }

            currentRewardedCallback = callback;

            rewardedAd.Show(reward =>
            {
                // User earned reward
                currentRewardedCallback?.Invoke(true);
                currentRewardedCallback = null;
                DebugLog("[AdMob]: Reward earned");

                // Request new rewarded ad for next time
                RequestRewardedVideo();
            });
        }

        public override bool IsRewardedVideoLoaded()
        {
            return rewardedAd != null && rewardedAd.CanShowAd();
        }
        #endregion

        #region Event Handlers
        // Banner Events
        private void HandleBannerLoaded()
        {
            UpdateBannerState(isBannerShowing, true);
            OnAdLoaded(AdType.Banner);

            // Auto-show if requested before loaded
            if (isBannerShowing)
            {
                bannerView?.Show();
                OnAdDisplayed(AdType.Banner);
            }

            DebugLog("[AdMob]: Banner loaded");
        }

        private void HandleBannerFailedToLoad(LoadAdError error)
        {
            UpdateBannerState(false, false);
            DebugLogError($"[AdMob]: Banner failed to load: {error?.GetMessage()}");
        }

        // Interstitial Events
        private void HandleInterstitialOpened()
        {
            OnAdDisplayed(AdType.Interstitial);
            DebugLog("[AdMob]: Interstitial opened");
        }

        private void HandleInterstitialClosed()
        {
            OnAdClosed(AdType.Interstitial);
            currentInterstitialCallback?.Invoke(true);
            currentInterstitialCallback = null;

            RequestInterstitial(); // Request new one for next time
            DebugLog("[AdMob]: Interstitial closed");
        }

        private void HandleInterstitialFailed(AdError error)
        {
            OnAdClosed(AdType.Interstitial);
            currentInterstitialCallback?.Invoke(false);
            currentInterstitialCallback = null;

            RequestInterstitial(); // Retry
            DebugLogError($"[AdMob]: Interstitial failed: {error?.GetMessage()}");
        }

        // Rewarded Video Events
        private void HandleRewardedOpened()
        {
            OnAdDisplayed(AdType.RewardedVideo);
            DebugLog("[AdMob]: Rewarded video opened");
        }

        private void HandleRewardedClosed()
        {
            OnAdClosed(AdType.RewardedVideo);

            // User closed without earning reward
            if (currentRewardedCallback != null)
            {
                currentRewardedCallback.Invoke(false);
                currentRewardedCallback = null;
            }

            DebugLog("[AdMob]: Rewarded video closed");
        }

        private void HandleRewardedFailed(AdError error)
        {
            OnAdClosed(AdType.RewardedVideo);
            currentRewardedCallback?.Invoke(false);
            currentRewardedCallback = null;

            RequestRewardedVideo(); // Retry
            DebugLogError($"[AdMob]: Rewarded video failed: {error?.GetMessage()}");
        }
        #endregion

        #region Helper Methods
        private AdRequest CreateAdRequest()
        {
            try
            {
                // Untuk SDK versi lama yang tidak punya Builder
                // Coba buat AdRequest dengan constructor langsung

                if (AdsManager.IsGDPRStateExist())
                {
                    bool gdprConsent = AdsManager.GetGDPRState();
                    string npaValue = gdprConsent ? "0" : "1";

                    DebugLog($"[AdMob]: GDPR consent: {gdprConsent}, npa: {npaValue}");

                    // Coba buat AdRequest dengan Extras menggunakan reflection
                    try
                    {
                        // Method 1: Coba gunakan constructor yang ada
                        var adRequest = new AdRequest();

                        // Coba set Extras property via reflection
                        var extrasProp = adRequest.GetType().GetProperty("Extras");
                        if (extrasProp != null && extrasProp.CanWrite)
                        {
                            var extras = new Dictionary<string, string>
                            {
                                { "npa", npaValue }
                            };
                            extrasProp.SetValue(adRequest, extras);
                        }
                        else
                        {
                            // Jika tidak ada Extras property, coba AddExtra method
                            var addExtraMethod = adRequest.GetType().GetMethod("AddExtra");
                            if (addExtraMethod != null)
                            {
                                addExtraMethod.Invoke(adRequest, new object[] { "npa", npaValue });
                            }
                        }

                        return adRequest;
                    }
                    catch (Exception ex)
                    {
                        DebugLog($"[AdMob]: Failed to add GDPR extras: {ex.Message}");
                        return new AdRequest(); // Return tanpa extras
                    }
                }
                else
                {
                    // No GDPR setting, create simple AdRequest
                    return new AdRequest();
                }
            }
            catch (Exception e)
            {
                DebugLogError($"[AdMob]: Failed to create AdRequest: {e.Message}");

                // Fallback: try to create using Activator
                try
                {
                    return (AdRequest)Activator.CreateInstance(typeof(AdRequest));
                }
                catch
                {
                    return null; // Last resort
                }
            }
        }

        private AdSize GetAdSize()
        {
            return adsSettings.AdMobContainer.BannerType switch
            {
                AdMobContainer.BannerPlacementType.MediumRectangle => AdSize.MediumRectangle,
                AdMobContainer.BannerPlacementType.IABBanner => AdSize.IABBanner,
                AdMobContainer.BannerPlacementType.Leaderboard => AdSize.Leaderboard,
                _ => AdSize.Banner,
            };
        }

        private AdPosition GetAdPosition()
        {
            return adsSettings.AdMobContainer.BannerPosition switch
            {
                BannerPosition.Top => AdPosition.Top,
                _ => AdPosition.Bottom,
            };
        }

        private void HandleAdLoadFailure(string adType, string error, ref int retryAttempt, Action retryAction)
        {
            DebugLogError($"[AdMob]: {adType} failed to load: {error}");

            retryAttempt++;
            float retryDelay = Mathf.Pow(2, Mathf.Min(retryAttempt, 6)); // Cap at 64 seconds

            // Use AdsManager to schedule retry in main thread
            AdsManager.CallEventInMainThread(() =>
            {
                DebugLog($"[AdMob]: Retrying {adType} in {retryDelay} seconds");

                // Gunakan MonoBehaviourExecution.Instance untuk menjalankan coroutine
                MonoBehaviourExecution.Instance.StartCoroutine(DelayedCall(retryDelay, retryAction));
            });
        }

        private System.Collections.IEnumerator DelayedCall(float delay, Action action)
        {
            yield return new WaitForSecondsRealtime(delay);
            action?.Invoke();
        }
        #endregion

        #region Event Subscription Management
        private void SubscribeBannerEvents(BannerView banner)
        {
            if (banner == null) return;

            banner.OnBannerAdLoaded += HandleBannerLoaded;
            banner.OnBannerAdLoadFailed += HandleBannerFailedToLoad;
            banner.OnAdFullScreenContentClosed += () => OnAdClosed(AdType.Banner);
        }

        private void UnsubscribeBannerEvents()
        {
            if (bannerView == null) return;

            bannerView.OnBannerAdLoaded -= HandleBannerLoaded;
            bannerView.OnBannerAdLoadFailed -= HandleBannerFailedToLoad;
            bannerView.OnAdFullScreenContentClosed -= () => OnAdClosed(AdType.Banner);
        }

        private void SubscribeInterstitialEvents(InterstitialAd ad)
        {
            if (ad == null) return;

            ad.OnAdFullScreenContentOpened += HandleInterstitialOpened;
            ad.OnAdFullScreenContentClosed += HandleInterstitialClosed;
            ad.OnAdFullScreenContentFailed += HandleInterstitialFailed;
        }

        private void UnsubscribeInterstitialEvents()
        {
            if (interstitial == null) return;

            interstitial.OnAdFullScreenContentOpened -= HandleInterstitialOpened;
            interstitial.OnAdFullScreenContentClosed -= HandleInterstitialClosed;
            interstitial.OnAdFullScreenContentFailed -= HandleInterstitialFailed;
        }

        private void SubscribeRewardedEvents(RewardedAd ad)
        {
            if (ad == null) return;

            ad.OnAdFullScreenContentOpened += HandleRewardedOpened;
            ad.OnAdFullScreenContentClosed += HandleRewardedClosed;
            ad.OnAdFullScreenContentFailed += HandleRewardedFailed;
        }

        private void UnsubscribeRewardedEvents()
        {
            if (rewardedAd == null) return;

            rewardedAd.OnAdFullScreenContentOpened -= HandleRewardedOpened;
            rewardedAd.OnAdFullScreenContentClosed -= HandleRewardedClosed;
            rewardedAd.OnAdFullScreenContentFailed -= HandleRewardedFailed;
        }
        #endregion

        #region Platform-Specific Methods
        private string GetBannerID()
        {
#if UNITY_EDITOR
            return "unused";
#elif UNITY_ANDROID
            return adsSettings.AdMobContainer.AndroidBannerID;
#elif UNITY_IOS
            return adsSettings.AdMobContainer.IOSBannerID;
#else
            return "unexpected_platform";
#endif
        }

        private string GetInterstitialID()
        {
#if UNITY_EDITOR
            return "unused";
#elif UNITY_ANDROID
            return adsSettings.AdMobContainer.AndroidInterstitialID;
#elif UNITY_IOS
            return adsSettings.AdMobContainer.IOSInterstitialID;
#else
            return "unexpected_platform";
#endif
        }

        private string GetRewardedVideoID()
        {
#if UNITY_EDITOR
            return "unused";
#elif UNITY_ANDROID
            return adsSettings.AdMobContainer.AndroidRewardedVideoID;
#elif UNITY_IOS
            return adsSettings.AdMobContainer.IOSRewardedVideoID;
#else
            return "unexpected_platform";
#endif
        }
        #endregion

        #region Cleanup & Dispose
        // Override the Dispose pattern from base class
        protected override void Dispose(bool disposing)
        {
            if (!adMobDisposed)
            {
                if (disposing)
                {
                    // Clean up managed resources
                    DestroyBanner();

                    if (interstitial != null)
                    {
                        UnsubscribeInterstitialEvents();
                        interstitial.Destroy();
                        interstitial = null;
                    }

                    if (rewardedAd != null)
                    {
                        UnsubscribeRewardedEvents();
                        rewardedAd = null;
                    }

                    // Clear callbacks
                    currentInterstitialCallback = null;
                    currentRewardedCallback = null;

                    DebugLog("[AdMob]: Resources cleaned up");
                }

                adMobDisposed = true;

                // Call base class Dispose
                base.Dispose(disposing);
            }
        }

        // Finalizer (destructor)
        ~AdMobHandler()
        {
            Dispose(false);
        }
        #endregion

        #region Privacy & Compliance
        public override void SetGDPR(bool state)
        {
            try
            {
                // GDPR for AdMob is handled via the "npa" parameter in each AdRequest
                DebugLog($"[AdMob]: GDPR state updated to: {state}");

                // Optional: Re-request ads to apply new GDPR setting immediately
                if (isInitialized)
                {
                    AdsManager.CallEventInMainThread(() =>
                    {
                        // Re-request ads with new GDPR setting
                        if (bannerView != null && isBannerLoaded)
                        {
                            bannerView.LoadAd(CreateAdRequest());
                        }

                        DebugLog("[AdMob]: Ads re-requested with new GDPR setting");
                    });
                }
            }
            catch (Exception e)
            {
                DebugLogError($"[AdMob]: Failed to update GDPR: {e.Message}");
            }
        }

        public override void SetCCPA(bool state)
        {
            // CCPA uses same npa parameter as GDPR
            DebugLog($"[AdMob]: CCPA set to {state}");
        }

        public override void SetAgeRestricted(bool state)
        {
            try
            {
                var config = MobileAds.GetRequestConfiguration();
                config.TagForChildDirectedTreatment = state ?
                    TagForChildDirectedTreatment.True :
                    TagForChildDirectedTreatment.False;
                MobileAds.SetRequestConfiguration(config);

                DebugLog($"[AdMob]: Age restriction set to {state}");
            }
            catch (Exception e)
            {
                DebugLogError($"[AdMob]: Failed to set age restriction: {e.Message}");
            }
        }

        public override void SetCOPPA(bool state)
        {
            // COPPA is handled via age restriction
            SetAgeRestricted(state);
        }

        public override void SetUserConsent(bool state)
        {
            // User consent is handled via GDPR/CCPA
            SetGDPR(state);
        }

        public override void SetUserLocation(double latitude, double longitude)
        {
            DebugLog($"[AdMob]: User location setting requested: {latitude}, {longitude}");
        }
        #endregion

        #region Simple Alternative Method (Jika semua gagal)
        private AdRequest CreateSimpleAdRequest()
        {
            // Versi paling sederhana, tanpa GDPR extras
            try
            {
                return new AdRequest();
            }
            catch
            {
                // Jika masih error, coba buat dengan Activator
                try
                {
                    return (AdRequest)Activator.CreateInstance(typeof(AdRequest));
                }
                catch
                {
                    return null;
                }
            }
        }
        #endregion
    }
#endif
}