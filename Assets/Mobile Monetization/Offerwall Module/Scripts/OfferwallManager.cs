using UnityEngine;
using System;
using MobileCore.Offerwall.Providers;
using System.Collections.Generic;

namespace MobileCore.Offerwall
{
    [MobileCore.DefineSystem.Define("TAPJOY_OFFERWALL", "TapjoyUnity.Tapjoy", "Tapjoy Offerwall SDK")]
    public static class OfferwallManager
    {
        private static OfferwallSettings settings;
        private static BaseOfferwallProvider activeProvider;

        public static event Action<int> OnCurrencyEarned;

        public static void Initialize(OfferwallManagerInitializer initializer)
        {
            InitInternal(initializer.Settings);
        }

        private static void InitInternal(OfferwallSettings settings)
        {
            OfferwallManager.settings = settings;

#if TAPJOY_OFFERWALL
            var provider = new Providers.Tapjoy.TapjoyProvider();
            provider.Initialize(settings);
            provider.OnCurrencyEarned += HandleCurrencyEarned;
            
            activeProvider = provider;
            
            if (settings.ShowLogs) Debug.Log("[OfferwallManager] Initialized with Tapjoy.");
#else
            Debug.LogWarning("[Offerwall] Tapjoy SDK not detected. Offerwall disabled.");
#endif
        }

        private static void HandleCurrencyEarned(int amount)
        {
            if (settings != null && settings.ShowLogs) Debug.Log($"[Offerwall] Currency Earned: {amount}");
            OnCurrencyEarned?.Invoke(amount);
        }

        public static void ShowOfferwall()
        {
            if (activeProvider != null && activeProvider.IsInitialized)
            {
                activeProvider.ShowOfferwall();
            }
            else
            {
                Debug.LogWarning("[Offerwall] Provider not initialized or unavailable");
            }
        }

        public static void GetCurrencyBalance(Action<int> onBalanceReceived)
        {
            if (activeProvider != null && activeProvider.IsInitialized)
            {
                activeProvider.GetCurrencyBalance(onBalanceReceived);
            }
            else
            {
                onBalanceReceived?.Invoke(0);
            }
        }

        public static void SpendCurrency(int amount, Action<bool> onSpent = null)
        {
            if (activeProvider != null && activeProvider.IsInitialized)
            {
                activeProvider.SpendCurrency(amount, onSpent);
            }
            else
            {
                onSpent?.Invoke(false);
            }
        }
        
        public static void AwardCurrency(int amount, Action<bool> onAwarded = null)
        {
            if (activeProvider != null && activeProvider.IsInitialized)
            {
                if (activeProvider is Providers.Tapjoy.TapjoyProvider tapjoyProvider)
                {
                    tapjoyProvider.AwardCurrency(amount, onAwarded);
                }
                else
                {
                    Debug.LogWarning("[OfferwallManager] Active provider does not support AwardCurrency");
                    onAwarded?.Invoke(false);
                }
            }
            else
            {
                onAwarded?.Invoke(false);
            }
        }
    }
}
namespace MobileCore.Offerwall.Providers { }
