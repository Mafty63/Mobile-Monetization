using UnityEngine;
using System;
using System.Threading.Tasks;

namespace MobileCore.Advertisements
{
    public abstract class BaseAdProviderHandler : IDisposable
    {
        protected AdProvider providerType;
        public AdProvider ProviderType => providerType;

        protected AdsSettings adsSettings;

        protected bool isInitialized = false;
        protected bool isBannerShowing = false;
        protected bool isBannerLoaded = false;
        private bool disposed = false;

        public BaseAdProviderHandler(AdProvider providerType)
        {
            this.providerType = providerType;
        }

        public bool IsInitialized()
        {
            return isInitialized;
        }

        public bool IsBannerShowing()
        {
            return isBannerShowing && isBannerLoaded;
        }

        protected void OnProviderInitialized()
        {
            isInitialized = true;
            AdsManager.OnProviderInitialized(providerType);

            if (adsSettings?.SystemLogs == true)
                Debug.Log($"[AdsManager]: {providerType} is initialized!");
        }

        protected void OnAdLoaded(AdType adType)
        {
            AdsManager.OnProviderAdLoaded(providerType, adType);
        }

        protected void OnAdDisplayed(AdType adType)
        {
            AdsManager.OnProviderAdDisplayed(providerType, adType);
        }

        protected void OnAdClosed(AdType adType)
        {
            AdsManager.OnProviderAdClosed(providerType, adType);
        }

        // Sync initialization (legacy support)
        public abstract void Initialize(AdsSettings adsSettings);

        // Async initialization (recommended)
        public virtual Task<bool> InitializeAsync(AdsSettings adsSettings)
        {
            try
            {
                Initialize(adsSettings);
                return Task.FromResult(isInitialized);
            }
            catch (Exception ex)
            {
                if (adsSettings?.SystemLogs == true)
                    Debug.LogError($"[AdsManager]: {providerType} initialization failed: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        // Banner methods
        public abstract void ShowBanner();
        public abstract void HideBanner();
        public abstract void DestroyBanner();

        // Interstitial methods
        public abstract void RequestInterstitial();
        public abstract void ShowInterstitial(InterstitialCallback callback);
        public abstract bool IsInterstitialLoaded();

        // Rewarded Video methods
        public abstract void RequestRewardedVideo();
        public abstract void ShowRewardedVideo(RewardedVideoCallback callback);
        public abstract bool IsRewardedVideoLoaded();

        // Ad status methods
        public virtual bool IsBannerLoaded() => isBannerLoaded;
        public virtual bool IsAnyAdShowing() => false;

        // Privacy and compliance methods
        public virtual void SetGDPR(bool state) { }
        public virtual void SetAgeRestricted(bool state) { }
        public virtual void SetCCPA(bool state) { }
        public virtual void SetCOPPA(bool state) { }
        public virtual void SetUserConsent(bool state) { }
        public virtual void SetUserLocation(double latitude, double longitude) { }

        // IDisposable implementation
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Clean managed resources in derived class
                }
                disposed = true;
            }
        }

        #region Callbacks
        public delegate void RewardedVideoCallback(bool hasReward);
        public delegate void InterstitialCallback(bool isDisplayed);
        #endregion

        #region Helper Methods for Derived Classes
        protected void DebugLog(string message)
        {
            if (adsSettings?.SystemLogs == true)
                Debug.Log(message);
        }

        protected void DebugLogWarning(string message)
        {
            if (adsSettings?.SystemLogs == true)
                Debug.LogWarning(message);
        }

        protected void DebugLogError(string message)
        {
            if (adsSettings?.SystemLogs == true)
                Debug.LogError(message);
        }

        protected void UpdateBannerState(bool showing, bool loaded)
        {
            isBannerShowing = showing;
            isBannerLoaded = loaded;
        }
        #endregion
    }

    // Enum for ad types
    public enum AdType
    {
        Banner,
        Interstitial,
        RewardedVideo,
        AppOpen
    }

    // Enum for banner positions
    public enum BannerPosition
    {
        Bottom,
        Top
    }
}