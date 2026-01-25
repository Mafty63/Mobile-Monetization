using MobileCore.MainModule;
using UnityEngine;

namespace MobileCore.Offerwall
{
    [Module("Offerwall Module", "Module to handle Offerwalls like Tapjoy", 3)]
    public class OfferwallManagerInitializer : BaseManagerInitializer
    {
        public OfferwallSettings Settings;

        protected override void OnEnable()
        {
            ModuleName = "Offerwall Module";
            ModuleDescription = "Module to handle Offerwalls";
        }

        public override void CreateComponent(MainSystemManager mainSystemManager)
        {
            if (Settings == null)
            {
                Debug.LogError("[Offerwall Manager] Settings not assigned!");
                return;
            }

            // OfferwallManager is now a static class, so we just initialize it directly.
            // No need to create a GameObject or assign parent.

            OfferwallManager.Initialize(this);
        }
    }
}
