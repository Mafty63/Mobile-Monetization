using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

#if META_AUDIENCE_NETWORK_PROVIDER
using AudienceNetwork;
#endif

namespace MobileCore.Advertisements.Providers
{
#if META_AUDIENCE_NETWORK_PROVIDER
    public class MetaAudienceNetworkHandler : BaseAdProviderHandler
    {
        // Ad objects
        private AdView bannerAd;
        private InterstitialAd interstitialAd;
        private RewardedVideoAd rewardedVideoAd;

        // Callback references
        private InterstitialCallback currentInterstitialCallback;
        private RewardedVideoCallback currentRewardedCallback;

        // Flags
        private bool isBannerLoadedAndReady = false;

        public MetaAudienceNetworkHandler(AdProvider providerType) : base(providerType) { }

        #region Initialization
        public override void Initialize(AdsSettings adsSettings)
        {
            if (isInitialized) return;

            this.adsSettings = adsSettings;
            DebugLog("[MetaAN]: Initializing...");

            try
            {
                // Set AdSettings for testing if needed
                if (adsSettings.MetaAudienceNetworkContainer.TestDevicesIDs.Count > 0)
                {
                    AudienceNetwork.AdSettings.AddTestDevices(adsSettings.MetaAudienceNetworkContainer.TestDevicesIDs);
                }

                AudienceNetworkAds.Initialize();
                
                isInitialized = true;
                OnProviderInitialized();
                DebugLog("[MetaAN]: Initialization completed");
            }
            catch (Exception e)
            {
                DebugLogError($"[MetaAN]: Initialization failed: {e.Message}");
            }
        }
        #endregion

        #region Banner Implementation
        public override void ShowBanner()
        {
            if (!isInitialized) return;

            UpdateBannerState(true, isBannerLoadedAndReady);

            if (bannerAd == null)
            {
                RequestBanner();
            }
            else
            {
                // Meta banners are Views, they show if valid and added to hierarchy.
                // Usually instantiation explicitly shows them in Unity integration.
                // If it's already loaded, we might need to ensure it's visible.
                // However, Meta SDK for Unity handles standard views differently than native.
                // If using AdView, it typically stays visible.
                DebugLog("[MetaAN]: Showing banner");
            }
        }

        public override void HideBanner()
        {
            UpdateBannerState(false, isBannerLoadedAndReady);
            
            if (bannerAd != null)
            {
                bannerAd.Dispose(); // Meta banners often need dispose to hide/destroy
                bannerAd = null;
                isBannerLoadedAndReady = false;
                DebugLog("[MetaAN]: Banner hidden/disposed");
            }
        }

        public override void DestroyBanner()
        {
             HideBanner();
        }

        private void RequestBanner()
        {
            if (bannerAd != null)
            {
                bannerAd.Dispose();
                bannerAd = null;
            }

            string placementId = GetBannerID();
            AdSize adSize = GetAdSize();

            DebugLog($"[MetaAN]: Requesting banner {placementId}");

            bannerAd = new AdView(placementId, adSize);
            bannerAd.Register(MonoBehaviourExecution.Instance.gameObject);

            // Set delegates
            bannerAd.AdViewDidLoad = () =>
            {
                isBannerLoadedAndReady = true;
                UpdateBannerState(isBannerShowing, true);
                OnAdLoaded(AdType.Banner);
                DebugLog("[MetaAN]: Banner loaded");
                
                // If we shouldn't be showing, hide it (dispose it because Meta doesn't have Hide())
                if (!isBannerShowing)
                {
                    DestroyBanner();
                }
                else
                {
                    bannerAd.Show(); // Explicit show if needed by specific SDK version
                    OnAdDisplayed(AdType.Banner);
                }
            };

            bannerAd.AdViewDidFailWithError = (error) =>
            {
                isBannerLoadedAndReady = false;
                UpdateBannerState(false, false);
                DebugLogError($"[MetaAN]: Banner failed to load: {error}");
            };

            bannerAd.LoadAd();
        }
        #endregion

        #region Interstitial Implementation
        public override void RequestInterstitial()
        {
            if (!isInitialized) return;

            string placementId = GetInterstitialID();
            
            if (interstitialAd != null)
            {
                interstitialAd.Dispose();
                interstitialAd = null;
            }

            DebugLog("[MetaAN]: Requesting interstitial...");

            interstitialAd = new InterstitialAd(placementId);
            interstitialAd.Register(MonoBehaviourExecution.Instance.gameObject);

            interstitialAd.InterstitialAdDidLoad = () =>
            {
                OnAdLoaded(AdType.Interstitial);
                DebugLog("[MetaAN]: Interstitial loaded");
            };

            interstitialAd.InterstitialAdDidFailWithError = (error) =>
            {
                DebugLogError($"[MetaAN]: Interstitial failed: {error}");
                // Retry logic could go here
            };

            interstitialAd.InterstitialAdDidClose = () =>
            {
                OnAdClosed(AdType.Interstitial);
                currentInterstitialCallback?.Invoke(true);
                currentInterstitialCallback = null;
                DebugLog("[MetaAN]: Interstitial closed");
                
                // Auto reload
                RequestInterstitial();
            };

            interstitialAd.LoadAd();
        }

        public override void ShowInterstitial(InterstitialCallback callback)
        {
            if (IsInterstitialLoaded())
            {
                currentInterstitialCallback = callback;
                interstitialAd.Show();
                OnAdDisplayed(AdType.Interstitial);
            }
            else
            {
                callback?.Invoke(false);
                RequestInterstitial();
            }
        }

        public override bool IsInterstitialLoaded()
        {
            return interstitialAd != null && interstitialAd.IsValid();
        }
        #endregion

        #region Rewarded Video Implementation
        public override void RequestRewardedVideo()
        {
            if (!isInitialized) return;

            string placementId = GetRewardedVideoID();

            if (rewardedVideoAd != null)
            {
                rewardedVideoAd.Dispose();
                rewardedVideoAd = null;
            }

            DebugLog("[MetaAN]: Requesting rewarded video...");

            rewardedVideoAd = new RewardedVideoAd(placementId);
            rewardedVideoAd.Register(MonoBehaviourExecution.Instance.gameObject);

            rewardedVideoAd.RewardedVideoAdDidLoad = () =>
            {
                OnAdLoaded(AdType.RewardedVideo);
                DebugLog("[MetaAN]: Rewarded video loaded");
            };

            rewardedVideoAd.RewardedVideoAdDidFailWithError = (error) =>
            {
                DebugLogError($"[MetaAN]: Rewarded video failed: {error}");
            };

            rewardedVideoAd.RewardedVideoAdDidClose = () =>
            {
                OnAdClosed(AdType.RewardedVideo);
                // DidSucceed is separate, so we handle callback logic carefully
                // If DidSucceed wasn't called, we might invoke with false here if not already invoked?
                // Meta usually calls DidSucceed then DidClose.
                if (currentRewardedCallback != null)
                {
                    currentRewardedCallback.Invoke(false); // Closed without reward if callback still exists
                    currentRewardedCallback = null;
                }
                
                DebugLog("[MetaAN]: Rewarded video closed");
                RequestRewardedVideo();
            };

            rewardedVideoAd.RewardedVideoAdComplete = () =>
            {
                if (currentRewardedCallback != null)
                {
                    currentRewardedCallback.Invoke(true);
                    currentRewardedCallback = null;
                }
                DebugLog("[MetaAN]: Rewarded video completed");
            };

            rewardedVideoAd.LoadAd();
        }

        public override void ShowRewardedVideo(RewardedVideoCallback callback)
        {
            if (IsRewardedVideoLoaded())
            {
                currentRewardedCallback = callback;
                rewardedVideoAd.Show();
                OnAdDisplayed(AdType.RewardedVideo);
            }
            else
            {
                callback?.Invoke(false);
                RequestRewardedVideo();
            }
        }

        public override bool IsRewardedVideoLoaded()
        {
            return rewardedVideoAd != null && rewardedVideoAd.IsValid();
        }
        #endregion

        #region Helper Methods
        private string GetBannerID()
        {
#if UNITY_ANDROID
            return adsSettings.MetaAudienceNetworkContainer.AndroidBannerID;
#elif UNITY_IOS
            return adsSettings.MetaAudienceNetworkContainer.IOSBannerID;
#else
            return "unused";
#endif
        }

        private string GetInterstitialID()
        {
#if UNITY_ANDROID
            return adsSettings.MetaAudienceNetworkContainer.AndroidInterstitialID;
#elif UNITY_IOS
            return adsSettings.MetaAudienceNetworkContainer.IOSInterstitialID;
#else
            return "unused";
#endif
        }

        private string GetRewardedVideoID()
        {
#if UNITY_ANDROID
            return adsSettings.MetaAudienceNetworkContainer.AndroidRewardedVideoID;
#elif UNITY_IOS
            return adsSettings.MetaAudienceNetworkContainer.IOSRewardedVideoID;
#else
            return "unused";
#endif
        }

        private AdSize GetAdSize()
        {
            return AdSize.BANNER_HEIGHT_50; // Default logic, can be expanded based on container settings
        }
        #endregion

        #region Privacy
        public override void SetGDPR(bool state) 
        { 
            // Meta handles GDPR via their settings or platform CMP
        }
        
        public override void SetCCPA(bool state) { }
        public override void SetAgeRestricted(bool state) 
        {
            AudienceNetwork.AdSettings.SetMixedAudience(state);
        }
        public override void SetCOPPA(bool state) { SetAgeRestricted(state); }
        public override void SetUserConsent(bool state) { }
        public override void SetUserLocation(double latitude, double longitude) { }
        #endregion
    }
#endif
}
