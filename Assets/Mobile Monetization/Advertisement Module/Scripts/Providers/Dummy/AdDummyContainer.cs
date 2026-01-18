#pragma warning disable 0649

using UnityEngine;

namespace MobileCore.Advertisements.Providers
{
    [System.Serializable]
    public class AdDummyContainer
    {
        public BannerPosition bannerPosition = BannerPosition.Bottom;

        [Header("Advanced Settings")]
        [Tooltip("If true, Handler will not auto-initialize on Start(). You must call Initialize() manually")]
        [SerializeField] bool dontAutoInitialize = false;
        public bool DontAutoInitialize => dontAutoInitialize;
    }
}