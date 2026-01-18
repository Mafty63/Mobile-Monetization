using UnityEngine;

namespace MobileCore.Advertisements.Providers
{
    public class AdDummyHandler : BaseAdProviderHandler
    {
        private AdDummyController dummyController;

        private bool isInterstitialLoaded = false;
        private bool isRewardVideoLoaded = false;
        private bool _isInitializationDelayed = false;

        // Fixed values untuk simulasi - tidak perlu configurable
        private const float INTERSTITIAL_CLOSE_DELAY = 2f;
        private const float REWARDED_SUCCESS_RATE = 0.8f;

        public AdDummyHandler(AdProvider providerType) : base(providerType) { }

        public override void Initialize(AdsSettings adsSettings)
        {
            this.adsSettings = adsSettings;

            if (isInitialized)
            {
                DebugLogError($"[AdsManager]: {providerType} is already initialized!");
                return;
            }

            DebugLog($"[AdsManager]: {providerType} is trying to initialize!");

            // Check DontAutoInitialize setting
            if (!adsSettings.DummyContainer.DontAutoInitialize)
            {
                InitializeDummy();
            }
            else
            {
                _isInitializationDelayed = true;
                DebugLog($"[AdsManager]: {providerType} auto-initialization disabled. Call ManualInitialize() to initialize.");
            }
        }

        /// <summary>
        /// Manual initialization of Dummy provider
        /// </summary>
        public void ManualInitialize()
        {
            if (!isInitialized && _isInitializationDelayed)
            {
                DebugLog($"[AdsManager]: Manually initializing {providerType}...");

                InitializeDummy();
            }
            else if (isInitialized)
            {
                DebugLog($"[AdsManager]: {providerType} already initialized.");
            }
            else
            {
                DebugLogWarning($"[AdsManager]: {providerType} not configured for manual initialization.");
            }
        }

        /// <summary>
        /// Internal method to initialize Dummy provider
        /// </summary>
        private void InitializeDummy()
        {
            DebugLog($"[AdsManager]: Module {providerType.ToString()} has initialized!");

            if (adsSettings.IsDummyEnabled())
            {
                GameObject dummyCanvasPrefab = AdsManager.InitModule.DummyCanvasPrefab;
                if (dummyCanvasPrefab != null)
                {
                    GameObject dummyCanvas = GameObject.Instantiate(dummyCanvasPrefab);
                    dummyCanvas.transform.position = Vector3.zero;
                    dummyCanvas.transform.localScale = Vector3.one;
                    dummyCanvas.transform.rotation = Quaternion.identity;

                    dummyController = dummyCanvas.GetComponent<AdDummyController>();
                    if (dummyController != null)
                    {
                        dummyController.Initialize(adsSettings);

                        DebugLog($"[AdsManager]: {providerType} dummy controller initialized successfully.");
                    }
                    else
                    {
                        DebugLogError($"[AdsManager]: {providerType} dummy controller component not found!");
                    }
                }
                else
                {
                    DebugLogError($"[AdsManager]: {providerType} dummy canvas prefab can't be null!");
                }
            }
            else
            {
                DebugLog($"[AdsManager]: {providerType} dummy mode is disabled.");
            }

            isInitialized = true;
            OnProviderInitialized();

            // Auto-request ads setelah inisialisasi berhasil
            if (!adsSettings.DummyContainer.DontAutoInitialize)
            {
                RequestInterstitial();
                RequestRewardedVideo();
            }
        }

        /// <summary>
        /// Manual request for ads after delayed initialization
        /// </summary>
        public void ManualRequestAds()
        {
            if (isInitialized)
            {
                DebugLog($"[AdsManager]: Manually requesting {providerType} ads...");

                RequestInterstitial();
                RequestRewardedVideo();
            }
            else
            {
                DebugLogWarning($"[AdsManager]: {providerType} not initialized yet. Call ManualInitialize() first.");
            }
        }

        public override void ShowBanner()
        {
            if (!isInitialized)
            {
                DebugLogWarning($"[AdsManager]: {providerType} not initialized. Cannot show banner.");
                return;
            }

            if (dummyController != null)
            {
                dummyController.ShowBanner();
                AdsManager.OnProviderAdDisplayed(providerType, AdType.Banner);

                DebugLog($"[AdsManager]: {providerType} banner shown");
            }
            else
            {
                DebugLogWarning($"[AdsManager]: {providerType} dummy controller not available!");
            }
        }

        public override void HideBanner()
        {
            if (dummyController != null)
            {
                dummyController.HideBanner();
                AdsManager.OnProviderAdClosed(providerType, AdType.Banner);

                DebugLog($"[AdsManager]: {providerType} banner hidden");
            }
        }

        public override void DestroyBanner()
        {
            if (dummyController != null)
            {
                dummyController.HideBanner();
                AdsManager.OnProviderAdClosed(providerType, AdType.Banner);

                DebugLog($"[AdsManager]: {providerType} banner destroyed");
            }
        }

        public override void RequestInterstitial()
        {
            if (!isInitialized)
            {
                DebugLogWarning($"[AdsManager]: {providerType} not initialized. Cannot request interstitial.");
                return;
            }

            isInterstitialLoaded = true;
            AdsManager.OnProviderAdLoaded(providerType, AdType.Interstitial);

            DebugLog($"[AdsManager]: {providerType} interstitial requested and loaded");
        }

        public override bool IsInterstitialLoaded()
        {
            return isInterstitialLoaded;
        }

        public override void ShowInterstitial(InterstitialCallback callback)
        {
            if (!isInitialized)
            {
                callback?.Invoke(false);
                return;
            }

            if (isInterstitialLoaded && dummyController != null)
            {
                dummyController.ShowInterstitial();
                AdsManager.OnProviderAdDisplayed(providerType, AdType.Interstitial);

                // Simulate interstitial close after fixed delay
                MonoBehaviourExecution.DelayedCall(INTERSTITIAL_CLOSE_DELAY, () =>
                {
                    AdsManager.OnProviderAdClosed(providerType, AdType.Interstitial);
                    AdsManager.ExecuteInterstitialCallback(true);
                    callback?.Invoke(true);

                    // Auto-reload interstitial
                    isInterstitialLoaded = false;
                    RequestInterstitial();
                });

                DebugLog($"[AdsManager]: {providerType} interstitial shown");
            }
            else
            {
                callback?.Invoke(false);

                if (!isInterstitialLoaded)
                    RequestInterstitial();

                DebugLogWarning($"[AdsManager]: {providerType} interstitial not ready!");
            }
        }

        public override void RequestRewardedVideo()
        {
            if (!isInitialized)
            {
                DebugLogWarning($"[AdsManager]: {providerType} not initialized. Cannot request rewarded video.");
                return;
            }

            isRewardVideoLoaded = true;
            AdsManager.OnProviderAdLoaded(providerType, AdType.RewardedVideo);

            DebugLog($"[AdsManager]: {providerType} rewarded video requested and loaded");
        }

        public override bool IsRewardedVideoLoaded()
        {
            return isRewardVideoLoaded;
        }

        public override void ShowRewardedVideo(RewardedVideoCallback callback)
        {
            if (!isInitialized)
            {
                callback?.Invoke(false);
                return;
            }

            if (isRewardVideoLoaded && dummyController != null)
            {
                dummyController.ShowRewardedVideo();
                AdsManager.OnProviderAdDisplayed(providerType, AdType.RewardedVideo);

                // Simulate rewarded video with fixed success rate
                bool rewardGranted = Random.value <= REWARDED_SUCCESS_RATE;

                MonoBehaviourExecution.DelayedCall(INTERSTITIAL_CLOSE_DELAY, () =>
                {
                    AdsManager.OnProviderAdClosed(providerType, AdType.RewardedVideo);
                    AdsManager.ExecuteRewardVideoCallback(rewardGranted);
                    callback?.Invoke(rewardGranted);

                    // Auto-reload rewarded video
                    isRewardVideoLoaded = false;
                    RequestRewardedVideo();

                    DebugLog($"[AdsManager]: {providerType} rewarded video completed - Reward: {rewardGranted}");
                });

                DebugLog($"[AdsManager]: {providerType} rewarded video shown");
            }
            else
            {
                callback?.Invoke(false);

                if (!isRewardVideoLoaded)
                    RequestRewardedVideo();

                DebugLogWarning($"[AdsManager]: {providerType} rewarded video not ready!");
            }
        }

        #region Privacy Methods (Dummy implementation)
        public override void SetGDPR(bool state)
        {
            DebugLog($"[AdsManager]: {providerType} GDPR set to: {state}");
        }

        public override void SetAgeRestricted(bool state)
        {
            DebugLog($"[AdsManager]: {providerType} age restriction set to: {state}");
        }

        public override void SetCCPA(bool state)
        {
            DebugLog($"[AdsManager]: {providerType} CCPA set to: {state}");
        }

        public override void SetUserConsent(bool state)
        {
            DebugLog($"[AdsManager]: {providerType} user consent set to: {state}");
        }

        public override void SetCOPPA(bool state)
        {
            DebugLog($"[AdsManager]: {providerType} COPPA set to: {state}");
        }

        public override void SetUserLocation(double latitude, double longitude)
        {
            DebugLog($"[AdsManager]: {providerType} user location set to: {latitude}, {longitude}");
        }
        #endregion
    }
}