using MobileCore.MainModule;
using UnityEngine;

#if UNITY_IOS
using Unity.Advertisement.IosSupport;
#endif

namespace MobileCore.Advertisements
{
    [Module("Advertisement Module", "Module to show Ads", 1)]
    public class AdsManagerInitializer : BaseManagerInitializer
    {
        public AdsSettings Settings;
        public GameObject DummyCanvasPrefab;
        public GameObject GDPRPrefab;

        protected override void OnEnable()
        {
            ModuleName = "Ads Module";
            ModuleDescription = "Module to show Ads";
        }

        public override void CreateComponent(MainSystemManager mainSystemManager)
        {
            if (Settings == null)
            {
                Debug.LogError("[Ads Manager] Ads Settings not assigned!");
                return;
            }

            AdsManager.Initialize(this);

#if UNITY_IOS
            // if (Settings.IsIDFAEnabled && !AdsManager.IsIDFADetermined())
            // {
            //     if (Settings.SystemLogs)
            //         Debug.Log("[Ads Manager]: Requesting IDFA..");

            //     ATTrackingStatusBinding.RequestAuthorizationTracking();
            // }
#endif
        }

        // Helper method to ensure we have settings
        public void EnsureSettings()
        {
            if (Settings == null)
            {
#if UNITY_EDITOR
                // This method is only for editor use
                if (!UnityEditor.EditorApplication.isPlaying)
                {
                    Debug.LogWarning("Ads Settings is null. Please assign an Ads Settings asset.");
                }
#endif
            }
        }
    }
}