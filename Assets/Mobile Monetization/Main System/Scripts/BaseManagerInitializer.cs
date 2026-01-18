using UnityEngine;

namespace MobileCore.MainModule
{
    public abstract class BaseManagerInitializer : ScriptableObject
    {
        [HideInInspector] public string ModuleName;
        [HideInInspector] public string ModuleDescription;



        [Header("Runtime")]
        [SerializeField]
        private bool isEnabled = true;
        public bool IsEnabled => isEnabled;

        public void SetEnabled(bool value) => isEnabled = value;

        public abstract void CreateComponent(MainSystemManager mainSystemManager);

        protected virtual void OnEnable()
        {
            if (string.IsNullOrEmpty(ModuleName))
                ModuleName = "Default Module";
        }
    }
}