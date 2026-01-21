using UnityEngine;
using System;
using System.Collections.Generic;

// We assume Tapjoy plugin resides in global namespace or TapjoyUnity namespace
// Since we don't have the SDK yet, we wrap usage in #if

#if TAPJOY_OFFERWALL
// using TapjoyUnity; // Avoiding using directive to prevent collision
#endif

namespace MobileCore.Offerwall.Providers.Tapjoy
{
    public class TapjoyProvider : BaseOfferwallProvider
    {
#if TAPJOY_OFFERWALL
        private TapjoyContainer tapjoySettings;
        private TapjoyUnity.TJPlacement offerwallPlacement;
        private bool isConnecting = false;
#endif

        public override void Initialize(OfferwallSettings settings)
        {
            this.settings = settings;
            
#if TAPJOY_OFFERWALL
            tapjoySettings = settings.TapjoyContainer;

            if (tapjoySettings.EnableDebug)
            {
                TapjoyUnity.Tapjoy.SetLoggingLevel(TapjoyUnity.LoggingLevel.Debug);
            }

            string sdkKey = "";
#if UNITY_ANDROID
            sdkKey = tapjoySettings.AndroidSdkKey;
#elif UNITY_IOS
            sdkKey = tapjoySettings.IosSdkKey;
#endif

            if (!string.IsNullOrEmpty(sdkKey))
            {
                isConnecting = true;
                
                // Connect delegates
                TapjoyUnity.Tapjoy.OnConnectSuccess += HandleConnectSuccess;
                TapjoyUnity.Tapjoy.OnConnectFailed += HandleConnectFailed;
                TapjoyUnity.Tapjoy.OnConnectWarning += HandleConnectWarning;

                // Placement delegates
                TapjoyUnity.TJPlacement.OnRequestSuccess += OnPlacementRequestSuccess;
                TapjoyUnity.TJPlacement.OnRequestFailure += OnPlacementRequestFailure;
                TapjoyUnity.TJPlacement.OnContentShow += OnPlacementContentShow;
                TapjoyUnity.TJPlacement.OnContentDismiss += OnPlacementContentDismiss;

                // Currency delegates
                TapjoyUnity.Tapjoy.OnGetCurrencyBalanceResponse += HandleGetCurrencyBalanceResponse;
                TapjoyUnity.Tapjoy.OnGetCurrencyBalanceResponseFailure += HandleGetCurrencyBalanceResponseFailure;
                TapjoyUnity.Tapjoy.OnEarnedCurrency += HandleEarnedCurrency;
                
                Dictionary<string, object> connectFlags = new Dictionary<string, object>();
                connectFlags.Add("TJC_OPTION_LOGGING_LEVEL", tapjoySettings.EnableDebug ? TapjoyUnity.LoggingLevel.Debug : TapjoyUnity.LoggingLevel.Info);
                
                // Gcm sender id if needed
                if (!string.IsNullOrEmpty(tapjoySettings.GcmSenderId))
                {
                    // connectFlags.Add("TJC_OPTION_GCM_SENDER_ID", tapjoySettings.GcmSenderId);
                }

                TapjoyUnity.Tapjoy.Connect(sdkKey, connectFlags);
                Debug.Log("[TapjoyProvider] Connecting...");
            }
            else
            {
                Debug.LogError("[TapjoyProvider] SDK Key is missing!");
            }
#endif
        }

#if TAPJOY_OFFERWALL
        private void HandleConnectSuccess()
        {
            isConnecting = false;
            isInitialized = true;
            Debug.Log("[TapjoyProvider] Connected successfully!");

            // Pre-load Offerwall placement when connected
            ConnectPlacement();
        }

        private void HandleConnectFailed(int code, string message)
        {
            isConnecting = false;
            isInitialized = false;
            Debug.LogError($"[TapjoyProvider] Connect Failed: {message} ({code})");
            NotifyOfferwallError($"Connect Failed: {message}");
        }

        private void HandleConnectWarning(int code, string message)
        {
            isConnecting = false;
            isInitialized = true;
            Debug.LogWarning($"[TapjoyProvider] Connect Warning: {message} ({code})");
            // Treat warning as success for functionality usually
            ConnectPlacement();
        }

        private void ConnectPlacement()
        {
            // Just create the placement, content request happens when showing or explicitly requested
            string placementName = tapjoySettings.OfferwallPlacementName;
            
            // Unsubscribe static events to prevent duplicates if re-connecting
            TapjoyUnity.TJPlacement.OnRequestSuccess -= OnPlacementRequestSuccess;
            TapjoyUnity.TJPlacement.OnRequestFailure -= OnPlacementRequestFailure;
            TapjoyUnity.TJPlacement.OnContentReady -= OnPlacementContentReady;
            TapjoyUnity.TJPlacement.OnContentShow -= OnPlacementContentShow;
            TapjoyUnity.TJPlacement.OnContentDismiss -= OnPlacementContentDismiss;

            // Subscribe to static events
            TapjoyUnity.TJPlacement.OnRequestSuccess += OnPlacementRequestSuccess;
            TapjoyUnity.TJPlacement.OnRequestFailure += OnPlacementRequestFailure;
            TapjoyUnity.TJPlacement.OnContentReady += OnPlacementContentReady;
            TapjoyUnity.TJPlacement.OnContentShow += OnPlacementContentShow;
            TapjoyUnity.TJPlacement.OnContentDismiss += OnPlacementContentDismiss;

            // Only create if null to avoid duplicates (though sample cleans up ref)
            if (offerwallPlacement == null)
            {
                 offerwallPlacement = TapjoyUnity.TJPlacement.CreatePlacement(placementName);
            }
            
            if (offerwallPlacement != null)
            {
                offerwallPlacement.RequestContent();
            }
        }
#endif

        public override void ShowOfferwall(string placementName = null)
        {
#if TAPJOY_OFFERWALL
            if (!isInitialized)
            {
                Debug.LogWarning("[TapjoyProvider] Not initialized.");
                return;
            }

            // If placement is null, try to recreate
            if (offerwallPlacement == null)
            {
                 ConnectPlacement();
            }

            if (offerwallPlacement != null)
            {
                // We define a local handler for when content is ready, IF we are requesting now
                if (offerwallPlacement.IsContentAvailable())
                {
                    offerwallPlacement.ShowContent();
                    NotifyOfferwallOpened();
                }
                else
                {
                    Debug.Log("[TapjoyProvider] Content not available, requesting...");
                    
                    // Temporary handler just for this show request
                    TapjoyUnity.TJPlacement.OnContentReadyHandler readyHandler = null;
                    readyHandler = (TapjoyUnity.TJPlacement p) =>
                    {
                        if (p.GetName() == tapjoySettings.OfferwallPlacementName)
                        {
                            Debug.Log("[TapjoyProvider] Content Ready (Late), Showing...");
                            p.ShowContent();
                            NotifyOfferwallOpened();
                            TapjoyUnity.TJPlacement.OnContentReady -= readyHandler; // Unsubscribe self
                        }
                    };
                    
                    TapjoyUnity.TJPlacement.OnContentReady += readyHandler;
                    offerwallPlacement.RequestContent();
                }
            }
#else
            Debug.LogWarning("[TapjoyProvider] Tapjoy SDK not installed (TAPJOY_OFFERWALL symbol missing)");
#endif
        }

        public override void GetCurrencyBalance(Action<int> onBalanceReceived)
        {
#if TAPJOY_OFFERWALL
            if (!isInitialized) return;
            TapjoyUnity.Tapjoy.GetCurrencyBalance();
            // Note: Listener HandleGetCurrencyBalanceResponse handles the callback globally
#endif
        }

#if TAPJOY_OFFERWALL
        private void HandleGetCurrencyBalanceResponse(string currencyName, int balance)
        {
            Debug.Log($"[TapjoyProvider] Balance: {currencyName} {balance}");
            // Since BaseOfferwallProvider doesn't have a direct callback field for this async request outside of event,
            // we might want to trigger a generic event or just log index.
            // If you need direct callback support, we'd need to store the 'onBalanceReceived' action in a list/queue.
        }

        private void HandleGetCurrencyBalanceResponseFailure(string error)
        {
             Debug.LogError($"[TapjoyProvider] GetCurrencyBalance failed: {error}");
        }

        private void HandleEarnedCurrency(string currencyName, int amount)
        {
             Debug.Log($"[TapjoyProvider] Earned: {amount} {currencyName}");
             TapjoyUnity.Tapjoy.ShowDefaultEarnedCurrencyAlert(); // Optional: Show built-in alert
             NotifyCurrencyEarned(amount);
        }

        // Static Event Handlers
        private void OnPlacementRequestSuccess(TapjoyUnity.TJPlacement placement)
        {
            if (placement.GetName() == tapjoySettings.OfferwallPlacementName)
            {
                Debug.Log($"[Tapjoy] Request success: {placement.GetName()}"); 
            }
        }

        private void OnPlacementRequestFailure(TapjoyUnity.TJPlacement placement, string error)
        {
             if (placement.GetName() == tapjoySettings.OfferwallPlacementName)
            {
                Debug.LogWarning($"[Tapjoy] Request failure: {placement.GetName()} - {error}");
            }
        }

        private void OnPlacementContentReady(TapjoyUnity.TJPlacement placement)
        {
             if (placement.GetName() == tapjoySettings.OfferwallPlacementName)
            {
                Debug.Log($"[Tapjoy] Content ready: {placement.GetName()}"); 
            }
        }

        private void OnPlacementContentShow(TapjoyUnity.TJPlacement placement)
        {
             if (placement.GetName() == tapjoySettings.OfferwallPlacementName)
            {
                 // NotifyOfferwallOpened(); 
            }
        }

        private void OnPlacementContentDismiss(TapjoyUnity.TJPlacement placement)
        {
             if (placement.GetName() == tapjoySettings.OfferwallPlacementName)
            {
                 NotifyOfferwallClosed();
                 // Request next content for future
                 placement.RequestContent();
            }
        }
#endif
    }
}
