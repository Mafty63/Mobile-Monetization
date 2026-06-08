using MobileCore.MainModule;
using UnityEngine;

namespace MobileCore.SystemModule
{
    [HelpURL("https://quick-setup-website.pages.dev/documentation/mobile-monetization/system-module/")]
    public class SystemModuleInitializer : BaseManagerInitializer
    {
        [InspectorName("Canvas Prefab")]
        [Tooltip("Canvas prefab containing core UI elements like message system and loading panels.")]
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