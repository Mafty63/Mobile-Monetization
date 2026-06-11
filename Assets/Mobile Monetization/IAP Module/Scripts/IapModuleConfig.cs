using UnityEngine;
using MobileCore.MainModule;

namespace MobileCore.IAPModule
{
    [CreateAssetMenu(fileName = "IapModuleConfig", menuName = "Mobile Core/Modules/IAP Module Config")]
    public class IapModuleConfig : MobileModule
    {
        public override string ModuleName => "In-App Purchase Module";

        [SerializeField] private IAPSettings settings;
        public IAPSettings Settings => settings;

        public override void Initialize(GameObject parent)
        {
            if (settings == null)
            {
                Debug.LogError("[IapModuleConfig] IAPSettings sub-asset is missing! " +
                               "Select this asset and click 'Repair Sub-Asset' in the Inspector.");
                return;
            }

            IAPManager.Initialize(settings);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Called by the editor to embed a fresh IAPSettings as a child of this asset.
        /// </summary>
        public void CreateEmbeddedSettings()
        {
            if (settings != null) return;

            settings = CreateInstance<IAPSettings>();
            settings.name = "IAPSettings";
            UnityEditor.AssetDatabase.AddObjectToAsset(settings, this);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private void OnValidate()
        {
            if (settings != null && !UnityEditor.AssetDatabase.IsSubAsset(settings))
                settings = null;
        }
#endif
    }
}
