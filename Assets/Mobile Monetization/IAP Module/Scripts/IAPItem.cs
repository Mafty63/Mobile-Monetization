using UnityEngine;

namespace MobileCore.IAPModule
{
    [System.Serializable]
    public class IAPItem
    {
        [InspectorName("Android ID")]
        [Tooltip("Product ID registered on Google Play Store.")]
        [SerializeField] private string androidID;

        [InspectorName("iOS ID")]
        [Tooltip("Product ID registered on Apple App Store.")]
        [SerializeField] private string iOSID;

        [InspectorName("Key Type")]
        [Tooltip("The key used to reference this product in code. Must match an entry in ProductKeyType enum.")]
        [SerializeField] private ProductKeyType productKeyType;

        [InspectorName("Product Type")]
        [Tooltip("Consumable (purchased multiple times), Non-Consumable (one-time purchase), or Subscription.")]
        [SerializeField] private ProductType productType;

        public string ID
        {
            get
            {
#if UNITY_ANDROID
                return androidID;
#elif UNITY_IOS
                return iOSID;
#else
                return string.Format("unknown_platform_{0}", productKeyType);
#endif
            }
        }

        public ProductType ProductType { get => productType; set => productType = value; }
        public ProductKeyType ProductKeyType { get => productKeyType; set => productKeyType = value; }

        public IAPItem()
        {
        }

        public IAPItem(string id, ProductKeyType productKeyType, ProductType productType)
        {
            this.androidID = id;
            this.iOSID = id;
            this.productKeyType = productKeyType;
            this.productType = productType;
        }

        public IAPItem(string androidID, string iOSID, ProductKeyType productKeyType, ProductType productType)
        {
            this.androidID = androidID;
            this.iOSID = iOSID;
            this.productKeyType = productKeyType;
            this.productType = productType;
        }
    }
}
