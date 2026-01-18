using System;

namespace MobileCore.Advertisements
{
    [AttributeUsage(AttributeTargets.Field)]
    public class AdsProviderContainerAttribute : Attribute
    {
        public string DisplayName { get; }
        public int Order { get; }
        public string SdkDownloadUrl { get; }

        public AdsProviderContainerAttribute(string displayName, int order = 0, string sdkDownloadUrl = "")
        {
            DisplayName = displayName;
            Order = order;
            SdkDownloadUrl = sdkDownloadUrl;
        }
    }
}
