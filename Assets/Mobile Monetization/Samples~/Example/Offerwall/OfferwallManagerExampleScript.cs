using UnityEngine;
using UnityEngine.UI;
using MobileCore.Offerwall;

namespace MobileCore.Offerwall.Example
{
    public class OfferwallManagerExampleScript : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text logText;
        [SerializeField] private Text balanceText;
        [SerializeField] private InputField amountInput;

        [Space]
        [SerializeField] private Button showOfferwallButton;
        [SerializeField] private Button getBalanceButton;
        [SerializeField] private Button spendButton;
        [SerializeField] private Button awardButton;

        private void Awake()
        {
            // Clear log on start
            if (logText) logText.text = "Initializing Example...\n";
        }

        private void Start()
        {
                // OfferwallManager is static, so we can't check for null Instance. 
                // We assume it's initialized by the system.
            Log("OfferwallManager usage started.");
            Log("OfferwallManager linked successfully.");
            RefreshBalance();
        }

        private void OnEnable()
        {
            if (showOfferwallButton) showOfferwallButton.onClick.AddListener(ShowOfferwall);
            if (getBalanceButton) getBalanceButton.onClick.AddListener(RefreshBalance);
            if (spendButton) spendButton.onClick.AddListener(SpendCurrency);
            if (awardButton) awardButton.onClick.AddListener(AwardCurrency);

            // Subscribe to static event if instance assumes singleton behavior useful for events
            // Ideally we subscribe to Instance event, but checking null is safer
            OfferwallManager.OnCurrencyEarned += OnCurrencyEarned;
        }

        private void OnDisable()
        {
            if (showOfferwallButton) showOfferwallButton.onClick.RemoveListener(ShowOfferwall);
            if (getBalanceButton) getBalanceButton.onClick.RemoveListener(RefreshBalance);
            if (spendButton) spendButton.onClick.RemoveListener(SpendCurrency);
            if (awardButton) awardButton.onClick.RemoveListener(AwardCurrency);

            OfferwallManager.OnCurrencyEarned -= OnCurrencyEarned;
        }

        private void OnCurrencyEarned(int amount)
        {
            Log($"<color=green>EVENT: User earned {amount} currency!</color>");
            RefreshBalance();
        }

        public void ShowOfferwall()
        {
            Log("Requesting ShowOfferwall...");
            if (CheckManager())
            {
                OfferwallManager.ShowOfferwall();
            }
        }

        public void RefreshBalance()
        {
            Log("Fetching Currency Balance...");
            if (CheckManager())
            {
                OfferwallManager.GetCurrencyBalance((balance) =>
                {
                    Log($"Balance Received: {balance}");
                    if (balanceText) balanceText.text = $"Balance: {balance}";
                });
            }
        }

        public void SpendCurrency()
        {
            int amount = ParseAmount();
            if (amount <= 0) return;

            Log($"Attempting to Spend {amount} currency...");
            if (CheckManager())
            {
                OfferwallManager.SpendCurrency(amount, (success) =>
                {
                    if (success)
                    {
                        Log($"<color=cyan>Spend Success! Spent {amount}.</color>");
                        RefreshBalance();
                    }
                    else
                    {
                        Log($"<color=red>Spend Failed.</color>");
                    }
                });
            }
        }

        public void AwardCurrency()
        {
            int amount = ParseAmount();
            if (amount <= 0) return;

            Log($"Attempting to Award {amount} currency...");
            if (CheckManager())
            {
                OfferwallManager.AwardCurrency(amount, (success) =>
                {
                    if (success)
                    {
                        Log($"<color=cyan>Award Success! Added {amount}.</color>");
                        RefreshBalance();
                    }
                    else
                    {
                        Log($"<color=red>Award Failed.</color>");
                    }
                });
            }
        }

        private bool CheckManager()
        {
             // Static manager is always "available" in terms of class access, 
             // though it might not be initialized internally. 
             // For this example, we proceed.
             return true;
        }

        private int ParseAmount()
        {
            if (amountInput != null && !string.IsNullOrEmpty(amountInput.text))
            {
                if (int.TryParse(amountInput.text, out int result))
                {
                    return result;
                }
            }
            Log("<color=yellow>Invalid Amount Input</color>");
            return 0;
        }

        private void Log(string msg)
        {
            Debug.Log($"[OfferwallExample] {msg}");
            if (logText)
            {
                // Simple circular buffer effect (optional, or just append to top)
                logText.text = $"- {msg}\n" + logText.text;
                if (logText.text.Length > 2000) logText.text = logText.text.Substring(0, 2000);
            }
        }
    }
}
