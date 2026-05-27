#pragma warning disable 0649

using UnityEngine;

namespace MobileCore.Advertisements.Providers
{
    public class AdDummyController : MonoBehaviour
    {
        [SerializeField] GameObject bannerObject;

        [Space]
        [SerializeField] GameObject interstitialObject;

        [Space]
        [SerializeField] GameObject rewardedVideoObject;

        private RectTransform bannerRectTransform;

        private void Awake()
        {
            bannerRectTransform = (RectTransform)bannerObject.transform;
            DontDestroyOnLoad(gameObject);
        }

        public void Initialize(AdsSettings settings)
        {
            switch (settings.DummyContainer.bannerPosition)
            {
                case BannerPosition.Bottom:
                    bannerRectTransform.pivot = new Vector2(0.5f, 0.0f);

                    bannerRectTransform.anchorMin = new Vector2(0.0f, 0.0f);
                    bannerRectTransform.anchorMax = new Vector2(1.0f, 0.0f);

                    bannerRectTransform.anchoredPosition = Vector2.zero;
                    break;
                case BannerPosition.Top:
                    bannerRectTransform.pivot = new Vector2(0.5f, 1.0f);

                    bannerRectTransform.anchorMin = new Vector2(0.0f, 1.0f);
                    bannerRectTransform.anchorMax = new Vector2(1.0f, 1.0f);

                    bannerRectTransform.anchoredPosition = Vector2.zero;
                    break;
            }
        }

        public void ShowBanner()
        {
            bannerObject.SetActive(true);
        }

        public void HideBanner()
        {
            bannerObject.SetActive(false);


            AdsManager.OnProviderAdClosed(AdProvider.Dummy, AdType.Banner);
        }

        private System.Action<bool> onInterstitialCompleted;
        private System.Action<bool> onRewardedCompleted;

        public void ShowInterstitial(System.Action<bool> onComplete)
        {
            onInterstitialCompleted = onComplete;
            interstitialObject.SetActive(true);
        }

        public void CloseInterstitial(bool completed)
        {
            interstitialObject.SetActive(false);

            AdsManager.OnProviderAdClosed(AdProvider.Dummy, AdType.Interstitial);
            onInterstitialCompleted?.Invoke(completed);
            onInterstitialCompleted = null;
        }

        public void ShowRewardedVideo(System.Action<bool> onComplete)
        {
            onRewardedCompleted = onComplete;
            rewardedVideoObject.SetActive(true);
        }

        public void CloseRewardedVideo(bool rewardGranted)
        {
            rewardedVideoObject.SetActive(false);

            AdsManager.OnProviderAdClosed(AdProvider.Dummy, AdType.RewardedVideo);
            onRewardedCompleted?.Invoke(rewardGranted);
            onRewardedCompleted = null;
        }

        #region Buttons
        public void CloseInterstitialButton()
        {
            CloseInterstitial(true);
        }

        public void CloseRewardedVideoButton()
        {
            CloseRewardedVideo(false);
        }

        public void GetRewardButton()
        {
            CloseRewardedVideo(true);
        }
        #endregion
    }
}