#pragma warning disable 0649

using MobileCore.Advertisements;
using UnityEngine;
using UnityEngine.UI;

namespace MobileCore.Advertisements.Example
{
    public class AdsManagerExampleScript : MonoBehaviour
    {
        [Space]
        [SerializeField] private Button showBannerButton;
        [SerializeField] private Button hideBannerButton;

        [Space]
        [SerializeField] private Button requestInterstitialButton;
        [SerializeField] private Button showInterstitialButton;

        [Space]
        [SerializeField] private Button requestRewardedVideoButton;
        [SerializeField] private Button showRewardedVideoButton;
        [SerializeField] private Text showRewardedVideoButtonText;

        private AdsSettings settings;

        private void OnEnable()
        {
            showBannerButton.onClick.AddListener(ShowBannerButton);
            hideBannerButton.onClick.AddListener(HideBannerButton);

            requestInterstitialButton.onClick.AddListener(RequestInterstitialButton);
            showInterstitialButton.onClick.AddListener(ShowInterstitialButton);

            requestRewardedVideoButton.onClick.AddListener(RequestRewardedVideoButton);
            showRewardedVideoButton.onClick.AddListener(ShowRewardedVideoButton);

            AdsManager.ForcedAdDisabled += RefreshButtonStates;
        }

        private void OnDisable()
        {
            showBannerButton.onClick.RemoveListener(ShowBannerButton);
            hideBannerButton.onClick.RemoveListener(HideBannerButton);

            requestInterstitialButton.onClick.RemoveListener(RequestInterstitialButton);
            showInterstitialButton.onClick.RemoveListener(ShowInterstitialButton);

            requestRewardedVideoButton.onClick.RemoveListener(RequestRewardedVideoButton);
            showRewardedVideoButton.onClick.RemoveListener(ShowRewardedVideoButton);

            AdsManager.ForcedAdDisabled -= RefreshButtonStates;
        }

        private void Start()
        {
            settings = AdsManager.Settings;
            if (settings == null) return;

            RefreshButtonStates();
        }

        private void RefreshButtonStates()
        {
            bool noAdsActive = AdsManager.IsNoAdsActive;

            if (showBannerButton != null)
                showBannerButton.interactable = !noAdsActive;

            if (hideBannerButton != null)
                hideBannerButton.interactable = !noAdsActive;

            if (showInterstitialButton != null)
                showInterstitialButton.interactable = !noAdsActive;

            if (showRewardedVideoButtonText != null)
                showRewardedVideoButtonText.text = noAdsActive ? "Claim Reward" : "Watch Ad";
        }

        #region Buttons
        private void ShowBannerButton()
        {
            AdsManager.ShowBanner();
        }

        private void HideBannerButton()
        {
            AdsManager.HideBanner();
        }

        private void RequestInterstitialButton()
        {
            AdsManager.RequestInterstitial();
        }

        private void ShowInterstitialButton()
        {
            AdsManager.ShowInterstitial((isDisplayed) =>
            {
            }, true);
        }

        private void RequestRewardedVideoButton()
        {
            AdsManager.RequestRewardBasedVideo();
        }

        private void ShowRewardedVideoButton()
        {
            AdsManager.ShowRewardBasedVideo((hasReward) =>
            {
                if (hasReward)
                {
                    Debug.Log("[AdsManager]: Reward is received");
                }
                else
                {
                    Debug.Log("[AdsManager]: Reward isn't received");
                }
            });
        }
        #endregion
    }
}