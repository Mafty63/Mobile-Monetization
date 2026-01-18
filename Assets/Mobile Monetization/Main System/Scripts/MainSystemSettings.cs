using UnityEngine;

namespace MobileCore.MainModule
{
    [CreateAssetMenu(fileName = "Main System Settings", menuName = "Mobile Core/Main System Settings")]
    public class MainSystemSettings : ScriptableObject
    {
        [SerializeField] private BaseManagerInitializer coreModule;
        public BaseManagerInitializer CoreModule => coreModule;

        [SerializeField] private BaseManagerInitializer[] modules;
        public BaseManagerInitializer[] Modules => modules;
        public void Initialize(MainSystemManager mainSystemManager)
        {
            if (modules == null) return;

            coreModule.CreateComponent(mainSystemManager);

            for (int i = 0; i < modules.Length; i++)
            {
                var mod = modules[i];
                if (mod != null)
                {
                    if (mod.IsEnabled)
                    {
                        mod.CreateComponent(mainSystemManager);
                    }
                }
            }
        }
    }
}
