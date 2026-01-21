using UnityEngine;
using System;

namespace MobileCore.Offerwall
{
    public abstract class BaseOfferwallProvider
    {
        protected OfferwallSettings settings;
        protected bool isInitialized = false;

        public bool IsInitialized => isInitialized;

        public abstract void Initialize(OfferwallSettings settings);
        public abstract void ShowOfferwall(string placementName = null);
        public abstract void GetCurrencyBalance(Action<int> onBalanceReceived);
        
        public virtual void SpendCurrency(int amount, Action<bool> onSpent) { }

        public event Action<int> OnCurrencyEarned;
        public event Action OnOfferwallOpened;
        public event Action OnOfferwallClosed;
        public event Action<string> OnOfferwallError;

        protected void NotifyCurrencyEarned(int amount)
        {
            OnCurrencyEarned?.Invoke(amount);
        }

        protected void NotifyOfferwallOpened() => OnOfferwallOpened?.Invoke();
        protected void NotifyOfferwallClosed() => OnOfferwallClosed?.Invoke();
        protected void NotifyOfferwallError(string error) => OnOfferwallError?.Invoke(error);
    }
}
