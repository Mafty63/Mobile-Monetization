using UnityEngine;
using System;
using MobileCore.Offerwall.Providers;
using System.Collections.Generic;

namespace MobileCore.Offerwall
{
    [MobileCore.DefineSystem.Define("TAPJOY_OFFERWALL", "TapjoyUnity.Tapjoy", "Tapjoy Offerwall SDK")]
    public class OfferwallManager : MonoBehaviour
    {
        public static OfferwallManager Instance { get; private set; }

        private OfferwallSettings settings;
        private BaseOfferwallProvider activeProvider;

        public event Action<int> OnCurrencyEarned;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public static void Initialize(OfferwallManagerInitializer initializer)
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("OfferwallManager");
                go.AddComponent<OfferwallManager>();
                DontDestroyOnLoad(go);
            }

            Instance.InitInternal(initializer.Settings);
        }

        private void InitInternal(OfferwallSettings settings)
        {
            this.settings = settings;

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

        private void HandleCurrencyEarned(int amount)
        {
            if (settings.ShowLogs) Debug.Log($"[Offerwall] Currency Earned: {amount}");
            OnCurrencyEarned?.Invoke(amount);
        }

        public void ShowOfferwall()
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
    }
}
namespace MobileCore.Offerwall.Providers { }
