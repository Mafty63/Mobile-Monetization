
#pragma warning disable 0649
#pragma warning disable 0162

using System.Collections.Generic;
using System.Threading.Tasks;
using MobileCore.SystemModule;
using UnityEngine;
using System.Linq;

#if MODULE_IAP
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
#endif

namespace MobileCore.IAPModule
{
    public class IAPWrapper : BaseIAPWrapper
    {
#if MODULE_IAP
        private HashSet<string> purchasedProductIds = new HashSet<string>();
#endif

        public override void Initialize(IAPSettings settings)
        {
#if MODULE_IAP
            InitializeAsync(settings);
#else
            IAPManager.Log("[IAP Manager]: Define MODULE_IAP is disabled!");
#endif
        }

#if MODULE_IAP
        private async void InitializeAsync(IAPSettings settings)
        {
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    var options = new InitializationOptions().SetEnvironmentName("production");
                    await UnityServices.InitializeAsync(options);
                }

                List<ProductDefinition> productDefinitions = new List<ProductDefinition>();
                IAPItem[] items = settings.StoreItems;
                for (int i = 0; i < items.Length; i++)
                {
                    ProductDefinition existingDef = null;
                    for (int j = 0; j < productDefinitions.Count; j++)
                    {
                        if (productDefinitions[j].id == items[i].ID)
                        {
                            existingDef = productDefinitions[j];
                            break;
                        }
                    }

                    if (existingDef == null)
                    {
                        productDefinitions.Add(new ProductDefinition(items[i].ID, items[i].ID, (UnityEngine.Purchasing.ProductType)items[i].ProductType));
                    }
                    else
                    {
                        if (existingDef.type != (UnityEngine.Purchasing.ProductType)items[i].ProductType)
                        {
                            IAPManager.LogWarning($"[IAPManager]: FATAL LOGIC ERROR! ID '{items[i].ID}' is used for both {existingDef.type} and {items[i].ProductType}. Google Play and App Store FORBID a single ID to have multiple types! The ID will be forced to {existingDef.type}. Please create separate Tier IDs for Consumable and NonConsumable.");
                        }
                    }
                }

                // Subscribe to all events BEFORE calling Connect/Fetch methods
                var storeController = UnityIAPServices.StoreController();
                var purchaseService = UnityIAPServices.DefaultPurchase();
                var productService = UnityIAPServices.DefaultProduct();

                // Store connection events
                storeController.OnStoreDisconnected -= OnStoreDisconnectedHandler;
                storeController.OnStoreDisconnected += OnStoreDisconnectedHandler;

                // Product fetch events
                productService.OnProductsFetched -= OnProductsFetchedHandler;
                productService.OnProductsFetched += OnProductsFetchedHandler;
                productService.OnProductsFetchFailed -= OnProductsFetchFailedHandler;
                productService.OnProductsFetchFailed += OnProductsFetchFailedHandler;

                // Purchase fetch events
                purchaseService.OnPurchasesFetched -= OnPurchasesFetchedHandler;
                purchaseService.OnPurchasesFetched += OnPurchasesFetchedHandler;
                purchaseService.OnPurchasesFetchFailed -= OnPurchasesFetchFailedHandler;
                purchaseService.OnPurchasesFetchFailed += OnPurchasesFetchFailedHandler;

                // Purchase pending/failed events
                purchaseService.OnPurchasePending -= OnPurchasePendingHandler;
                purchaseService.OnPurchasePending += OnPurchasePendingHandler;
                purchaseService.OnPurchaseFailed -= OnPurchaseFailedHandler;
                purchaseService.OnPurchaseFailed += OnPurchaseFailedHandler;
                purchaseService.OnPurchaseDeferred -= OnPurchaseDeferredHandler;
                purchaseService.OnPurchaseDeferred += OnPurchaseDeferredHandler;
                purchaseService.OnPurchaseConfirmed -= OnPurchaseConfirmedHandler;
                purchaseService.OnPurchaseConfirmed += OnPurchaseConfirmedHandler;

                // Now we can call the methods
                await storeController.Connect();
                storeController.FetchProducts(productDefinitions);
                storeController.FetchPurchases();

                IAPManager.OnModuleInitialized();
            }
            catch (System.Exception exception)
            {
                IAPManager.LogError("[IAPWrapper] Init Error: " + exception.Message);
                OnInitializeFailed(exception.Message);
            }
        }

        private void OnPurchasePendingHandler(PendingOrder order)
        {
            // Get product ID from the order info
            string id = null;
            if (order.Info.PurchasedProductInfo != null && order.Info.PurchasedProductInfo.Count > 0)
            {
                id = order.Info.PurchasedProductInfo[0].productId;
            }

            if (string.IsNullOrEmpty(id))
            {
                IAPManager.LogError("[IAPManager]: Could not get product ID from PendingOrder");
                return;
            }

            IAPManager.Log("[IAPManager]: Purchasing - " + id + " is completed!");
            purchasedProductIds.Add(id);

            IAPItem item = IAPManager.GetIAPItem(id);
            ProductKeyType actualKey = item != null ? item.ProductKeyType : IAPManager.PendingProductKey;

            IAPManager.NotifyPurchaseComplete(actualKey);
            SystemManager.ShowMessage("Payment complete!");

            // Confirm the purchase
            UnityIAPServices.DefaultPurchase().ConfirmPurchase(order);
        }

        private void OnPurchaseFailedHandler(FailedOrder order)
        {
            IAPManager.IsPurchasing = false; // Release the lock immediately

            string message = "Payment failed.";
            switch (order.FailureReason)
            {
                case UnityEngine.Purchasing.PurchaseFailureReason.UserCancelled:
                    message = "Purchase cancelled.";
                    break;
                case UnityEngine.Purchasing.PurchaseFailureReason.PurchasingUnavailable:
                    message = "Purchasing is currently unavailable.";
                    break;
                case UnityEngine.Purchasing.PurchaseFailureReason.PaymentDeclined:
                    message = "Payment was declined by the store.";
                    break;
                case UnityEngine.Purchasing.PurchaseFailureReason.ProductUnavailable:
                    message = "This product is currently unavailable.";
                    break;
                default:
                    message = $"Payment failed: {order.FailureReason}";
                    break;
            }

            SystemManager.ShowMessage(message);

            // Get product ID from the order info
            string id = null;
            if (order.Info != null && order.Info.PurchasedProductInfo != null && order.Info.PurchasedProductInfo.Count > 0)
            {
                id = order.Info.PurchasedProductInfo[0].productId;
            }

            if (string.IsNullOrEmpty(id))
            {
                IAPManager.LogWarning("[IAPManager]: Cancelled without product ID from FailedOrder.");
                return;
            }

            IAPManager.Log("[IAPManager]: Purchasing - " + id + " is failed!");
            IAPManager.Log("[IAPManager]: Fail reason - " + order.FailureReason + ": " + order.Details);

            IAPItem item = IAPManager.GetIAPItem(id);
            ProductKeyType actualKey = item != null ? item.ProductKeyType : IAPManager.PendingProductKey;

            IAPManager.NotifyPurchaseFailed(actualKey, (MobileCore.IAPModule.PurchaseFailureReason)(int)order.FailureReason);
        }

        private void OnPurchaseDeferredHandler(DeferredOrder order)
        {
            IAPManager.IsPurchasing = false;
            SystemManager.ShowMessage("Purchase deferred. Waiting for approval.");
            IAPManager.Log("[IAPManager]: Purchase is deferred!");
        }

        private void OnPurchaseConfirmedHandler(Order order)
        {
            if (order is ConfirmedOrder confirmedOrder)
            {
                var items = confirmedOrder.CartOrdered?.Items();
                string id = (items != null && items.Count > 0) ? items[0]?.Product?.definition?.id : null;
                IAPManager.Log($"[IAPManager]: Purchase confirmation is successful for {id}");
            }
            else if (order is FailedOrder failedOrder)
            {
                IAPManager.LogWarning($"[IAPManager]: Purchase confirmation failed: {failedOrder.Details}");
            }
        }

        private void OnStoreDisconnectedHandler(StoreConnectionFailureDescription description)
        {
            IAPManager.LogError("[IAPManager]: Store disconnected - " + description.Message);
            SystemManager.ShowMessage("Unable to connect to store. Please check your internet connection.");
        }

        private void OnProductsFetchedHandler(List<Product> products)
        {
            IAPManager.Log("[IAPManager]: Products fetched successfully. Count: " + products.Count);
        }

        private void OnProductsFetchFailedHandler(ProductFetchFailed failed)
        {
            IAPManager.LogError("[IAPManager]: Product fetch failed - " + failed.FailureReason);
            SystemManager.ShowMessage("Failed to load products. Please try again later.");
        }

        private void OnPurchasesFetchedHandler(Orders orders)
        {
            IAPManager.Log("[IAPManager]: Purchases fetched. Pending: " + orders.PendingOrders.Count);
            // Update our cache with fetched products
            foreach (var pendingOrder in orders.PendingOrders)
            {
                if (pendingOrder.Info?.PurchasedProductInfo != null && pendingOrder.Info.PurchasedProductInfo.Count > 0)
                {
                    string id = pendingOrder.Info.PurchasedProductInfo[0].productId;
                    purchasedProductIds.Add(id);
                }
            }
        }

        private void OnPurchasesFetchFailedHandler(PurchasesFetchFailureDescription failed)
        {
            IAPManager.LogError("[IAPManager]: Purchases fetch failed - " + failed.Message);
        }

        private void OnInitializeFailed(string message)
        {
            IAPManager.Log("[IAPManager]: Module initialization failed! " + message);
        }

        public Product GetProduct(string id)
        {
            try
            {
                var storeController = UnityIAPServices.StoreController();
                if (storeController == null) return null;

                var products = storeController.GetProducts();
                if (products != null)
                {
                    for (int i = 0; i < products.Count; i++)
                    {
                        if (products[i].definition.id == id)
                        {
                            return products[i];
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                IAPManager.LogWarning($"[IAPWrapper]: Failed to get product {id}: {e.Message}");
            }
            return null;
        }

        private bool CheckSubscriptionActive(Product product)
        {
            if (product == null) return false;
            if (!product.hasReceipt || string.IsNullOrEmpty(product.receipt)) return false;

            try
            {
                var subscriptionManager = new SubscriptionManager(product, null);
                var info = subscriptionManager.getSubscriptionInfo();
                return info.isSubscribed() == Result.True;
            }
            catch (System.Exception e)
            {
                IAPManager.LogWarning($"[IAPWrapper]: Subscription validation failed, fallback to hasReceipt. Error: {e.Message}");
                return product.hasReceipt;
            }
        }
#endif

        public override void BuyProduct(ProductKeyType productKeyType)
        {
#if MODULE_IAP
            try
            {
                SystemManager.ChangeLoadingMessage("Payment in progress..");

                IAPItem item = IAPManager.GetIAPItem(productKeyType);
                if (item != null)
                {
                    var storeController = UnityIAPServices.StoreController();
                    if (storeController != null)
                    {
                        storeController.PurchaseProduct(item.ID);
                    }
                    else
                    {
                        SystemManager.ShowMessage("Store not available.");
                        IAPManager.NotifyPurchaseFailed(productKeyType, PurchaseFailureReason.PurchasingUnavailable);
                    }
                }
                else
                {
                    SystemManager.ShowMessage("Product not found.");
                    IAPManager.NotifyPurchaseFailed(productKeyType, PurchaseFailureReason.ProductUnavailable);
                }
            }
            catch (System.Exception e)
            {
                IAPManager.LogError($"[IAPWrapper]: Exception during purchase: {e.Message}");
                SystemManager.ShowMessage("An error occurred. Please try again.");
                IAPManager.NotifyPurchaseFailed(productKeyType, PurchaseFailureReason.Unknown);
            }
#else
            SystemManager.ShowMessage("Network error.");
            IAPManager.NotifyPurchaseFailed(productKeyType, PurchaseFailureReason.Unknown);
#endif
        }

        public override void RestorePurchases()
        {
#if MODULE_IAP
            if (!IAPManager.IsInitialized)
            {
                SystemManager.ShowMessage("Network error. Please try again later");
                return;
            }

            SystemManager.ShowMessage("Restoring purchased products..");

            try
            {
                var storeController = UnityIAPServices.StoreController();
                if (storeController != null)
                {
                    storeController.RestoreTransactions((result, error) =>
                    {
                        if (result)
                        {
                            SystemManager.ShowMessage("Restoration completed!");
                        }
                        else
                        {
                            SystemManager.ShowMessage("Restoration failed: " + error);
                        }
                    });
                }
                else
                {
                    SystemManager.ShowMessage("Store not available.");
                }
            }
            catch (System.Exception e)
            {
                IAPManager.LogError($"[IAPWrapper]: Exception during restore: {e.Message}");
                SystemManager.ShowMessage("Restoration error occurred.");
            }
#endif
        }

        public override ProductData GetProductData(ProductKeyType productKeyType)
        {
#if MODULE_IAP
            try
            {
                IAPItem item = IAPManager.GetIAPItem(productKeyType);
                if (item != null)
                {
                    Product product = GetProduct(item.ID);
                    if (product != null)
                    {
                        bool isPurchased = product.hasReceipt || purchasedProductIds.Contains(item.ID);
                        bool isSubscribed = false;
                        if (item.ProductType == ProductType.Subscription)
                        {
                            isSubscribed = CheckSubscriptionActive(product);
                        }
                        return new ProductData(product, item.ProductType, isPurchased, isSubscribed);
                    }
                }
            }
            catch (System.Exception e)
            {
                IAPManager.LogWarning($"[IAPWrapper]: GetProductData failed for {productKeyType}: {e.Message}");
            }
#endif
            return null;
        }

        public override bool IsSubscribed(ProductKeyType productKeyType)
        {
#if MODULE_IAP
            try
            {
                IAPItem item = IAPManager.GetIAPItem(productKeyType);
                if (item != null)
                {
                    Product product = GetProduct(item.ID);
                    if (product != null)
                    {
                        if (item.ProductType == ProductType.Subscription)
                        {
                            return CheckSubscriptionActive(product);
                        }
                        return product.hasReceipt || purchasedProductIds.Contains(item.ID);
                    }
                }
            }
            catch (System.Exception e)
            {
                IAPManager.LogWarning($"[IAPWrapper]: IsSubscribed failed for {productKeyType}: {e.Message}");
            }
#endif
            return false;
        }
    }
}
