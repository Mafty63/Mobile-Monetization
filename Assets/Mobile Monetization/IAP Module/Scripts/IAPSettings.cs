using UnityEngine;

namespace MobileCore.IAPModule
{
    [HelpURL("https://quick-setup-website.pages.dev/documentation/mobile-monetization/iap-inspector/")]
    public class IAPSettings : ScriptableObject
    {
        [SerializeField] IAPItem[] storeItems;
        public IAPItem[] StoreItems => storeItems;
    }
}