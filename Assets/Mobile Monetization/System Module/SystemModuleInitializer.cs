using MobileCore.MainModule;
using UnityEngine;

namespace MobileCore.SystemModule
{
    public class SystemModuleInitializer : BaseManagerInitializer
    {
        public GameObject SystemCanvas;
        public ScreenSettings ScreenSettings;

        public override void CreateComponent(MainSystemManager mainSystemManager)
        {
            GameObject canvasGameObject = Instantiate(SystemCanvas);
            canvasGameObject.transform.SetParent(mainSystemManager.transform);
            canvasGameObject.transform.localScale = Vector3.one;
            canvasGameObject.transform.localPosition = Vector3.zero;
            canvasGameObject.transform.localRotation = Quaternion.identity;
            ScreenSettings.Initialize();
        }

        protected override void OnEnable()
        {
            if (string.IsNullOrEmpty(ModuleName))
                ModuleName = "System Module";
        }
    }
}