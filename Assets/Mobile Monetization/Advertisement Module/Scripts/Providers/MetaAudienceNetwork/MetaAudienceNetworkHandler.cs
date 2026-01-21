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

        // Flags (following Meta examples)
        private bool isBannerLoadedAndReady = false;
        private bool isInterstitialLoaded = false;
        private bool isRewardedVideoLoaded = false;
#pragma warning disable 0414
        private bool didCloseInterstitial = false;
        private bool didCloseRewardedVideo = false;
#pragma warning restore 0414

        public MetaAudienceNetworkHandler(AdProvider providerType) : base(providerType) { }

        #region Initialization
        public override void Initialize(AdsSettings adsSettings)
        {
            if (isInitialized) return;

            this.adsSettings = adsSettings;
            DebugLog("[MetaAN]: Initializing...");

            try
            {
                // Meta SDK auto-initializes when creating ad objects
                // No explicit Initialize() call needed (AudienceNetworkAds.Initialize is internal)
                
                // Set AdSettings for testing if needed
                if (adsSettings.MetaAudienceNetworkContainer.TestDevicesIDs.Count > 0)
                {
                    foreach (var deviceId in adsSettings.MetaAudienceNetworkContainer.TestDevicesIDs)
                    {
                        AudienceNetwork.AdSettings.AddTestDevice(deviceId);
                    }
                }
                
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

            // Set delegates (following Meta examples)
            bannerAd.AdViewDidLoad = () =>
            {
                isBannerLoadedAndReady = true;
                UpdateBannerState(isBannerShowing, true);
                OnAdLoaded(AdType.Banner);
                string isAdValid = bannerAd.IsValid() ? "valid" : "invalid";
                DebugLog($"[MetaAN]: Banner loaded and is {isAdValid}");
                
                // If we shouldn't be showing, hide it (dispose it because Meta doesn't have Hide())
                if (!isBannerShowing)
                {
                    DestroyBanner();
                }
                else
                {
                    // Show banner at bottom position
                    bannerAd.Show(AdPosition.BOTTOM);
                    OnAdDisplayed(AdType.Banner);
                }
            };

            bannerAd.AdViewDidFailWithError = (error) =>
            {
                isBannerLoadedAndReady = false;
                UpdateBannerState(false, false);
                DebugLogError($"[MetaAN]: Banner failed to load: {error}");
            };

            bannerAd.AdViewWillLogImpression = () =>
            {
                DebugLog("[MetaAN]: Banner logged impression");
            };

            bannerAd.AdViewDidClick = () =>
            {
                DebugLog("[MetaAN]: Banner clicked");
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

            // Set delegates (following Meta examples)
            interstitialAd.InterstitialAdDidLoad = () =>
            {
                isInterstitialLoaded = true;
                didCloseInterstitial = false;
                string isAdValid = interstitialAd.IsValid() ? "valid" : "invalid";
                OnAdLoaded(AdType.Interstitial);
                DebugLog($"[MetaAN]: Interstitial loaded and is {isAdValid}");
            };

            interstitialAd.InterstitialAdDidFailWithError = (error) =>
            {
                isInterstitialLoaded = false;
                DebugLogError($"[MetaAN]: Interstitial failed to load: {error}");
            };

            interstitialAd.InterstitialAdWillLogImpression = () =>
            {
                DebugLog("[MetaAN]: Interstitial logged impression");
            };

            interstitialAd.InterstitialAdDidClick = () =>
            {
                DebugLog("[MetaAN]: Interstitial clicked");
            };

            interstitialAd.InterstitialAdDidClose = () =>
            {
                DebugLog("[MetaAN]: Interstitial did close");
                didCloseInterstitial = true;
                isInterstitialLoaded = false;
                
                OnAdClosed(AdType.Interstitial);
                currentInterstitialCallback?.Invoke(true);
                currentInterstitialCallback = null;
                
                // Cleanup
                if (interstitialAd != null)
                {
                    interstitialAd.Dispose();
                }
                
                // Auto reload
                RequestInterstitial();
            };

#if UNITY_ANDROID
            // Android-specific: Handle activity destroyed without proper close
            // (following Meta example for apps with launchMode:singleTask)
            interstitialAd.interstitialAdActivityDestroyed = () =>
            {
                if (!didCloseInterstitial)
                {
                    DebugLogWarning("[MetaAN]: Interstitial activity destroyed without being closed first");
                    DebugLog("[MetaAN]: Game should resume");
                    
                    isInterstitialLoaded = false;
                    currentInterstitialCallback?.Invoke(false);
                    currentInterstitialCallback = null;
                }
            };
#endif

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
            return isInterstitialLoaded && interstitialAd != null && interstitialAd.IsValid();
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

            // Set delegates (following Meta examples)
            rewardedVideoAd.RewardedVideoAdDidLoad = () =>
            {
                isRewardedVideoLoaded = true;
                didCloseRewardedVideo = false;
                string isAdValid = rewardedVideoAd.IsValid() ? "valid" : "invalid";
                OnAdLoaded(AdType.RewardedVideo);
                DebugLog($"[MetaAN]: Rewarded video loaded and is {isAdValid}");
            };

            rewardedVideoAd.RewardedVideoAdDidFailWithError = (error) =>
            {
                isRewardedVideoLoaded = false;
                DebugLogError($"[MetaAN]: Rewarded video failed to load: {error}");
            };

            rewardedVideoAd.RewardedVideoAdWillLogImpression = () =>
            {
                DebugLog("[MetaAN]: Rewarded video logged impression");
            };

            rewardedVideoAd.RewardedVideoAdDidClick = () =>
            {
                DebugLog("[MetaAN]: Rewarded video clicked");
            };

            // Server-to-Server (S2S) validation callbacks
            // These are called when using RewardData for server-side validation
            rewardedVideoAd.RewardedVideoAdDidSucceed = () =>
            {
                DebugLog("[MetaAN]: Rewarded video validated by server");
            };

            rewardedVideoAd.RewardedVideoAdDidFail = () =>
            {
                DebugLogWarning("[MetaAN]: Rewarded video not validated, or no response from server");
            };

            // RewardedVideoAdComplete is called when user completes the video
            // This is called BEFORE DidClose
            rewardedVideoAd.RewardedVideoAdComplete = () =>
            {
                DebugLog("[MetaAN]: Rewarded video completed - User earned reward");
                
                // Invoke callback with reward = true
                if (currentRewardedCallback != null)
                {
                    currentRewardedCallback.Invoke(true);
                    currentRewardedCallback = null;
                }
            };

            rewardedVideoAd.RewardedVideoAdDidClose = () =>
            {
                DebugLog("[MetaAN]: Rewarded video did close");
                didCloseRewardedVideo = true;
                isRewardedVideoLoaded = false;
                
                OnAdClosed(AdType.RewardedVideo);
                
                // If callback still exists here, user closed without completing
                if (currentRewardedCallback != null)
                {
                    currentRewardedCallback.Invoke(false);
                    currentRewardedCallback = null;
                }
                
                // Cleanup
                if (rewardedVideoAd != null)
                {
                    rewardedVideoAd.Dispose();
                }
                
                // Auto reload
                RequestRewardedVideo();
            };

#if UNITY_ANDROID
            // Android-specific: Handle activity destroyed without proper close
            // (following Meta example for apps with launchMode:singleTask)
            rewardedVideoAd.RewardedVideoAdActivityDestroyed = () =>
            {
                if (!didCloseRewardedVideo)
                {
                    DebugLogWarning("[MetaAN]: Rewarded video activity destroyed without being closed first");
                    DebugLog("[MetaAN]: Game should resume. User should NOT get a reward");
                    
                    isRewardedVideoLoaded = false;
                    
                    // User should NOT get reward in this case
                    if (currentRewardedCallback != null)
                    {
                        currentRewardedCallback.Invoke(false);
                        currentRewardedCallback = null;
                    }
                }
            };
#endif

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
            return isRewardedVideoLoaded && rewardedVideoAd != null && rewardedVideoAd.IsValid();
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
