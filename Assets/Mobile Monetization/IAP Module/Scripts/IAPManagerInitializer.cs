using MobileCore.MainModule;
using UnityEngine;

namespace MobileCore.IAPModule
{
    [Module("In-App Purchase Module", "Module to show IAP", 2)]
    public class IAPManagerInitializer : BaseManagerInitializer
    {
        public IAPSettings Settings;

        protected override void OnEnable()
        {
            ModuleName = "IAP Manager";
            ModuleDescription = "Module to show IAP";
        }

        public override void CreateComponent(MainSystemManager mainSystemManager)
        {
            if (Settings == null)
            {
                Debug.LogError("[IAP Manager] IAP Settings not assigned!");
                return;
            }

            IAPManager.Initialize(mainSystemManager.gameObject, this);
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
                    Debug.LogWarning("IAP Settings is null. Please assign an IAP Settings asset.");
                }
#endif
            }
        }
    }
}