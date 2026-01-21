using UnityEngine;

namespace MobileCore.Offerwall.Providers.Tapjoy
{
    [System.Serializable]
    public class TapjoyContainer
    {
        [Header("SDK Keys")]
        [SerializeField] private string androidSdkKey = "YOUR_ANDROID_SDK_KEY";
        [SerializeField] private string iosSdkKey = "YOUR_IOS_SDK_KEY";
        [SerializeField] private string gcmSenderId = "YOUR_GCM_SENDER_ID";

        [Header("Placements")]
        [SerializeField] private string offerwallPlacementName = "Offerwall";

        [Header("Debug")]
        [SerializeField] private bool enableDebug = false;

        public string AndroidSdkKey => androidSdkKey;
        public string IosSdkKey => iosSdkKey;
        public string GcmSenderId => gcmSenderId;
        public string OfferwallPlacementName => offerwallPlacementName;
        public bool EnableDebug => enableDebug;
    }
}
