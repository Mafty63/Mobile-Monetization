
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
        // We will use the new StoreController and PurchaseService classes/interfaces
        // Note: IStoreController is obsolete, so we avoid exposing it publicly if possible.
        // We persist a private reference to the controller interface if needed, or just use services.
        
        // However, since UnityIAPServices.StoreController() returns the controller, we can cache it.
        // But the type returned might be 'StoreController' (concrete) not 'IStoreController'.
        
        // To fix IAPManager errors, we will expose a method to get the product, instead of exposing Controller.
        // We will mock the 'Controller' property to return null or throw because it should NOT be used.
        // We will update IAPManager to not use it.
        
        // Cache
        private HashSet<string> purchasedProductIds = new HashSet<string>();
#endif


        public override async void Initialize(IAPSettings settings)
        {
#if MODULE_IAP
            try
            {
                var options = new InitializationOptions().SetEnvironmentName("production");
                await UnityServices.InitializeAsync(options);

                List<ProductDefinition> productDefinitions = new List<ProductDefinition>();
                IAPItem[] items = settings.StoreItems;
                for (int i = 0; i < items.Length; i++)
                {
                     productDefinitions.Add(new ProductDefinition(items[i].ID, items[i].ID, (UnityEngine.Purchasing.ProductType)items[i].ProductType));
                }

                 // Subscribe to all events BEFORE calling Connect/Fetch methods
                 var storeController = UnityIAPServices.StoreController();
                 var purchaseService = UnityIAPServices.DefaultPurchase();
                 var productService = UnityIAPServices.DefaultProduct();
                 
                 // Store connection events
                 storeController.OnStoreDisconnected += OnStoreDisconnectedHandler;
                 
                 // Product fetch events
                 productService.OnProductsFetched += OnProductsFetchedHandler;
                 productService.OnProductsFetchFailed += OnProductsFetchFailedHandler;
                 
                 // Purchase fetch events
                 purchaseService.OnPurchasesFetched += OnPurchasesFetchedHandler;
                 purchaseService.OnPurchasesFetchFailed += OnPurchasesFetchFailedHandler;
                 
                 // Purchase pending/failed events
                 purchaseService.OnPurchasePending += OnPurchasePendingHandler;
                 purchaseService.OnPurchaseFailed += OnPurchaseFailedHandler;
                 
                 // Now we can call the methods
                 await storeController.Connect();
                 storeController.FetchProducts(productDefinitions);
                 storeController.FetchPurchases();

                IAPManager.OnModuleInitialized();
            }
            catch (System.Exception exception)
            {
                Debug.LogError("[IAPWrapper] Init Error: " + exception.Message);
                OnInitializeFailed(exception.Message);
            }
#else
            await Task.Run(() => Debug.Log("[IAP Manager]: Define MODULE_IAP is disabled!"));
#endif
        }

#if MODULE_IAP
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
                 Debug.LogError("[IAPManager]: Could not get product ID from PendingOrder");
                 return;
             }
             
             Debug.Log("[IAPManager]: Purchasing - " + id + " is completed!");
             purchasedProductIds.Add(id);

             IAPItem item = IAPManager.GetIAPItem(id);
             if (item != null)
             {
                 IAPManager.OnPurchaseCompled(item.ProductKeyType);
             }
             SystemManager.ShowMessage("Payment complete!");
             
             // Confirm the purchase
             UnityIAPServices.DefaultPurchase().ConfirmPurchase(order);
        }

        private void OnPurchaseFailedHandler(FailedOrder order)
        {
             // Get product ID from the order info
             string id = null;
             if (order.Info.PurchasedProductInfo != null && order.Info.PurchasedProductInfo.Count > 0)
             {
                 id = order.Info.PurchasedProductInfo[0].productId;
             }
             
             if (string.IsNullOrEmpty(id))
             {
                 Debug.LogError("[IAPManager]: Could not get product ID from FailedOrder");
                 return;
             }
             
             Debug.Log("[IAPManager]: Purchasing - " + id + " is failed!");
             Debug.Log("[IAPManager]: Fail reason - " + order.FailureReason + ": " + order.Details);
             
             IAPItem item = IAPManager.GetIAPItem(id);
             if (item != null)
             {
                 IAPManager.OnPurchaseFailed(item.ProductKeyType, (MobileCore.IAPModule.PurchaseFailureReason)(int)order.FailureReason);
             }
             SystemManager.ShowMessage("Payment failed!");
        }

        private void OnStoreDisconnectedHandler(StoreConnectionFailureDescription description)
        {
            Debug.LogError("[IAPManager]: Store disconnected - " + description.Message);
            SystemManager.ShowMessage("Unable to connect to store. Please check your internet connection.");
        }

        private void OnProductsFetchedHandler(List<Product> products)
        {
            Debug.Log("[IAPManager]: Products fetched successfully. Count: " + products.Count);
        }

        private void OnProductsFetchFailedHandler(ProductFetchFailed failed)
        {
            Debug.LogError("[IAPManager]: Product fetch failed - " + failed.FailureReason);
            SystemManager.ShowMessage("Failed to load products. Please try again later.");
        }

        private void OnPurchasesFetchedHandler(Orders orders)
        {
            Debug.Log("[IAPManager]: Purchases fetched. Pending: " + orders.PendingOrders.Count);
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
            Debug.LogError("[IAPManager]: Purchases fetch failed - " + failed.Message);
        }

        private void OnInitializeFailed(string message)
        {
            Debug.Log("[IAPManager]: Module initialization failed! " + message);
        }

        public Product GetProduct(string id)
        {
             var products = UnityIAPServices.StoreController().GetProducts();
             if (products != null)
             {
                 return products.FirstOrDefault(p => p.definition.id == id);
             }
             return null;
        }
#endif

        public override void BuyProduct(ProductKeyType productKeyType)
        {
#if MODULE_IAP
            SystemManager.ChangeLoadingMessage("Payment in progress..");

            IAPItem item = IAPManager.GetIAPItem(productKeyType);
            if (item != null)
            {
                UnityIAPServices.StoreController().PurchaseProduct(item.ID);
            }
#else
            SystemManager.ShowMessage("Network error.");
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

            UnityIAPServices.StoreController().RestoreTransactions((result, error) => {
                 if (result)
                 {
                    SystemManager.ShowMessage("Restoration completed!");
                 }
                 else
                 {
                    SystemManager.ShowMessage("Restoration failed: " + error);
                 }
            });
#endif
        }


        public override ProductData GetProductData(ProductKeyType productKeyType)
        {
#if MODULE_IAP
            IAPItem item = IAPManager.GetIAPItem(productKeyType);
            if (item != null)
            {
                Product product = GetProduct(item.ID);
                if (product != null)
                {
                    bool isPurchased = purchasedProductIds.Contains(item.ID);
                    return new ProductData(product, isPurchased);
                }
            }
#endif
            return null;
        }

        public override bool IsSubscribed(ProductKeyType productKeyType)
        {
#if MODULE_IAP
            IAPItem item = IAPManager.GetIAPItem(productKeyType);
            if (item != null)
            {
                 // Check if in our local purchased set
                 if (purchasedProductIds.Contains(item.ID)) return true;
                 return false; 
            }
#endif
            return false;
        }
    }
}
