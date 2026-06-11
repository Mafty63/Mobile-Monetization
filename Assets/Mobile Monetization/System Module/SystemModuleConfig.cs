using UnityEngine;
using MobileCore.MainModule;

namespace MobileCore.SystemModule
{
    [CreateAssetMenu(fileName = "SystemModuleConfig", menuName = "Mobile Core/Modules/System Module Config")]
    public class SystemModuleConfig : MobileModule
    {
        public override string ModuleName => "System Module";

        [Header("Canvas")]
        [Tooltip("Canvas prefab containing core UI elements like message system and loading panels.")]
        [SerializeField] private GameObject systemCanvas;

        [Header("Screen Settings")]
        [SerializeField] private ScreenSettings screenSettings = new ScreenSettings();

        public override void Initialize(GameObject parent)
        {
            if (systemCanvas != null)
            {
                GameObject canvasGameObject = Object.Instantiate(systemCanvas);
                canvasGameObject.transform.SetParent(parent.transform);
                canvasGameObject.transform.localScale = Vector3.one;
                canvasGameObject.transform.localPosition = Vector3.zero;
                canvasGameObject.transform.localRotation = Quaternion.identity;
            }

            screenSettings?.Initialize();
        }
    }
}
