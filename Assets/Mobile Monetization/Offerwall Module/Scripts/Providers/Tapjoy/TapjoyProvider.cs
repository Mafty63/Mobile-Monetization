using UnityEngine;
using System;
using System.Collections.Generic;

#if TAPJOY_OFFERWALL
#if UNITY_IOS
using Unity.Advertisement.IosSupport;
#endif
#endif

namespace MobileCore.Offerwall.Providers.Tapjoy
{
    public class TapjoyProvider : BaseOfferwallProvider
    {
#if TAPJOY_OFFERWALL
        private TapjoyContainer tapjoySettings;
        private TapjoyUnity.TJPlacement offerwallPlacement;
        private bool isConnecting = false;
        
        private Action<int> pendingBalanceCallback;
        private Action<bool> pendingSpendCallback;
        private Action<bool> pendingAwardCallback;
#endif

        public override void Initialize(OfferwallSettings settings)
        {
            this.settings = settings;
            
#if TAPJOY_OFFERWALL
            tapjoySettings = settings.TapjoyContainer;

            // Handle iOS App Tracking Transparency (ATT) before connecting
            RequestATTPermission();

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
                TapjoyUnity.Tapjoy.OnSpendCurrencyResponse += HandleSpendCurrencyResponse;
                TapjoyUnity.Tapjoy.OnSpendCurrencyResponseFailure += HandleSpendCurrencyResponseFailure;
                TapjoyUnity.Tapjoy.OnAwardCurrencyResponse += HandleAwardCurrencyResponse;
                TapjoyUnity.Tapjoy.OnAwardCurrencyResponseFailure += HandleAwardCurrencyResponseFailure;
                TapjoyUnity.Tapjoy.OnEarnedCurrency += HandleEarnedCurrency;
                
                Dictionary<string, object> connectFlags = new Dictionary<string, object>();
                connectFlags.Add("TJC_OPTION_LOGGING_LEVEL", tapjoySettings.EnableDebug ? TapjoyUnity.LoggingLevel.Debug : TapjoyUnity.LoggingLevel.Info);
                
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
        private void RequestATTPermission()
        {
#if UNITY_IOS
            // Check if we need to request ATT permission (iOS 14+)
            if (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() == 
                ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
            {
                Debug.Log("[TapjoyProvider] Requesting ATT permission...");
                ATTrackingStatusBinding.RequestAuthorizationTracking();
            }
            else
            {
                Debug.Log($"[TapjoyProvider] ATT status: {ATTrackingStatusBinding.GetAuthorizationTrackingStatus()}");
            }
#endif
        }

        private void HandleConnectSuccess()
        {
            isConnecting = false;
            isInitialized = true;
            Debug.Log("[TapjoyProvider] Connected successfully!");

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
            ConnectPlacement();
        }

        private void ConnectPlacement()
        {
            string placementName = tapjoySettings.OfferwallPlacementName;
            
            TapjoyUnity.TJPlacement.OnRequestSuccess -= OnPlacementRequestSuccess;
            TapjoyUnity.TJPlacement.OnRequestFailure -= OnPlacementRequestFailure;
            TapjoyUnity.TJPlacement.OnContentReady -= OnPlacementContentReady;
            TapjoyUnity.TJPlacement.OnContentShow -= OnPlacementContentShow;
            TapjoyUnity.TJPlacement.OnContentDismiss -= OnPlacementContentDismiss;

            TapjoyUnity.TJPlacement.OnRequestSuccess += OnPlacementRequestSuccess;
            TapjoyUnity.TJPlacement.OnRequestFailure += OnPlacementRequestFailure;
            TapjoyUnity.TJPlacement.OnContentReady += OnPlacementContentReady;
            TapjoyUnity.TJPlacement.OnContentShow += OnPlacementContentShow;
            TapjoyUnity.TJPlacement.OnContentDismiss += OnPlacementContentDismiss;

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

            if (offerwallPlacement == null)
            {
                 ConnectPlacement();
            }

            if (offerwallPlacement != null)
            {
                if (offerwallPlacement.IsContentAvailable())
                {
                    offerwallPlacement.ShowContent();
                    NotifyOfferwallOpened();
                }
                else
                {
                    Debug.Log("[TapjoyProvider] Content not available, requesting...");
                    
                    TapjoyUnity.TJPlacement.OnContentReadyHandler readyHandler = null;
                    readyHandler = (TapjoyUnity.TJPlacement p) =>
                    {
                        if (p.GetName() == tapjoySettings.OfferwallPlacementName)
                        {
                            Debug.Log("[TapjoyProvider] Content Ready (Late), Showing...");
                            p.ShowContent();
                            NotifyOfferwallOpened();
                            TapjoyUnity.TJPlacement.OnContentReady -= readyHandler;
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
            if (!isInitialized) 
            {
                onBalanceReceived?.Invoke(0);
                return;
            }
            
            pendingBalanceCallback = onBalanceReceived;
            TapjoyUnity.Tapjoy.GetCurrencyBalance();
#else
            onBalanceReceived?.Invoke(0);
#endif
        }

        public override void SpendCurrency(int amount, Action<bool> onSpent)
        {
#if TAPJOY_OFFERWALL
            if (!isInitialized)
            {
                Debug.LogWarning("[TapjoyProvider] Not initialized, cannot spend currency.");
                onSpent?.Invoke(false);
                return;
            }
            
            if (amount <= 0)
            {
                Debug.LogWarning("[TapjoyProvider] Invalid amount to spend.");
                onSpent?.Invoke(false);
                return;
            }
            
            pendingSpendCallback = onSpent;
            TapjoyUnity.Tapjoy.SpendCurrency(amount);
            Debug.Log($"[TapjoyProvider] Spending {amount} currency...");
#else
            onSpent?.Invoke(false);
#endif
        }

        public void AwardCurrency(int amount, Action<bool> onAwarded = null)
        {
#if TAPJOY_OFFERWALL
            if (!isInitialized)
            {
                Debug.LogWarning("[TapjoyProvider] Not initialized, cannot award currency.");
                onAwarded?.Invoke(false);
                return;
            }
            
            if (amount <= 0)
            {
                Debug.LogWarning("[TapjoyProvider] Invalid amount to award.");
                onAwarded?.Invoke(false);
                return;
            }
            
            pendingAwardCallback = onAwarded;
            TapjoyUnity.Tapjoy.AwardCurrency(amount);
            Debug.Log($"[TapjoyProvider] Awarding {amount} currency...");
#else
            onAwarded?.Invoke(false);
#endif
        }

#if TAPJOY_OFFERWALL
        #region Currency Response Handlers
        
        private void HandleGetCurrencyBalanceResponse(string currencyName, int balance)
        {
            Debug.Log($"[TapjoyProvider] Balance: {currencyName} = {balance}");
            pendingBalanceCallback?.Invoke(balance);
            pendingBalanceCallback = null;
        }

        private void HandleGetCurrencyBalanceResponseFailure(string error)
        {
            Debug.LogError($"[TapjoyProvider] GetCurrencyBalance failed: {error}");
            pendingBalanceCallback?.Invoke(0);
            pendingBalanceCallback = null;
        }

        private void HandleSpendCurrencyResponse(string currencyName, int balance)
        {
            Debug.Log($"[TapjoyProvider] SpendCurrency success. {currencyName} balance: {balance}");
            pendingSpendCallback?.Invoke(true);
            pendingSpendCallback = null;
        }

        private void HandleSpendCurrencyResponseFailure(string error)
        {
            Debug.LogError($"[TapjoyProvider] SpendCurrency failed: {error}");
            pendingSpendCallback?.Invoke(false);
            pendingSpendCallback = null;
        }

        private void HandleAwardCurrencyResponse(string currencyName, int balance)
        {
            Debug.Log($"[TapjoyProvider] AwardCurrency success. {currencyName} balance: {balance}");
            pendingAwardCallback?.Invoke(true);
            pendingAwardCallback = null;
        }

        private void HandleAwardCurrencyResponseFailure(string error)
        {
            Debug.LogError($"[TapjoyProvider] AwardCurrency failed: {error}");
            pendingAwardCallback?.Invoke(false);
            pendingAwardCallback = null;
        }

        private void HandleEarnedCurrency(string currencyName, int amount)
        {
            Debug.Log($"[TapjoyProvider] Earned: {amount} {currencyName}");
            TapjoyUnity.Tapjoy.ShowDefaultEarnedCurrencyAlert();
            NotifyCurrencyEarned(amount);
        }

        #endregion

        #region Placement Event Handlers

        private void OnPlacementRequestSuccess(TapjoyUnity.TJPlacement placement)
        {
            if (placement.GetName() == tapjoySettings.OfferwallPlacementName)
            {
                if (placement.IsContentAvailable())
                {
                    Debug.Log($"[TapjoyProvider] Content available for: {placement.GetName()}");
                }
                else
                {
                    Debug.Log($"[TapjoyProvider] No content available for: {placement.GetName()}");
                }
            }
        }

        private void OnPlacementRequestFailure(TapjoyUnity.TJPlacement placement, string error)
        {
            if (placement.GetName() == tapjoySettings.OfferwallPlacementName)
            {
                Debug.LogWarning($"[TapjoyProvider] Request failure: {placement.GetName()} - {error}");
                NotifyOfferwallError(error);
            }
        }

        private void OnPlacementContentReady(TapjoyUnity.TJPlacement placement)
        {
            if (placement.GetName() == tapjoySettings.OfferwallPlacementName)
            {
                Debug.Log($"[TapjoyProvider] Content ready: {placement.GetName()}"); 
            }
        }

        private void OnPlacementContentShow(TapjoyUnity.TJPlacement placement)
        {
            if (placement.GetName() == tapjoySettings.OfferwallPlacementName)
            {
                Debug.Log($"[TapjoyProvider] Content showing: {placement.GetName()}");
            }
        }

        private void OnPlacementContentDismiss(TapjoyUnity.TJPlacement placement)
        {
            if (placement.GetName() == tapjoySettings.OfferwallPlacementName)
            {
                NotifyOfferwallClosed();
                placement.RequestContent();
            }
        }

        #endregion
#endif
    }
}
