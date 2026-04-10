using UnityEngine;

namespace MobileCore.IAPModule
{
    [HelpURL("https://quick-setup-website.pages.dev/documentation/mobile-monetization/iap-inspector/")]
    public class IAPSettings : ScriptableObject
    {
        [Header("Module Settings")]
        [SerializeField] private bool systemLogs = true;
        public bool SystemLogs => systemLogs;

        [Space]
        [Header("Items")]
        [SerializeField] IAPItem[] storeItems;
        public IAPItem[] StoreItems => storeItems;
    }
}