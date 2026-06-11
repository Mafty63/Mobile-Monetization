using UnityEngine;
using MobileCore.MainModule;

namespace MobileCore.SystemModule
{
    [CreateAssetMenu(fileName = "SystemModuleConfig", menuName = "Mobile Core/Modules/System Module Config")]
    public class SystemModuleConfig : MobileModule
    {
        public override string ModuleName => "System Module";

        [SerializeField] private SystemSettings settings;
        public SystemSettings Settings => settings;

        public override void Initialize(GameObject parent)
        {
            if (settings == null)
            {
                Debug.LogError("[SystemModuleConfig] SystemSettings sub-asset is missing! " +
                               "Select this asset and click 'Repair Sub-Asset' in the Inspector.");
                return;
            }

            if (settings.SystemCanvas != null)
            {
                GameObject canvasGameObject = Object.Instantiate(settings.SystemCanvas);
                canvasGameObject.transform.SetParent(parent.transform);
                canvasGameObject.transform.localScale = Vector3.one;
                canvasGameObject.transform.localPosition = Vector3.zero;
                canvasGameObject.transform.localRotation = Quaternion.identity;
            }

            settings.ScreenSettings?.Initialize();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Called by the editor to embed a fresh SystemSettings as a child of this asset.
        /// </summary>
        public void CreateEmbeddedSettings()
        {
            if (settings != null) return;

            settings = CreateInstance<SystemSettings>();
            settings.name = "SystemSettings";
            UnityEditor.AssetDatabase.AddObjectToAsset(settings, this);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private void OnValidate()
        {
            // If the sub-asset was deleted externally, clear the reference
            if (settings != null && !UnityEditor.AssetDatabase.IsSubAsset(settings))
                settings = null;
        }
#endif
    }
}
