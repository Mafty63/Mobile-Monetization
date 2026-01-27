#pragma warning disable 0649

using MobileCore.Advertisements;
using UnityEngine;
using UnityEngine.UI;

namespace MobileCore.Advertisements.Example
{
    public class AdsManagerExampleScript : MonoBehaviour
    {
        [SerializeField] private Text logText;

        [Space]
        [SerializeField] private Text bannerTitleText;
        [SerializeField] private Button showBannerButton;
        [SerializeField] private Button hideBannerButton;
        [SerializeField] private Button destroyBannerButton;
        [SerializeField] private Button[] bannerButtons;

        [Space]
        [SerializeField] private Text interstitialTitleText;
        [SerializeField] private Button interstitialStatusButton;
        [SerializeField] private Button requestInterstitialButton;
        [SerializeField] private Button showInterstitialButton;
        [SerializeField] private Button[] interstitialButtons;

        [Space]
        [SerializeField] private Text rewardVideoTitleText;
        [SerializeField] private Button rewardedVideoStatusButton;
        [SerializeField] private Button requestRewardedVideoButton;
        [SerializeField] private Button showRewardedVideoButton;
        [SerializeField] private Button[] rewardVideoButtons;

        private AdsSettings settings;

        private void Awake()
        {
            Application.logMessageReceived += Log;
        }

        private void OnEnable()
        {
            showBannerButton.onClick.AddListener(ShowBannerButton);
            hideBannerButton.onClick.AddListener(HideBannerButton);
            destroyBannerButton.onClick.AddListener(DestroyBannerButton);

            interstitialStatusButton.onClick.AddListener(InterstitialStatusButton);
            requestInterstitialButton.onClick.AddListener(RequestInterstitialButton);
            showInterstitialButton.onClick.AddListener(ShowInterstitialButton);

            rewardedVideoStatusButton.onClick.AddListener(RewardedVideoStatusButton);
            requestRewardedVideoButton.onClick.AddListener(RequestRewardedVideoButton);
            showRewardedVideoButton.onClick.AddListener(ShowRewardedVideoButton);
        }

        private void OnDisable()
        {
            showBannerButton.onClick.RemoveListener(ShowBannerButton);
            hideBannerButton.onClick.RemoveListener(HideBannerButton);
            destroyBannerButton.onClick.RemoveListener(DestroyBannerButton);

            interstitialStatusButton.onClick.RemoveListener(InterstitialStatusButton);
            requestInterstitialButton.onClick.RemoveListener(RequestInterstitialButton);
            showInterstitialButton.onClick.RemoveListener(ShowInterstitialButton);

            rewardedVideoStatusButton.onClick.RemoveListener(RewardedVideoStatusButton);
            requestRewardedVideoButton.onClick.RemoveListener(RequestRewardedVideoButton);
            showRewardedVideoButton.onClick.RemoveListener(ShowRewardedVideoButton);
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= Log;
        }

        private void Start()
        {
            settings = AdsManager.Settings;
            if (settings == null) return;

            logText.text = string.Empty;

            bannerTitleText.text = string.Format("Banner ({0})", settings.BannerType.ToString());
            if (settings.BannerType == AdProvider.Disable)
            {
                for (int i = 0; i < bannerButtons.Length; i++)
                {
                    bannerButtons[i].interactable = false;
                }
            }

            interstitialTitleText.text = string.Format("Interstitial ({0})", settings.InterstitialType.ToString());
            if (settings.InterstitialType == AdProvider.Disable)
            {
                for (int i = 0; i < interstitialButtons.Length; i++)
                {
                    interstitialButtons[i].interactable = false;
                }
            }

            rewardVideoTitleText.text = string.Format("Rewarded Video ({0})", settings.RewardedVideoType.ToString());
            if (settings.RewardedVideoType == AdProvider.Disable)
            {
                for (int i = 0; i < rewardVideoButtons.Length; i++)
                {
                    rewardVideoButtons[i].interactable = false;
                }
            }
        }

        private void Log(string condition, string stackTrace, LogType type)
        {
            logText.text = logText.text.Insert(0, condition + "\n");
        }

        private void Log(string condition)
        {
            logText.text = logText.text.Insert(0, condition + "\n");
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

        private void DestroyBannerButton()
        {
            AdsManager.DestroyBanner();
        }

        private void InterstitialStatusButton()
        {
            Log("[AdsManager]: Interstitial " + (AdsManager.IsInterstitialLoaded() ? "is loaded" : "isn't loaded"));
        }

        private void RequestInterstitialButton()
        {
            AdsManager.RequestInterstitial();
        }

        private void ShowInterstitialButton()
        {
            AdsManager.ShowInterstitial((isDisplayed) =>
            {
                Debug.Log("[AdsManager]: Interstitial " + (isDisplayed ? "is" : "isn't") + " displayed!");
            }, true);
        }

        private void RewardedVideoStatusButton()
        {
            Log("[AdsManager]: Rewarded video " + (AdsManager.IsRewardBasedVideoLoaded() ? "is loaded" : "isn't loaded"));
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
                    Log("[AdsManager]: Reward is received");
                }
                else
                {
                    Log("[AdsManager]: Reward isn't received");
                }
            });
        }
        #endregion
    }
}