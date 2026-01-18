#pragma warning disable 0414

using UnityEngine;
using System;
using System.Collections;
#if LEVELPLAY_PROVIDER
using Unity.Services.LevelPlay;
#endif
// TIDAK ADA using com.unity3d.mediation;

namespace MobileCore.Advertisements.Providers
{
#if LEVELPLAY_PROVIDER
    public class LevelPlayHandler : BaseAdProviderHandler
    {
        // Ad objects (Merujuk ke Unity.Services.LevelPlay.LevelPlayBannerAd, dll.)
        private LevelPlayBannerAd bannerAd;
        private LevelPlayInterstitialAd interstitialAd;
        private LevelPlayRewardedAd rewardedAd;

        // State tracking
        private bool isInterstitialLoaded = false;
        private bool isRewardedLoaded = false;

        // Retry configuration
        private const int RETRY_ATTEMPT_DEFAULT_VALUE = 1;
        private const int MAX_RETRY_ATTEMPTS = 3;

        private int interstitialRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;
        private int rewardedRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;

        // Callback references
        private InterstitialCallback currentInterstitialCallback;
        private RewardedVideoCallback currentRewardedCallback;

        // Track if disposed
        private bool levelPlayDisposed = false;

        // Event holder
        private GameObject eventHolder;

        public LevelPlayHandler(AdProvider providerType) : base(providerType) { }

        // --- Initialization ---

        #region Initialization
        public override void Initialize(AdsSettings adsSettings)
        {
            if (isInitialized)
            {
                DebugLogWarning("[LevelPlay]: Already initialized!");
                return;
            }

            this.adsSettings = adsSettings;
            DebugLog("[LevelPlay]: Initializing...");

            try
            {
                CreateEventHolder();

                // Menggunakan FQN untuk event Init agar kompatibel dengan potongan SDK Anda
                LevelPlay.OnInitSuccess += OnInitSuccess;
                LevelPlay.OnInitFailed += OnInitFailed;

                if (adsSettings.TestMode)
                {
                    LevelPlay.SetMetaData("is_test_suite", "enable");
                    LevelPlay.ValidateIntegration();
                }

                // Memanggil Init tanpa parameter adFormats untuk menghindari peringatan obsolete.
                LevelPlay.Init(GetAppKey());

                DebugLog("[LevelPlay]: Initialization started");
            }
            catch (Exception e)
            {
                DebugLogError($"[LevelPlay]: Initialization failed: {e.Message}");
            }
        }

        private void CreateEventHolder()
        {
            if (eventHolder == null)
            {
                eventHolder = new GameObject("LevelPlayEventHolder");
                UnityEngine.Object.DontDestroyOnLoad(eventHolder);

                var listener = eventHolder.AddComponent<LevelPlayListener>();
                listener.Init(this);
            }
        }

        // Signature event menggunakan FQN tipe data lama
        private void OnInitSuccess(com.unity3d.mediation.LevelPlayConfiguration configuration)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                isInitialized = true;
                OnProviderInitialized();
                DebugLog("[LevelPlay]: Initialized successfully!");

                SetupAdObjects();
                RequestRewardedVideo();
                RequestInterstitial();
            });
        }

        // Signature event menggunakan FQN tipe data lama
        private void OnInitFailed(com.unity3d.mediation.LevelPlayInitError error)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                DebugLogError($"[LevelPlay]: Initialization failed! Error: #{error.ErrorCode} {error.ErrorMessage}");
            });
        }

        private void SetupAdObjects()
        {
            if (rewardedAd == null)
            {
                rewardedAd = new LevelPlayRewardedAd(GetRewardedVideoID());
                SetupRewardedVideoEvents();
            }
        }

        private void SetupRewardedVideoEvents()
        {
            try
            {
                rewardedAd.OnAdLoaded += HandleRewardedLoaded;
                rewardedAd.OnAdLoadFailed += HandleRewardedLoadFailed;
                rewardedAd.OnAdDisplayed += HandleRewardedOpened;
                rewardedAd.OnAdDisplayFailed += HandleRewardedShowFailed;
                rewardedAd.OnAdClosed += HandleRewardedClosed;
                rewardedAd.OnAdRewarded += HandleRewardedEarned;

                DebugLog("[LevelPlay]: Rewarded video events setup complete");
            }
            catch (Exception e)
            {
                DebugLogError($"[LevelPlay]: Failed to setup rewarded video events: {e.Message}");
            }
        }

        public void ManualRequestAds()
        {
            if (!isInitialized)
            {
                DebugLogWarning("[LevelPlay]: Not initialized yet");
                return;
            }
        }
        #endregion

        // --- Banner Implementation ---

        #region Banner Implementation
        private void RequestBanner()
        {
            if (!isInitialized) return;

            if (bannerAd != null)
            {
                DestroyBanner();
            }

            DebugLog("[LevelPlay]: Requesting banner...");

            try
            {
                var bannerSize = GetBannerSize();
                var bannerPosition = GetBannerPosition();

                // LevelPlayBannerAd constructor mengharapkan tipe data lama, 
                // yang dipenuhi oleh GetBannerSize/Position yang sudah dikoreksi.
                bannerAd = new LevelPlayBannerAd(GetBannerID(), bannerSize, bannerPosition);

                bannerAd.OnAdLoaded += HandleBannerLoaded;
                bannerAd.OnAdLoadFailed += HandleBannerLoadFailed;
                bannerAd.OnAdDisplayed += HandleBannerDisplayed;

                bannerAd.LoadAd();

                DebugLog("[LevelPlay]: Banner request sent");
            }
            catch (Exception e)
            {
                DebugLogError($"[LevelPlay]: Banner request failed: {e.Message}");
            }
        }

        public override void ShowBanner()
        {
            if (!isInitialized) return;

            UpdateBannerState(true, isBannerLoaded);

            if (bannerAd == null)
            {
                RequestBanner();
                return;
            }

            if (isBannerLoaded)
            {
                bannerAd.ShowAd();
                OnAdDisplayed(AdType.Banner);
                DebugLog("[LevelPlay]: Banner shown");
            }
        }

        public override void HideBanner()
        {
            UpdateBannerState(false, isBannerLoaded);

            if (bannerAd != null && isBannerLoaded)
            {
                bannerAd.HideAd();
                OnAdClosed(AdType.Banner);
                DebugLog("[LevelPlay]: Banner hidden");
            }
        }

        public override void DestroyBanner()
        {
            UpdateBannerState(false, false);

            if (bannerAd != null)
            {
                UnsubscribeBannerEvents();
                bannerAd.DestroyAd();
                bannerAd = null;
                DebugLog("[LevelPlay]: Banner destroyed");
            }
        }
        #endregion

        // --- Interstitial Implementation ---

        #region Interstitial Implementation
        public override void RequestInterstitial()
        {
            if (!isInitialized) return;

            DebugLog("[LevelPlay]: Requesting interstitial...");

            try
            {
                if (interstitialAd == null)
                {
                    interstitialAd = new LevelPlayInterstitialAd(GetInterstitialID());

                    interstitialAd.OnAdLoaded += HandleInterstitialLoaded;
                    interstitialAd.OnAdLoadFailed += HandleInterstitialLoadFailed;
                    interstitialAd.OnAdDisplayed += HandleInterstitialDisplayed;
                    interstitialAd.OnAdDisplayFailed += HandleInterstitialDisplayFailed;
                    interstitialAd.OnAdClosed += HandleInterstitialClosed;
                }

                interstitialAd.LoadAd();
            }
            catch (Exception e)
            {
                DebugLogError($"[LevelPlay]: Interstitial request failed: {e.Message}");
                HandleAdLoadFailure("Interstitial", e.Message, ref interstitialRetryAttempt, RequestInterstitial);
            }
        }

        public override void ShowInterstitial(InterstitialCallback callback)
        {
            if (!isInitialized || interstitialAd == null || !interstitialAd.IsAdReady())
            {
                DebugLogWarning("[LevelPlay]: Interstitial Ad is not ready.");
                callback?.Invoke(false);
                RequestInterstitial();
                return;
            }

            currentInterstitialCallback = callback;
            interstitialAd.ShowAd();
        }

        public override bool IsInterstitialLoaded()
        {
            return isInitialized && interstitialAd != null && interstitialAd.IsAdReady();
        }
        #endregion
        // --- Rewarded Video Implementation ---

        #region Rewarded Video Implementation
        public override void RequestRewardedVideo()
        {
            if (!isInitialized || rewardedAd == null) return;

            DebugLog("[LevelPlay]: Requesting rewarded video...");

            try
            {
                isRewardedLoaded = false;
                rewardedAd.LoadAd();
            }
            catch (Exception e)
            {
                DebugLogError($"[LevelPlay]: Rewarded Video request failed: {e.Message}");
                HandleAdLoadFailure("Rewarded", e.Message, ref rewardedRetryAttempt, RequestRewardedVideo);
            }
        }

        public override void ShowRewardedVideo(RewardedVideoCallback callback)
        {
            if (!isInitialized || rewardedAd == null || !rewardedAd.IsAdReady())
            {
                DebugLogWarning("[LevelPlay]: Rewarded Ad is not ready.");
                callback?.Invoke(false);
                return;
            }

            currentRewardedCallback = callback;
            rewardedAd.ShowAd();
        }

        public override bool IsRewardedVideoLoaded()
        {
            return isInitialized && rewardedAd != null && rewardedAd.IsAdReady();
        }
        #endregion
        // --- Event Handlers ---

        #region Event Handlers
        // Event handlers (menggunakan tipe data LevelPlayAdInfo dari namespace Unity.Services.LevelPlay)
        private void HandleBannerLoaded(LevelPlayAdInfo info)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                isBannerLoaded = true;
                OnAdLoaded(AdType.Banner);

                if (isBannerShowing)
                {
                    bannerAd?.ShowAd();
                    OnAdDisplayed(AdType.Banner);
                }

                DebugLog("[LevelPlay]: Banner loaded");
            });
        }

        private void HandleBannerLoadFailed(LevelPlayAdError error)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                isBannerLoaded = false;
                DebugLogError($"[LevelPlay]: Banner failed to load: {error.ErrorMessage}");
            });
        }

        private void HandleBannerDisplayed(LevelPlayAdInfo info)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                DebugLog("[LevelPlay]: Banner displayed");
            });
        }

        private void HandleInterstitialLoaded(LevelPlayAdInfo info)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                isInterstitialLoaded = true;
                interstitialRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;
                OnAdLoaded(AdType.Interstitial);
                DebugLog("[LevelPlay]: Interstitial loaded");
            });
        }

        private void HandleInterstitialLoadFailed(LevelPlayAdError error)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                isInterstitialLoaded = false;
                HandleAdLoadFailure("Interstitial", error.ErrorMessage,
                    ref interstitialRetryAttempt, RequestInterstitial);
            });
        }

        private void HandleInterstitialDisplayed(LevelPlayAdInfo info)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                OnAdDisplayed(AdType.Interstitial);
                DebugLog("[LevelPlay]: Interstitial displayed");
            });
        }

        private void HandleInterstitialDisplayFailed(LevelPlayAdDisplayInfoError error)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                OnAdClosed(AdType.Interstitial);
                currentInterstitialCallback?.Invoke(false);
                currentInterstitialCallback = null;

                RequestInterstitial();
                DebugLogError($"[LevelPlay]: Interstitial display failed: {error.LevelPlayError.ErrorMessage}");
            });
        }

        private void HandleInterstitialClosed(LevelPlayAdInfo info)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                OnAdClosed(AdType.Interstitial);
                currentInterstitialCallback?.Invoke(true);
                currentInterstitialCallback = null;

                RequestInterstitial();
                DebugLog("[LevelPlay]: Interstitial closed");
            });
        }

        private void HandleRewardedLoaded(LevelPlayAdInfo info)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                isRewardedLoaded = true;
                rewardedRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;
                OnAdLoaded(AdType.RewardedVideo);
                DebugLog("[LevelPlay]: Rewarded video loaded");
            });
        }

        private void HandleRewardedLoadFailed(LevelPlayAdError error)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                isRewardedLoaded = false;
                HandleAdLoadFailure("Rewarded", error.ErrorMessage,
                    ref rewardedRetryAttempt, RequestRewardedVideo);
            });
        }

        private void HandleRewardedOpened(LevelPlayAdInfo info)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                OnAdDisplayed(AdType.RewardedVideo);
                DebugLog("[LevelPlay]: Rewarded video opened");
            });
        }

        private void HandleRewardedClosed(LevelPlayAdInfo info)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                OnAdClosed(AdType.RewardedVideo);

                if (currentRewardedCallback != null)
                {
                    currentRewardedCallback.Invoke(false);
                    currentRewardedCallback = null;
                }

                RequestRewardedVideo();

                DebugLog("[LevelPlay]: Rewarded video closed");
            });
        }

        private void HandleRewardedShowFailed(LevelPlayAdDisplayInfoError error)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                OnAdClosed(AdType.RewardedVideo);
                currentRewardedCallback?.Invoke(false);
                currentRewardedCallback = null;

                RequestRewardedVideo();

                DebugLogError($"[LevelPlay]: Rewarded video display failed: {error.LevelPlayError.ErrorMessage}");
            });
        }

        private void HandleRewardedEarned(LevelPlayAdInfo info, LevelPlayReward reward)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                currentRewardedCallback?.Invoke(true);
                currentRewardedCallback = null;
                DebugLog($"[LevelPlay]: Reward earned: {reward.Amount} {reward.Name}");
            });
        }
        #endregion
        // --- Helper Methods & Internal Logic ---

        #region Helper Methods
        // KOREKSI: Menggunakan FQN untuk tipe data lama com.unity3d.mediation.LevelPlayAdSize
        private com.unity3d.mediation.LevelPlayAdSize GetBannerSize()
        {
            return adsSettings.LevelPlayContainer.BannerType switch
            {
                LevelPlayContainer.BannerPlacementType.Large => com.unity3d.mediation.LevelPlayAdSize.LARGE,
                LevelPlayContainer.BannerPlacementType.Rectangle => com.unity3d.mediation.LevelPlayAdSize.MEDIUM_RECTANGLE,
                LevelPlayContainer.BannerPlacementType.Leaderboard => com.unity3d.mediation.LevelPlayAdSize.LEADERBOARD,
                _ => com.unity3d.mediation.LevelPlayAdSize.BANNER,
            };
        }

        // KOREKSI: Menggunakan FQN untuk tipe data lama com.unity3d.mediation.LevelPlayBannerPosition
        private com.unity3d.mediation.LevelPlayBannerPosition GetBannerPosition()
        {
            return adsSettings.LevelPlayContainer.BannerPosition == BannerPosition.Top ?
                com.unity3d.mediation.LevelPlayBannerPosition.TopCenter : com.unity3d.mediation.LevelPlayBannerPosition.BottomCenter;
        }

        private void HandleAdLoadFailure(string adType, string error, ref int retryAttempt, Action retryAction)
        {
            DebugLogError($"[LevelPlay]: {adType} failed to load: {error}");

            retryAttempt++;
            float retryDelay = Mathf.Pow(2, Mathf.Min(retryAttempt, MAX_RETRY_ATTEMPTS));

            AdsManager.CallEventInMainThread(() =>
            {
                DebugLog($"[LevelPlay]: Retrying {adType} in {retryDelay} seconds");
                MonoBehaviourExecution.Instance.StartCoroutine(DelayedCall(retryDelay, retryAction));
            });
        }

        private System.Collections.IEnumerator DelayedCall(float delay, Action action)
        {
            yield return new WaitForSecondsRealtime(delay);
            action?.Invoke();
        }

        private void UnsubscribeBannerEvents()
        {
            if (bannerAd != null)
            {
                try
                {
                    // Events menggunakan tipe data dari Unity.Services.LevelPlay, 
                    // namun karena LevelPlayBannerAd mewarisi/menggunakan tipe data lama di event-nya, 
                    // pastikan Unsubscribe ini tetap berjalan.
                    bannerAd.OnAdLoaded -= HandleBannerLoaded;
                    bannerAd.OnAdLoadFailed -= HandleBannerLoadFailed;
                    bannerAd.OnAdDisplayed -= HandleBannerDisplayed;
                }
                catch (Exception e)
                {
                    DebugLogWarning($"[LevelPlay]: Error unsubscribing banner events: {e.Message}");
                }
            }
        }

        private void UnsubscribeInterstitialEvents()
        {
            if (interstitialAd != null)
            {
                try
                {
                    interstitialAd.OnAdLoaded -= HandleInterstitialLoaded;
                    interstitialAd.OnAdLoadFailed -= HandleInterstitialLoadFailed;
                    interstitialAd.OnAdDisplayed -= HandleInterstitialDisplayed;
                    interstitialAd.OnAdDisplayFailed -= HandleInterstitialDisplayFailed;
                    interstitialAd.OnAdClosed -= HandleInterstitialClosed;
                }
                catch (Exception e)
                {
                    DebugLogWarning($"[LevelPlay]: Error unsubscribing interstitial events: {e.Message}");
                }
            }
        }

        private void UnsubscribeRewardedEvents()
        {
            if (rewardedAd != null)
            {
                try
                {
                    rewardedAd.OnAdLoaded -= HandleRewardedLoaded;
                    rewardedAd.OnAdLoadFailed -= HandleRewardedLoadFailed;
                    rewardedAd.OnAdDisplayed -= HandleRewardedOpened;
                    rewardedAd.OnAdDisplayFailed -= HandleRewardedShowFailed;
                    rewardedAd.OnAdClosed -= HandleRewardedClosed;
                    rewardedAd.OnAdRewarded -= HandleRewardedEarned;
                }
                catch (Exception e)
                {
                    DebugLogWarning($"[LevelPlay]: Error unsubscribing rewarded events: {e.Message}");
                }
            }
        }
        #endregion

        // --- Platform-Specific Methods ---

        #region Platform-Specific Methods
        private string GetAppKey()
        {
#if UNITY_EDITOR
            return "unused";
#elif UNITY_ANDROID
                return string.IsNullOrEmpty(adsSettings.LevelPlayContainer.AndroidAppKey) ? 
                    "test_app_key" : adsSettings.LevelPlayContainer.AndroidAppKey;
#elif UNITY_IOS
                return string.IsNullOrEmpty(adsSettings.LevelPlayContainer.IOSAppKey) ? 
                    "test_app_key" : adsSettings.LevelPlayContainer.IOSAppKey;
#else
                return "unexpected_platform";
#endif
        }

        private string GetBannerID()
        {
#if UNITY_EDITOR
            return "unused";
#elif UNITY_ANDROID
                return string.IsNullOrEmpty(adsSettings.LevelPlayContainer.AndroidBannerID) ? 
                    "test_banner" : adsSettings.LevelPlayContainer.AndroidBannerID;
#elif UNITY_IOS
                return string.IsNullOrEmpty(adsSettings.LevelPlayContainer.IOSBannerID) ? 
                    "test_banner" : adsSettings.LevelPlayContainer.IOSBannerID;
#else
                return "unexpected_platform";
#endif
        }

        private string GetInterstitialID()
        {
#if UNITY_EDITOR
            return "unused";
#elif UNITY_ANDROID
                return string.IsNullOrEmpty(adsSettings.LevelPlayContainer.AndroidInterstitialID) ? 
                    "test_interstitial" : adsSettings.LevelPlayContainer.AndroidInterstitialID;
#elif UNITY_IOS
                return string.IsNullOrEmpty(adsSettings.LevelPlayContainer.IOSInterstitialID) ? 
                    "test_interstitial" : adsSettings.LevelPlayContainer.IOSInterstitialID;
#else
                return "unexpected_platform";
#endif
        }

        private string GetRewardedVideoID()
        {
#if UNITY_EDITOR
            return "unused";
#elif UNITY_ANDROID
                return string.IsNullOrEmpty(adsSettings.LevelPlayContainer.AndroidRewardedVideoID) ? 
                    "test_rewarded" : adsSettings.LevelPlayContainer.AndroidRewardedVideoID;
#elif UNITY_IOS
                return string.IsNullOrEmpty(adsSettings.LevelPlayContainer.IOSRewardedVideoID) ? 
                    "test_rewarded" : adsSettings.LevelPlayContainer.IOSRewardedVideoID;
#else
                return "unexpected_platform";
#endif
        }
        #endregion

        // --- Cleanup & Dispose ---

        #region Cleanup & Dispose
        protected override void Dispose(bool disposing)
        {
            if (!levelPlayDisposed)
            {
                if (disposing)
                {
                    DestroyBanner();

                    if (interstitialAd != null)
                    {
                        UnsubscribeInterstitialEvents();
                        interstitialAd = null;
                    }

                    if (rewardedAd != null)
                    {
                        UnsubscribeRewardedEvents();
                        rewardedAd = null;
                    }

                    LevelPlay.OnInitSuccess -= OnInitSuccess;
                    LevelPlay.OnInitFailed -= OnInitFailed;

                    if (eventHolder != null)
                    {
                        UnityEngine.Object.Destroy(eventHolder);
                        eventHolder = null;
                    }

                    currentInterstitialCallback = null;
                    currentRewardedCallback = null;

                    DebugLog("[LevelPlay]: Resources cleaned up");
                }

                levelPlayDisposed = true;
                base.Dispose(disposing);
            }
        }

        ~LevelPlayHandler()
        {
            Dispose(false);
        }
        #endregion

        // --- Privacy & Compliance ---

        #region Privacy & Compliance
        public override void SetGDPR(bool state)
        {
            if (!isInitialized) return;
            LevelPlay.SetConsent(state);
            DebugLog($"[LevelPlay]: SetConsent (GDPR) set to: {state}");
        }

        public override void SetCCPA(bool state)
        {
            if (!isInitialized) return;
            LevelPlay.SetMetaData("do_not_sell", state ? "false" : "true");
            DebugLog($"[LevelPlay]: CCPA (Do Not Sell) set to: {state}");
        }

        public override void SetAgeRestricted(bool state)
        {
            if (!isInitialized) return;
            LevelPlay.SetMetaData("is_child_directed", state ? "true" : "false");
            DebugLog($"[LevelPlay]: Age restriction (COPPA) set to: {state}");
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
            DebugLog($"[LevelPlay]: User location setting requested: {latitude}, {longitude}");
        }
        #endregion

        // Helper class for application pause handling
        private class LevelPlayListener : MonoBehaviour
        {
            private LevelPlayHandler handler;

            public void Init(LevelPlayHandler handler)
            {
                this.handler = handler;
            }

            private void OnApplicationPause(bool isPaused)
            {
                LevelPlay.SetPauseGame(isPaused);
            }

            private void OnDestroy()
            {
                handler = null;
            }
        }
    }
#endif
}