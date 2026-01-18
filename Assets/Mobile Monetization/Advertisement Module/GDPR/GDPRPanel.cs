#pragma warning disable 0649 

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

namespace MobileCore.Advertisements
{
    public class GDPRPanel : MonoBehaviour
    {
        [SerializeField] private GameObject termsOfUseObject;
        [SerializeField] private GameObject privacyPolicyObject;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button declineButton;

        [SerializeField] private Toggle agreePrivacyToggle;
        [SerializeField] Toggle agreeTermsToggle;

        private Action onCompleted;

        public void Initialize(Action onCompletedCallback)
        {
            this.onCompleted = onCompletedCallback;

            SetupLinkButtons();
            SetupToggles();

            DontDestroyOnLoad(gameObject);
            gameObject.SetActive(true);
        }

        private void SetupLinkButtons()
        {
            EventTrigger termsTrigger = termsOfUseObject.AddComponent<EventTrigger>();
            EventTrigger.Entry termsEntry = new EventTrigger.Entry();
            termsEntry.eventID = EventTriggerType.PointerDown;
            termsEntry.callback.AddListener((eventData) => { OpenTermsOfUseLinkButton(); });
            termsTrigger.triggers.Add(termsEntry);

            EventTrigger privacyTrigger = privacyPolicyObject.AddComponent<EventTrigger>();
            EventTrigger.Entry privacyEntry = new EventTrigger.Entry();
            privacyEntry.eventID = EventTriggerType.PointerDown;
            privacyEntry.callback.AddListener((eventData) => { OpenPrivacyLinkButton(); });
            privacyTrigger.triggers.Add(privacyEntry);
        }

        private void SetupToggles()
        {
            agreePrivacyToggle.onValueChanged.AddListener(OnToggleValueChanged);
            agreeTermsToggle.onValueChanged.AddListener(OnToggleValueChanged);

            agreePrivacyToggle.isOn = false;
            agreeTermsToggle.isOn = false;

            acceptButton.onClick.AddListener(() => SetGDPRStateButton(false));
            acceptButton.interactable = false;

            declineButton.onClick.AddListener(CloseApplication);
        }

        private void OnToggleValueChanged(bool value)
        {
            acceptButton.interactable = agreePrivacyToggle.isOn && agreeTermsToggle.isOn;
        }

        public void OpenPrivacyLinkButton()
        {
            Application.OpenURL(AdsManager.Settings.PrivacyLink);
        }

        public void OpenTermsOfUseLinkButton()
        {
            Application.OpenURL(AdsManager.Settings.TermsOfUseLink);
        }

        public void SetGDPRStateButton(bool state)
        {
            AdsManager.SetGDPR(state);
            CloseWindow();
            onCompleted?.Invoke();
        }

        private void CloseApplication()
        {
            Debug.Log("[GDPRPanel] Closing application due to GDPR rejection.");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void CloseWindow()
        {
            gameObject.SetActive(false);
            Destroy(gameObject, 1f);
        }
    }
}