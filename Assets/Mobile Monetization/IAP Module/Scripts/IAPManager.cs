using System.Collections.Generic;
using UnityEngine;
using MobileCore.Utilities;
using MobileCore.DefineSystem;

#if MODULE_IAP
using UnityEngine.Purchasing;
#endif

namespace MobileCore.IAPModule
{
    [Define("MODULE_IAP", "UnityEngine.Purchasing.IAPButton", "Unity In-App Purchasing SDK")]
    public static class IAPManager
    {
        private static Dictionary<ProductKeyType, IAPItem> productsTypeToProductLink = new Dictionary<ProductKeyType, IAPItem>();
        private static Dictionary<string, IAPItem> productsKeyToProductLink = new Dictionary<string, IAPItem>();

        private static bool isInitialized = false;
        public static bool IsInitialized => isInitialized;

        private static BaseIAPWrapper wrapper;

        public static event PrimitiveCallback OnPurchaseModuleInitted;
        public static event ProductCallback OnPurchaseComplete;
        public static event ProductFailCallback OnPurchaseFailded;

        public static void Initialize(GameObject initObject, IAPManagerInitializer managerInitializer)
        {
            if (isInitialized)
            {
                Debug.Log("[IAP Manager]: Module is already initialized!");
                return;
            }

            IAPItem[] items = managerInitializer.Settings.StoreItems;
            for (int i = 0; i < items.Length; i++)
            {
                productsTypeToProductLink.Add(items[i].ProductKeyType, items[i]);
                productsKeyToProductLink.Add(items[i].ID, items[i]);
            }

#if MODULE_IAP
            wrapper = new IAPWrapper();
#else
            wrapper = new DummyIAPWrapper();
#endif

            wrapper.Initialize(managerInitializer.Settings);
        }

        public static IAPItem GetIAPItem(ProductKeyType productKeyType)
        {
            if (productsTypeToProductLink.ContainsKey(productKeyType))
                return productsTypeToProductLink[productKeyType];

            return null;
        }

        public static IAPItem GetIAPItem(string ID)
        {
            if (productsKeyToProductLink.ContainsKey(ID))
                return productsKeyToProductLink[ID];

            return null;
        }

#if MODULE_IAP
        public static Product GetProduct(ProductKeyType productKeyType)
        {
            IAPItem iapItem = GetIAPItem(productKeyType);
            if (iapItem != null)
            {
                if (wrapper is IAPWrapper iapWrapper)
                {
                   return iapWrapper.GetProduct(iapItem.ID);
                }
                return null;
            }

            return null;
        }
#endif

        public static void RestorePurchases()
        {
            wrapper.RestorePurchases();
        }

        public static void SubscribeOnPurchaseModuleInitted(PrimitiveCallback callback)
        {
            if (isInitialized)
                callback?.Invoke();
            else
                OnPurchaseModuleInitted += callback;
        }

        public static void BuyProduct(ProductKeyType productKeyType)
        {
            wrapper.BuyProduct(productKeyType);
        }

        public static ProductData GetProductData(ProductKeyType productKeyType)
        {
            return wrapper.GetProductData(productKeyType);
        }

        public static bool IsSubscribed(ProductKeyType productKeyType)
        {
            return wrapper.IsSubscribed(productKeyType);
        }

        public static string GetProductLocalPriceString(ProductKeyType productKeyType)
        {
            ProductData product = GetProductData(productKeyType);

            if (product == null)
                return string.Empty;

            return string.Format("{0} {1}", product.ISOCurrencyCode, product.Price);
        }

        public static void OnModuleInitialized()
        {
            isInitialized = true;

            OnPurchaseModuleInitted?.Invoke();

            Debug.Log("[IAPManager]: Module is initialized!");
        }

        public static void OnPurchaseCompled(ProductKeyType productKey)
        {
            OnPurchaseComplete?.Invoke(productKey);
        }

        public static void OnPurchaseFailed(ProductKeyType productKey, PurchaseFailureReason failureReason)
        {
            OnPurchaseFailded?.Invoke(productKey, failureReason);
        }

        public delegate void ProductCallback(ProductKeyType productKeyType);
        public delegate void ProductFailCallback(ProductKeyType productKeyType, PurchaseFailureReason failureReason);
    }

    public enum PurchaseFailureReason
    {
        PurchasingUnavailable = 0,
        ExistingPurchasePending = 1,
        ProductUnavailable = 2,
        SignatureInvalid = 3,
        UserCancelled = 4,
        PaymentDeclined = 5,
        DuplicateTransaction = 6,
        Unknown = 7
    }

    public enum ProductType
    {
        Consumable = 0,
        NonConsumable = 1,
        Subscription = 2
    }
}
