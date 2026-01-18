using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MobileCore.SystemModule
{
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
    public class SystemManager : MonoBehaviour
    {
        private static SystemManager floatingMessage;

        [Header("Messages")]
        [SerializeField] RectTransform messagePanelRectTransform;
        [SerializeField] TextMeshProUGUI messageText;

        [Header("Loading")]
        [SerializeField] GameObject loadingPanelObject;
        [SerializeField] TextMeshProUGUI loadingStatusText;
        [SerializeField] RectTransform loadingIconRectTransform;

        private Coroutine currentAnimationCoroutine;
        private CanvasGroup messagePanelCanvasGroup;
        private bool isLoadingActive;

        private void Start()
        {
            if (floatingMessage != null) return;

            floatingMessage = this;

            CanvasScaler canvasScaler = gameObject.GetComponent<CanvasScaler>();
            canvasScaler.matchWidthOrHeight = ((float)Screen.width / Screen.height) > (9f / 16f) ? 1.0f : 0.0f;

            messagePanelCanvasGroup = messagePanelRectTransform.gameObject.GetComponent<CanvasGroup>();
            if (messagePanelCanvasGroup == null)
                messagePanelCanvasGroup = messagePanelRectTransform.gameObject.AddComponent<CanvasGroup>();

            // Add click event to message text
            EventTrigger trigger = messageText.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener((data) => OnPanelClick());
            trigger.triggers.Add(entry);

            loadingPanelObject.SetActive(false);
            messagePanelRectTransform.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (isLoadingActive)
            {
                loadingIconRectTransform.Rotate(0, 0, -50 * Time.deltaTime);
            }
        }

        private void OnPanelClick()
        {
            if (currentAnimationCoroutine != null)
                StopCoroutine(currentAnimationCoroutine);

            currentAnimationCoroutine = StartCoroutine(FadeOutPanel());
        }

        private IEnumerator FadeOutPanel()
        {
            float duration = 0.3f;
            float elapsed = 0f;
            float startAlpha = messagePanelCanvasGroup.alpha;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                messagePanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0, elapsed / duration);
                yield return null;
            }

            messagePanelCanvasGroup.alpha = 0;
            messagePanelRectTransform.gameObject.SetActive(false);
        }

        public static void ShowMessage(string message, float duration = 2.5f)
        {
            if (floatingMessage != null)
            {
                if (floatingMessage.isLoadingActive) return;

                if (floatingMessage.currentAnimationCoroutine != null)
                    floatingMessage.StopCoroutine(floatingMessage.currentAnimationCoroutine);

                floatingMessage.messageText.text = message;
                floatingMessage.messagePanelRectTransform.gameObject.SetActive(true);
                floatingMessage.messagePanelCanvasGroup.alpha = 1.0f;

                floatingMessage.currentAnimationCoroutine = floatingMessage.StartCoroutine(
                    floatingMessage.ShowMessageCoroutine(duration)
                );
            }
            else
            {
                Debug.Log("[System Message]: " + message);
                Debug.LogError("[System Message]: ShowMessage() method has called, but module isn't initialized!");
            }
        }

        private IEnumerator ShowMessageCoroutine(float duration)
        {
            // Wait for the display duration
            yield return new WaitForSecondsRealtime(duration);

            // Fade out the panel
            float fadeDuration = 0.5f;
            float elapsed = 0f;
            float startAlpha = messagePanelCanvasGroup.alpha;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                messagePanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0, elapsed / fadeDuration);
                yield return null;
            }

            messagePanelCanvasGroup.alpha = 0;
            messagePanelRectTransform.gameObject.SetActive(false);
        }

        public static void ShowLoadingPanel()
        {
            if (floatingMessage == null) return;
            if (floatingMessage.isLoadingActive) return;

            // Disable message panel if it is active
            if (floatingMessage.currentAnimationCoroutine != null)
            {
                floatingMessage.StopCoroutine(floatingMessage.currentAnimationCoroutine);
                floatingMessage.messagePanelRectTransform.gameObject.SetActive(false);
            }

            // Activate loading
            floatingMessage.isLoadingActive = true;
            floatingMessage.loadingPanelObject.SetActive(true);
        }

        public static void ChangeLoadingMessage(string message)
        {
            if (floatingMessage == null) return;

            floatingMessage.loadingStatusText.text = message;
        }

        public static void HideLoadingPanel()
        {
            if (floatingMessage == null) return;

            // Disable loading
            floatingMessage.isLoadingActive = false;
            floatingMessage.loadingPanelObject.SetActive(false);
        }
    }
}