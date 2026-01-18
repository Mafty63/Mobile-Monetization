using UnityEngine;

namespace MobileCore.IAPModule
{
    [HelpURL("https://google.com/")]
    public class IAPSettings : ScriptableObject
    {
        [SerializeField] bool useTestMode;
        public bool UseTestMode => useTestMode;

        [SerializeField] IAPItem[] storeItems;
        public IAPItem[] StoreItems => storeItems;
    }
}